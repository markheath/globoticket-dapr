#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ordering/ordering.csproj", "ordering/"]
RUN dotnet restore "ordering/ordering.csproj"
COPY . .
WORKDIR "/src/ordering"
RUN dotnet build "ordering.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ordering.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ordering.dll"]