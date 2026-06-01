FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Share/Share.csproj", "Share/"]
COPY ["MarketTrader/MarketTrader.csproj", "MarketTrader/"]

RUN dotnet restore "MarketTrader/MarketTrader.csproj"

COPY ["Share/", "Share/"]
COPY ["MarketTrader/", "MarketTrader/"]

WORKDIR /src/MarketTrader
RUN dotnet publish "MarketTrader.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MarketTrader.dll"]
