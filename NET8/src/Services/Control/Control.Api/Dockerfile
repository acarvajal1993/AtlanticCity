FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Control/Control.Api/Control.Api.csproj", "Services/Control/Control.Api/"]
COPY ["src/Services/Control/Control.Application/Control.Application.csproj", "Services/Control/Control.Application/"]
COPY ["src/Services/Control/Control.Domain/Control.Domain.csproj", "Services/Control/Control.Domain/"]
COPY ["src/Services/Control/Control.Infrastructure/Control.Infrastructure.csproj", "Services/Control/Control.Infrastructure/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/Control/Control.Api/Control.Api.csproj"
COPY src/ .
WORKDIR "/src/Services/Control/Control.Api"
RUN dotnet build "Control.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Control.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Control.Api.dll"]
