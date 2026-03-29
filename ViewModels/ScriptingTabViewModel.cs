using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ScriptingTabViewModel : ViewModelBase
{
    private readonly IScriptService _scriptService;
    private readonly IScriptRepository _repository;
    private readonly IMessageBoxService _messageBoxService;

    public ObservableCollection<ScriptItemViewModel> Scripts => _scriptService.Scripts;

    [ObservableProperty]
    private ScriptItemViewModel? _selectedScript;

    public ScriptingTabViewModel(
        IScriptService scriptService,
        IScriptRepository repository,
        IMessageBoxService messageBoxService)
    {
        _scriptService = scriptService;
        _repository = repository;
        _messageBoxService = messageBoxService;

        Title = "Scripting";
    }

    private bool CanExecute() => SelectedScript != null;

    partial void OnSelectedScriptChanged(ScriptItemViewModel? value)
    {
        DeleteScriptCommand.NotifyCanExecuteChanged();
        SaveScriptCommand.NotifyCanExecuteChanged();
    }

    private void Script_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScriptItemViewModel.IsDirty))
            UpdateTitle();
    }

    private void UpdateTitle()
    {
        Title = Scripts.Any(s => s.IsDirty) ? "Scripting*" : "Scripting";
    }

    [RelayCommand]
    private async Task ReloadScripts()
    {
        foreach (var script in Scripts)
            script.PropertyChanged -= Script_PropertyChanged;

        await _scriptService.LoadAllAsync();

        foreach (var script in Scripts)
            script.PropertyChanged += Script_PropertyChanged;
        
        UpdateTitle();

        SelectedScript ??= Scripts.FirstOrDefault();
    }

    [RelayCommand]
    private async Task NewScript()
    {
        var vm = _scriptService.CreateNew();
        Scripts.Add(vm);

        vm.PropertyChanged += Script_PropertyChanged;
        SelectedScript = vm;

        await _scriptService.SaveAsync(vm);
        UpdateTitle();
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task DeleteScript()
    {
        if (SelectedScript is null) return;

        var confirm = await _messageBoxService.ShowConfirmationAsync(
            "Delete Script",
            "The selected script will be permanently deleted.");

        if (!confirm) return;

        var vmToRemove = SelectedScript;
        SelectedScript = Scripts.FirstOrDefault(s => s != vmToRemove);

        vmToRemove.PropertyChanged -= Script_PropertyChanged;

        await _scriptService.DeleteAsync(vmToRemove);
        UpdateTitle();
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task SaveScript()
    {
        if (SelectedScript is null) return;

        await _scriptService.SaveAsync(SelectedScript);
        UpdateTitle();
    }

    [RelayCommand]
    private async Task ChangeRepository()
    {
        await _repository.ConfigureRepositoryFolderAsync();
        await ReloadScripts();
    }
}