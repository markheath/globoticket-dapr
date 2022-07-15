dapr run `
    --app-id ordering `
    --app-port 5293 `
    --dapr-http-port 3502 `
    --components-path ../dapr/components `
    --config ../dapr/components/daprConfig.yaml `
    dotnet run