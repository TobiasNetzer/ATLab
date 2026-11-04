using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// Role of a board net in the boundary scan interconnect test, referenced by its KiCad name. Only nets that are not
/// SAFE or that are connected to another net are stored. A drive net is driven by every boundary scan pin on it in turn.
/// </summary>
public partial class BoundaryScanInterconnectNet : ObservableObject
{
    [ObservableProperty]
    private string _net = string.Empty;

    [ObservableProperty]
    private InterconnectNetRole _role;

    /// <summary>
    /// Another net this one is connected to on the board, e.g. through a series resistor; empty for none. Connected
    /// nets are tested as one net and share their role.
    /// </summary>
    [ObservableProperty]
    private string _connectedTo = string.Empty;

    public BoundaryScanInterconnectNet Clone() => new()
    {
        Net = Net,
        Role = Role,
        ConnectedTo = ConnectedTo
    };
}
