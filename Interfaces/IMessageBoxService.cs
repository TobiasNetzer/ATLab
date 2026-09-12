using System.Threading.Tasks;
using ATLab.Enums;

namespace ATLab.Interfaces;

public interface IMessageBoxService
{
    bool IsDialogOpen { get; }
    Task<bool> ShowConfirmationDestructiveAsync(string title, string message);
    Task<bool> ShowConfirmationImageAsync(string title, string message, string imagePath, DialogFunction dialogFunction = DialogFunction.CONFIRMATION);
    Task ShowMessageAsync(string title, string message);
}