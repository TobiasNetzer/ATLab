using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface IMessageBoxService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    
    Task<bool> ShowConfirmationImageAsync(string title, string message, string imagePath);
    Task ShowMessageAsync(string title, string message);
}
