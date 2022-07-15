dapr run `
    --app-id frontend `
    --app-port 5266 `
    --dapr-http-port 3500 `
    --components-path ../dapr/components `
    --config ../dapr/components/daprConfig.yaml `
    dotnet run