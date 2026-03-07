FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY src/EInvoiceBridge.Core/EInvoiceBridge.Core.csproj src/EInvoiceBridge.Core/
COPY src/EInvoiceBridge.Application/EInvoiceBridge.Application.csproj src/EInvoiceBridge.Application/
COPY src/EInvoiceBridge.Validation/EInvoiceBridge.Validation.csproj src/EInvoiceBridge.Validation/
COPY src/EInvoiceBridge.Transformation/EInvoiceBridge.Transformation.csproj src/EInvoiceBridge.Transformation/
COPY src/EInvoiceBridge.Delivery/EInvoiceBridge.Delivery.csproj src/EInvoiceBridge.Delivery/
COPY src/EInvoiceBridge.Persistence/EInvoiceBridge.Persistence.csproj src/EInvoiceBridge.Persistence/
COPY src/EInvoiceBridge.Infrastructure/EInvoiceBridge.Infrastructure.csproj src/EInvoiceBridge.Infrastructure/
COPY src/EInvoiceBridge.Api/EInvoiceBridge.Api.csproj src/EInvoiceBridge.Api/

RUN dotnet restore src/EInvoiceBridge.Api/EInvoiceBridge.Api.csproj

COPY src/ src/
COPY db/ db/

RUN dotnet publish src/EInvoiceBridge.Api/EInvoiceBridge.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/db /app/db
ENTRYPOINT ["dotnet", "EInvoiceBridge.Api.dll"]
