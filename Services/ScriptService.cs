using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class ScriptService : IScriptService
{
    private readonly IScriptRepository _repository;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ObservableCollection<ScriptViewModel> Scripts { get; } = new();

    public ScriptService(IScriptRepository repository)
    {
        _repository = repository;
    }

    public async Task LoadAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var models = await _repository.LoadAllAsync();
            
            // Sync logic
            Scripts.Clear();
            foreach (var model in models)
            {
                var vm = new ScriptViewModel(_repository, model);
                vm.LoadFromModel();
                Scripts.Add(vm);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync(ScriptViewModel script)
    {
        await script.SaveAsync();
        // If it's a new script not yet in the collection, add it
        if (!Scripts.Contains(script))
        {
            Scripts.Add(script);
        }
    }

    public async Task DeleteAsync(ScriptViewModel script)
    {
        await _repository.DeleteAsync(script.Id);
        Scripts.Remove(script);
    }

    public ScriptViewModel CreateNew()
    {
        var model = new CustomScript
        {
            Name = "New Script",
            Description = "",
        };
        var vm = new ScriptViewModel(_repository, model);
        vm.LoadFromModel();
        return vm;
    }
}