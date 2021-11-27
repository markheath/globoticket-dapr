dapr run `
    --app-id ordering `
    --app-port 5293 `
    --dapr-http-port 3502 `
    --components-path ../dapr/components `
    dotnet run