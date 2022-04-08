### STEP 1 - create the AKS cluster
# instructions - https://docs.dapr.io/operations/hosting/kubernetes/cluster/setup-aks/

# log in to Azure CLI
az login
# select the subscription we want to use
az account set -s "My sub name"

$RESOURCEGROUP = "globoticket-dapr"
$LOCATION = "westeurope"
$AKS_NAME = "globoticketdapr"

# create the resource group
az group create -n $RESOURCEGROUP -l $LOCATION 

# create the AKS cluster
az aks create -g $RESOURCEGROUP -n $AKS_NAME --node-count 1 `
  --enable-addons http_application_routing --generate-ssh-keys

# Get credentials for kubectl to use
az aks get-credentials -g $RESOURCEGROUP -n $AKS_NAME --overwrite-existing

# check kubectl config is as expected
kubectl config current-context

### STEP 2 - set up blob storage for state
# $RAND = -join ((48..57) + (97..122) | Get-Random -Count 6 | % {[char]$_})
$STORAGE_ACCOUNT = "globoticketstate"

az storage account create -n $STORAGE_ACCOUNT -g $RESOURCEGROUP -l $LOCATION --sku Standard_LRS

$STORAGE_CONNECTION_STRING = az storage account show-connection-string `
  -n $STORAGE_ACCOUNT -g $RESOURCEGROUP --query connectionString -o tsv

$STORAGE_ACCOUNT_KEY = az storage account keys list -g $RESOURCEGROUP `
  -n $STORAGE_ACCOUNT --query [0].value -o tsv

$env:AZURE_STORAGE_CONNECTION_STRING = $STORAGE_CONNECTION_STRING

az storage container create -n "statestore" --public-access off

### STEP 3 - set up Azure service bus for pub sub
$SERVICE_BUS = "globoticketpubsub"

az servicebus namespace create -g $RESOURCEGROUP `
    -n $SERVICE_BUS -l $LOCATION --sku Standard

$SERVICE_BUS_CONNECTION_STRING = az servicebus namespace authorization-rule keys list `
      -g $RESOURCEGROUP --namespace-name $SERVICE_BUS `
      -n RootManageSharedAccessKey `
      --query primaryConnectionString `
      --output tsv

### STEP 4 - install Dapr onto the cluster
# instructions - https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-deploy/
# (n.b. there is now a dapr extension for AKS is preview: https://docs.microsoft.com/en-us/azure/aks/dapr)
dapr init -k

# to verify installation
dapr status -k

# or to upgrade an existing deployment
dapr upgrade -k --runtime-version 1.7.0

### STEP 5 - get containers pushed to docker

# ensure we've built all our containers
docker build -f .\frontend\Dockerfile -t markheath/globoticket-dapr-frontend .
docker build -f .\catalog\Dockerfile -t markheath/globoticket-dapr-catalog .
docker build -f .\ordering\Dockerfile -t markheath/globoticket-dapr-ordering .

# and push them to Docker hub 
# (real world would use ACR instead for private hosting and faster download in Azure)
docker push markheath/globoticket-dapr-frontend
docker push markheath/globoticket-dapr-catalog
docker push markheath/globoticket-dapr-ordering

# STEP 6 - put connection strings into Kubernetes secrets

kubectl create secret generic blob-secret `
  --from-literal=account-key="$STORAGE_ACCOUNT_KEY"
kubectl create secret generic servicebus-secret `
  --from-literal=connection-string="$SERVICE_BUS_CONNECTION_STRING"
kubectl create secret generic eventcatalogdb `
  --from-literal=eventcatalogdb="Event Catalog Connection String from Kubernetes"

#kubectl get secrets
#kubectl describe secrets/blob-secret
#kubectl get secret blob-secret -o jsonpath='{.data}'
#kubectl delete secret blob-secret

### STEP 7 - set up zipkin
# instructions - https://docs.dapr.io/operations/monitoring/tracing/supported-tracing-backends/zipkin/

kubectl create deployment zipkin --image openzipkin/zipkin
kubectl expose deployment zipkin --type ClusterIP --port 9411
kubectl apply -f .\deploy\appconfig.yaml

# to test it out (avoiding a port number clash with self-hosted zipkin)
kubectl port-forward svc/zipkin 9412:9411
# navigate to http://localhost:9412

### STEP 8 - deploy Dapr component definitions for pub-sub, state store and cron jon
kubectl apply -f .\deploy\azure-pubsub.yaml
kubectl apply -f .\deploy\azure-statestore.yaml
kubectl apply -f .\deploy\cron.yaml

### STEP 9 - deploy maildev service
kubectl create deployment maildev --image maildev/maildev
kubectl expose deployment maildev --type ClusterIP --port 25,80

### STEP 10 - deploy email component definition
kubectl apply -f .\deploy\email.yaml

# to check that maildev is working:
kubectl port-forward svc/maildev 8081:80
# navigate to http://localhost:8081

# can look at the components in Dapr dashboard
dapr dashboard -k

# STEP 11 - install app onto AKS
kubectl apply -f .\deploy\frontend.yaml
kubectl apply -f .\deploy\ordering.yaml
kubectl apply -f .\deploy\catalog.yaml # n.b. to remove its kubectl delete

# or to restart a service if its already been deployed
kubectl rollout restart deployment frontend

kubectl get deployments
kubectl get pods
kubectl get services

# examples for how to look at the logs for specific containers
$CATALOG_POD_NAME = kubectl get pods --selector=app=catalog -o jsonpath='{.items[*].metadata.name}'
$FRONTEND_POD_NAME = kubectl get pods --selector=app=frontend -o jsonpath='{.items[*].metadata.name}'
$ORDERING_POD_NAME = kubectl get pods --selector=app=ordering -o jsonpath='{.items[*].metadata.name}'

kubectl logs $CATALOG_POD_NAME catalog
kubectl logs $FRONTEND_POD_NAME frontend
kubectl logs $ORDERING_POD_NAME ordering
kubectl logs $FRONTEND_POD_NAME daprd # to see the logs from the sidecar

# to view the AKS cluster in the Azure portal
az aks browse -n $AKS_NAME -g $RESOURCEGROUP

### STEP 11 - TEST THE APP

$FRONTEND_IP = kubectl get svc frontend -o jsonpath="{.status.loadBalancer.ingress[*].ip}"

# frontend is running on port 8080
Start-Process "http://$($FRONTEND_IP):8080"

# let's check in zipkin - make sure we're looking at the right zipkin (the one in AKS) by using a different port number
kubectl port-forward svc/zipkin 9412:9411
# navigate to http://localhost:9412

### STEP 12 - CLEAN UP
az group delete -n $RESOURCEGROUP