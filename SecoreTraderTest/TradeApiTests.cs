using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using SecoreTrader;
using SharedModels;
using Xunit;

namespace SecoreTraderTest;

public class TradeApiTests : IClassFixture<TradeApiFactory>
{
    private readonly HttpClient _client;

    public TradeApiTests(TradeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostTrade_ReturnsOkAndReference()
    {
        var trade = new Instruction
        {
            SecoreReference = "SEC-005",
            MarketReference = "MKT-005",
            Quantity = 10,
            Amount = 1234.56m,
            TradeDate = System.DateTime.UtcNow,
            SettlementDate = DateTime.UtcNow.AddDays(2),
            Status = InstructionStatus.New,
            CreatedDatetime = DateTime.UtcNow,
            LastUpdatedTime = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/trades", trade);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Trade submitted successfully", body.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("reference").GetString()));
    }
}

public class TradeApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITradeProducer));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }
            
            // for real integration test, we would use the actual TradeProducer that connects to RabbitMQ
            // services.AddSingleton<ITradeProducer, TradeProducer>();

            // for unit test, we can use a fake TradeProducer that does nothing
            services.AddSingleton<ITradeProducer, FakeTradeProducer>();
        });
    }
}

public class FakeTradeProducer : ITradeProducer
{
    public Task SendTradeAsync(Instruction trade, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendBatchTradesAsync(IEnumerable<Instruction> trades)
    {
        return Task.CompletedTask;
    }
}
