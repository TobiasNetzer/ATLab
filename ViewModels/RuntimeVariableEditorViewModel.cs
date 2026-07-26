using System.Collections.ObjectModel;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class RuntimeVariableEditorViewModel : ViewModelBase
{
    private readonly ProjectModel _projectModel;
    
    public ObservableCollection<CustomVariable> RuntimeVariables => _projectModel.RuntimeVariables;

    [ObservableProperty]
    private CustomVariable? _selectedVariable;

    public RuntimeVariableEditorViewModel(ProjectModel projectModel)
    {
        _projectModel = projectModel;
    }
    
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