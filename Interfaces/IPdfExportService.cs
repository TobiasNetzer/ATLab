using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.Records;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

/// <summary>Creates the PDF test report.</summary>
public interface IPdfExportService
{
    Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo);

    /// <param name="testEquipment">Already identified devices for the test equipment section.</param>
    Task ExportToPathAsync(
        IEnumerable<TestStepViewModel> steps,
        TestInfo testInfo,
        IReadOnlyList<DeviceIdentification> testEquipment,
        string path);
}