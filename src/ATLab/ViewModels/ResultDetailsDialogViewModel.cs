using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace ATLab.ViewModels;

/// <summary>Shows all result details of a step (e.g. NACKs, failed interconnect nets) as selectable text.</summary>
public partial class ResultDetailsDialogViewModel : ViewModelBase
{
    private readonly DialogManager _dialogManager;

    [ObservableProperty]
    private string _stepTitle = string.Empty;

    [ObservableProperty]
    private string _details = string.Empty;

    public ResultDetailsDialogViewModel(DialogManager dialogManager)
    {
        _dialogManager = dialogManager;
    }

    public void Show(TestStepViewModel step)
    {
        if (!step.HasResultDetails)
            return;

        StepTitle = $"{step.TestStep.Number}  {step.TestStep.Name}";
        Details = step.ResultDetails ?? string.Empty;

        _dialogManager
            .CreateDialog(this)
            .WithMaxWidth(1000)
            .Dismissible()
            .Show();
    }

    [RelayCommand]
    private void Close()
    {
        _dialogManager.Close(this);
    }
}
