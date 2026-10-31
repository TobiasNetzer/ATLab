using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces
{
    /// <summary>Exports test step results as tab-separated CSV file.</summary>
    public interface ICsvExportService
    {
        Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps);
        
        Task ExportToPathAsync(IEnumerable<TestStepViewModel> steps, string path);
    }
}