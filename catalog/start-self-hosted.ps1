dapr run `
    --app-id catalog `
    --app-port 5016 `
    --dapr-http-port 3501 `
    --components-path ../dapr/components `
    --config ../dapr/components/daprConfig.yaml `
    dotnet run