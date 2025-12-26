using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ScpiScriptItemViewModel : ViewModelBase
{
    private readonly IScpiScriptRepository _repository;
    private readonly ScpiScript _model;
    
    public ObservableCollection<ScpiCommand> Commands { get; } = new();

    [ObservableProperty]
    private int _selectedCommandIndex;

    public ObservableCollection<ScpiVariable> Variables { get; } = new();
    
    [ObservableProperty]
    private int _selectedVariableIndex;

    public ScpiScriptItemViewModel(IScpiScriptRepository repository, ScpiScript model)
    {
        _repository = repository;
        _model = model;

        LoadFromModel();
    }

    public string Id => _model.Id;

    public string Name
    {
        get => _model.Name;
        set
        {
            if (_model.Name != value)
            {
                _model.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Description
    {
        get => _model.Description;
        set
        {
            if (_model.Description != value)
            {
                _model.Description = value;
                OnPropertyChanged();
            }
        }
    }
    
    public void LoadFromModel()
    {
        Commands.Clear();
        foreach (var command in _model.Commands)
            Commands.Add(command);

        Variables.Clear();
        foreach (var p in _model.Variables)
            Variables.Add(p);
    }

    private void ApplyToModel()
    {
        _model.Commands = Commands.ToList();
        _model.Variables = Variables.ToList();
    }

    public async Task SaveAsync()
    {
        ApplyToModel();
        await _repository.SaveAsync(_model);
    }

    public ScpiScript GetModel() => _model;

    [RelayCommand]
    private void AddCommand()
    {
        var indexToInsertNewCommand = SelectedCommandIndex < 0 ? 0 : SelectedCommandIndex + 1;
        var newCommand = new ScpiCommand
        {
            Command = "",
            Expect = "none"
        };
        Commands.Insert(indexToInsertNewCommand, newCommand);
        SelectedCommandIndex = indexToInsertNewCommand;
    }
    
    [RelayCommand]
    private void RemoveCommand()
    {
        if (SelectedCommandIndex >= 0 && SelectedCommandIndex < Commands.Count)
        {
            Commands.RemoveAt(SelectedCommandIndex);
        }
    }

    [RelayCommand]
    private void AddVariable()
    {
        var indexToInsertNewVariable = SelectedVariableIndex < 0 ? 0 : SelectedVariableIndex + 1;
        var newVariable = new ScpiVariable
        {
            Name = "",
            DefaultValue = "0"
        };
        Variables.Insert(indexToInsertNewVariable, newVariable);
        SelectedVariableIndex = indexToInsertNewVariable;
    }
    
    [RelayCommand]
    private void RemoveVariable()
    {
        if (SelectedVariableIndex >= 0 && SelectedVariableIndex < Variables.Count)
        {
            Variables.RemoveAt(SelectedVariableIndex);
        }
    }

}
