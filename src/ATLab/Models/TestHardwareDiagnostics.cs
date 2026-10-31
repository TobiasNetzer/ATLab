using System.Collections.Generic;

namespace ATLab.Models;

/// <summary>Result of the relay matrix self-test: the defective relays per matrix side.</summary>
public class TestHardwareDiagnostics
{
    public List<string> DefectiveRelaysMatrixH { get; set; } = new ();
    public List<string> DefectiveRelaysMatrixL { get; set; } = new ();
}