// TradeConsumerWorker.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedModels;

namespace MarketTrader;

public class TradeConsumerWorker : BackgroundService
{
    private readonly ILogger<TradeConsumerWorker> _logger;
    private readonly TradeConsumer _consumer;

    public TradeConsumerWorker(
        ILogger<TradeConsumerWorker> logger,
        TradeConsumer consumer)
    {
        _logger = logger;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TradeConsumerWorker starting...");
        
        await _consumer.StartConsumingAsync(stoppingToken);
        
        // Keep the worker running until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TradeConsumerWorker stopping...");
        await _consumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}