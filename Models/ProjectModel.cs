using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ATLab.Helpers;
using ATLab.Services;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ProjectModel : ObservableObject
{
    private readonly IHardwareAccessor _hardwareAccessor;
    private IHardwareInfo HardwareInfo => _hardwareAccessor.Hardware.HardwareInfo;
    
    [ObservableProperty]
    private bool _isDirty;
    
    [ObservableProperty]
    private string? _filePath;
    
    [ObservableProperty]
    private string? _scriptFolderPath;

    public string ProjectName =>
        string.IsNullOrEmpty(FilePath)
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(FilePath);
    
    public ProjectSettings Settings { get; }
    public ProjectDocumentation Documentation { get; } = new();
    public DeviceUnderTestInfo DeviceUnderTestInfo { get; } = new();
    public BoundaryScanConfiguration BoundaryScan { get; } = new();
    public ObservableCollection<TestModule> Modules { get; } = new();
    public ObservableCollection<Device> Devices { get; } = new();
    public ObservableCollection<CustomVariable> RuntimeVariables { get; } = new();
    public ObservableCollection<CustomRelayChannelName> StimChannelNames { get; } = new();
    public ObservableCollection<CustomRelayChannelName> ExtStimChannelNames { get; } = new();
    public ObservableCollection<CustomRelayChannelName> MeasChannelNames { get; } = new();
    
    public Action? RuntimeVariableChanged;
    
    private int _suppressChangesCount;
    
    public ProjectModel(IHardwareAccessor hardwareAccessor, ProjectSettings settings)
    {
        _hardwareAccessor = hardwareAccessor;
        Settings = settings;

        InitializeRelayChannels();
        
        Modules.CollectionChanged += ModulesChanged;
        Settings.SettingsChanged += MarkDirty;
        Documentation.DocumentationChanged += MarkDirty;
        DeviceUnderTestInfo.DeviceUnderTestInfoChanged += MarkDirty;
        BoundaryScan.ConfigurationChanged += MarkDirty;

        Devices.CollectionChanged += DevicesChanged;
        RuntimeVariables.CollectionChanged += RuntimeVariablesChanged;

        StimChannelNames.CollectionChanged += RelayChannelNamesChanged;
        ExtStimChannelNames.CollectionChanged += RelayChannelNamesChanged;
        MeasChannelNames.CollectionChanged += RelayChannelNamesChanged;

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ScriptFolderPath))
                MarkDirty();
        };

        foreach (var device in Devices)
            SubscribeToDevice(device);

        foreach (var variable in RuntimeVariables)
            SubscribeToVariable(variable);

        foreach (var channel in StimChannelNames)
            SubscribeToRelayChannel(channel);

        foreach (var channel in ExtStimChannelNames)
            SubscribeToRelayChannel(channel);

        foreach (var channel in MeasChannelNames)
            SubscribeToRelayChannel(channel);
    }
    
    public void Reset()
    {
        using (SuppressDirtyTracking())
        {
            Settings.ResetToDefault();
            Documentation.ResetToDefault();
            DeviceUnderTestInfo.ResetToDefault();
            BoundaryScan.ResetToDefault();
            
            Modules.Clear();
            
            Devices.Clear();
            
            RuntimeVariables.Clear();
            
            foreach (var channel in StimChannelNames)
                channel.ChannelName = string.Empty;

            foreach (var channel in ExtStimChannelNames)
                channel.ChannelName = string.Empty;

            foreach (var channel in MeasChannelNames)
                channel.ChannelName = string.Empty;

            FilePath = null;
            ScriptFolderPath = null;
        }

        IsDirty = false;
    }
    
    public AtlabFileDto ToDto()
    {
        var pathService = string.IsNullOrWhiteSpace(FilePath) 
            ? null 
            : new PathService(FilePath);

        var modulesClone = Modules.Select(module => module.Clone(true)).ToList();
        var documentationClone = Documentation.Clone();
        var settingsClone = Settings.Clone();
        var boundaryScanClone = BoundaryScan.Clone();

        foreach (var step in modulesClone.SelectMany(m => m.TestSteps))
            step.UpdateDtos(pathService);

        if (pathService != null)
        {
            documentationClone.PrepareForSave(pathService);
            settingsClone.PrepareForSave(pathService);
            boundaryScanClone.PrepareForSave(pathService);
        }

        return new AtlabFileDto
        {
            FormatVersion = AtlabFileDto.CurrentFormatVersion,
            ScriptFolderPath = pathService?.ToRelative(ScriptFolderPath ?? string.Empty) ?? ScriptFolderPath,
            Modules = modulesClone,
            StimChannelNames = StimChannelNames.ToList(),
            ExtStimChannelNames = ExtStimChannelNames.ToList(),
            MeasChannelNames = MeasChannelNames.ToList(),
            RuntimeVariables = RuntimeVariables.ToList(),
            Devices = Devices.ToList(),
            ProjectSettings = settingsClone,
            ProjectDocumentation = documentationClone,
            DeviceUnderTestInfo = DeviceUnderTestInfo,
            BoundaryScan = boundaryScanClone
        };
    }
    
    public void CreateEmptyProject()
    {
        using (SuppressDirtyTracking())
        {
            Reset();

            var module = new TestModule
            {
                Name = TestModule.CreateDefaultName([], LocalizationService.Instance["Text_DefaultModuleName"])
            };

            var step = new TestStep();
            step.InitializeRuntimeState(HardwareInfo);
            module.TestSteps.Add(step);

            Modules.Add(module);

            MarkSaved(null);
        }
    }
    
    public void Load(AtlabFileDto dto)
    {
        var pathService = !string.IsNullOrWhiteSpace(FilePath)
            ? new PathService(FilePath)
            : null;

        Modules.Clear();
        foreach (var module in dto.Modules)
        {
            foreach (var step in module.TestSteps)
            {
                if (pathService != null)
                    step.RestoreAfterLoad(pathService);

                TestStepRuntimeInitializer.InitializeRuntimeValues(step);
                step.InitializeRuntimeState(HardwareInfo);
            }

            module.Renumber();
            Modules.Add(module);
        }

        if (Modules.Count == 0)
        {
            Modules.Add(new TestModule
            {
                Name = TestModule.CreateDefaultName([], LocalizationService.Instance["Text_DefaultModuleName"])
            });
        }
        
        Devices.Clear();
        foreach (var device in dto.Devices)
            Devices.Add(device);
        
        RuntimeVariables.Clear();
        foreach (var variable in dto.RuntimeVariables)
            RuntimeVariables.Add(variable);
        
        ApplyRelayChannelNames(StimChannelNames, dto.StimChannelNames);
        ApplyRelayChannelNames(ExtStimChannelNames, dto.ExtStimChannelNames);
        ApplyRelayChannelNames(MeasChannelNames, dto.MeasChannelNames);
        
        ScriptFolderPath = pathService != null && !string.IsNullOrWhiteSpace(dto.ScriptFolderPath)
            ? pathService.ToAbsolute(dto.ScriptFolderPath)
            : dto.ScriptFolderPath;

        if (pathService != null)
        {
            dto.ProjectSettings.RestoreAfterLoad(pathService);
            dto.ProjectDocumentation.RestoreAfterLoad(pathService);
            dto.BoundaryScan.RestoreAfterLoad(pathService);
        }

        Settings.CopyFrom(dto.ProjectSettings);
        Documentation.CopyFrom(dto.ProjectDocumentation);
        DeviceUnderTestInfo.CopyFrom(dto.DeviceUnderTestInfo);
        BoundaryScan.CopyFrom(dto.BoundaryScan);

        IsDirty = false;
    }
    
    public IDisposable SuppressDirtyTracking()
    {
        _suppressChangesCount++;
        return new ActionOnDispose(() => _suppressChangesCount--);
    }

    public void MarkDirty()
    {
        if (_suppressChangesCount > 0)
            return;

        IsDirty = true;
    }
    
    public void MarkSaved()
    {
        IsDirty = false;
    }
    
    public void MarkSaved(string? path)
    {
        FilePath = path;
        IsDirty = false;
    }
    
    private void ModulesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // A Move lists the item in both NewItems and OldItems, handling it would end up unsubscribing the item
        if (e.Action == NotifyCollectionChangedAction.Move)
        {
            MarkDirty();
            return;
        }

        if (e.NewItems != null)
        {
            foreach (TestModule module in e.NewItems)
                SubscribeToModule(module);
        }

        if (e.OldItems != null)
        {
            foreach (TestModule module in e.OldItems)
                UnsubscribeFromModule(module);
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var module in _subscribedModules.Where(m => !Modules.Contains(m)).ToList())
                UnsubscribeFromModule(module);
        }

        MarkDirty();
    }

    // Collection.Clear() raises Reset without OldItems, so subscriptions are tracked to unhook them reliably
    private readonly HashSet<TestModule> _subscribedModules = new();

    private void SubscribeToModule(TestModule module)
    {
        if (!_subscribedModules.Add(module))
            return;

        module.PropertyChanged += ModuleChanged;
        module.TestSteps.CollectionChanged += TestStepsChanged;

        foreach (var step in module.TestSteps)
            SubscribeToTestStep(step);
    }

    private void UnsubscribeFromModule(TestModule module)
    {
        if (!_subscribedModules.Remove(module))
            return;

        module.PropertyChanged -= ModuleChanged;
        module.TestSteps.CollectionChanged -= TestStepsChanged;

        foreach (var step in module.TestSteps)
            UnsubscribeFromTestStep(step);
    }

    private void ModuleChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
    }

    private void TestStepsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // A Move lists the item in both NewItems and OldItems, handling it would end up unsubscribing the item
        if (e.Action == NotifyCollectionChangedAction.Move)
        {
            MarkDirty();
            return;
        }

        if (e.NewItems != null)
        {
            foreach (TestStep step in e.NewItems)
                SubscribeToTestStep(step);
        }

        if (e.OldItems != null)
        {
            foreach (TestStep step in e.OldItems)
                UnsubscribeFromTestStep(step);
        }

        MarkDirty();
    }
    
    private void SubscribeToTestStep(TestStep step)
    {
        step.PropertyChanged += TestStepChanged;
    }

    private void UnsubscribeFromTestStep(TestStep step)
    {
        step.PropertyChanged -= TestStepChanged;
    }

    private void TestStepChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
    }
    
    private void RuntimeVariablesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (CustomVariable variable in e.NewItems)
                SubscribeToVariable(variable);

        if (e.OldItems != null)
            foreach (CustomVariable variable in e.OldItems)
                UnsubscribeFromVariable(variable);
        
        RuntimeVariableChanged?.Invoke();
        MarkDirty();
    }
    
    private void SubscribeToVariable(CustomVariable variable)
    {
        variable.PropertyChanged += VariableChanged;
    }

    private void UnsubscribeFromVariable(CustomVariable variable)
    {
        variable.PropertyChanged -= VariableChanged;
    }

    private void VariableChanged(object? sender, PropertyChangedEventArgs e)
    {
        RuntimeVariableChanged?.Invoke();
        MarkDirty();
    }
    
    private void DevicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (Device device in e.NewItems)
                SubscribeToDevice(device);
        }

        if (e.OldItems != null)
        {
            foreach (Device device in e.OldItems)
                UnsubscribeFromDevice(device);
        }

        MarkDirty();
    }

    private void SubscribeToDevice(Device device)
    {
        device.PropertyChanged += DeviceChanged;
        device.Configuration.PropertyChanged += DeviceConfigurationChanged;
    }

    private void UnsubscribeFromDevice(Device device)
    {
        device.PropertyChanged -= DeviceChanged;
        device.Configuration.PropertyChanged -= DeviceConfigurationChanged;
    }

    private void DeviceChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
    }

    private void DeviceConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
    }
    
    private void InitializeRelayChannels()
    {
        for (var i = 1; i <= HardwareInfo.StimChannelCount; i++)
            StimChannelNames.Add(new CustomRelayChannelName(string.Empty, i));

        for (var i = 1; i <= HardwareInfo.ExtStimChannelCount; i++)
            ExtStimChannelNames.Add(new CustomRelayChannelName(string.Empty, i));

        for (var i = 1; i <= HardwareInfo.MeasChannelCount; i++)
            MeasChannelNames.Add(new CustomRelayChannelName(string.Empty, i));
    }
    
    private void RelayChannelNamesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CustomRelayChannelName channel in e.NewItems)
                SubscribeToRelayChannel(channel);
        }

        if (e.OldItems != null)
        {
            foreach (CustomRelayChannelName channel in e.OldItems)
                UnsubscribeFromRelayChannel(channel);
        }

        MarkDirty();
    }

    private void SubscribeToRelayChannel(CustomRelayChannelName channel)
    {
        channel.PropertyChanged += RelayChannelChanged;
    }

    private void UnsubscribeFromRelayChannel(CustomRelayChannelName channel)
    {
        channel.PropertyChanged -= RelayChannelChanged;
    }

    private void RelayChannelChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
    }
    
    private static void ApplyRelayChannelNames(
        ObservableCollection<CustomRelayChannelName> target,
        IReadOnlyList<CustomRelayChannelName> source)
    {
        for (var i = 0; i < Math.Min(target.Count, source.Count); i++)
        {
            target[i].ChannelName = source[i].ChannelName;
        }
    }
    
    partial void OnFilePathChanged(string? value)
    {
        OnPropertyChanged(nameof(ProjectName));
    }
}