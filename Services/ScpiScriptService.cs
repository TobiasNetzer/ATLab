using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class ScpiScriptService : IScpiScriptService
{
    private readonly IScpiScriptRepository _repository;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ObservableCollection<ScpiScriptItemViewModel> Scripts { get; } = new();

    public ScpiScriptService(IScpiScriptRepository repository)
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
                var vm = new ScpiScriptItemViewModel(_repository, model);
                vm.LoadFromModel();
                Scripts.Add(vm);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync(ScpiScriptItemViewModel script)
    {
        await script.SaveAsync();
        // If it's a new script not yet in the collection, add it
        if (!Scripts.Contains(script))
        {
            Scripts.Add(script);
        }
    }

    public async Task DeleteAsync(ScpiScriptItemViewModel script)
    {
        await _repository.DeleteAsync(script.Id);
        Scripts.Remove(script);
    }

    public ScpiScriptItemViewModel CreateNew()
    {
        var model = new ScpiScript
        {
            Name = "New Script",
            Description = "",
        };
        var vm = new ScpiScriptItemViewModel(_repository, model);
        vm.LoadFromModel();
        return vm;
    }
}
