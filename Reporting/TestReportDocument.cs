using System.Collections.Generic;
using ATLab.Interfaces;
using ATLab.ViewModels;
using ATLab.Models;
using ATLab.Records;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ATLab.Reporting;

public class TestReportDocument : IDocument
{
    private readonly TestReportComposer _composer;

    public TestReportDocument(List<TestStepViewModel> testResults, TestInfo testInfo, IHardwareInfo hardwareInfo, List<DeviceIdentification> deviceIdentification)
    {
        _composer = new TestReportComposer(testResults, testInfo, hardwareInfo, deviceIdentification);
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