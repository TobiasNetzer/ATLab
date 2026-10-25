namespace ATLab.Enums;

public enum BoundaryScanStepMode
{
    /// <summary>Drives and checks the listed pins.</summary>
    PIN_ACTIONS,

    /// <summary>Returns the chips to normal operation and releases the probe.</summary>
    TURN_OFF
}