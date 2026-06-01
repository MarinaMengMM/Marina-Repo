namespace SharedModels;

// in project Share, Create a model names Instruction with fields ID, SecoreReference, MarketReference, Quantity, Amount, TradeDate, SettlementDate, Status, CreatedDatetime, LastUpdatedTime,
public class Instruction
{
    public int? ID { get; set; }
    public string? SecoreReference { get; set; }
    public TradeType Type { get; set; }
    public string? MarketReference { get; set; }
    public int? Quantity { get; set; }
    public decimal? Amount { get; set; }
    public DateTime TradeDate { get; set; }
    public DateTime? SettlementDate { get; set; }
    public InstructionStatus Status { get; set; }
    public DateTime? CreatedDatetime { get; set; }
    public DateTime? LastUpdatedTime { get; set; }
}


// Create a enum named InstructionStatus with values New, Hold,Released, Cancelled, Settled.
public enum InstructionStatus
{
    New,
    Received,
    Hold,
    Released,
    Cancelled,
    Settled
}

public enum TradeType
{
    Buy,
    Sell
}