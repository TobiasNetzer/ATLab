using System.Text.Json.Serialization;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// One pin of a boundary scan test step. The pin is referenced by device id and port, which stay valid when
/// the device is renamed, the package changes or the user label is edited.
/// </summary>
public partial class BoundaryScanPinAction : ObservableObject
{
    [ObservableProperty]
    private string _deviceId = string.Empty;

    [ObservableProperty]
    private string _port = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDrive), nameof(IsExpect))]
    private BoundaryScanPinActionType _action;

    [JsonIgnore]
    public bool IsDrive => IsDriveAction(Action);
    [JsonIgnore]
    public bool IsExpect => !IsDrive;

    public static bool IsDriveAction(BoundaryScanPinActionType action) =>
        action is BoundaryScanPinActionType.DRIVE_LOW or BoundaryScanPinActionType.DRIVE_HIGH or BoundaryScanPinActionType.DRIVE_Z;

    public BoundaryScanPinAction()
    {
    }

    public BoundaryScanPinAction(BoundaryScanPinAction other)
    {
        DeviceId = other.DeviceId;
        Port = other.Port;
        Action = other.Action;
    }
}