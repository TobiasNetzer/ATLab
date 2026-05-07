using System.Collections.ObjectModel;
using ATLab.Models;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class RuntimeVariableEditorViewModel
{
    public ObservableCollection<CustomVariable> RuntimeVariables { get; } = new();
    public int SelectedVariableIndex { get; set; }
    
    [RelayCommand]
    private void AddVariable()
    {
        var index = SelectedVariableIndex <= 0 ? 0 : SelectedVariableIndex + 1;
        if (index > RuntimeVariables.Count) index = RuntimeVariables.Count;

        var newVar = new CustomVariable();

        RuntimeVariables.Insert(index, newVar);
        SelectedVariableIndex = index;
    }

    [RelayCommand]
    private void RemoveVariable()
    {
        if (SelectedVariableIndex < 0 || SelectedVariableIndex >= RuntimeVariables.Count)
            return;
        
        var v = RuntimeVariables[SelectedVariableIndex];

        RuntimeVariables.RemoveAt(SelectedVariableIndex);
    }
}