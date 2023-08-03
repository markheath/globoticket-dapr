$Host.UI.RawUI.WindowTitle = "Ordering"
dapr run `
    --app-id ordering `
    --app-port 5293 `
    --dapr-http-port 3502 `
    --resources-path ../dapr/components `
    dotnet run