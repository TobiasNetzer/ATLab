using System.Collections.Generic;

namespace ATLab.Models;

public class TestHardwareDiagnostics
{
    public List<string> DefectiveRelaysMatrixH { get; set; } = new ();
    public List<string> DefectiveRelaysMatrixL { get; set; } = new ();
}