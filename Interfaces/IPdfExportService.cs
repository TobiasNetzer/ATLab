using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

/// <summary>Creates the PDF test report.</summary>
public interface IPdfExportService
{
    Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo);
        
    Task ExportToPathAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, string path);
}