using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface IMessageBoxService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowMessageAsync(string title, string message);
}
