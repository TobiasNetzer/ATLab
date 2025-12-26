using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ScpiScriptsManagerViewModel : ViewModelBase
{
    private readonly IScpiScriptRepository _repository;
    private readonly IMessageBoxService _messageBoxService;

    public ObservableCollection<ScpiScriptItemViewModel> Scripts { get; } = new();

    [ObservableProperty]
    private ScpiScriptItemViewModel? _selectedScript;

    public ScpiScriptsManagerViewModel(
        IScpiScriptRepository repository,
        IMessageBoxService messageBoxService)
    {
        _repository = repository;
        _messageBoxService = messageBoxService;
        
        Title = "Script Manager";
    }

    private bool CanExecute() => SelectedScript != null;

    partial void OnSelectedScriptChanged(ScpiScriptItemViewModel? value)
    {
        DeleteScriptCommand.NotifyCanExecuteChanged();
        SaveScriptCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task ReloadScripts()
    {
        Scripts.Clear();
        var scripts = await _repository.LoadAllAsync();
        foreach (var model in scripts)
        {
            var vm = new ScpiScriptItemViewModel(_repository, model);
            vm.LoadFromModel();
            Scripts.Add(vm);
        }

        SelectedScript = Scripts.FirstOrDefault();
    }

    [RelayCommand]
    private async Task NewScript()
    {
        var model = new ScpiScript
        {
            Name = "New Script",
            Description = "Describe this script...",
        };

        var vm = new ScpiScriptItemViewModel(_repository, model);
        vm.LoadFromModel();
        Scripts.Add(vm);
        SelectedScript = vm;
        await vm.SaveAsync();
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task DeleteScript()
    {
        if (SelectedScript is null) return;
        
        var confirm = await _messageBoxService.ShowConfirmationAsync("Delete Script", "The selected script will be permanently deleted.");
        if (!confirm) return;

        var id = SelectedScript.Id;
        Scripts.Remove(SelectedScript);
        SelectedScript = Scripts.FirstOrDefault();
        await _repository.DeleteAsync(id);
    }
    
    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task SaveScript()
    {
        if (SelectedScript is null) return;
        await SelectedScript.SaveAsync();
    }
}
