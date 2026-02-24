FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Notifications/Notifications.Worker/Notifications.Worker.csproj", "Services/Notifications/Notifications.Worker/"]
COPY ["src/Services/Notifications/Notifications.Application/Notifications.Application.csproj", "Services/Notifications/Notifications.Application/"]
COPY ["src/Services/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj", "Services/Notifications/Notifications.Infrastructure/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/Notifications/Notifications.Worker/Notifications.Worker.csproj"
COPY src/ .
WORKDIR "/src/Services/Notifications/Notifications.Worker"
RUN dotnet build "Notifications.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notifications.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notifications.Worker.dll"]
