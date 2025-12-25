using Avalonia.Controls;
using ATLab.ViewModels;

namespace ATLab.Views;

public partial class MessageBox : Window
{
    public enum MessageBoxResult
    {
        Ok,
        Cancel
    }

    public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

    public MessageBox()
    {
        InitializeComponent();
        
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MessageBoxViewModel vm)
            {
                vm.CloseRequested += (isOk) =>
                {
                    Result = isOk ? MessageBoxResult.Ok : MessageBoxResult.Cancel;
                    Close();
                };
            }
        };
    }
}
