using System.Text.Json.Serialization;

namespace ATLab.Models;

public class TestStep
{
    public int Number { get; set; }
    public string? Name { get; set; }
    public double NominalValue { get; set; }
    public double LowerLimit { get; set; }
    public double UpperLimit { get; set; }
    public int Delay { get; set; }
    public string? Comment { get; set; }
    
    [JsonIgnore]
    public string? Result { get; set; }

    public RelayGroupDto? StimState { get; set; }
    public RelayGroupDto? ExtStimState { get; set; }
    public RelayMatrix? MatrixState { get; set; }
}