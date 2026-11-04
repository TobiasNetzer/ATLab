namespace ATLab.Enums;

/// <summary>
/// What the boundary scan interconnect test does with a net. Every net starts as SAFE: its pins keep their BSDL safe
/// values and nothing is driven, so only nets the user set up are tested. New values are appended.
/// </summary>
public enum InterconnectNetRole
{
    SAFE,
    DRIVE,
    EXPECT_HIGH,
    EXPECT_LOW
}
