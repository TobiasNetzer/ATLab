using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces
{
    public interface ICsvExportService
    {
        Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps);
        
        Task ExportToPathAsync(IEnumerable<TestStepViewModel> steps, string path);
    }
}