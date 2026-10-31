using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.Records;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

/// <summary>Exports the results of a run together with its metadata as JSON test result file.</summary>
public interface IJsonExportService
{
    Task ExportToPathAsync(
        IEnumerable<TestStepViewModel> steps,
        TestInfo testInfo,
        IReadOnlyList<DeviceIdentification> testEquipment,
        string path);
}