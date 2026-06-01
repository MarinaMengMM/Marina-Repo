using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SharedModels;

namespace SecoreTrader;

public class TradeProducer : RabbitMqServiceBase, ITradeProducer
{
    private readonly ILogger<TradeProducer>? _logger;

    public TradeProducer(
        string queueName = "secore_inbound_queue",
        ILogger<TradeProducer>? logger = null)
        : base(queueName)
    {
        _logger = logger;
        _logger?.LogInformation("TradeProducer initialized for queue: {QueueName}", queueName);
    }

    public async Task SendTradeAsync(Instruction trade, CancellationToken cancellationToken = default)
    {
        var body = SerializeMessage(trade);
        var properties = new BasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.MessageId = trade.SecoreReference ?? Guid.NewGuid().ToString();
        properties.AppId = "TradeProducer";
        properties.Headers = new Dictionary<string, object?>
        {
            { "Source", "OrderService" },
            { "Priority", 1 }
        };

        await Channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger?.LogInformation("Sent trade {SecoreReference} to {QueueName}", trade.SecoreReference, QueueName);
    }

    public async Task SendBatchTradesAsync(IEnumerable<Instruction> trades)
    {
        foreach (var trade in trades)
        {
            await SendTradeAsync(trade);
        }
    }
}
