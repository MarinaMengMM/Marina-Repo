// Interface for providing instruction data

namespace SharedModels;
public interface IInstructionProvider
{
    Task<Instruction?> GetInstructionAsync(int id);

    Task<Instruction?> GetInstructionBySecoreRefAsync(string secoreReference);

    Task<Instruction?> GetInstructionByMarketRefAsync(string marketReference);

    Task<IEnumerable<Instruction>> ListInstructionsAsync();
    Task<bool> CreateInstructionAsync(Instruction instruction);
    Task<bool> UpdateInstructionAsync(Instruction instruction);
    Task<bool> DeleteInstructionAsync(int id);

    int GetTotalInstructions();
}