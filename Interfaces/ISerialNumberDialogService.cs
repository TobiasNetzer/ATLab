using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface ISerialNumberDialogService
{
    bool IsDialogOpen { get; }
    Task<string?> AskForSerialNumberAsync();
}
