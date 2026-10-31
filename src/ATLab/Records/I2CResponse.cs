namespace ATLab.Records;

/// <summary>Result of an I²C transfer: whether the target acknowledged, and the data read.</summary>
public record I2CResponse(bool Success, byte[]? Data = null);