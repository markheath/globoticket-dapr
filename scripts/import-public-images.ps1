#!/usr/bin/env pwsh
# Pre-creates the RG and ACR (idempotent), then imports the public container
# images this demo depends on, skipping anything already present.
#
# Runs as an azd PRE-provision hook because ACA validates manifests when it
# creates the container app resource — the images must already be in ACR by
# the time bicep gets to creating mailpit / mailpit-ui / aspire-dashboard,
# otherwise the deployment fails with MANIFEST_UNKNOWN.
#
# `az acr import` is a control-plane call: ACR pulls from the source registry
# server-side, so this script doesn't ship image bytes from the dev machine.

$ErrorActionPreference = 'Stop'

$acr = $env:AZURE_CONTAINER_REGISTRY_NAME
$rg = $env:AZURE_RESOURCE_GROUP
$location = $env:AZURE_LOCATION
$envName = $env:AZURE_ENV_NAME

if (-not $acr -or -not $rg -or -not $location) {
    Write-Host "[skip] First-time provisioning detected — no cached ACR name yet."
    Write-Host "[skip] Re-run 'azd up' once provisioning fails on the container apps."
    Write-Host "[skip] (After the first attempt, azd caches AZURE_CONTAINER_REGISTRY_NAME"
    Write-Host "[skip]  and this hook will populate ACR before bicep tries to use it.)"
    exit 0
}

# Ensure RG and ACR exist before bicep runs. Both are idempotent: if bicep
# already created them on a previous run, these are no-ops; on a re-run after
# `azd down --purge` they (re)create the resources with the same names so the
# `az acr import` calls below have somewhere to land.
Write-Host "[ensure] resource group $rg in $location"
az group create -n $rg -l $location --tags "azd-env-name=$envName" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "az group create failed" }

Write-Host "[ensure] container registry $acr"
az acr create -n $acr -g $rg --sku Basic -l $location --tags "azd-env-name=$envName" --admin-enabled false | Out-Null
if ($LASTEXITCODE -ne 0) { throw "az acr create failed" }

$images = @(
    @{ Source = 'docker.io/axllent/mailpit:latest';              Image = 'mailpit:latest' }
    @{ Source = 'mcr.microsoft.com/dotnet/aspire-dashboard:9.0'; Image = 'aspire-dashboard:9.0' }
    @{ Source = 'docker.io/library/caddy:latest';                Image = 'caddy:latest' }
)

foreach ($img in $images) {
    az acr repository show --name $acr --image $img.Image 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[skip]   $($img.Image) already in $acr"
        continue
    }
    Write-Host "[import] $($img.Source) -> $acr/$($img.Image)"
    az acr import --name $acr --source $img.Source --image $img.Image
    if ($LASTEXITCODE -ne 0) {
        throw "az acr import failed for $($img.Source)"
    }
}
