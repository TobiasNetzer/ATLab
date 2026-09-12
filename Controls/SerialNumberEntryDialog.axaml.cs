using Avalonia.Controls;

namespace ATLab.Controls;

public partial class SerialNumberEntryDialog : UserControl
{
    public SerialNumberEntryDialog()
    {
        InitializeComponent();
        
        SerialNoTextBox.AttachedToVisualTree += (_, _) =>
        {
            SerialNoTextBox.Focus();
            SerialNoTextBox.CaretIndex = SerialNoTextBox.Text?.Length ?? 0;
        };
    }

}