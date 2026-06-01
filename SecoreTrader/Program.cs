
using SecoreTrader;
using SharedModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Register TradeProducer as a singleton implementation of ITradeProducer
builder.Services.AddSingleton<ITradeProducer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TradeProducer>>();
    return new TradeProducer("secore_inbound_queue", logger);
});

var app = builder.Build();

// API endpoint to submit trades
app.MapPost("/api/trades", async (Instruction trade, ITradeProducer producer) =>
{
    // Generate reference if not provided
    trade.SecoreReference ??= Guid.NewGuid().ToString();

    Console.WriteLine($"Received trade submission: {trade.SecoreReference} - {trade.Type} - {trade.Quantity}@{trade.Amount}");
    
    await producer.SendTradeAsync(trade);
    
    return Results.Ok(new { 
        message = "Trade submitted successfully", 
        reference = trade.SecoreReference 
    });
});

// Batch endpoint
app.MapPost("/api/trades/batch", async (List<Instruction> trades, ITradeProducer producer) =>
{
    foreach (var trade in trades)
    {
        trade.SecoreReference ??= Guid.NewGuid().ToString();
    }
    
    await producer.SendBatchTradesAsync(trades);
    
    return Results.Ok(new { 
        message = $"{trades.Count} trades submitted successfully" 
    });
});

app.Run();