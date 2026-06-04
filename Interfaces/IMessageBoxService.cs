using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface IMessageBoxService
{
    Task<bool> ShowConfirmationAsync(string title, string message, string okText = "Ok", string cancelText = "Cancel", bool useControlModule = false);
    
    Task<bool> ShowConfirmationImageAsync(string title, string message, string imagePath, string okText = "Ok", string cancelText = "Cancel", bool useControlModule = false);
    Task ShowMessageAsync(string title, string message, bool useControlModule = false);
}