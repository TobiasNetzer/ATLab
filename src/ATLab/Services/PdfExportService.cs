using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;
using ATLab.Reporting;
using ATLab.ViewModels;
using Avalonia.Platform.Storage;
using ATLab.Pcb.Kicad;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Rendering;
using QuestPDF.Fluent;

namespace ATLab.Services;

/// <summary>
/// Creates the PDF test report, including the identification of the devices marked for the report.
/// </summary>
public class PdfExportService : IPdfExportService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly ILoggingService _loggingService;
    private readonly IHardwareInfo _hardwareInfo;
    private readonly ApplicationState _applicationState;
    private readonly IDeviceIdentificationService _deviceIdentificationService;
    private readonly ProjectModel _projectModel;
    private readonly BoardService _boardService;
    private readonly ISettingsService _settingsService;

    /// <summary>More drawings would make the report long without helping the repair.</summary>
    private const int MaxBoardViews = 10;

    public PdfExportService(
        IFileDialogService fileDialogService,
        ILoggingService loggingService,
        IHardwareAccessor hardwareAccessor,
        ApplicationState applicationState,
        IDeviceIdentificationService deviceIdentificationService,
        ProjectModel projectModel,
        BoardService boardService,
        ISettingsService settingsService)
    {
        _boardService = boardService;
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        _loggingService = loggingService;
        _hardwareInfo = hardwareAccessor.Hardware.HardwareInfo;
        _applicationState = applicationState;
        _deviceIdentificationService = deviceIdentificationService;
        _projectModel = projectModel;
    }

    public async Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo)
    {
        var file = await _fileDialogService.SaveFileAsync(
            title: "Export Test Report",
            suggestedName: "Test Report",
            defaultExtension: "pdf",
            extensions: new[] { "pdf" });

        if (file == null)
        {
            _loggingService.Info("PDF export canceled.");
            return;
        }

        try
        {
            await ExportToFileAsync(steps, testInfo, file);

            _loggingService.Info(
                $"PDF export completed: {file.Name}");
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"PDF export failed. {ex.Message}");
        }
    }

    public async Task ExportToPathAsync(
        IEnumerable<TestStepViewModel> steps,
        TestInfo testInfo,
        IReadOnlyList<DeviceIdentification> testEquipment,
        string path)
    {
        try
        {
            var outputPath = $"{path}.pdf";
            var pdfBytes = BuildPdf(steps, testInfo, testEquipment);

            await File.WriteAllBytesAsync(outputPath, pdfBytes);

            _loggingService.Info(
                $"PDF exported to '{outputPath}'.");
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"Failed to export PDF to '{path}.pdf'. {ex.Message}");
        }
    }

    private async Task ExportToFileAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, IStorageFile file)
    {
        var pdfBytes = BuildPdf(steps, testInfo, await _deviceIdentificationService.IdentifyReportDevicesAsync(_projectModel.Devices, CancellationToken.None));

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(pdfBytes);
    }

    private byte[] BuildPdf(
        IEnumerable<TestStepViewModel> steps,
        TestInfo testInfo,
        IReadOnlyList<DeviceIdentification> testEquipment)
    {
        var executed = steps
            .Where(vm => !vm.TestStep.IsIgnoreStep && vm.IsExecuted)
            .ToList();

        var stepList = executed
            .Where(vm => !vm.TestStep.IsExcludeFromExport)
            .ToList();

        // Failed steps hidden from the table still make the run fail; the report says how many are not shown
        var failedExcludedSteps = executed
            .Count(vm => vm.TestStep.IsExcludeFromExport && !vm.IsPassed);

        var document = new TestReportDocument(
            stepList,
            failedExcludedSteps,
            testInfo,
            _hardwareInfo,
            _applicationState.IsSimulationMode,
            testEquipment.ToList(),
            CreateBoardViews(stepList));

        return document.GeneratePdf();
    }

    /// <summary>Drawings of the failed steps on the board, if the project asks for them and has a board.</summary>
    private List<ReportBoardView> CreateBoardViews(IEnumerable<TestStepViewModel> steps)
    {
        if (!_projectModel.Settings.IsIncludeBoardViewInReport || _boardService.Board is not { } board)
            return [];

        var views = new List<ReportBoardView>();
        foreach (var step in steps.Where(s => !s.IsPassed))
        {
            var highlight = _boardService.GetHighlight(step.TestStep);
            if (highlight.IsEmpty)
                continue;

            var side = _projectModel.Settings.ReportBoardSide switch
            {
                ReportBoardSide.Top => BoardSide.Top,
                ReportBoardSide.Bottom => BoardSide.Bottom,
                _ => BoardSvgRenderer.BestSide(board, highlight.Nets, highlight.Pads)
            };
            // Each side has layers of its own; what the project leaves open comes from the user's default, else the
            // pads of the side
            var style = _projectModel.Settings.ReportLayers().Over(_settingsService.Settings.DefaultReportLayers)
                .StyleFor(side == BoardSide.Top ? ProjectSettings.TopView : ProjectSettings.BottomView, [LayerColors.PadsOf(side)]);
            var svg = BoardSvgRenderer.Render(board, side, highlight.Nets, highlight.Pads, highlight.Footprints, highlight.Probes,
                _settingsService.Settings.IsBoardHighlightZones, style.Layers, style.Opacity, style.Stack, style.Colors);
            views.Add(new ReportBoardView(step.TestStep.Number, step.TestStep.Name, highlight.Legend, svg, side == BoardSide.Bottom));
            if (views.Count == MaxBoardViews)
                break;
        }

        return views;
    }
}