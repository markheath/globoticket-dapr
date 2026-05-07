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
| `pg`        | `postgres://postgres:postgres@localhost:5432` (catalogdb, orderingdb) |

Dapr sidecar ports are *not* pinned — Aspire allocates them dynamically, and the .NET apps reach Dapr via the `DAPR_HTTP_PORT` env var the toolkit injects. If you want to call a Dapr API from outside (e.g. the example `.http` files), look up the sidecar's port next to `<app>-dapr` in the Aspire dashboard.

Distributed traces, structured logs, and resource metrics are all in the dashboard.

If you need a dummy credit card number on the checkout page, use `4242424242424242` or `5555555555554444`. Any card number ending in `0000` is rejected by the mock card-charge step in the checkout workflow — useful for demonstrating the saga compensation path.

## Architecture overview

- **frontend** — ASP.NET Core MVC site. Lets visitors browse the catalog and place orders. Talks to `catalog` via Dapr service invocation, stores the shopping basket in a Dapr state store (Redis), and submits orders via Dapr pub/sub.
- **catalog** — Web API that returns the list of events from a PostgreSQL database (via EF Core + Npgsql). The connection string is injected by Aspire. A Dapr cron binding fires every 5 minutes to rotate which event is on special offer and reset ticket inventory back to its seeded levels. Exposes `POST/DELETE /event/{id}/reserve` for the workflow to atomically reserve and release tickets.
- **ordering** — Web API that subscribes to the `orders` topic and hands each incoming order to a **Dapr Workflow** (`CheckoutWorkflow`). The workflow runs the saga: reserve tickets for every line → mock-charge the card → persist the order to a state store → send the confirmation email via the SMTP output binding. If reservation or charge fails, every reservation made so far is released as a compensating action. Workflow state lives in a Redis store flagged `actorStateStore: true`. The SMTP binding's credentials are pulled from the Dapr **secret store** via `secretKeyRef`, so the component YAML never contains the username or password directly.

The basket stays on Redis on purpose — it is ephemeral session state and Redis is the right backend for that. PostgreSQL is used for the things that need to outlive a process restart (catalog data, order history).

The seed data is intentionally varied so the workflow's branches can all be demonstrated: one event with plenty of stock (happy path), one nearly sold out (small order succeeds, larger order triggers compensation), and one fully sold out (immediate failure).

Dapr components live in `dapr/components/`:

| File                    | Component name   | Purpose                                                                          |
|-------------------------|------------------|----------------------------------------------------------------------------------|
| `pubsub.yaml`           | `pubsub`         | Redis pub/sub (orders topic — workflow trigger)                                  |
| `stateStore.yml`        | `shopstate`      | Redis state store for the shopping basket                                        |
| `orderstore.yaml`       | `orderstore`     | PostgreSQL state store for persisted orders                                      |
| `workflowstate.yml`     | `workflowstate`  | Redis state store for Dapr Workflow runtime (actor-backed)                       |
| `email.yml`             | `sendmail`       | SMTP output binding pointed at MailPit                                           |
| `cron.yml`              | `scheduled`      | Cron input binding that calls the catalog                                        |
| `localSecretStore.yml`  | `secretstore`    | Local file secret store; supplies SMTP credentials to `sendmail`                 |

The Aspire AppHost wires the same components folder into every Dapr sidecar via `DaprSidecarOptions.ResourcesPaths`. Component-level `scopes:` restrict the order and workflow stores to the `ordering` service only.

## Deploying

The `aks-deploy.ps1` and `azure-container-apps-deploy.ps1` scripts and the manifests under `deploy/` and `dapr/containerapps-components/` were last current for the Dapr 1.13 / .NET 8 era. They predate the move to PostgreSQL and the outbox pattern and are **out of date**. Bringing them back to parity is tracked as a separate piece of work — until that lands, treat the [`dapr-1-13`](https://github.com/markheath/globoticket-dapr/tree/dapr-1-13) branch as the source of truth for working K8s/ACA deploys.

## Notable tooling choices

- **`Aspire.AppHost.Sdk` 13.x** with [`CommunityToolkit.Aspire.Hosting.Dapr`](https://github.com/CommunityToolkit/Aspire/tree/main/src/CommunityToolkit.Aspire.Hosting.Dapr) — the Microsoft `Aspire.Hosting.Dapr` package was handed to the Community Toolkit.
- **PostgreSQL** for the catalog and orders, via `Aspire.Hosting.PostgreSQL` and the Aspire `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` integration on the consumer side. One Postgres instance, two databases (`catalogdb`, `orderingdb`) with `WithDataVolume()` so data survives restarts.
- **[MailPit](https://mailpit.axllent.org/)** instead of maildev for local SMTP capture — actively maintained, has a first-party Aspire integration.
- **`Microsoft.AspNetCore.OpenApi` + [Scalar](https://scalar.com/)** instead of Swashbuckle — Swashbuckle was removed from the Microsoft ASP.NET Core Web API template in .NET 9.

## Troubleshooting

- **`Failed to load components` from a sidecar.** Make sure `dapr init` has been run on this machine. Aspire shells out to the Dapr CLI; if `dapr` isn't on PATH the sidecar resource will fail to start.
- **mDNS errors when `frontend` calls `catalog`.** [Known Dapr issue](https://github.com/dapr/dapr/issues/3256) when certain VPN or Cisco networking software is running. Workaround is to stop the offending software temporarily.
- **After upgrading Dapr on AKS**, restart the deployments. The `aks-deploy.ps1` script has an example.
