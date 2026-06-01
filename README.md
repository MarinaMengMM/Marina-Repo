**MyTrader**

Small microservices sample that publishes and consumes trade instructions via RabbitMQ.

**Contents**
- Share: common models and RabbitMQ helper (`Share/Instruction.cs`, `Share/RabbitmqServiceBase.cs`)
- SecoreTrader: HTTP API that publishes instructions to RabbitMQ (`SecoreTrader/`)
- MarketTrader: consumer that processes instructions from the queue (`MarketTrader/`)
- SecoreTraderTest: integration test for the API (`SecoreTraderTest/`)

**Prerequisites**
- Docker & Docker Compose
- .NET 8 SDK (for local builds / tests)

**Run locally with Docker Compose**
1. Build and start services (RabbitMQ + API + consumer):

```
docker-compose up --build
```

2. Start one service only (starts dependencies):

```
docker-compose up --build secoretrader
docker-compose up --build markettrader
```

The SecoreTrader API is exposed on http://localhost:5000 by default.

**API**
- POST /api/trades — submit a trade instruction (JSON). Example (bash):

```
curl -X POST http://localhost:5000/api/trades \
  -H "Content-Type: application/json" \
  -d '{"marketReference":"MKT-001","quantity":10,"amount":1234.56,"tradeDate":"2026-06-01T00:00:00Z","settlementDate":"2026-06-03T00:00:00Z","status":"New","createdDatetime":"2026-06-01T00:00:00Z","lastUpdatedTime":"2026-06-01T00:00:00Z"}'
```

PowerShell example (use Invoke-RestMethod):

```
$body = @{ marketReference = 'MKT-001'; quantity = 10; amount = 1234.56 } | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:5000/api/trades -Method Post -Body $body -ContentType 'application/json'
```

**Configuration**
- RabbitMQ connection can be provided via environment variables, a `.env` file, or `appsettings.json`.
- Supported environment keys:
  - `RABBITMQ_HOST` (default: `rabbitmq` when using docker-compose)
  - `RABBITMQ_PORT` (default: `5672`)
  - `RABBITMQ_USER` (default: `guest`)
  - `RABBITMQ_PASSWORD` (default: `guest`)
  - `RABBITMQ_VHOST` (default: `/`)
  - `RABBITMQ_QUEUE` (queue name fallback)

`Share/RabbitmqServiceBase.cs` contains settings lookup logic (environment → .env → appsettings.json).

**Testing**
- Integration tests live in `SecoreTraderTest`. Run tests with:

```
dotnet test SecoreTraderTest\SecoreTraderTest.csproj
```

Note: the provided tests replace the real `ITradeProducer` with a fake implementation so they verify the HTTP endpoint behaviour only. Remove the test-specific DI replacement to perform end-to-end tests that publish to RabbitMQ.

**Development**
- Build a project locally:

```
dotnet build SecoreTrader\SecoreTrader.csproj
dotnet build MarketTrader\MarketTrader.csproj
```

- Publish (used by Dockerfiles):

```
dotnet publish SecoreTrader\SecoreTrader.csproj -c Release -o ./out
```

**Notes & key files**
- `Share/Instruction.cs` — `Instruction` model used across projects.
- `Share/RabbitmqServiceBase.cs` — helper for creating connections and serializing messages.
- `SecoreTrader/TradeProducer.cs` — publishes JSON messages to the `secore_inbound_queue`.
- `MarketTrader/TradeConsumer.cs` — consumes messages and persists via `IInstructionProvider`.

If you want, I can add a small health-check endpoint, an example `.env`, or convert tests to full end-to-end RabbitMQ tests.
