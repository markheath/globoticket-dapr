## These scripts are to deploy GloboTicket onto Azure Container Apps
## https://docs.microsoft.com/en-us/azure/container-apps/
## Note that Azure Container Apps are currently still in preview and these
## instructions were made with 0.3.2 of the containerapp extension
## There are a few issues at the moment
## - the catalog service can't directly access secrets via the Dapr API (I've tried the kubernetes store as well as an environment variable secret store, but neither have worked so far)
## - the ordering microservice can't seem to talk to the maildev server at the moment

### STEP 1 - create the Azure Container Apps Environment
# more info https://docs.microsoft.com/en-us/azure/container-apps/microservices-dapr?tabs=powershell

# log in to Azure CLI
az login
# select the subscription we want to use
az account set -s "My sub name"

# ensure we have the Azure CLI container apps extension installed 
az extension add --name containerapp --upgrade

# register the Microsoft.App namespace
az provider register --namespace Microsoft.App

$RESOURCE_GROUP = "globoticket-containerapps"
$LOCATION = "westeurope"
$CONTAINERAPPS_ENVIRONMENT="globoticket-env"

# create the resource group
az group create -n $RESOURCE_GROUP -l $LOCATION 

# create the container apps environment (will auto-generate a log analytics workspace for us)
az containerapp env create `
  --name $CONTAINERAPPS_ENVIRONMENT `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION

### STEP 2 - set up blob storage for state
# $RAND = -join ((48..57) + (97..122) | Get-Random -Count 6 | % {[char]$_})
$STORAGE_ACCOUNT = "globoticketstate1"

az storage account create -n $STORAGE_ACCOUNT -g $RESOURCE_GROUP -l $LOCATION --sku Standard_LRS

$STORAGE_CONNECTION_STRING = az storage account show-connection-string `
  -n $STORAGE_ACCOUNT -g $RESOURCE_GROUP --query connectionString -o tsv

$STORAGE_ACCOUNT_KEY = az storage account keys list -g $RESOURCE_GROUP `
  -n $STORAGE_ACCOUNT --query [0].value -o tsv

$env:AZURE_STORAGE_CONNECTION_STRING = $STORAGE_CONNECTION_STRING

az storage container create -n "statestore" --public-access off

### STEP 3 - set up Azure service bus for pub sub
$SERVICE_BUS = "globoticketpubsub1"

az servicebus namespace create -g $RESOURCE_GROUP `
    -n $SERVICE_BUS -l $LOCATION --sku Standard

$SERVICE_BUS_CONNECTION_STRING = az servicebus namespace authorization-rule keys list `
      -g $RESOURCE_GROUP --namespace-name $SERVICE_BUS `
      -n RootManageSharedAccessKey `
      --query primaryConnectionString `
      --output tsv

### STEP 4 - get containers pushed to docker

# ensure we've built all our containers
docker build -f .\frontend\Dockerfile -t markheath/globoticket-dapr-frontend .
docker build -f .\catalog\Dockerfile -t markheath/globoticket-dapr-catalog .
docker build -f .\ordering\Dockerfile -t markheath/globoticket-dapr-ordering .

# and push them to Docker hub 
# (real world would use ACR instead for private hosting and faster download in Azure)
docker push markheath/globoticket-dapr-frontend
docker push markheath/globoticket-dapr-catalog
docker push markheath/globoticket-dapr-ordering

# STEP 5 - deploy component definitions
# unfortunately, there seems to be no simple way to deal with secrets at the moment
# we will use temporary files to avoid checking secrets into source control

$COMPONENTS_FOLDER = "./dapr/containerapps-components" 

az containerapp env dapr-component set `
  --name $CONTAINERAPPS_ENVIRONMENT --resource-group $RESOURCE_GROUP `
  --dapr-component-name scheduled `
  --yaml "$COMPONENTS_FOLDER/scheduled.yaml"

(Get-Content -Path "$COMPONENTS_FOLDER/pubsub.yaml" -Raw).Replace('<SERVICE_BUS_CONNECTION_STRING>',$SERVICE_BUS_CONNECTION_STRING) | Set-Content -Path "$COMPONENTS_FOLDER/pubsub.tmp.yaml" -NoNewline

az containerapp env dapr-component set `
  --name $CONTAINERAPPS_ENVIRONMENT --resource-group $RESOURCE_GROUP `
  --dapr-component-name pubsub `
  --yaml "$COMPONENTS_FOLDER/pubsub.tmp.yaml"

(Get-Content -Path "$COMPONENTS_FOLDER/statestore.yaml" -Raw).Replace('<STORAGE_ACCOUNT_KEY>',$STORAGE_ACCOUNT_KEY).Replace('<STORAGE_ACCOUNT_NAME>',$STORAGE_ACCOUNT) | Set-Content -Path "$COMPONENTS_FOLDER/statestore.tmp.yaml" -NoNewline

az containerapp env dapr-component set `
  --name $CONTAINERAPPS_ENVIRONMENT --resource-group $RESOURCE_GROUP `
  --dapr-component-name shopstate `
  --yaml "$COMPONENTS_FOLDER/statestore.tmp.yaml"

az containerapp env dapr-component set `
  --name $CONTAINERAPPS_ENVIRONMENT --resource-group $RESOURCE_GROUP `
  --dapr-component-name sendmail `
  --yaml "$COMPONENTS_FOLDER/sendmail.yaml"

# https://docs.microsoft.com/en-us/azure/container-apps/manage-secrets?tabs=azure-cli

# STEP 5 - deploy apps
# maildev does not need to be Dapr enabled. It listens on 1025 for SMTP and 1080
az containerapp create `
  --name maildev `
  --resource-group $RESOURCE_GROUP `
  --environment $CONTAINERAPPS_ENVIRONMENT `
  --image maildev/maildev `
  --target-port 1080 `
  --ingress 'external' `
  --min-replicas 1 `
  --max-replicas 1

az containerapp create `
  --name frontend `
  --resource-group $RESOURCE_GROUP `
  --environment $CONTAINERAPPS_ENVIRONMENT `
  --image markheath/globoticket-dapr-frontend `
  --target-port 80 `
  --ingress 'external' `
  --min-replicas 1 `
  --max-replicas 1 `
  --enable-dapr `
  --dapr-app-port 80 `
  --dapr-app-id frontend

# https://github.com/microsoft/azure-container-apps/issues/147


az containerapp create `
  --name catalog `
  --resource-group $RESOURCE_GROUP `
  --environment $CONTAINERAPPS_ENVIRONMENT `
  --image markheath/globoticket-dapr-catalog `
  --target-port 80 `
  --ingress 'internal' `
  --min-replicas 1 `
  --max-replicas 1 `
  --enable-dapr `
  --dapr-app-port 80 `
  --dapr-app-id catalog `
  --env-vars SECRET_STORE_NAME="Kubernetes" eventcatalogdb="DBConnectionStringFromEnvVar" `
  --secrets "eventcatalogdb=EventCatalogConnectionStringFromContainerApps"

# example of updating an app:
# az containerapp update -n catalog -g $RESOURCE_GROUP --set-env-vars SECRET_STORE_NAME="kubernetes"

az containerapp create `
  --name ordering `
  --resource-group $RESOURCE_GROUP `
  --environment $CONTAINERAPPS_ENVIRONMENT `
  --image markheath/globoticket-dapr-ordering `
  --target-port 80 `
  --ingress 'internal' `
  --min-replicas 1 `
  --max-replicas 1 `
  --enable-dapr `
  --dapr-app-port 80 `
  --dapr-app-id ordering

### STEP 11 - TEST THE APP
$FQDN = az containerapp show --name frontend --resource-group $RESOURCE_GROUP `
  --query properties.configuration.ingress.fqdn -o tsv

# launch frontend in a browser
Start-Process "https://$FQDN"

az containerapp logs show -n frontend -g $RESOURCE_GROUP
az containerapp logs show -n catalog -g $RESOURCE_GROUP

$MAILDEV_SERVER = az containerapp show --name maildev --resource-group $RESOURCE_GROUP `
  --query properties.configuration.ingress.fqdn -o tsv
Start-Process "https://$MAILDEV_SERVER"

# Log analytics query
# ContainerAppConsoleLogs_CL | where ContainerAppName_s == "ordering"
# | project ContainerAppName_s, LogLevel_s, Message=coalesce(Exception_s, Log_s), Category, TimeGenerated, ContainerName_s

### STEP 12 - CLEAN UP
az group delete -n $RESOURCE_GROUP