using System.Collections.ObjectModel;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class RuntimeVariableEditorViewModel : ViewModelBase
{
    public ObservableCollection<CustomVariable> RuntimeVariables { get; } = new();

    [ObservableProperty]
    private CustomVariable? _selectedVariable;
    
    [RelayCommand]
    private void AddVariable()
    {
        var newVar = new CustomVariable();

        RuntimeVariables.Add(newVar);
        SelectedVariable = newVar;
    }

    [RelayCommand]
    private void RemoveVariable()
    {
        if (SelectedVariable is null)
            return;

        RuntimeVariables.Remove(SelectedVariable);
        SelectedVariable = null;
    }
}