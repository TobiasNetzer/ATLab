namespace ATLab.Records;

public record I2CResponse(bool Success, byte[]? Data = null);