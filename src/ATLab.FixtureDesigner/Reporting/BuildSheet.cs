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

    /// <summary>Components in the wiring, for the bill of materials.</summary>
    public IReadOnlyList<FixtureComponent> Components { get; init; } = [];

    /// <summary>Wires besides the probes' own pins, with both ends.</summary>
    public IReadOnlyList<Wire> Wires { get; init; } = [];

    /// <summary>Board layers of the drawings as the user chose them; null for the defaults.</summary>
    public ReportLayerSettings? Layers { get; init; }

    /// <summary>The plates of the fixture with their outline, frame holes and cut-outs in board coordinates, drawn dashed.</summary>
    public IReadOnlyDictionary<FixturePlate, IReadOnlyList<IReadOnlyList<Point2>>> PlateOutlines { get; init; } =
        new Dictionary<FixturePlate, IReadOnlyList<IReadOnlyList<Point2>>>();

    /// <summary>The board lies upside down in the fixture: its top side faces the bottom plates.</summary>
    public bool IsBoardFlipped { get; init; }

    /// <summary>The pressure plate holds the top probes and the push rods; there is no top probe plate.</summary>
    public bool IsTopPlateCombined { get; init; }

    /// <summary>The plate that holds the probes of a side, where the wires go.</summary>
    public FixturePlate ProbePlate(BoardSide side) => FixturePlates.ProbePlate(side, IsBoardFlipped, IsTopPlateCombined);

    /// <summary>
    /// The plates a drawing shows: for the assembly the plates of the side (the probe plate, then the moving or the
    /// pressure plate), for the wiring the probe plate, where the wires go.
    /// </summary>
    public IReadOnlyList<PlateOutlineDrawing> PlatesOf(BoardSide side, bool wiring) =>
        PlateOutlines.Where(p => p.Key.Side(IsBoardFlipped) == side && (!wiring || p.Key == ProbePlate(side)))
            .OrderBy(p => p.Key)
            .Select(p => new PlateOutlineDrawing(p.Key, p.Value))
            .ToList();
}

/// <summary>
/// PDF for whoever builds the fixture: bill of materials, the probes by type and the pins to fit with a drawing of every
/// side, then the wiring table from probe to adapter pin with a drawing of every probe plate. A4 landscape, laid out like ATLab's test report.
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
    /// In the order of the work: what the fixture is and what to get (bill of materials); the mechanical assembly of
    /// every side, its probes by type, pins and push rods and its drawing; then the wiring, its table, the probes not
    /// wired and the drawing of every probe plate. A drawing whose probe numbers are too small gets detail pages.
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

            // Assembly: the plates of every side with probes or parts, seen from the side where they touch the board, so it
            // shows what they meet there: the plates above from the top, the plates below from below (mirrored); a board
            // without either gets both sides
            var sides = data.Probes.Select(p => p.Side).Concat(data.Parts.Select(p => p.PlateSide)).Distinct().Order().ToList();
            if (sides.Count == 0)
                sides = [BoardSide.Top, BoardSide.Bottom];

            foreach (var side in sides)
            {
                var probes = data.Probes.Where(p => p.Side == side).ToList();
                var parts = data.Parts.Where(p => p.PlateSide == side).ToList();
                if (probes.Count > 0 || parts.Count > 0)
                {
                    col.Item().PageBreak();
                    col.Item().Column(c =>
                    {
                        c.Spacing(6);
                        c.Item().Element(x => Section(x, BoardSvg.Caption(side, false, data.ProbePlate(side))));
                        if (probes.Count > 0)
                        {
                            c.Item().Element(x => SubSection(x, L("Report_ProbesByType")));
                            c.Item().Element(x => ComposeTypeTable(x, probes, data.ProbePlate(side)));
                        }

                        if (parts.Count > 0)
                        {
                            c.Item().PaddingTop(6).Element(x => SubSection(x, L("Report_Parts")));
                            c.Item().Element(x => ComposePartTable(x, parts));
                        }
                    });
                }

                ComposeDrawings(col, side, side, wiring: false);
            }

            // Wiring: the table of all wires and the probes not wired, then the probe plate of every side with probes,
            // seen from its outside where the wires go, the bottom one from below
            var wiringSides = data.Probes.Where(p => p.Pad is not null).Select(p => p.Side).Distinct().Order().ToList();
            var rows = WiringRows();
            if (rows.Count > 0 || data.Probes.Count > 0)
            {
                col.Item().PageBreak();
                col.Item().Column(c =>
                {
                    c.Spacing(6);
                    c.Item().Element(x => Section(x, L("Report_Wiring")));
                    if (rows.Count > 0)
                        c.Item().Element(x => ComposeWiringTable(x, rows));
                    ComposeNotWired(c);
                });
            }

            foreach (var side in wiringSides)
                ComposeDrawings(col, side, side, wiring: true);
        });
    }

    /// <summary>Area of the page left to a drawing below its caption, hint and legend (pt), for its scale.</summary>
    private const float DrawingAreaWidth = 800;

    private const float DrawingAreaHeight = 380;

    /// <summary>
    /// The pages with the drawing of a side: for the assembly with its parts, the outlines of the plates of that side and
    /// the openings, for the wiring with the probes and the probe plate only. The whole side first, its detail areas framed,
    /// then a page for every detail.
    /// </summary>
    private void ComposeDrawings(ColumnDescriptor col, BoardSide side, BoardSide view, bool wiring)
    {
        var parts = wiring ? [] : data.Parts;
        var plates = data.PlatesOf(side, wiring);
        var drawing = BoardSvg.Plan(data.Board, data.Probes, side, view, wiring, parts, plates, DrawingAreaWidth, DrawingAreaHeight);
        var style = (data.Layers ?? new()).StyleFor(ViewKey(side, wiring), DefaultLayers(ViewKey(side, wiring)));
        var caption = BoardSvg.Caption(side, wiring, data.ProbePlate(side));

        foreach (var detail in new DrawingDetail?[] { null }.Concat(drawing.Details))
        {
            var hint = BoardSvg.ViewHint(view);
            if (detail is null && drawing.Details.Count > 0)
                hint += " " + L("Report_DetailHint");

            col.Item().PageBreak();
            col.Item().Element(c => Section(c, detail is null ? caption : Localizer.Format("Report_Detail", detail.Letter, caption)));
            col.Item().Text(hint).FontColor(Colors.Grey.Darken2);
            col.Item().PaddingTop(4).Element(c => ComposeLegend(c, drawing, plates.Select(p => p.Plate).ToList(), withOpenings: !wiring, parts));
            col.Item().ExtendVertical().AlignCenter().AlignMiddle()
                .Svg(BoardSvg.Render(drawing, data.Board, style, parts, plates, withOpenings: !wiring, detail)).FitArea();
        }
    }

    /// <summary>
    /// Probe names as ranges in the order of their numbers, e.g. "P1–P4, P7, P9–P12": runs of consecutive numbers are
    /// joined.
    /// </summary>
    private static string Ranges(IEnumerable<Probe> probes)
    {
        var numbers = probes.Select(p => p.Number).Distinct().Order().ToList();
        var parts = new List<string>();
        for (var i = 0; i < numbers.Count;)
        {
            var j = i;
            while (j + 1 < numbers.Count && numbers[j + 1] == numbers[j] + 1)
                j++;
            parts.Add(j == i ? $"P{numbers[i]}" : j == i + 1 ? $"P{numbers[i]}, P{numbers[j]}" : $"P{numbers[i]}–P{numbers[j]}");
            i = j + 1;
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// The probe types of a side, in the order of the bill of materials, with how many and which probes get them and the
    /// plate they go into.
    /// </summary>
    private static void ComposeTypeTable(IContainer container, IReadOnlyList<Probe> probes, FixturePlate plate)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(130); // Type
                c.ConstantColumn(50);  // Quantity
                c.RelativeColumn();    // Probes
                c.ConstantColumn(120);  // Plate
                c.ConstantColumn(30);  // Tick
            });

            table.Header(h =>
            {
                foreach (var key in new[] { "Column_ProbeType", "Column_Quantity", "Column_Probes", "Column_Plate" })
                    h.Cell().Element(HeaderCell).Text(L(key)).SemiBold();
                h.Cell().Element(HeaderCell).AlignCenter().Text(L("Column_Done")).SemiBold();
            });

            foreach (var group in probes.GroupBy(p => p.Type).OrderBy(g => g.Key is null).ThenBy(g => g.Key?.Name, StringComparer.CurrentCulture))
            {
                table.Cell().Element(BodyCell).Text(group.Key?.Name ?? L("Report_NoType")).SemiBold();
                table.Cell().Element(BodyCell).AlignRight().Text(group.Count().ToString(CultureInfo.CurrentCulture)).SemiBold();
                table.Cell().Element(BodyCell).Text(Ranges(group));
                table.Cell().Element(BodyCell).Text(ViewModels.PlateRow.NameOf(plate));
                table.Cell().Element(TickBox);
            }
        });
    }

    /// <summary>
    /// The probes no wire goes to, as ranges, to check that none was forgotten; by side when there are probes on both.
    /// </summary>
    private void ComposeNotWired(ColumnDescriptor col)
    {
        var notWired = data.Probes.Where(p => p.Connections.Length == 0).ToList();
        if (notWired.Count == 0)
            return;

        var bySide = data.Probes.Select(p => p.Side).Distinct().Count() > 1;
        foreach (var group in notWired.GroupBy(p => p.Side).OrderBy(g => g.Key))
        {
            var label = bySide ? Localizer.Format("Report_NotWiredSide", Localizer.Enum(group.Key)) : L("Report_NotWired");
            col.Item().PaddingTop(4).Text(text =>
            {
                text.Span(label + ": ").SemiBold();
                text.Span(Ranges(group));
            });
        }
    }

    private static void Section(IContainer container, string title)
    {
        container.Column(c =>
        {
            c.Item().Text(title).FontSize(14).SemiBold().FontColor(Colors.Black);
            c.Item().PaddingTop(2).BorderBottom(2).BorderColor(Colors.Grey.Darken1);
        });
    }

    private static void SubSection(IContainer container, string title) =>
        container.Text(title).FontSize(11).SemiBold().FontColor(Colors.Black);

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
            if (data.IsBoardFlipped)
                Fact(col, L("Report_Orientation"), L("Report_BoardFlipped"));
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
    private static void ComposeLegend(IContainer container, BoardDrawing drawing, IReadOnlyList<FixturePlate> plates, bool withOpenings,
        IReadOnlyList<FixturePart> parts)
    {
        container.Inlined(inline =>
        {
            inline.HorizontalSpacing(14);
            inline.VerticalSpacing(4);
            foreach (var (symbol, label) in BoardSvg.Legend(drawing, parts, plates, withOpenings))
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

    /// <summary>A wire to make: its name from the Wiring tab ("-" for a probe's pin) and both ends.</summary>
    private sealed record WiringRow(string Name, WireEndpoint From, WireEndpoint To, string Note);

    /// <summary>
    /// Every wire of the fixture: first each probe to its pin, sorted by pin so the probes of a pin are together, then the
    /// wires of the Wiring tab under their names there.
    /// </summary>
    private List<WiringRow> WiringRows()
    {
        var rows = new List<WiringRow>();
        foreach (var probe in data.Probes.Where(p => p.Pin is not null)
                     .OrderBy(p => p.Pin!.SortKey.Row).ThenBy(p => p.Pin!.SortKey.Number).ThenBy(p => p.Number))
            rows.Add(new WiringRow("-", WireEndpoint.Of(probe), WireEndpoint.Of(probe.Pin!), string.Empty));

        foreach (var wire in data.Wires.OrderBy(w => w.Number))
            rows.Add(new WiringRow(wire.Name, wire.From!, wire.To!, wire.Note));

        return rows;
    }

    /// <summary>Side of the probes of a wire, to find their probe plate; "-" for a wire without a probe.</summary>
    private static string SideOf(WiringRow row)
    {
        var sides = new[] { row.From.Probe, row.To.Probe }.OfType<Probe>().Select(p => p.Side).Distinct().Order().ToList();
        return sides.Count == 0 ? "-" : string.Join(" / ", sides.Select(s => Localizer.Enum(s)));
    }

    private void ComposeWiringTable(IContainer container, IReadOnlyList<WiringRow> rows)
    {
        // The side only matters when there are probe plates on both sides, the wire names only with wires of the Wiring
        // tab: a probe's wire to its pin has none
        var withSide = data.Probes.Select(p => p.Side).Distinct().Count() > 1;
        var withName = data.Wires.Count > 0;

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                if (withName)
                    c.ConstantColumn(40);  // Wire
                if (withSide)
                    c.ConstantColumn(70);  // Side
                c.RelativeColumn(3);   // From
                c.RelativeColumn(3);   // To
                c.RelativeColumn(2);   // Note
                c.ConstantColumn(30);  // Tick
            });

            table.Header(h =>
            {
                var keys = new[] { withName ? "Column_Wire" : null, withSide ? "Column_Side" : null, "Column_From", "Column_To", "Column_Note" };
                foreach (var key in keys.OfType<string>())
                    h.Cell().Element(HeaderCell).Text(L(key)).SemiBold();
                h.Cell().Element(HeaderCell).AlignCenter().Text(L("Column_Done")).SemiBold();
            });

            static void End(IContainer cell, WireEndpoint end) => cell.Text(text =>
            {
                text.Span(end.Name).SemiBold();
                text.Span("  " + end.Detail).FontColor(Colors.Grey.Darken2);
            });

            foreach (var row in rows)
            {
                if (withName)
                    table.Cell().Element(BodyCell).Text(row.Name).SemiBold();
                if (withSide)
                    table.Cell().Element(BodyCell).Text(SideOf(row));
                End(table.Cell().Element(BodyCell), row.From);
                End(table.Cell().Element(BodyCell), row.To);
                table.Cell().Element(BodyCell).Text(row.Note);
                table.Cell().Element(TickBox);
            }
        });
    }

    /// <summary>
    /// A line of the bill of materials: a type of probe, pin or rod and how many of it the fixture needs. The diameter is
    /// written with its unit, mil for probes and mm for pins and rods.
    /// </summary>
    private sealed record BomLine(string Kind, string Type, int Quantity, string Diameter, double Drill, double Opening, double Height, string Note);

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
        // The opening of the type, if a probe of it comes through the moving plate; probes with an opening of their own
        // have it in the plate files
        foreach (var group in data.Probes.GroupBy(p => p.Type).OrderBy(g => g.Key is null).ThenBy(g => g.Key?.Name, StringComparer.CurrentCulture))
        {
            var opening = group.Any(p => p.HasOpening) ? Probe.TypeOpening(group.Key) : 0;
            lines.Add(group.Key is { } type
                ? new BomLine(probe, type.Name, group.Count(), Size(type.Diameter, "0.#", "mil"), type.DrillDiameter, opening, type.Height, type.Note)
                : new BomLine(probe, L("Report_NoType"), group.Count(), "-", 0, 0, 0, string.Empty));
        }

        foreach (var group in data.Parts.GroupBy(p => (p.Kind, p.Type)).OrderBy(g => g.Key.Kind).ThenBy(g => g.Key.Type is null)
                     .ThenBy(g => g.Key.Type?.Name, StringComparer.CurrentCulture))
        {
            var kind = Localizer.Get(FixturePart.KindKey(group.Key.Kind));
            var opening = group.Key.Kind == FixturePartKind.PushRod && data.PlateOutlines.ContainsKey(FixturePlate.TopProbePlate)
                ? FixturePart.TypeOpening(group.Key.Type)
                : 0;
            lines.Add(group.Key.Type is { } type
                ? new BomLine(kind, type.Name, group.Count(), Size(type.Diameter, "0.00", "mm"), type.DrillDiameter, opening, type.Height, type.Note)
                : new BomLine(kind, L("Report_NoType"), group.Count(), "-", 0, 0, 0, string.Empty));
        }

        // Components of the wiring, one line per description and note; the designators belong to the wiring tables
        var component = L("Report_KindComponent");
        foreach (var group in data.Components.GroupBy(c => (Description: c.Description.Trim(), Note: c.Note.Trim()))
                     .OrderBy(g => g.Key.Description, StringComparer.CurrentCulture))
        {
            var type = group.Key.Description;
            lines.Add(new BomLine(component, type.Length > 0 ? type : "-", group.Count(), "-", 0, 0, 0, group.Key.Note));
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
                c.ConstantColumn(70);  // Drill
                c.ConstantColumn(80);  // Opening
                c.ConstantColumn(60);  // Height
                c.RelativeColumn();    // Note
                c.ConstantColumn(30);  // Tick
            });

            table.Header(h =>
            {
                foreach (var key in new[] { "Column_PartKind", "Column_PartType", "Column_Quantity", "Column_Diameter", "Column_Drill",
                             "Column_Opening", "Column_PartHeight", "Column_Note" })
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
                table.Cell().Element(BodyCell).AlignRight().Text(Size(line.Opening));
                table.Cell().Element(BodyCell).AlignRight().Text(Size(line.Height));
                table.Cell().Element(BodyCell).Text(line.Note);
                table.Cell().Element(TickBox);
            }
        });
    }

    /// <summary>Locating, pre-centering and support pins and push rods with their type and plate, to fit and tick off.</summary>
    private static void ComposePartTable(IContainer container, IReadOnlyList<FixturePart> parts)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(40);  // Part
                c.ConstantColumn(130); // Kind
                c.RelativeColumn();    // Type
                c.ConstantColumn(120);  // Plate
                c.ConstantColumn(30);  // Tick
            });

            table.Header(h =>
            {
                foreach (var key in new[] { "Column_Part", "Column_PartKind", "Column_PartType", "Column_Plate" })
                    h.Cell().Element(HeaderCell).Text(L(key)).SemiBold();
                h.Cell().Element(HeaderCell).AlignCenter().Text(L("Column_Done")).SemiBold();
            });

            foreach (var part in parts.OrderBy(p => p.Kind).ThenBy(p => p.Number))
            {
                table.Cell().Element(BodyCell).Text(part.Name).SemiBold();
                table.Cell().Element(BodyCell).Text(part.KindLabel);
                table.Cell().Element(BodyCell).Text(part.Type?.Name ?? "-");
                table.Cell().Element(BodyCell).Text(ViewModels.PlateRow.NameOf(part.Plate));
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