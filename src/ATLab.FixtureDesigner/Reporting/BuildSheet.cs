using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ATLab.FixtureDesigner.Fixture;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Rendering;
using ATLab.Pcb.Kicad;
using ATLab.FixtureDesigner.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATLab.FixtureDesigner.Reporting;

/// <summary>Everything the build sheet shows, collected by the view model.</summary>
public sealed class BuildSheetData
{
    public string Name { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string DeviceRevision { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string? FixtureFile { get; init; }
    public required Board Board { get; init; }
    public required AdapterPinout Pinout { get; init; }
    public required IReadOnlyList<Probe> Probes { get; init; }
    public IReadOnlyList<FixturePart> Parts { get; init; } = [];

    /// <summary>Board layers of the drawings as the user chose them; null for the defaults.</summary>
    public ReportLayerSettings? Layers { get; init; }

    /// <summary>Outline of each plate with its frame holes and cut-outs in board coordinates, drawn dashed; null for none.</summary>
    public IReadOnlyList<IReadOnlyList<Point2>>? TopPlateOutline { get; init; }

    public IReadOnlyList<IReadOnlyList<Point2>>? BottomPlateOutline { get; init; }

    public IReadOnlyList<IReadOnlyList<Point2>>? PlateOutlineOf(BoardSide side) =>
        side == BoardSide.Top ? TopPlateOutline : BottomPlateOutline;
}

/// <summary>
/// PDF for whoever builds the fixture: bill of materials, probes and pins to fit, wiring table from adapter pin to probe
/// and a drawing of every side. A4 landscape, laid out like ATLab's test report.
/// </summary>
public sealed class BuildSheet(BuildSheetData data) : IDocument
{
    private static readonly byte[] Logo = LoadResource("ATLab.FixtureDesigner.Assets.AppIcon.png");

    private static string AppName =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "ATLab Fixture Designer";

    private static string AppVersion =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "";

    private readonly DateTime _created = DateTime.Now;

    /// <summary>Keys of the four drawings in the layer settings.</summary>
    public const string AssemblyTop = "assemblyTop";

    public const string AssemblyBottom = "assemblyBottom";
    public const string WiringTop = "wiringTop";
    public const string WiringBottom = "wiringBottom";

    public static string ViewKey(BoardSide plate, bool wiring) => (plate, wiring) switch
    {
        (BoardSide.Top, false) => AssemblyTop,
        (BoardSide.Bottom, false) => AssemblyBottom,
        (BoardSide.Top, true) => WiringTop,
        _ => WiringBottom
    };

    /// <summary>
    /// Layers of a drawing without a choice of the user: the pads, holes and silkscreen of the board side seen, to find
    /// one's way on the board; the assembly drawings also the courtyards of that side, the components the probes, pins
    /// and rods must keep clear of.
    /// </summary>
    public static IReadOnlyCollection<string> DefaultLayers(string view)
    {
        const string topPads = ATLab.Pcb.Controls.LayerColors.TopPads;
        const string bottomPads = ATLab.Pcb.Controls.LayerColors.BottomPads;
        const string holes = ATLab.Pcb.Controls.LayerColors.Holes;
        return view switch
        {
            AssemblyTop => [topPads, holes, "F.SilkS", "F.CrtYd"],
            AssemblyBottom => [bottomPads, holes, "B.SilkS", "B.CrtYd"],
            WiringTop => [topPads, holes, "F.SilkS"],
            _ => [bottomPads, holes, "B.SilkS"]
        };
    }

    public static void Generate(BuildSheetData data, string path)
    {
        var temp = path + ".tmp";
        new BuildSheet(data).GeneratePdf(temp);
        File.Move(temp, path, overwrite: true);
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{L("Report_Title")} – {data.Name}",
        Author = data.Author,
        Creator = AppName,
        CreationDate = _created
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(t => t.FontSize(9));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private static string L(string key) => Localizer.Get(key);

    private static string Mm(double value) => value.ToString("0.00", CultureInfo.CurrentCulture);

    private void ComposeHeader(IContainer container)
    {
        container.PaddingBottom(12).Row(row =>
        {
            row.ConstantItem(36).Image(Logo).FitWidth();
            row.Spacing(10);
            row.RelativeItem().AlignMiddle().Text($"{L("Report_Title")} – {(data.Name.Length > 0 ? data.Name : data.DeviceName)}")
                .FontSize(18).Bold().FontColor(Colors.Black);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text($"{AppName} – {L("Report_Version")} {AppVersion}").FontSize(8);
            row.RelativeItem().AlignCenter().Text(_created.ToString("g", CultureInfo.CurrentCulture)).FontSize(8);
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(t => t.FontSize(8));
                text.Span(L("Report_Page"));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    /// <summary>
    /// In the order of the work: what the fixture is, what to get (bill of materials), where every probe and pin goes,
    /// how to wire it, and the drawings of both sides.
    /// </summary>
    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().Row(row =>
            {
                row.Spacing(16);
                row.RelativeItem().Element(ComposeFixtureInfo);
                row.RelativeItem().Element(ComposeBoardInfo);
            });

            if (!string.IsNullOrWhiteSpace(data.Notes))
            {
                col.Item().Column(c =>
                {
                    c.Item().Text(L("Report_Notes")).SemiBold();
                    c.Item().Text(data.Notes);
                });
            }

            // Heading and the start of its table stay together; long tables continue on the next page
            if (BillOfMaterials().Count > 0)
            {
                col.Item().EnsureSpace(90).Column(c =>
                {
                    c.Item().Element(x => Section(x, L("Report_Bom")));
                    c.Item().PaddingTop(6).Element(ComposeBomTable);
                });
            }

            col.Item().EnsureSpace(90).Column(c =>
            {
                c.Item().Element(x => Section(x, L("Report_Probes")));
                c.Item().PaddingTop(6).Element(ComposeProbeTable);
            });

            if (data.Parts.Count > 0)
            {
                col.Item().EnsureSpace(90).Column(c =>
                {
                    c.Item().Element(x => Section(x, L("Report_Parts")));
                    c.Item().PaddingTop(6).Element(ComposePartTable);
                });
            }

            col.Item().EnsureSpace(110).Column(c =>
            {
                c.Item().Element(x => Section(x, L("Report_Wiring")));
                c.Item().PaddingTop(6).Text(L("Report_WiringHint"))
                    .FontColor(Colors.Grey.Darken2);
                c.Item().PaddingTop(6).Element(ComposeWiringTable);
            });

            // Assembly: every plate with probes or parts, seen from the side where they touch the board, so it shows what
            // they meet there: the top plate from the top, the bottom plate from below (mirrored); a board without either
            // gets both plates
            var sides = data.Probes.Select(p => p.Side).Concat(data.Parts.Select(p => p.PlateSide)).Distinct().Order().ToList();
            if (sides.Count == 0)
                sides = [BoardSide.Top, BoardSide.Bottom];

            foreach (var side in sides)
                ComposeDrawing(col, side, side, wiring: false);

            // Wiring: every plate with probes, seen from its outside where the wires go, the bottom plate from below
            foreach (var side in data.Probes.Where(p => p.Pad is not null).Select(p => p.Side).Distinct().Order())
                ComposeDrawing(col, side, side, wiring: true);
        });
    }

    /// <summary>A page with the drawing of a plate: for the assembly with its parts, for the wiring with the probes only.</summary>
    private void ComposeDrawing(ColumnDescriptor col, BoardSide side, BoardSide view, bool wiring)
    {
        var parts = wiring ? [] : data.Parts;
        col.Item().PageBreak();
        col.Item().Element(c => Section(c, BoardSvg.Caption(side, wiring)));
        col.Item().Text(BoardSvg.ViewHint(view)).FontColor(Colors.Grey.Darken2);
        if (!wiring)
            col.Item().PaddingTop(4).Element(c => ComposeLegend(c, side));
        col.Item().ExtendVertical().AlignCenter().AlignMiddle()
            .Svg(BoardSvg.Render(data.Board, data.Probes, side, view, (data.Layers ?? new()).StyleFor(ViewKey(side, wiring), DefaultLayers(ViewKey(side, wiring))),
                parts, data.PlateOutlineOf(side), withPins: wiring)).FitArea();
    }

    private static void Section(IContainer container, string title)
    {
        container.Column(c =>
        {
            c.Item().Text(title).FontSize(14).SemiBold().FontColor(Colors.Black);
            c.Item().PaddingTop(2).BorderBottom(2).BorderColor(Colors.Grey.Darken1);
        });
    }

    private static void Fact(ColumnDescriptor col, string label, string? value, float labelWidth = 95)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(labelWidth).Text(label);
            r.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).SemiBold();
        });
    }

    private void ComposeFixtureInfo(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(4);
            col.Item().Element(c => Section(c, L("Report_Fixture")));
            Fact(col, L("Report_FixtureName"), data.Name);
            Fact(col, L("Report_FixtureFile"), data.FixtureFile is null ? L("Text_Unsaved") : Path.GetFileName(data.FixtureFile));
            Fact(col, L("Report_Author"), data.Author);
            Fact(col, L("Report_Adapter"), $"{data.Pinout.TestHardware.Name} ({data.Pinout.TestHardware.DeviceId})");
            Fact(col, L("Report_ProbeCount"), Localizer.Format("Text_ProbeCount", data.Probes.Count,
                data.Probes.Count(p => p.Side == BoardSide.Top), data.Probes.Count(p => p.Side == BoardSide.Bottom)));
        });
    }

    private void ComposeBoardInfo(IContainer container)
    {
        var board = data.Board;
        container.Column(col =>
        {
            col.Spacing(4);
            col.Item().Element(c => Section(c, L("Report_Board")));
            Fact(col, L("Report_Device"), data.DeviceName);
            Fact(col, L("Report_Revision"), data.DeviceRevision);
            Fact(col, L("Report_BoardFile"), Path.GetFileName(board.FilePath));
            Fact(col, L("Report_BoardSize"), $"{Mm(board.Bounds.Width)} × {Mm(board.Bounds.Height)} mm");
            Fact(col, L("Report_Origin"), L(board.IsOriginFromFile ? "Report_OriginFile" : "Report_OriginCorner"));
        });
    }

    /// <summary>The symbols of the drawing of one side with their names, in one or more lines.</summary>
    private void ComposeLegend(IContainer container, BoardSide side)
    {
        container.Inlined(inline =>
        {
            inline.HorizontalSpacing(14);
            inline.VerticalSpacing(4);
            foreach (var (symbol, label) in BoardSvg.Legend(data.Probes, side, data.Parts, data.PlateOutlineOf(side) is { Count: > 0 }))
            {
                inline.Item().Row(row =>
                {
                    row.ConstantItem(10).Height(10).Svg(symbol);
                    row.AutoItem().PaddingLeft(4).Text(label).FontSize(9).FontColor(Colors.Grey.Darken3);
                });
            }
        });
    }

    private static IContainer HeaderCell(IContainer c) => c.Padding(3).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
    private static IContainer BodyCell(IContainer c) => c.Padding(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);

    /// <summary>Empty box for ticking off a probe or wire while building.</summary>
    private static void TickBox(IContainer c) =>
        c.Element(BodyCell).AlignCenter().AlignMiddle().PaddingBottom(3).Width(9).Height(9).Border(0.8f).BorderColor(Colors.Grey.Darken2);

    private void ComposeProbeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(34);  // Probe
                c.ConstantColumn(70);  // Pad
                c.RelativeColumn(2);   // Net
                c.ConstantColumn(48);  // Side
                c.ConstantColumn(48);  // X
                c.ConstantColumn(48);  // Y
                c.ConstantColumn(80);  // Pad type and size
                c.ConstantColumn(62);  // Probe type
                c.ConstantColumn(32);  // Pin
                c.ConstantColumn(78);  // Resource
                c.RelativeColumn(2);   // Note
                c.ConstantColumn(30);  // Tick
            });

            table.Header(h =>
            {
                foreach (var key in new[] { "Column_Probe", "Column_Pad", "Column_Net", "Column_Side", "Column_X", "Column_Y",
                             "Column_PadType", "Column_ProbeType", "Column_Pin", "Column_Resource", "Column_Note" })
                    h.Cell().Element(HeaderCell).Text(L(key)).SemiBold();
                h.Cell().Element(HeaderCell).AlignCenter().Text(L("Column_Done")).SemiBold();
            });

            foreach (var probe in data.Probes)
            {
                var pad = probe.Pad;
                table.Cell().Element(BodyCell).Text(probe.Name).SemiBold();
                table.Cell().Element(BodyCell).Text(probe.PadDisplay);
                table.Cell().Element(BodyCell).Text(probe.ShortNet);
                table.Cell().Element(BodyCell).Text(Localizer.Enum(probe.Side));
                table.Cell().Element(BodyCell).AlignRight().Text(pad is null ? "-" : Mm(probe.X));
                table.Cell().Element(BodyCell).AlignRight().Text(pad is null ? "-" : Mm(probe.Y));
                table.Cell().Element(BodyCell).Text(pad is null ? L("Report_PadMissing")
                    : $"{(pad.Kind == PadKind.Smd ? "SMD" : "THT")} {pad.Width.ToString("0.##", CultureInfo.CurrentCulture)}×{pad.Height.ToString("0.##", CultureInfo.CurrentCulture)}");
                table.Cell().Element(BodyCell).Text(probe.TypeName.Length > 0 ? probe.TypeName : "-");
                table.Cell().Element(BodyCell).Text(probe.Pin?.Pin ?? "-").SemiBold();
                table.Cell().Element(BodyCell).Text(probe.Pin?.Resource ?? L("Report_NotWired"));
                table.Cell().Element(BodyCell).Text(probe.Note);
                table.Cell().Element(TickBox);
            }
        });
    }

    /// <summary>The used pins of the DUT connector in connector order, with the probes to wire to each.</summary>
    private void ComposeWiringTable(IContainer container)
    {
        var byPin = data.Probes.Where(p => p.Pin is not null).GroupBy(p => p.Pin!).ToDictionary(g => g.Key, g => g.ToList());
        var pins = data.Pinout.FixturePins.Where(byPin.ContainsKey).OrderBy(p => p.SortKey.Row).ThenBy(p => p.SortKey.Number).ToList();

        if (pins.Count == 0)
        {
            container.Text(L("Report_NoWiring")).Italic();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(40);  // Pin
                c.ConstantColumn(90);  // Resource
                c.RelativeColumn(2);   // Signal
                c.RelativeColumn(2);   // Net
                c.RelativeColumn(3);   // Probes
                c.ConstantColumn(30);  // Tick
            });

            table.Header(h =>
            {
                foreach (var key in new[] { "Column_Pin", "Column_Resource", "Column_Signal", "Column_Net", "Column_Probes" })
                    h.Cell().Element(HeaderCell).Text(L(key)).SemiBold();
                h.Cell().Element(HeaderCell).AlignCenter().Text(L("Column_Done")).SemiBold();
            });

            foreach (var pin in pins)
            {
                var probes = byPin[pin];
                var nets = probes.Select(p => p.ShortNet).Where(n => n.Length > 0).Distinct().ToList();
                var color = nets.Count > 1 ? Colors.Red.Darken2 : Colors.Black;

                table.Cell().Element(BodyCell).Text(pin.Pin).SemiBold();
                table.Cell().Element(BodyCell).Text(pin.Resource);
                table.Cell().Element(BodyCell).Text(pin.Signal).FontColor(Colors.Grey.Darken2);
                table.Cell().Element(BodyCell).Text(string.Join(", ", nets)).FontColor(color);
                table.Cell().Element(BodyCell).Text(string.Join(", ", probes.Select(p => $"{p.Name} ({p.PadDisplay})")));
                table.Cell().Element(TickBox);
            }
        });
    }

    /// <summary>
    /// A line of the bill of materials: a type of probe, pin or rod and how many of it the fixture needs. The diameter is
    /// written with its unit, mil for probes and mm for pins and rods.
    /// </summary>
    private sealed record BomLine(string Kind, string Type, int Quantity, string Diameter, double Drill, double Height, string Note);

    /// <summary>
    /// The probe types, then the types of the locating, pre-centering and support pins and push rods, each with the
    /// number in use; probes or parts without a type are counted in a line of their own.
    /// </summary>
    private List<BomLine> BillOfMaterials()
    {
        var lines = new List<BomLine>();
        var probe = L("Report_KindProbe");
        static string Size(double value, string format, string unit) =>
            value > 0 ? value.ToString(format, CultureInfo.CurrentCulture) + " " + unit : "-";
        foreach (var group in data.Probes.GroupBy(p => p.Type).OrderBy(g => g.Key is null).ThenBy(g => g.Key?.Name, StringComparer.CurrentCulture))
        {
            lines.Add(group.Key is { } type
                ? new BomLine(probe, type.Name, group.Count(), Size(type.Diameter, "0.#", "mil"), type.DrillDiameter, type.Height, type.Note)
                : new BomLine(probe, L("Report_NoType"), group.Count(), "-", 0, 0, string.Empty));
        }

        foreach (var group in data.Parts.GroupBy(p => (p.Kind, p.Type)).OrderBy(g => g.Key.Kind).ThenBy(g => g.Key.Type is null)
                     .ThenBy(g => g.Key.Type?.Name, StringComparer.CurrentCulture))
        {
            var kind = Localizer.Get(FixturePart.KindKey(group.Key.Kind));
            lines.Add(group.Key.Type is { } type
                ? new BomLine(kind, type.Name, group.Count(), Size(type.Diameter, "0.00", "mm"), type.DrillDiameter, type.Height, type.Note)
                : new BomLine(kind, L("Report_NoType"), group.Count(), "-", 0, 0, string.Empty));
        }

        return lines;
    }

    private void ComposeBomTable(IContainer container)
    {
        static string Size(double value) => value > 0 ? Mm(value) : "-";

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(110); // Kind
                c.ConstantColumn(130); // Type
                c.ConstantColumn(50);  // Quantity
                c.ConstantColumn(65);  // Diameter
                c.ConstantColumn(75);  // Drill
                c.ConstantColumn(65);  // Height
                c.RelativeColumn();    // Note
                c.ConstantColumn(30);  // Tick
            });

            table.Header(h =>
            {
                foreach (var key in new[] { "Column_PartKind", "Column_PartType", "Column_Quantity", "Column_Diameter", "Column_Drill",
                             "Column_PartHeight", "Column_Note" })
                    h.Cell().Element(HeaderCell).Text(L(key)).SemiBold();
                h.Cell().Element(HeaderCell).AlignCenter().Text(L("Column_Done")).SemiBold();
            });

            foreach (var line in BillOfMaterials())
            {
                table.Cell().Element(BodyCell).Text(line.Kind);
                table.Cell().Element(BodyCell).Text(line.Type).SemiBold();
                table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString(CultureInfo.CurrentCulture)).SemiBold();
                table.Cell().Element(BodyCell).AlignRight().Text(line.Diameter);
                table.Cell().Element(BodyCell).AlignRight().Text(Size(line.Drill));
                table.Cell().Element(BodyCell).AlignRight().Text(Size(line.Height));
                table.Cell().Element(BodyCell).Text(line.Note);
                table.Cell().Element(TickBox);
            }
        });
    }

    /// <summary>Locating, pre-centering and support pins and push rods with their place and plate, to fit and tick off.</summary>
    private void ComposePartTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(40);  // Part
                c.ConstantColumn(130); // Kind
                c.RelativeColumn();    // Type
                c.ConstantColumn(60);  // X
                c.ConstantColumn(60);  // Y
                c.ConstantColumn(80);  // Plate
                c.ConstantColumn(30);  // Tick
            });

            table.Header(h =>
            {
                foreach (var key in new[] { "Column_Part", "Column_PartKind", "Column_PartType", "Column_X", "Column_Y", "Column_Plate" })
                    h.Cell().Element(HeaderCell).Text(L(key)).SemiBold();
                h.Cell().Element(HeaderCell).AlignCenter().Text(L("Column_Done")).SemiBold();
            });

            foreach (var part in data.Parts.OrderBy(p => p.Kind).ThenBy(p => p.Number))
            {
                table.Cell().Element(BodyCell).Text(part.Name).SemiBold();
                table.Cell().Element(BodyCell).Text(part.KindLabel);
                table.Cell().Element(BodyCell).Text(part.Type?.Name ?? "-");
                table.Cell().Element(BodyCell).AlignRight().Text(Mm(part.X));
                table.Cell().Element(BodyCell).AlignRight().Text(Mm(part.Y));
                table.Cell().Element(BodyCell).Text(L(part.PlateSide == BoardSide.Top ? "Plate_Top" : "Plate_Bottom"));
                table.Cell().Element(TickBox);
            }
        });
    }

    private static byte[] LoadResource(string name)
    {
        using var stream = typeof(BuildSheet).Assembly.GetManifestResourceStream(name);
        if (stream is null)
            return [];
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}