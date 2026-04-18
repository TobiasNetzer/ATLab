using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface IPdfExportService
{
    Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps);
        
    Task ExportToPathAsync(IEnumerable<TestStepViewModel> steps, string path);
}