namespace ATLab.Enums;

/// <summary>What a Boundary Scan test step does with a pin.</summary>
public enum BoundaryScanPinActionType
{
    DRIVE_LOW,
    DRIVE_HIGH,
    DRIVE_Z,
    EXPECT_LOW,
    EXPECT_HIGH
}