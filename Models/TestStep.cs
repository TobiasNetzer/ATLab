namespace ATLab.Models;

public partial class TestStep
{
    public int Number { get; set; }
    public string Name { get; set; }
    public double Value { get; set; }
    public double LowerLimit { get; set; }
    public double UpperLimit { get; set; }
    public bool Result { get; set; }

    public RelayGroup StimState = new (16);
    public RelayGroup ExtStimState = new (4);
    public RelayMatrix MatrixState = new (0, 0);
}