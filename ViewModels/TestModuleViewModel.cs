using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestModuleViewModel : ViewModelBase
{
    private readonly IHardwareInfo _hardwareInfo;

    public TestModule Module { get; }

    public ObservableCollection<TestStepViewModel> TestSteps { get; } = new();

    // Forwards PropertyChanged of the contained step view models (sender is the step view model)
    public event PropertyChangedEventHandler? StepPropertyChanged;

    [ObservableProperty]
    private bool _isExecuted;

    [ObservableProperty]
    private bool _isPassed;

    [ObservableProperty]
    private bool _isRunning;

    public TestModuleViewModel(TestModule module, IHardwareInfo hardwareInfo)
    {
        Module = module;
        _hardwareInfo = hardwareInfo;

        foreach (var step in module.TestSteps)
            TestSteps.Add(CreateViewModel(step));

        module.TestSteps.CollectionChanged += ModuleTestStepsChanged;
    }

    public void Detach()
    {
        Module.TestSteps.CollectionChanged -= ModuleTestStepsChanged;

        foreach (var vm in TestSteps)
            vm.PropertyChanged -= OnStepPropertyChanged;
    }

    public TestStepViewModel? FindViewModel(TestStep model)
    {
        return TestSteps.FirstOrDefault(x => ReferenceEquals(x.TestStep, model));
    }

    public void ResetResults()
    {
        foreach (var step in TestSteps)
            step.ResetResults();

        IsRunning = false;
        UpdateResult();
    }

    private TestStepViewModel CreateViewModel(TestStep step)
    {
        var vm = new TestStepViewModel(step, Module, _hardwareInfo);
        vm.PropertyChanged += OnStepPropertyChanged;
        return vm;
    }

    private void ModuleTestStepsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                var index = e.NewStartingIndex;

                foreach (TestStep step in e.NewItems!)
                    TestSteps.Insert(index++, CreateViewModel(step));

                break;
            }
            case NotifyCollectionChangedAction.Remove:
            {
                foreach (TestStep step in e.OldItems!)
                {
                    var vm = TestSteps.First(x => ReferenceEquals(x.TestStep, step));
                    vm.PropertyChanged -= OnStepPropertyChanged;
                    TestSteps.Remove(vm);
                }

                break;
            }
            case NotifyCollectionChangedAction.Move:
            {
                // The DataGrid does not refresh its rows on a Move notification, so remove and re-insert instead
                var item = TestSteps[e.OldStartingIndex];
                TestSteps.RemoveAt(e.OldStartingIndex);
                TestSteps.Insert(e.NewStartingIndex, item);
                break;
            }
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
            {
                foreach (var vm in TestSteps)
                    vm.PropertyChanged -= OnStepPropertyChanged;

                TestSteps.Clear();

                foreach (var step in Module.TestSteps)
                    TestSteps.Add(CreateViewModel(step));

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        UpdateResult();
    }

    private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TestStepViewModel.IsExecuted) or nameof(TestStepViewModel.IsPassed))
            UpdateResult();

        StepPropertyChanged?.Invoke(sender, e);
    }

    private void UpdateResult()
    {
        var executed = TestSteps
            .Where(s => s.IsExecuted && !s.TestStep.IsIgnoreStep)
            .ToList();

        IsExecuted = executed.Count > 0;
        IsPassed = executed.All(s => s.IsPassed);
    }
}