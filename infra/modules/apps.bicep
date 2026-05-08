// All six Container Apps: catalog, ordering, frontend, mailpit (private),
// mailpit-ui (public Caddy proxy in front of mailpit:8025), aspire-dashboard.
// The three .NET services use placeholder images on first deploy — `azd
// deploy` swaps them for the freshly-built images using the azd-service-name
// tag as the routing key.

param location string
param tags object

param containerAppsEnvironmentId string
param containerRegistryLoginServer string
param managedIdentityResourceId string
param managedIdentityClientId string
param keyVaultName string

// Public placeholder image; azd replaces it with the built image of each
// service on first `azd deploy`.
var placeholderImage = 'mcr.microsoft.com/k8se/quickstart:latest'

var kvBaseUrl = 'https://${keyVaultName}${az.environment().suffixes.keyvaultDns}'

resource acaEnv 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: last(split(containerAppsEnvironmentId, '/'))
}

// -------- Aspire dashboard --------
// Standalone OTEL collector + dashboard image. External HTTP ingress on
// 18888 for the UI; an additional internal port mapping on 18889 lets
// the other apps push OTLP/gRPC to it. BrowserToken auth means the
// dashboard URL is gated behind a token printed in its container logs.
resource aspireDashboard 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'aspire-dashboard'
  location: location
  tags: union(tags, { 'azd-service-name': 'aspire-dashboard' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    environmentId: acaEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 18888
        transport: 'http'
        additionalPortMappings: [
          {
            external: false
            targetPort: 18889
            exposedPort: 18889
          }
        ]
      }
      registries: [
        {
          server: containerRegistryLoginServer
          identity: managedIdentityResourceId
        }
      ]
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'aspire-dashboard'
          image: '${containerRegistryLoginServer}/aspire-dashboard:9.0'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            // Allow non-token OTLP from inside the env so the apps don't
            // need to authenticate to the dashboard. The web UI keeps its
            // BrowserToken gate, so only the OTLP path is unauthenticated
            // and that path is internal-only.
            {
              name: 'DASHBOARD__OTLP__AUTHMODE'
              value: 'Unsecured'
            }
            {
              name: 'DASHBOARD__FRONTEND__AUTHMODE'
              value: 'BrowserToken'
            }
          ]
        }
      ]
      // Scale to zero when nobody's looking at the dashboard. Apps push
      // OTLP continuously while they're awake, but for an idle demo env
      // we'd rather not pay for an idle dashboard replica.
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

// -------- MailPit (internal only, smtp + ui) --------
// Main ingress is TCP on 1025 so the Dapr SMTP binding from ordering's
// sidecar can speak raw SMTP wire protocol — additional ports inherit the
// main transport, so we couldn't keep main as HTTP and tunnel SMTP through
// it. The web UI on 8025 is exposed as an additional internal port; ACA
// Consumption-only envs don't allow external TCP ingress without a custom
// VNet, so the UI is not publicly reachable here. A small public-facing
// proxy app can be layered on later to surface the UI for demos.
resource mailpit 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'mailpit'
  location: location
  tags: union(tags, { 'azd-service-name': 'mailpit' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    environmentId: acaEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 1025
        exposedPort: 1025
        transport: 'tcp'
        additionalPortMappings: [
          {
            external: false
            targetPort: 8025
            exposedPort: 8025
          }
        ]
      }
      registries: [
        {
          server: containerRegistryLoginServer
          identity: managedIdentityResourceId
        }
      ]
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'mailpit'
          image: '${containerRegistryLoginServer}/mailpit:latest'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            // MailPit by default rejects SMTP AUTH on non-TLS connections.
            // The Dapr SMTP binding sends AUTH (it has user/password set
            // even though the values are placeholders), so we relax the
            // defaults to match how Aspire's MailPit integration runs it
            // in local dev.
            {
              name: 'MP_SMTP_AUTH_ACCEPT_ANY'
              value: 'true'
            }
            {
              name: 'MP_SMTP_AUTH_ALLOW_INSECURE'
              value: 'true'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// -------- mailpit-ui: public Caddy reverse proxy in front of MailPit --------
// MailPit's own ingress is TCP-only (so SMTP works), which means its web UI
// isn't reachable from outside the env. This tiny Caddy app fronts the UI
// over HTTP so the demo can show the received order confirmation email.
// Caddy's CLI proxy mode means no Caddyfile or extra config is needed — the
// container just runs `caddy reverse-proxy --from :8080 --to mailpit:8025`.
resource mailpitUi 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'mailpit-ui'
  location: location
  tags: union(tags, { 'azd-service-name': 'mailpit-ui' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    environmentId: acaEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: containerRegistryLoginServer
          identity: managedIdentityResourceId
        }
      ]
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'caddy'
          image: '${containerRegistryLoginServer}/caddy:latest'
          command: [ 'caddy' ]
          args: [ 'reverse-proxy', '--from', ':8080', '--to', 'mailpit:8025' ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      // Scale to zero — the proxy is stateless, cold-starts in seconds.
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

var commonEnv = [
  // Picked up by DefaultAzureCredential so the apps and Dapr sidecars
  // know which user-assigned MI to use for outbound auth.
  {
    name: 'AZURE_CLIENT_ID'
    value: managedIdentityClientId
  }
  // OTLP forwarding to the in-env Aspire dashboard. Internal-only, no auth.
  {
    name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
    value: 'http://aspire-dashboard:18889'
  }
  {
    name: 'OTEL_EXPORTER_OTLP_PROTOCOL'
    value: 'grpc'
  }
]

// -------- catalog --------
resource catalog 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'catalog'
  location: location
  tags: union(tags, { 'azd-service-name': 'catalog' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    environmentId: acaEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: containerRegistryLoginServer
          identity: managedIdentityResourceId
        }
      ]
      secrets: [
        {
          name: 'catalogdb-connection-string'
          keyVaultUrl: '${kvBaseUrl}/secrets/catalog-connection-string'
          identity: managedIdentityResourceId
        }
      ]
      dapr: {
        enabled: true
        appId: 'catalog'
        appPort: 8080
        appProtocol: 'http'
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'catalog'
          image: placeholderImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: concat(commonEnv, [
            {
              name: 'ConnectionStrings__catalogdb'
              secretRef: 'catalogdb-connection-string'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
          ])
        }
      ]
      // Scale to zero. Cold-start cost on first frontend->catalog hit
      // is ~5s. Side effect: the cron binding (5-min inventory reset)
      // doesn't fire while idle, which is fine — there's nobody using
      // the demo at that moment anyway.
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

// -------- ordering --------
resource ordering 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ordering'
  location: location
  tags: union(tags, { 'azd-service-name': 'ordering' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    environmentId: acaEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: containerRegistryLoginServer
          identity: managedIdentityResourceId
        }
      ]
      dapr: {
        enabled: true
        appId: 'ordering'
        appPort: 8080
        appProtocol: 'http'
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'ordering'
          image: placeholderImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: concat(commonEnv, [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
          ])
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// -------- frontend --------
resource frontend 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'frontend'
  location: location
  tags: union(tags, { 'azd-service-name': 'frontend' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    environmentId: acaEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: containerRegistryLoginServer
          identity: managedIdentityResourceId
        }
      ]
      dapr: {
        enabled: true
        appId: 'frontend'
        appPort: 8080
        appProtocol: 'http'
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: placeholderImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: concat(commonEnv, [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
          ])
        }
      ]
      // Scale to zero. The default ACA HTTP scaler wakes a replica on
      // first request — adds ~5s cold-start to the first browse, then
      // hot until the next idle period.
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

output frontendUrl string = 'https://${frontend.properties.configuration.ingress.fqdn}'
output dashboardUrl string = 'https://${aspireDashboard.properties.configuration.ingress.fqdn}'
output mailpitUiUrl string = 'https://${mailpitUi.properties.configuration.ingress.fqdn}'
