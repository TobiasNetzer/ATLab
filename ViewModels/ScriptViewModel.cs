using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ScriptViewModel : ViewModelBase
{
    private readonly IScriptRepository _repository;
    private readonly CustomScript _model;

    [ObservableProperty]
    private bool _isDirty;

    public ObservableCollection<ScriptCommandViewModel> Commands { get; } = new();
    public ObservableCollection<ScriptVariable> Variables { get; } = new();

    [ObservableProperty]
    private int _selectedCommandIndex;

    [ObservableProperty]
    private int _selectedVariableIndex;
    
    public string Id => _model.Id;

    public string Name
    {
        get => _model.Name;
        set
        {
            if (_model.Name != value)
            {
                _model.Name = value;
                IsDirty = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
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
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    public string DisplayName => IsDirty ? $"{Name}*" : Name;

    public ScriptViewModel(IScriptRepository repository, CustomScript model)
    {
        _repository = repository;
        _model = model;
        
        Commands.CollectionChanged += Commands_CollectionChanged;
        Variables.CollectionChanged += Variables_CollectionChanged;

        LoadFromModel();
    }
    
    public void ClearEvaluateExcept(ScriptCommandViewModel selected)
    {
        foreach (var cmd in Commands)
        {
            if (cmd != selected && cmd.Evaluate)
                cmd.Evaluate = false;
        }
    }

    partial void OnIsDirtyChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayName));
    }
    
    public void LoadFromModel()
    {
        foreach (var c in Commands)
            c.PropertyChanged -= ChildChanged;

        Commands.Clear();
        foreach (var command in _model.Commands)
        {
            var vm = new ScriptCommandViewModel(command, this);
            vm.PropertyChanged += ChildChanged;
            Commands.Add(vm);
        }
        
        foreach (var v in Variables)
            v.PropertyChanged -= ChildChanged;

        Variables.Clear();
        foreach (var v in _model.Variables)
        {
            v.PropertyChanged += ChildChanged;
            Variables.Add(v);
        }

        IsDirty = false;
    }

    private void ApplyToModel()
    {
        _model.Commands = Commands.Select(c => c.GetModel()).ToList();
        _model.Variables = Variables.ToList();
    }

    public async Task SaveAsync()
    {
        ApplyToModel();
        await _repository.SaveAsync(_model);
        IsDirty = false;
    }

    private void ChildChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsDirty = true;
    }

    private void Commands_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ScriptCommandViewModel old in e.OldItems)
                old.PropertyChanged -= ChildChanged;
        }

        if (e.NewItems != null)
        {
            foreach (ScriptCommandViewModel added in e.NewItems)
                added.PropertyChanged += ChildChanged;
        }

        IsDirty = true;
    }

    private void Variables_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ScriptVariable old in e.OldItems)
                old.PropertyChanged -= ChildChanged;
        }

        if (e.NewItems != null)
        {
            foreach (ScriptVariable added in e.NewItems)
                added.PropertyChanged += ChildChanged;
        }

        IsDirty = true;
    }

    [RelayCommand]
    private void AddCommand()
    {
        var index = SelectedCommandIndex < 0 ? 0 : SelectedCommandIndex + 1;
        if (index > Commands.Count) index = Commands.Count;

        var newCommand = new ScriptCommand();

        var vm = new ScriptCommandViewModel(newCommand, this);
        vm.PropertyChanged += ChildChanged;

        Commands.Insert(index, vm);
        SelectedCommandIndex = index;
        IsDirty = true;
    }

    [RelayCommand]
    private void RemoveCommand()
    {
        if (SelectedCommandIndex < 0 || SelectedCommandIndex >= Commands.Count)
            return;
        
        var vm = Commands[SelectedCommandIndex];
        vm.PropertyChanged -= ChildChanged;

        Commands.RemoveAt(SelectedCommandIndex);
        IsDirty = true;
    }

    [RelayCommand]
    private void AddVariable()
    {
        var index = SelectedVariableIndex <= 0 ? 0 : SelectedVariableIndex + 1;
        if (index > Variables.Count) index = Variables.Count;

        var newVar = new ScriptVariable();
        newVar.PropertyChanged += ChildChanged;

        Variables.Insert(index, newVar);
        SelectedVariableIndex = index;
        IsDirty = true;
    }

    [RelayCommand]
    private void RemoveVariable()
    {
        if (SelectedVariableIndex < 0 || SelectedVariableIndex >= Variables.Count)
            return;
        
        var v = Variables[SelectedVariableIndex];
        v.PropertyChanged -= ChildChanged;

        Variables.RemoveAt(SelectedVariableIndex);
        IsDirty = true;
    }
}