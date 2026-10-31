using System.Collections.Generic;
using ATLab.Interfaces;
using ATLab.ViewModels;
using ATLab.Models;
using ATLab.Records;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ATLab.Reporting;

/// <summary>QuestPDF document of the test report, delegates the layout to TestReportComposer.</summary>
public class TestReportDocument : IDocument
{
    private readonly TestReportComposer _composer;

    public TestReportDocument(List<TestStepViewModel> testResults, int failedExcludedSteps, TestInfo testInfo, IHardwareInfo hardwareInfo, List<DeviceIdentification> deviceIdentification)
    {
        _composer = new TestReportComposer(testResults, failedExcludedSteps, testInfo, hardwareInfo, deviceIdentification);
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(20);

            page.Header().Element(_composer.ComposeHeader);
            page.Content().Element(_composer.ComposeContent);
            page.Footer().Element(_composer.ComposeFooter);
        });
    }
}