using System.Threading.Tasks;

namespace ATLab.Interfaces;

/// <summary>Asks the operator for the serial number of the device under test before a run.</summary>
public interface ISerialNumberDialogService
{
    bool IsDialogOpen { get; }
    Task<string?> AskForSerialNumberAsync();
}
