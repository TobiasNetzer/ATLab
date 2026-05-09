using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestResultExportService
{
    Task SaveAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, int failedSteps);
}