# Dapr Fundamentals GloboTicket Demo Application

This application is intended to demonstrate the basics of using Dapr to build a microservices application

It is based on the demo application in the Pluralsight Microservices in ASP.NET Core learning path

## Running the app locally
The recommended way for running locally is to use Docker Compose. That way you only need Docker installed.

### Option 1 - Running with Docker Compose

In same folder as the `docker-compose.yml` file, run `docker-compose build` then `docker-compose up`. The frontend service will be at `https://localhost:5001`.

Note: The Docker Compose version has its own components folder, as the relative path of the local secrets is different, and redis is not on localhost.
You will be able to access Zipkin traces on: `http://localhost:9412/zipkin/`

You can see the emails sent by the ordering service using maildev on: `http://localhost:1080/#/`

### Option 2- Running with Docker Compose in Visual Studio 2022
Set the startup project to Docker Compose. If you've used option 1, make sure the other Docker Compose containers are removed or there will be a name conflict. The frontend service will be at `https://localhost:5001`.  The catalog service will be at `http://localhost:5003/swagger/index.html`, and the ordering service at `http://localhost:5004/swagger/index.html`.
You will be able to access Zipkin traces on: `http://localhost:9412/zipkin/`

### Option 3 - Running self-hosted from Visual Studio 2022
Not implemented yet - plan to try Dapr sidekick to enable.

### Option 4 - Running self-hosted from the command line
Open three terminal windows. In the `frontend` folder run `start-self-hosted.p1s`. Do the same in the `catalog` and `ordering` folders. The ports used are specified in the PowerShell start up scripts. The frontend app will be available at `https://localhost:7274/`. The catalog service will be at `http://localhost:5016/swagger/index.html`, and the ordering service at `http://localhost:5293/swagger/index.html`

### Option 5 - Running without Dapr from Visual Studio 2022
Set multiple startup projects in VS of `ordering`, `frontend` and `catalog`. Ensure they are not running containerized. They will run with the ports specified in their `Properties/launchSettings.json` file and communicate directly with HTTP. The frontend will be at `https://localhost:7274/`, the catalog service will be at `http://localhost:5016/swagger/index.html`,  and the ordering  service at `http://localhost:5293/swagger/index.html`


## Architecture Overview

- The **frontend** microservice is a simple ASP.NET Core 6 website. It allows visitors to browse the catalog of events, and place an order for tickets
- The **catalog** microservice provides the list of events that tickets can be purchased for. To keep this demo as simple as possible, the catalog microservice returns a hard-coded in-memory list. Created with `dotnet new webapi -o catalog --no-https` (no https because we're going to rely on dapr for securing communication between microservices). A dapr cron job calls a scheduled endpoint on this.
- The **ordering** microservice takes new orders. It receives the order via pub-sub messaging. It sends an email to thank the user for purchasing. A dapr output

## Deploying to Azure
The `ask-deploy.ps1` PowerShell script shows the steps needed to deploy this to Azure. Don't run this directly. You'll need the Azure CLI installed, and you'll also need to pick unique resource names that are available. The script includes example commands you can use to check it's all working as expected.