FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/BulkLoad/BulkLoad.Worker/BulkLoad.Worker.csproj", "Services/BulkLoad/BulkLoad.Worker/"]
COPY ["src/Services/BulkLoad/BulkLoad.Application/BulkLoad.Application.csproj", "Services/BulkLoad/BulkLoad.Application/"]
COPY ["src/Services/BulkLoad/BulkLoad.Domain/BulkLoad.Domain.csproj", "Services/BulkLoad/BulkLoad.Domain/"]
COPY ["src/Services/BulkLoad/BulkLoad.Infrastructure/BulkLoad.Infrastructure.csproj", "Services/BulkLoad/BulkLoad.Infrastructure/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/BulkLoad/BulkLoad.Worker/BulkLoad.Worker.csproj"
COPY src/ .
WORKDIR "/src/Services/BulkLoad/BulkLoad.Worker"
RUN dotnet build "BulkLoad.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BulkLoad.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BulkLoad.Worker.dll"]
