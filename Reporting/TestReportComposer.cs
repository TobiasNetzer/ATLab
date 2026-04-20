using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ATLab.Enums;
using ATLab.Models;
using ATLab.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATLab.Reporting;

public class TestReportComposer
{
    private readonly List<TestStepViewModel> _testResults;
    private readonly TestInfo _info;
    
    private static string AppName =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyProductAttribute>()?
            .Product
        ?? "Unknown";
    
    private static string AppVersion =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? "Unknown";
    
    private static readonly byte[] LogoImage =
        LoadResource("ATLab.Reporting.Assets.Logo.png");

    private static readonly byte[] PassIcon =
        LoadResource("ATLab.Reporting.Assets.Pass.png");

    private static readonly byte[] FailIcon =
        LoadResource("ATLab.Reporting.Assets.Fail.png");

    public TestReportComposer(List<TestStepViewModel> testResults, TestInfo info)
    {
        _testResults = testResults;
        _info = info;
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
                        col.Item().Text($"Test Report - {_info.DeviceUnderTestInfo.DeviceName} {_info.SerialNumber}")
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
        });
    }

    public void ComposeFooter(IContainer container)
    {
        container.PaddingTop(10)
            .Row(row =>
            {
                row.RelativeItem().AlignLeft().Text($"{AppName} - Version: {AppVersion}");
                
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ");
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
            col.Spacing(6);

            col.Item().Text("Test Information")
                .FontSize(16)
                .SemiBold()
                .FontColor(Colors.Black);
            
            col.Item().BorderBottom(2).BorderColor(Colors.Grey.Darken1);

            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text("Project:");
                r.RelativeItem().Text(info.ProjectName ?? "-").SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text("Operator:");
                r.RelativeItem().Text(info.Operator ?? "-").SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text("Date:");
                r.RelativeItem().Text(info.Date ?? "-").SemiBold();
            });
            
            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text("Time:");
                r.RelativeItem().Text(info.Time ?? "-").SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text("Duration:");
                r.RelativeItem().Text(info.Duration ?? "-").SemiBold();
            });
        });
    }

    private void ComposeDutSection(IContainer container)
    {
        var info = _info;

        container.Padding(5).Column(col =>
        {
            col.Spacing(6);

            col.Item().Text("Device Under Test")
                .FontSize(16)
                .SemiBold()
                .FontColor(Colors.Black);
            
            col.Item().BorderBottom(2).BorderColor(Colors.Grey.Darken1);

            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text("Serial Number:");
                r.RelativeItem().Text(string.IsNullOrEmpty(info.SerialNumber) ? "-" : info.SerialNumber).SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text("Device Name:");
                r.RelativeItem().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.DeviceName) ? "-" : info.DeviceUnderTestInfo.DeviceName).SemiBold();
            });

            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text("Revision:");
                r.RelativeItem().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.Revision) ? "-" : info.DeviceUnderTestInfo.Revision).SemiBold();
            });
            
            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text("Variant:");
                r.RelativeItem().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.Variant) ? "-" : info.DeviceUnderTestInfo.Variant).SemiBold();
            });
            
            col.Item().Row(r =>
            {
                r.ConstantItem(90).Text("Part Number:");
                r.RelativeItem().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.PartNumber) ? "-" : info.DeviceUnderTestInfo.PartNumber).SemiBold();
            });
        });
    }
    
    private void ComposeAdditionalNotesSection(IContainer container)
    {
        var info = _info;

        container.Padding(5).Column(c =>
        {
            c.Item().Text("Additional Notes:")
                .SemiBold()
                .FontColor(Colors.Black);
            c.Item().Text(string.IsNullOrEmpty(info.DeviceUnderTestInfo.AdditionalNotes) ? "-" : info.DeviceUnderTestInfo.AdditionalNotes);
        });
    }

    private void ComposeResultBanner(IContainer container)
    {
        var allPassed = _testResults.All(x => x.IsPassed);

        var text = allPassed ? "PASS" : "FAIL";
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
                columns.ConstantColumn(40);  // Step
                columns.RelativeColumn();          // Name
                columns.ConstantColumn(80);  // Lower
                columns.ConstantColumn(80);  // Measured
                columns.ConstantColumn(80);  // Upper
                columns.ConstantColumn(45);  // Result
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Step");
                header.Cell().Element(HeaderCell).Text("Name");
                header.Cell().Element(HeaderCell).Text("Lower");
                header.Cell().Element(HeaderCell).Text("Measured");
                header.Cell().Element(HeaderCell).Text("Upper");
                header.Cell().Element(HeaderCell).Text("Result");
            });

            // Rows
            foreach (var row in _testResults)
            {
                table.Cell().Element(BodyCell).Text(row.TestStep.Number.ToString()).FontSize(10);
                table.Cell().Element(BodyCell).Text(row.TestStep.Name).FontSize(10);

                var lower = row.TestStep.EvaluationSource == TestEvaluationSource.NONE
                    ? "-"
                    : $"{row.TestStep.LowerLimit} {row.TestStep.Unit}";
                
                table.Cell().Element(BodyCell).Text(lower).FontSize(10);

                var measured = string.IsNullOrWhiteSpace(row.Result)
                    ? "-"
                    : $"{row.ResultNoFormatting} {row.TestStep.Unit}";

                table.Cell().Element(BodyCell).Text(measured).FontSize(10);
                
                var upper = row.TestStep.EvaluationSource == TestEvaluationSource.NONE
                    ? "-"
                    : $"{row.TestStep.UpperLimit} {row.TestStep.Unit}";
                
                table.Cell().Element(BodyCell).Text(upper).FontSize(10);

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