FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["global.json", "./"]
COPY ["catalog/catalog.csproj", "catalog/"]
COPY ["servicedefaults/servicedefaults.csproj", "servicedefaults/"]
RUN dotnet restore "catalog/catalog.csproj"
COPY . .
WORKDIR "/src/catalog"
RUN dotnet publish "catalog.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "catalog.dll"]
