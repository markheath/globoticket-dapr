# Dapr Fundamentals GloboTicket Demo Application

This application is intended to demonstrate the basics of using Dapr to build a microservices application. It is the demo project for the Pluralsight Dapr 1 Fundamentals course, by Mark Heath.

This branch is the "before" example - a version of the demo application that does not have Dapr installed at all. It is unable to send emails, and simply uses HTTP calls for all communication between microservices.

## Running the app locally
The recommended way for running locally is to use self-hosted mode (option 1). I have also managed to get it running in Docker Compose, although that option has not been tested so much.

Open three terminal windows. In the `frontend` folder run `dotnet run`. Do the same in the `catalog` and `ordering` folders. The ports used are specified in the PowerShell start up scripts. The frontend app will be available at `http://localhost:5266/`. The catalog service will be at `http://localhost:5016/swagger/index.html`, and the ordering service at `http://localhost:5293/swagger/index.html`


## Architecture Overview

- The **frontend** microservice is a simple ASP.NET Core 6 website. It allows visitors to browse the catalog of events, and place an order for tickets
- The **catalog** microservice provides the list of events that tickets can be purchased for. To keep this demo as simple as possible, the catalog microservice returns a hard-coded in-memory list.
- The **ordering** microservice takes new orders. It receives the order via pub-sub messaging. It sends an email to thank the user for purchasing. A dapr output
