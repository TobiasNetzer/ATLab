namespace ATLab.Records;

/// <summary>Name and identification string of a device for the test equipment section of the report.</summary>
public record DeviceIdentification(
    string Name,
    string? Identification
);