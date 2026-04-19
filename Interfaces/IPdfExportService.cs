using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface IPdfExportService
{
    Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo);
        
    Task ExportToPathAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, string path);
}