FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CaseCellShop.slnx ./
COPY src/CaseCellShop.Domain/CaseCellShop.Domain.csproj src/CaseCellShop.Domain/
COPY src/CaseCellShop.Application/CaseCellShop.Application.csproj src/CaseCellShop.Application/
COPY src/CaseCellShop.Infrastructure/CaseCellShop.Infrastructure.csproj src/CaseCellShop.Infrastructure/
COPY src/CaseCellShop.Api/CaseCellShop.Api.csproj src/CaseCellShop.Api/
COPY tests/CaseCellShop.Tests/CaseCellShop.Tests.csproj tests/CaseCellShop.Tests/
RUN dotnet restore src/CaseCellShop.Api/CaseCellShop.Api.csproj

COPY . .
RUN dotnet publish src/CaseCellShop.Api/CaseCellShop.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CaseCellShop.Api.dll"]
