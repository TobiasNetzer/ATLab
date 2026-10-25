using System;
using System.Text.Json.Serialization;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>Instrument or other device from the Device Manager, referenced by test steps through its id.</summary>
public partial class Device : ObservableObject
{
    
    public Device()
    {
        Id = Guid.NewGuid().ToString("N");
    }
    
    [JsonConstructor]
    public Device(string id)
    {
        Id = id;
    }
    
    public string Id { get; }
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private bool _isIncludeInReport;
    
    [ObservableProperty]
    private string _identificationQuery = string.Empty;

    [ObservableProperty]
    private string _resourceString = string.Empty;

    [ObservableProperty]
    private DeviceType _type;
    
    [ObservableProperty]
    private DeviceConfiguration _configuration = new();
}