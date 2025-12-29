using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ScriptsManagerViewModel : ViewModelBase
{
    private readonly IScriptService _scriptService;
    private readonly IScriptRepository _repository;
    private readonly IMessageBoxService _messageBoxService;

    public ObservableCollection<ScriptItemViewModel> Scripts => _scriptService.Scripts;

    [ObservableProperty]
    private ScriptItemViewModel? _selectedScript;

    public ScriptsManagerViewModel(
        IScriptService scriptService,
        IScriptRepository repository,
        IMessageBoxService messageBoxService)
    {
        _scriptService = scriptService;
        _repository = repository;
        _messageBoxService = messageBoxService;
        
        Title = "Script Manager";
    }

    private bool CanExecute() => SelectedScript != null;

    partial void OnSelectedScriptChanged(ScriptItemViewModel? value)
    {
        DeleteScriptCommand.NotifyCanExecuteChanged();
        SaveScriptCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task ReloadScripts()
    {
        await _scriptService.LoadAllAsync();
        if (SelectedScript == null)
            SelectedScript = Scripts.FirstOrDefault();
    }

    [RelayCommand]
    private async Task NewScript()
    {
        var vm = _scriptService.CreateNew();
        SelectedScript = vm;
        await _scriptService.SaveAsync(vm);
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task DeleteScript()
    {
        if (SelectedScript is null) return;
        
        var confirm = await _messageBoxService.ShowConfirmationAsync("Delete Script", "The selected script will be permanently deleted.");
        if (!confirm) return;

        var vmToRemove = SelectedScript;
        SelectedScript = Scripts.FirstOrDefault(s => s != vmToRemove);
        await _scriptService.DeleteAsync(vmToRemove);
    }
    
    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task SaveScript()
    {
        if (SelectedScript is null) return;
        await _scriptService.SaveAsync(SelectedScript);
    }

    [RelayCommand]
    private async Task ChangeRepository()
    {
        await _repository.ConfigureRepositoryFolderAsync();
        await ReloadScripts();
    }
}
