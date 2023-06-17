dapr run `
    --app-id frontend `
    --app-port 5266 `
    --dapr-http-port 3500 `
    --resources-path ../dapr/components `
    dotnet run