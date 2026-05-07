# Dapr Fundamentals GloboTicket Demo Application

This application demonstrates the basics of using [Dapr](https://dapr.io/) to build a microservices application. It is the demo project for the [Pluralsight Dapr 1 Fundamentals](https://pluralsight.pxf.io/c/1192349/424552/7490?u=www%2Epluralsight%2Ecom%2Fcourses%2Fdapr-1-fundamentals) course by Mark Heath.

This version targets **.NET 10**, **Dapr 1.17**, and uses **.NET Aspire** to orchestrate everything locally with a single `dotnet run`.

> **Following the Pluralsight course?** The course was recorded against Dapr 1.13 / .NET 8, with PowerShell start scripts and Docker Compose. That version is preserved on the [`dapr-1-13`](https://github.com/markheath/globoticket-dapr/tree/dapr-1-13) branch. Switch to it if you want the code to match the videos exactly.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/), with `dapr init` already run (this provisions the local Redis used for state and pub/sub)
- A container runtime (Docker Desktop, Podman, or Rancher Desktop)

That's it. There is no separate Aspire workload to install — `Aspire.AppHost.Sdk` is restored automatically.

## Run it

```powershell
dotnet run --project apphost
```

Or open `globoticket-dapr.sln` in Visual Studio (the `apphost` project is set as the startup project) and press F5.

The Aspire dashboard opens automatically and is the canonical place to find every service URL. The app ports below are pinned via each project's `launchSettings.json` so the demo URLs are stable:

| Service     | URL                                |
|-------------|------------------------------------|
| `frontend`  | http://localhost:5266              |
| `catalog`   | http://localhost:5016/scalar/v1    |
| `ordering`  | http://localhost:5293/scalar/v1    |
| `mailpit`   | http://localhost:8025              |

Dapr sidecar ports are *not* pinned — Aspire allocates them dynamically, and the .NET apps reach Dapr via the `DAPR_HTTP_PORT` env var the toolkit injects. If you want to call a Dapr API from outside (e.g. the example `.http` files), look up the sidecar's port next to `<app>-dapr` in the Aspire dashboard.

Distributed traces, structured logs, and resource metrics are all in the dashboard.

If you need a dummy credit card number on the checkout page, use `4242424242424242` or `5555555555554444`.

## Architecture overview

- **frontend** — ASP.NET Core MVC site. Lets visitors browse the catalog and place orders. Talks to `catalog` via Dapr service invocation, stores the shopping basket in a Dapr state store (Redis), and submits orders via Dapr pub/sub.
- **catalog** — Web API that returns the list of events. The list is hard-coded in-memory for simplicity. A Dapr cron binding fires every 5 minutes to rotate which event is on special offer.
- **ordering** — Web API that subscribes to the `orders` topic. When an order arrives it sends a confirmation email via the Dapr SMTP output binding (which targets MailPit locally).

Dapr components live in `dapr/components/`:

| File                    | Component name | Purpose                                       |
|-------------------------|----------------|-----------------------------------------------|
| `pubsub.yaml`           | `pubsub`       | Redis pub/sub used for order submission       |
| `stateStore.yml`        | `shopstate`    | Redis state store for the shopping basket     |
| `email.yml`             | `sendmail`     | SMTP output binding pointed at MailPit        |
| `cron.yml`              | `scheduled`    | Cron input binding that calls the catalog     |
| `localSecretStore.yml`  | `secretstore`  | Local file secret store (reads `secrets.json`) |

The Aspire AppHost wires the same components folder into every Dapr sidecar via `DaprSidecarOptions.ResourcesPaths`.

## Deploying

The `aks-deploy.ps1` and `azure-container-apps-deploy.ps1` scripts contain the steps to deploy this to Azure. They are reference scripts — read them before running, you'll need to pick unique resource names. Kubernetes manifests live in `deploy/`, and the ACA-flavoured Dapr components live in `dapr/containerapps-components/`.

## Notable tooling choices

- **`Aspire.AppHost.Sdk` 13.x** with [`CommunityToolkit.Aspire.Hosting.Dapr`](https://github.com/CommunityToolkit/Aspire/tree/main/src/CommunityToolkit.Aspire.Hosting.Dapr) — the Microsoft `Aspire.Hosting.Dapr` package was handed to the Community Toolkit.
- **[MailPit](https://mailpit.axllent.org/)** instead of maildev for local SMTP capture — actively maintained, has a first-party Aspire integration.
- **`Microsoft.AspNetCore.OpenApi` + [Scalar](https://scalar.com/)** instead of Swashbuckle — Swashbuckle was removed from the Microsoft ASP.NET Core Web API template in .NET 9.

## Troubleshooting

- **`Failed to load components` from a sidecar.** Make sure `dapr init` has been run on this machine. Aspire shells out to the Dapr CLI; if `dapr` isn't on PATH the sidecar resource will fail to start.
- **mDNS errors when `frontend` calls `catalog`.** [Known Dapr issue](https://github.com/dapr/dapr/issues/3256) when certain VPN or Cisco networking software is running. Workaround is to stop the offending software temporarily.
- **After upgrading Dapr on AKS**, restart the deployments. The `aks-deploy.ps1` script has an example.
