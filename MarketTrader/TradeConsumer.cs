// TradeConsumer.cs - Fixed for RabbitMQ.Client 7.x
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedModels;
using System.Text;
using System.Xml;

namespace MarketTrader;

public class TradeConsumer : RabbitMqServiceBase
{
    private readonly ILogger<TradeConsumer> _logger;
    private readonly IInstructionProvider _instructionProvider;

    public TradeConsumer(
        IInstructionProvider instructionProvider,
        string queueName = "secore_inbound_queue",        
        ILogger<TradeConsumer>? logger = null) 
        : base(queueName)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instructionProvider = instructionProvider ;
        _logger.LogInformation("TradeConsumer initialized for queue: {QueueName}", queueName);
    }
    
    // Separate method to start consuming (can't do async in constructor)
    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        var consumer = new AsyncEventingBasicConsumer(Channel);
        
        // ✅ Use ReceivedAsync (not Received) - v7.x API
        consumer.ReceivedAsync += OnMessageReceivedAsync;
        
        await Channel.BasicConsumeAsync(
            queue: QueueName, 
            autoAck: false, 
            consumer: consumer,
            cancellationToken: cancellationToken
        );
        
        _logger.LogInformation("Started consuming from queue: {QueueName}", QueueName);
    }

    // ✅ Make the handler async and return Task
    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs e)
    {
        try
        {
            var body = e.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var instruction = DeserializeMessage<Instruction>(body);
            
            _logger.LogInformation("Received trade instruction: {SecoreReference} - {Type} - {Quantity}@{Price}", 
                instruction.SecoreReference, instruction.Type, instruction.Quantity, instruction.Amount);
            
            var existingInstruction = await _instructionProvider.GetInstructionBySecoreRefAsync(instruction.SecoreReference);
            if (existingInstruction != null)
            {
                _logger.LogWarning("Instruction with SecoreReference {SecoreReference} already exists. Skipping.", instruction.SecoreReference);
                await Channel.BasicAckAsync(e.DeliveryTag, false);
                return;
            }else
            {
                instruction.Status = InstructionStatus.Received;
                await _instructionProvider.CreateInstructionAsync(instruction);
                _logger.LogInformation("Instruction {SecoreReference} saved to provider.", instruction.SecoreReference);
                _logger.LogInformation("Total instructions in provider: {TotalCount}", _instructionProvider.GetTotalInstructions());
                await Channel.BasicAckAsync(e.DeliveryTag, false);
            }
           
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message from queue {QueueName}", QueueName);
            // ✅ Use Async method for Nack
            await Channel.BasicNackAsync(e.DeliveryTag, false, true);
        }
    }
}