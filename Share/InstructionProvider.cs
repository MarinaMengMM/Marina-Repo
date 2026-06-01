// create a class that implements the IInstructionProvider interface
namespace SharedModels;
public class InstructionProvider : IInstructionProvider
{
    private readonly List<Instruction> _instructions = new List<Instruction>();

    public async Task<Instruction?> GetInstructionAsync(int id)
    {
        var instruction = _instructions.FirstOrDefault(i => i.ID == id);
        return await Task.FromResult(instruction);
    }

    public async Task<Instruction?> GetInstructionBySecoreRefAsync(string secoreReference)
    {
        var instruction = _instructions.FirstOrDefault(i => i.SecoreReference == secoreReference);
        return await Task.FromResult(instruction);
    }

    public async Task<Instruction?> GetInstructionByMarketRefAsync(string marketReference)
    {
        var instruction = _instructions.FirstOrDefault(i => i.MarketReference == marketReference);
        return await Task.FromResult(instruction);
    }

    public async Task<IEnumerable<Instruction>> ListInstructionsAsync()
    {
        return await Task.FromResult(_instructions.AsEnumerable());
    }

    public async Task<bool> CreateInstructionAsync(Instruction instruction)
    {
        instruction.ID = _instructions.Count + 1; // Simple ID generation
        instruction.CreatedDatetime = DateTime.UtcNow;
        instruction.LastUpdatedTime = DateTime.UtcNow;
        _instructions.Add(instruction);
        return await Task.FromResult(true);
    }

    public async Task<bool> UpdateInstructionAsync(Instruction instruction)
    {
        var existingInstruction = _instructions.FirstOrDefault(i => i.ID == instruction.ID);
        if (existingInstruction == null)
            return await Task.FromResult(false);

        existingInstruction.SecoreReference = instruction.SecoreReference;
        existingInstruction.MarketReference = instruction.MarketReference;
        existingInstruction.Quantity = instruction.Quantity;
        existingInstruction.Amount = instruction.Amount;
        existingInstruction.TradeDate = instruction.TradeDate;
        existingInstruction.SettlementDate = instruction.SettlementDate;
        existingInstruction.Status = instruction.Status;
        existingInstruction.LastUpdatedTime = DateTime.UtcNow;

        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteInstructionAsync(int id)
    {
        var instruction = _instructions.FirstOrDefault(i => i.ID == id);
        if (instruction == null)
            return await Task.FromResult(false);

        _instructions.Remove(instruction);
        return await Task.FromResult(true);
    }

    public int GetTotalInstructions()
    {
        return _instructions.Count;
    }
}