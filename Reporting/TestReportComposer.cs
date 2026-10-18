using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;
using ATLab.Services;
using ATLab.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATLab.Reporting;

public class TestReportComposer
{
    private readonly List<TestStepViewModel> _testResults;
    private readonly TestInfo _info;
    private readonly IHardwareInfo _hardwareInfo;
    private readonly List<DeviceIdentification> _deviceIdentification;
    
    private static string AppName =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyProductAttribute>()?
            .Product
        ?? "Unknown";
    
    private static string AppVersion =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .Split('+')[0]
        ?? "Unknown";
    
    private static readonly byte[] LogoImage =
        LoadResource("ATLab.Assets.AppIcon.png");

    private static readonly byte[] PassIcon =
        LoadResource("ATLab.Assets.Pass.png");

    private static readonly byte[] FailIcon =
        LoadResource("ATLab.Assets.Fail.png");

    public TestReportComposer(List<TestStepViewModel> testResults, TestInfo info, IHardwareInfo hardwareInfo, List<DeviceIdentification> deviceIdentification)
    {
        _testResults = testResults;
        _info = info;
        _hardwareInfo = hardwareInfo;
        _deviceIdentification = deviceIdentification;
    }

    public void ComposeHeader(IContainer container)
    {
        container.PaddingBottom(20)
            .Row(row =>
            {
                row.ConstantItem(40)
                    .Image(LogoImage)
                    .FitWidth();

                row.Spacing(10);

                row.RelativeItem()
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item().Text($"{LocalizationService.Instance["ReportTitle_TestReport"]} - {_info.DeviceUnderTestInfo.DeviceName} {_info.SerialNumber}")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Black);
                    });
            });
    }

    public void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(20);
            
            col.Item().Row(row =>
            {
                row.Spacing(10);
                row.RelativeItem().Element(ComposeInfoSection);
                row.RelativeItem().Element(ComposeDutSection);
            });

            if (!string.IsNullOrWhiteSpace(_info.DeviceUnderTestInfo.AdditionalNotes))
            {
                col.Item().Element(ComposeAdditionalNotesSection);
            }

            col.Item().Element(ComposeResultBanner);
            col.Item().Element(ComposeTable);

            if (_hardwareInfo is not DummyHardwareInfo || _deviceIdentification.Count > 0)
            {
                col.Item().Element(ComposeEquipmentSection);
            }
            
        });
    }

    public void ComposeFooter(IContainer container)
    {
        container.PaddingTop(10)
            .Row(row =>
            {
                row.RelativeItem().AlignLeft().Text($"{AppName} - {LocalizationService.Instance["ReportLabel_Version"]} {AppVersion}");
                
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span(LocalizationService.Instance["ReportLabel_Page"]);
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
    }
    
    private void ComposeInfoSection(IContainer container)
    {
        var info = _info;

        container.Padding(5).Column(col =>
        {
            col.Spacing(5);

            col.Item().Text(LocalizationService.Instance["ReportLabel_TestInformation"])
                .FontSize(16)
                .SemiBold()
                .FontColor(Colors.Black);
            
            col.Item().BorderBottom(2).BorderColor(Colors.Grey.Darken1);

            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text(LocalizationService.Instance["ReportLabel_Project"]);
                r.RelativeItem().Text(info.ProjectName ?? "-").SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text(LocalizationService.Instance["ReportLabel_Operator"]);
                r.RelativeItem().Text(info.Operator ?? "-").SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text(LocalizationService.Instance["ReportLabel_Date"]);
                r.RelativeItem().Text(info.Date).SemiBold();
            });
            
            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text(LocalizationService.Instance["ReportLabel_Time"]);
                r.RelativeItem().Text(info.Time).SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text(LocalizationService.Instance["ReportLabel_Duration"]);
                r.RelativeItem().Text(info.Duration ?? "-").SemiBold();
            });
        });
    }

    private void ComposeDutSection(IContainer container)
    {
        var info = _info;

        container.Padding(5).Column(col =>
        {
            col.Spacing(5);

            col.Item().Text(LocalizationService.Instance["ReportLabel_DeviceUnderTest"])
                .FontSize(16)
                .SemiBold()
                .FontColor(Colors.Black);
            
            col.Item().BorderBottom(2).BorderColor(Colors.Grey.Darken1);

            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text(LocalizationService.Instance["ReportLabel_SerialNumber"]);
                r.RelativeItem().Text(string.IsNullOrEmpty(info.SerialNumber) ? "-" : info.SerialNumber).SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text(LocalizationService.Instance["ReportLabel_DeviceName"]);
                r.RelativeItem().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.DeviceName) ? "-" : info.DeviceUnderTestInfo.DeviceName).SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text(LocalizationService.Instance["ReportLabel_Revision"]);
                r.RelativeItem().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.Revision) ? "-" : info.DeviceUnderTestInfo.Revision).SemiBold();
            });
            
            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text(LocalizationService.Instance["ReportLabel_Variant"]);
                r.RelativeItem().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.Variant) ? "-" : info.DeviceUnderTestInfo.Variant).SemiBold();
            });
            
            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text(LocalizationService.Instance["ReportLabel_PartNumber"]);
                r.RelativeItem().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.PartNumber) ? "-" : info.DeviceUnderTestInfo.PartNumber).SemiBold();
            });
        });
    }
    
    private void ComposeAdditionalNotesSection(IContainer container)
    {
        var info = _info;

        container.Padding(5).Column(c =>
        {
            c.Item().Text(LocalizationService.Instance["ReportLabel_AdditionalNotes"])
                .SemiBold()
                .FontColor(Colors.Black);
            c.Item().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.AdditionalNotes) ? "-" : info.DeviceUnderTestInfo.AdditionalNotes);
        });
    }

    private void ComposeResultBanner(IContainer container)
    {
        var allPassed = _testResults.All(x => x.IsPassed);

        var text = allPassed ? LocalizationService.Instance["ReportLabel_Pass"] : LocalizationService.Instance["ReportLabel_Fail"];
        var color = allPassed ? Colors.Green.Medium : Colors.Red.Medium;

        container
            .Background(color)
            .CornerRadius(5)
            .AlignCenter()
            .Text(text)
            .FontSize(32)
            .Bold()
            .FontColor(Colors.White);
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(45);  // Step
                columns.RelativeColumn();          // Name
                columns.ConstantColumn(80);  // Lower
                columns.ConstantColumn(80);  // Measured
                columns.ConstantColumn(80);  // Upper
                columns.ConstantColumn(55);  // Result
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(LocalizationService.Instance["ReportLabel_Step"]).LineHeight(1.3f);
                header.Cell().Element(HeaderCell).Text(LocalizationService.Instance["ReportLabel_Name"]).LineHeight(1.3f);
                header.Cell().Element(HeaderCell).Text(LocalizationService.Instance["ReportLabel_LowerLimit"]).LineHeight(1.3f);
                header.Cell().Element(HeaderCell).Text(LocalizationService.Instance["ReportLabel_Measured"]).LineHeight(1.3f);
                header.Cell().Element(HeaderCell).Text(LocalizationService.Instance["ReportLabel_UpperLimit"]).LineHeight(1.3f);
                header.Cell().Element(HeaderCell).Text(LocalizationService.Instance["ReportLabel_Result"]).LineHeight(1.3f);
            });

            // Rows, grouped by module with a header row whenever the module changes
            TestModule? currentModule = null;

            foreach (var row in _testResults)
            {
                if (!ReferenceEquals(row.Module, currentModule))
                {
                    currentModule = row.Module;

                    table.Cell().ColumnSpan(6).Element(ModuleCell)
                        .Text(currentModule.Name)
                        .FontSize(11)
                        .SemiBold()
                        .LineHeight(1.3f);
                }

                table.Cell().Element(BodyCell).Text(row.TestStep.Number.ToString()).FontSize(10).LineHeight(1.3f);
                table.Cell().Element(BodyCell).Text(row.TestStep.Name).FontSize(10).LineHeight(1.3f);

                var lowerLimit = !string.IsNullOrWhiteSpace(row.TestStep.Unit)
                    ? UnitParser.Format(row.TestStep.LowerLimit, row.TestStep.Unit)
                    : row.TestStep.LowerLimit.ToString(CultureInfo.CurrentCulture);

                var lower = row.TestStep.EvaluationSource == TestEvaluationSource.NONE
                    ? "-"
                    : $"{lowerLimit}";
                
                table.Cell().Element(BodyCell).Text(lower).FontSize(10).LineHeight(1.3f);

                var measured = row.TestStep.EvaluationSource == TestEvaluationSource.NONE
                    ? "-"
                    : $"{row.Result}";

                table.Cell().Element(BodyCell).Text(measured).FontSize(10).LineHeight(1.3f);
                
                var upperLimit = !string.IsNullOrWhiteSpace(row.TestStep.Unit)
                    ? UnitParser.Format(row.TestStep.UpperLimit, row.TestStep.Unit)
                    : row.TestStep.UpperLimit.ToString(CultureInfo.CurrentCulture);
                
                var upper = row.TestStep.EvaluationSource == TestEvaluationSource.NONE
                    ? "-"
                    : $"{upperLimit}";
                
                table.Cell().Element(BodyCell).Text(upper).FontSize(10).LineHeight(1.3f);

                // Result icon
                table.Cell().Element(BodyCell)
                    .AlignCenter()
                    .Height(10)
                    .Image(row.IsPassed ? PassIcon : FailIcon)
                    .FitHeight();
            }
        });
        
        return;

        static IContainer HeaderCell(IContainer c) =>
            c.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);

        static IContainer BodyCell(IContainer c) =>
            c.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);

        static IContainer ModuleCell(IContainer c) =>
            c.PaddingTop(8).Padding(4).Background(Colors.Grey.Lighten4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
    }
    
    private void ComposeEquipmentSection(IContainer container)
    {
        container.Padding(5).Column(col =>
        {
            col.Spacing(5);

            col.Item().Text(LocalizationService.Instance["ReportLabel_UsedEquipment"])
                .FontSize(16)
                .SemiBold()
                .FontColor(Colors.Black);
            
            col.Item().BorderBottom(2).BorderColor(Colors.Grey.Darken1);

            if (_hardwareInfo is not DummyHardwareInfo)
            {
                col.Item().PaddingTop(10)
                    .Text(LocalizationService.Instance["ReportLabel_TestHardware"])
                    .SemiBold()
                    .FontColor(Colors.Black);
                
                col.Item().Row(r =>
                {
                    r.ConstantItem(120).Text(LocalizationService.Instance["ReportLabel_DeviceName"]);
                    r.RelativeItem().Text(_hardwareInfo.DeviceName).SemiBold();
                });
                
                col.Item().Row(r =>
                {
                    r.ConstantItem(120).Text(LocalizationService.Instance["ReportLabel_SerialNumber"]);
                    r.RelativeItem().Text(_hardwareInfo.SerialNumber).SemiBold();
                });
                
                col.Item().Row(r =>
                {
                    r.ConstantItem(120).Text(LocalizationService.Instance["ReportLabel_FirmwareVersion"]);
                    r.RelativeItem().Text(_hardwareInfo.FirmwareVersion).SemiBold();
                });
            }

            if (_deviceIdentification.Count > 0)
            {
                col.Item().PaddingTop(10)
                    .Text(LocalizationService.Instance["ReportLabel_TestGear"])
                    .FontSize(12)
                    .SemiBold()
                    .FontColor(Colors.Black);

                foreach (var device in _deviceIdentification)
                {
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text(device.Name + ":");
                        r.RelativeItem().Text(device.Identification ?? "-").SemiBold();
                    });
                }
            }
        });
    }
    
    private static byte[] LoadResource(string resourceName)
    {
        var assembly = typeof(TestReportComposer).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Resource not found: {resourceName}");

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}