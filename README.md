# Dapr Fundamentals GloboTicket Demo Application

This application is intended to demonstrate the basics of using Dapr to build a microservices application. It is the demo project for the [Pluralsight Dapr 1 Fundamentals](https://pluralsight.pxf.io/c/1192349/424552/7490?u=www%2Epluralsight%2Ecom%2Fcourses%2Fdapr-1-fundamentals) course, by Mark Heath.

This version of the code is using Dapr 1.8

## Running the app locally
The recommended way for running locally is to use self-hosted mode (option 1). I have also managed to get it running in Docker Compose, although that option has not been tested so much.

### Option 1 - Running self-hosted from the command line

**Prerequisites:** You need to have the [Dapr CLI installed](https://docs.dapr.io/getting-started/install-dapr-cli/), as well as Docker installed (e.g. Docker Desktop for Windows), and to have set up Dapr in self-hosted mode with `dapr init`

And in order to use the email sending feature, you'll want a local container running maildev, which you can start using: `docker run -p 1080:80 -p 1025:25 maildev/maildev`. If you need a dummy credit card number to place an order you can use `4242424242424242` or `5555555555554444`

Open three terminal windows. In the `frontend` folder run `start-self-hosted.ps1`. Do the same in the `catalog` and `ordering` folders. The ports used are specified in the PowerShell start up scripts. The frontend app will be available at `http://localhost:5266/`. The catalog service will be at `http://localhost:5016/swagger/index.html`, and the ordering service at `http://localhost:5293/swagger/index.html`

You can view Zipkin traces at http://localhost:9411/zipkin/?
You can see the emails sent by the ordering service using maildev on: `http://localhost:1080/#/`

### Option 2 - Running with Docker Compose

In same folder as the `docker-compose.yml` file, run `docker-compose build` then `docker-compose up`. The frontend service will be at `https://localhost:5001`.

Note: The Docker Compose version has its own components folder, as the relative path of the local secrets is different, and redis is not on localhost.
You will be able to access Zipkin traces on: `http://localhost:9412/zipkin/`

You can see the emails sent by the ordering service using maildev on: `http://localhost:1080/#/`

### Option 3 - Running with Docker Compose in Visual Studio 2022
Set the startup project to Docker Compose. If you've used option 1, make sure the other Docker Compose containers are removed or there will be a name conflict. The frontend service will be at `https://localhost:5001`.  The catalog service will be at `http://localhost:5003/swagger/index.html`, and the ordering service at `http://localhost:5004/swagger/index.html`.
You will be able to access Zipkin traces on: `http://localhost:9412/zipkin/`


## Architecture Overview

- The **frontend** microservice is a simple ASP.NET Core 6 website. It allows visitors to browse the catalog of events, and place an order for tickets
- The **catalog** microservice provides the list of events that tickets can be purchased for. To keep this demo as simple as possible, the catalog microservice returns a hard-coded in-memory list. Created with `dotnet new webapi -o catalog --no-https` (no https because we're going to rely on dapr for securing communication between microservices). A dapr cron job calls a scheduled endpoint on this.
- The **ordering** microservice takes new orders. It receives the order via pub-sub messaging. It sends an email to thank the user for purchasing. A dapr output

## Deploying to Kubernetes (AKS) on Azure
The `aks-deploy.ps1` PowerShell script shows the steps needed to deploy this to Azure. Don't run this directly. You'll need the Azure CLI installed, and you'll also need to pick unique resource names that are available. The script includes example commands you can use to check it's all working as expected.

## Troubleshooting notes

- the `maildev` docker image switched its default ports from 80 & 25 to 1080 and 1025, so make sure you're using the correct email YAML component definition from this repo if you're having troubles with those
- there is a [known issue](https://github.com/dapr/dapr/issues/3256) when running locally with the mDNS resolution in Dapr where if you are running certain VPNs or CISCO networking apps it can cause it to fail. The workaround is usually to temporarily stop the offending software. In the GloboTicket app, this error would cause the homepage to fail to load, unable to communicate with the catalog service. An other workaround is to use consul for name resolution is to use Consul (instructions below)

- When running on AKS, after you've upgraded Dapr, it's a good idea to restart the deployments. The AKS demo script has an example.

### Using Consul for self-hosted mode

1. Start the consul container with `docker run -d -p 8500:8500 --name=dev-consul -e CONSUL_BIND_INTERFACE=eth0 consul`
2. Ensure you can visit it at http://127.0.0.1/8500
3. Create a `daprConfig.yaml` file with the following contents (or update your existing config to include the `nameResolution` section)

```yaml
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: daprConfig
spec:
  nameResolution:
    component: "consul"
    configuration:
      selfRegister: true
  tracing:
    samplingRate: "1"
    zipkin:
      endpointAddress: http://localhost:9411/api/v2/spans
```

4. Create a `consul.yaml` file in your local components folder with the following contents

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: consul
  namespace: default
spec:
  type: state.consul
  version: v1
  metadata:
  - name: datacenter
    value: dc1 # Required. Example: dc1
  - name: httpAddr
    value: 127.0.0.1:8500 # Required. Example: "consul.default.svc.cluster.local:8500"
```

5. Update your `dapr run` command to point to the updated config and components folder. Here's an example powershell script I used:

```powershell
dapr run `
    --app-id frontend `
    --app-port 5266 `
    --dapr-http-port 3500 `
    --components-path ../dapr/components `
    --config ../dapr/components/daprConfig.yaml `
    dotnet run
```

6. Start your services and check that they appear in the consul UI at http://127.0.0.1/8500
