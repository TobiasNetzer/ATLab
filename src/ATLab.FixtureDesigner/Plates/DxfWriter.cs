using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>
/// Plate in DXF R12 (millimeters) for CAM and CAD, which every program reads: only what is machined, so nothing else
/// gets cut or engraved by mistake. The plate outline is one closed polyline, the contour to cut; the holes are
/// circles, on a layer per kind so each can get its tool. Pockets of an imported outline are on a layer of their own,
/// their depth is set in CAM; lines of the imported drawing that belong to no shape are kept on another one. Names of the
/// holes and the board are in the build sheet.
/// </summary>
public static class DxfWriter
{
    public const string PlateLayer = "PLATE_OUTLINE";
    public const string ProbeLayer = "PROBE_HOLES";
    public const string ProbeOpeningLayer = "PROBE_OPENINGS";
    public const string LocatingPinLayer = "LOCATING_PIN_HOLES";
    public const string FrameLayer = "FRAME_HOLES";
    public const string PreCenteringPinLayer = "PRE_CENTERING_PINS";
    public const string SupportPinLayer = "SUPPORT_PINS";
    public const string PushRodLayer = "PUSH_RODS";
    public const string PushRodOpeningLayer = "PUSH_ROD_OPENINGS";
    public const string CutoutLayer = "CUTOUTS";
    public const string PocketLayer = "POCKETS";
    public const string OtherLayer = "OTHER_GEOMETRY";

    /// <summary>The layer of a kind of hole, the same in the DXF and the SVG.</summary>
    public static string LayerOf(PlateHoleKind kind) => kind switch
    {
        PlateHoleKind.Probe => ProbeLayer,
        PlateHoleKind.ProbeOpening => ProbeOpeningLayer,
        PlateHoleKind.LocatingPin => LocatingPinLayer,
        PlateHoleKind.Frame => FrameLayer,
        PlateHoleKind.PreCenteringPin => PreCenteringPinLayer,
        PlateHoleKind.SupportPin => SupportPinLayer,
        PlateHoleKind.PushRodOpening => PushRodOpeningLayer,
        _ => PushRodLayer
    };

    // Every layer black (AutoCAD color 7, shown white on a dark background): the layers tell the kinds apart
    private const int LayerColor = 7;

    private static readonly string[] Layers =
    [
        PlateLayer, ProbeLayer, LocatingPinLayer, FrameLayer, PreCenteringPinLayer, SupportPinLayer, PushRodLayer, CutoutLayer,
        ProbeOpeningLayer, PushRodOpeningLayer, PocketLayer, OtherLayer
    ];

    /// <summary>The layer of a hole: a pocket on the pockets', else that of its kind.</summary>
    public static string LayerOf(PlateHole hole) => hole.Pocket is null ? LayerOf(hole.Kind) : PocketLayer;

    /// <summary>The layer of a cut-out: through or pocket.</summary>
    public static string LayerOf(OutlineCutout cutout) => cutout.Pocket is null ? CutoutLayer : PocketLayer;

    public static string Write(Plate plate)
    {
        var dxf = new StringBuilder();

        void Group(int code, string value) => dxf.Append(code).Append('\n').Append(value).Append('\n');
        void Number(int code, double value) => Group(code, value.ToString("0.######", CultureInfo.InvariantCulture));

        // Header: version and drawing extents
        Group(0, "SECTION");
        Group(2, "HEADER");
        Group(9, "$ACADVER");
        Group(1, "AC1009");
        Group(9, "$EXTMIN");
        Number(10, plate.MinX);
        Number(20, plate.MinY);
        Group(9, "$EXTMAX");
        Number(10, plate.MaxX);
        Number(20, plate.MaxY);
        Group(0, "ENDSEC");

        // Line type and layers
        Group(0, "SECTION");
        Group(2, "TABLES");
        Group(0, "TABLE");
        Group(2, "LTYPE");
        Group(70, "1");
        Group(0, "LTYPE");
        Group(2, "CONTINUOUS");
        Group(70, "0");
        Group(3, "Solid line");
        Group(72, "65");
        Group(73, "0");
        Number(40, 0);
        Group(0, "ENDTAB");
        Group(0, "TABLE");
        Group(2, "LAYER");
        Group(70, Layers.Length.ToString(CultureInfo.InvariantCulture));
        foreach (var name in Layers)
        {
            Group(0, "LAYER");
            Group(2, name);
            Group(70, "0");
            Group(62, LayerColor.ToString(CultureInfo.InvariantCulture));
            Group(6, "CONTINUOUS");
        }

        Group(0, "ENDTAB");
        Group(0, "ENDSEC");

        Group(0, "SECTION");
        Group(2, "ENTITIES");

        // The plate outline and every cut-out as one closed polyline each (R12 POLYLINE, flag 1 = closed), so CAM takes
        // each as one contour; arcs stay arcs: the bulge of a vertex (group 42) bends the edge to the next one
        void Contour(IReadOnlyList<OutlineVertex> vertices, string layer, bool closed = true)
        {
            Group(0, "POLYLINE");
            Group(8, layer);
            Group(66, "1");
            Number(10, 0);
            Number(20, 0);
            Number(30, 0);
            Group(70, closed ? "1" : "0");
            foreach (var vertex in vertices)
            {
                Group(0, "VERTEX");
                Group(8, layer);
                Number(10, vertex.X);
                Number(20, vertex.Y);
                Number(30, 0);
                if (Math.Abs(vertex.Bulge) > 1e-12)
                    Group(42, vertex.Bulge.ToString("0.##########", CultureInfo.InvariantCulture));
            }

            Group(0, "SEQEND");
            Group(8, layer);
        }

        Contour(plate.Contour, PlateLayer);
        foreach (var cutout in plate.Cutouts)
            Contour(cutout.Vertices, LayerOf(cutout));

        // Loose lines as they were drawn, open; a closed one ends at its first point again
        foreach (var piece in plate.Loose)
            Contour(piece, OtherLayer, closed: false);

        // Merged openings on the layer of the openings, milled along their outline
        foreach (var opening in plate.MergedOpenings)
            Contour(opening.Contour, LayerOf(plate.OpeningKind));

        foreach (var hole in plate.Holes)
        {
            Group(0, "CIRCLE");
            Group(8, LayerOf(hole));
            Number(10, hole.X);
            Number(20, hole.Y);
            Number(30, 0);
            Number(40, hole.Diameter / 2);
        }

        Group(0, "ENDSEC");
        Group(0, "EOF");
        return dxf.ToString();
    }

    public static void Save(Plate plate, string path) => File.WriteAllText(path, Write(plate), new UTF8Encoding(false));
}