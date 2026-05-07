using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

// MailPit replaces the old maildev container. Pinning the SMTP port to 1025 so
// the existing Dapr SMTP binding (dapr/components/sendmail.yaml) keeps working
// without per-run wiring.
var mailpit = builder.AddMailPit("mailpit", httpPort: 8025, smtpPort: 1025);

// All sidecars share the same Dapr components directory at /dapr/components.
var componentsPath = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "..", "dapr", "components"));

DaprSidecarOptions Sidecar(string appId) => new()
{
    AppId = appId,
    ResourcesPaths = ImmutableHashSet.Create(componentsPath)
};

var catalog = builder.AddProject<Projects.catalog>("catalog")
    .WithDaprSidecar(Sidecar("catalog"));

var ordering = builder.AddProject<Projects.ordering>("ordering")
    .WaitFor(mailpit)
    .WithDaprSidecar(Sidecar("ordering"));

builder.AddProject<Projects.frontend>("frontend")
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WithDaprSidecar(Sidecar("frontend"));

builder.Build().Run();
