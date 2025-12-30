using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface ISerialNumberDialogService
{
    Task<string?> AskForSerialNumberAsync();
}
