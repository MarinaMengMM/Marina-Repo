using SharedModels;

namespace SecoreTrader;

public interface ITradeProducer
{
    Task SendTradeAsync(Instruction trade, CancellationToken cancellationToken = default);
    Task SendBatchTradesAsync(IEnumerable<Instruction> trades);
}
