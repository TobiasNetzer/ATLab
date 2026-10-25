using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

/// <summary>Exports the results of a finished run automatically according to the project settings.</summary>
public interface ITestResultExportService
{
    Task SaveAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, int failedSteps);
}