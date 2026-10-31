namespace ATLab.Enums;

/// <summary>
/// Boundary scan instructions used by ATLab: bypass the chip, sample its pins, or drive them (EXTEST).
/// </summary>
public enum BoundaryScanInstruction
{
    BYPASS,
    SAMPLE,
    EXTEST
}