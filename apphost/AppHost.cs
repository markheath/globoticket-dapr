using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

// MailPit replaces the old maildev container. Pinning the SMTP port to 1025 so
// the existing Dapr SMTP binding (dapr/components/sendmail.yaml) keeps working
// without per-run wiring.
var mailpit = builder.AddMailPit("mailpit", httpPort: 8025, smtpPort: 1025);

// Postgres backs the catalog (EF Core via Aspire-injected connection string)
// and the ordering Dapr state store (component YAML references localhost:5432
// directly). Pinning the password keeps the orderstore.yaml component static —
// same trade-off accepted for MailPit. The basket and workflow state stores
// run on the Redis that `dapr init` provisions; in Azure they move to
// Postgres so the cloud topology doesn't need a separate Redis service.
var pgPassword = builder.AddParameter("pg-password", "postgres", secret: true);
var postgres = builder.AddPostgres("pg", password: pgPassword, port: 5432)
                      .WithDataVolume();
var catalogDb = postgres.AddDatabase("catalogdb");
var orderingDb = postgres.AddDatabase("orderingdb");

// All sidecars share the same Dapr components directory at /dapr/components.
var componentsPath = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "..", "dapr", "components"));

DaprSidecarOptions Sidecar(string appId) => new()
{
    AppId = appId,
    ResourcesPaths = ImmutableHashSet.Create(componentsPath)
};

var catalog = builder.AddProject<Projects.catalog>("catalog")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithDaprSidecar(Sidecar("catalog"));

var ordering = builder.AddProject<Projects.ordering>("ordering")
    .WaitFor(mailpit)
    .WaitFor(orderingDb)
    .WithDaprSidecar(Sidecar("ordering"));

builder.AddProject<Projects.frontend>("frontend")
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WithDaprSidecar(Sidecar("frontend"));

builder.Build().Run();
