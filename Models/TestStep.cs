namespace ATLab.Models;

public partial class TestStep
{
    public int Number { get; set; }
    public string? Name { get; set; }
    public double Value { get; set; }
    public double LowerLimit { get; set; }
    public double UpperLimit { get; set; }
    public string? Result { get; set; }
    public string? Comment { get; set; }

    public RelayGroup? StimState { get; set; }
    public RelayGroup? ExtStimState { get; set; }
    public RelayMatrix? MatrixState { get; set; }
}