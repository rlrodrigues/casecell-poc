FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CaseCellShop.slnx ./
COPY src/CaseCellShop.Api/CaseCellShop.Api.csproj src/CaseCellShop.Api/
COPY tests/CaseCellShop.Tests/CaseCellShop.Tests.csproj tests/CaseCellShop.Tests/
RUN dotnet restore CaseCellShop.slnx

COPY . .
RUN dotnet publish src/CaseCellShop.Api/CaseCellShop.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CaseCellShop.Api.dll"]
