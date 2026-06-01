FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Share/Share.csproj", "Share/"]
COPY ["SecoreTrader/SecoreTrader.csproj", "SecoreTrader/"]

RUN dotnet restore "SecoreTrader/SecoreTrader.csproj"

COPY ["Share/", "Share/"]
COPY ["SecoreTrader/", "SecoreTrader/"]

WORKDIR /src/SecoreTrader
RUN dotnet publish "SecoreTrader.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS="http://+:80"
EXPOSE 80
ENTRYPOINT ["dotnet", "SecoreTrader.dll"]
