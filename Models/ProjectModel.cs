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
    private readonly IHardwareInfo _hardwareInfo;
    
    [ObservableProperty]
    private bool _isDirty;
    
    [ObservableProperty]
    private string? _filePath;

    public string ProjectName =>
        string.IsNullOrEmpty(FilePath)
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(FilePath);
    
    public ProjectSettings Settings { get; } = new();
    public ProjectDocumentation Documentation { get; } = new();
    public DeviceUnderTestInfo DeviceUnderTestInfo { get; } = new();
    public ObservableCollection<TestStep> TestSteps { get; } = new();
    public ObservableCollection<Device> Devices { get; } = new();
    public ObservableCollection<CustomVariable> RuntimeVariables { get; } = new();
    public ObservableCollection<CustomRelayChannelName> StimChannelNames { get; } = new();
    public ObservableCollection<CustomRelayChannelName> ExtStimChannelNames { get; } = new();
    public ObservableCollection<CustomRelayChannelName> MeasChannelNames { get; } = new();
    
    private int _suppressChangesCount;
    
    public ProjectModel(IHardwareInfo hardwareInfo)
    {
        _hardwareInfo = hardwareInfo;

        InitializeRelayChannels();
        
        TestSteps.CollectionChanged += TestStepsChanged;
        Settings.SettingsChanged += MarkDirty;
        Documentation.DocumentationChanged += MarkDirty;
        DeviceUnderTestInfo.DeviceUnderTestInfoChanged += MarkDirty;

        Devices.CollectionChanged += DevicesChanged;
        RuntimeVariables.CollectionChanged += RuntimeVariablesChanged;

        StimChannelNames.CollectionChanged += RelayChannelNamesChanged;
        ExtStimChannelNames.CollectionChanged += RelayChannelNamesChanged;
        MeasChannelNames.CollectionChanged += RelayChannelNamesChanged;

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
            
            TestSteps.Clear();
            
            Devices.Clear();
            
            RuntimeVariables.Clear();
            
            foreach (var channel in StimChannelNames)
                channel.ChannelName = string.Empty;

            foreach (var channel in ExtStimChannelNames)
                channel.ChannelName = string.Empty;

            foreach (var channel in MeasChannelNames)
                channel.ChannelName = string.Empty;

            FilePath = null;
        }

        IsDirty = false;
    }
    
    public AtlabFileDto ToDto()
    {
        foreach (var step in TestSteps)
            step.UpdateDtos();

        return new AtlabFileDto
        {
            TestSteps = TestSteps.ToList(),
            StimChannelNames = StimChannelNames.ToList(),
            ExtStimChannelNames = ExtStimChannelNames.ToList(),
            MeasChannelNames = MeasChannelNames.ToList(),
            RuntimeVariables = RuntimeVariables.ToList(),
            Devices = Devices.ToList(),
            ProjectSettings = Settings,
            ProjectDocumentation = Documentation,
            DeviceUnderTestInfo = DeviceUnderTestInfo
        };
    }
    
    public void CreateEmptyProject()
    {
        using (SuppressDirtyTracking())
        {
            Reset();

            TestSteps.Add(new TestStep());

            MarkSaved(null);
        }
    }
    
    public void Load(AtlabFileDto dto)
    {
        TestSteps.Clear();
        foreach (var step in dto.TestSteps)
        {
            TestStepRuntimeInitializer.InitializeRuntimeValues(step);
            TestSteps.Add(step);
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
        
        Settings.CopyFrom(dto.ProjectSettings);
        Documentation.CopyFrom(dto.ProjectDocumentation);
        DeviceUnderTestInfo.CopyFrom(dto.DeviceUnderTestInfo);

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
    
    private void TestStepsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
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
        for (int i = 1; i <= _hardwareInfo.StimChannelCount; i++)
            StimChannelNames.Add(new CustomRelayChannelName(string.Empty, i));

        for (int i = 1; i <= _hardwareInfo.ExtStimChannelCount; i++)
            ExtStimChannelNames.Add(new CustomRelayChannelName(string.Empty, i));

        for (int i = 1; i <= _hardwareInfo.MeasChannelCount; i++)
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
        for (int i = 0; i < Math.Min(target.Count, source.Count); i++)
        {
            target[i].ChannelName = source[i].ChannelName;
        }
    }
    
    partial void OnFilePathChanged(string? value)
    {
        OnPropertyChanged(nameof(ProjectName));
    }
}