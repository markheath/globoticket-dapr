FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["global.json", "./"]
COPY ["frontend/frontend.csproj", "frontend/"]
COPY ["servicedefaults/servicedefaults.csproj", "servicedefaults/"]
RUN dotnet restore "frontend/frontend.csproj"
COPY . .
WORKDIR "/src/frontend"
RUN dotnet publish "frontend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "frontend.dll"]
