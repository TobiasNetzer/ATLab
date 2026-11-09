using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ATLab.Pcb.Fixture;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>
/// 3D model of a plate as a STEP file (AP214): a block with the plate outline and thickness, a cylindrical hole through
/// it for every receptacle and pin, every cut-out through it and every pocket milled to its depth from its side. The
/// side facing the board is at z = 0, the plate extends to z = −thickness. Written as an exact B-rep, so CAD programs
/// import it as a solid; arcs of the outline, the cut-outs and the pockets stay arcs (circle edges and cylinder walls).
/// A printed pin is part of that solid: it stands on the side facing the board, from z = 0 up.
/// </summary>
/// <remarks>
/// The outline and the borders of the holes, cut-outs and pockets divide the plate into areas (a
/// <see cref="PlanarGraph"/>). Over each area the material is one span of z, or none: the block less what the holes,
/// cut-outs and pockets over it take away. Each area gets a top and a bottom face at the ends of its span, each edge
/// between two areas a wall where one side has material and the other not, and the vertical edges at a node are split
/// at every height a wall there ends at, so every edge of the solid joins two faces.
/// </remarks>
public static class StepWriter
{
    public static string Write(Plate plate, double thickness, string name, DateTime timestamp)
    {
        var w = new EntityWriter();
        var faces = Solid(w, plate, -thickness, 0);

        // ----- Solid and product structure -----
        var shell = w.Add($"CLOSED_SHELL('',({string.Join(",", faces.Select(Ref))}))");
        var solid = w.Add($"MANIFOLD_SOLID_BREP('{Text(name)}',{Ref(shell)})");

        var lengthUnit = w.Add("(LENGTH_UNIT()NAMED_UNIT(*)SI_UNIT(.MILLI.,.METRE.))");
        var angleUnit = w.Add("(NAMED_UNIT(*)PLANE_ANGLE_UNIT()SI_UNIT($,.RADIAN.))");
        var solidAngleUnit = w.Add("(NAMED_UNIT(*)SI_UNIT($,.STERADIAN.)SOLID_ANGLE_UNIT())");
        var uncertainty = w.Add($"UNCERTAINTY_MEASURE_WITH_UNIT(LENGTH_MEASURE(1.E-07),{Ref(lengthUnit)},'distance_accuracy_value','confusion accuracy')");
        var context = w.Add($"(GEOMETRIC_REPRESENTATION_CONTEXT(3)GLOBAL_UNCERTAINTY_ASSIGNED_CONTEXT(({Ref(uncertainty)}))" +
                            $"GLOBAL_UNIT_ASSIGNED_CONTEXT(({Ref(lengthUnit)},{Ref(angleUnit)},{Ref(solidAngleUnit)}))REPRESENTATION_CONTEXT('Context','3D'))");
        var origin = w.Placement(0, 0, 0, (0, 0, 1), (1, 0, 0));
        var representation = w.Add($"ADVANCED_BREP_SHAPE_REPRESENTATION('',({origin},{Ref(solid)}),{Ref(context)})");

        var application = w.Add("APPLICATION_CONTEXT('automotive design')");
        w.Add($"APPLICATION_PROTOCOL_DEFINITION('international standard','automotive_design',2000,{Ref(application)})");
        var productContext = w.Add($"PRODUCT_CONTEXT('',{Ref(application)},'mechanical')");
        var product = w.Add($"PRODUCT('{Text(name)}','{Text(name)}','',({Ref(productContext)}))");
        w.Add($"PRODUCT_RELATED_PRODUCT_CATEGORY('part',$,({Ref(product)}))");
        var formation = w.Add($"PRODUCT_DEFINITION_FORMATION('','',{Ref(product)})");
        var definitionContext = w.Add($"PRODUCT_DEFINITION_CONTEXT('part definition',{Ref(application)},'design')");
        var definition = w.Add($"PRODUCT_DEFINITION('design','',{Ref(formation)},{Ref(definitionContext)})");
        var shape = w.Add($"PRODUCT_DEFINITION_SHAPE('','',{Ref(definition)})");
        w.Add($"SHAPE_DEFINITION_REPRESENTATION({Ref(shape)},{Ref(representation)})");

        var file = new StringBuilder();
        file.Append("ISO-10303-21;\nHEADER;\n");
        file.Append($"FILE_DESCRIPTION(('{Text(name)}'),'2;1');\n");
        file.Append($"FILE_NAME('{Text(name)}.step','{timestamp:yyyy-MM-ddTHH:mm:ss}',(''),(''),'ATLab Fixture Designer','ATLab Fixture Designer','');\n");
        file.Append("FILE_SCHEMA(('AUTOMOTIVE_DESIGN { 1 0 10303 214 1 1 1 1 }'));\nENDSEC;\nDATA;\n");
        file.Append(w.Data);
        file.Append("ENDSEC;\nEND-ISO-10303-21;\n");
        return file.ToString();
    }

    /// <summary>The material over an area, from Lo to Hi; none if they meet.</summary>
    private readonly record struct Span(double Lo, double Hi)
    {
        public static Span None => new(0, 0);
        public bool IsEmpty => Hi - Lo < 1e-9;
    }

    /// <summary>The faces of the plate's solid, from z0 (the far side) to z1 (the side facing the board).</summary>
    private static List<int> Solid(EntityWriter w, Plate plate, double z0, double z1)
    {
        var thickness = z1 - z0;

        // What a hole, cut-out or pocket takes away: the whole thickness, or its depth from its side
        (double Lo, double Hi) Removal(Pocket? pocket) => pocket switch
        {
            null => (z0, z1),
            { Depth: var d } when d >= thickness - 1e-9 => (z0, z1),
            { Side: PocketSide.BoardSide, Depth: var d } => (z1 - Math.Max(d, 0), z1),
            { Depth: var d } => (z0, z0 + Math.Max(d, 0))
        };

        var pieces = new List<IReadOnlyList<OutlineVertex>> { PlanarGraph.Closed(plate.Contour) };
        var removals = new List<(List<(double X, double Y)> Points, double Lo, double Hi)>();
        void Remove(IReadOnlyList<OutlineVertex> loop, Pocket? pocket)
        {
            pieces.Add(PlanarGraph.Closed(loop));
            var (lo, hi) = Removal(pocket);
            removals.Add((OutlineShape.Flatten(loop), lo, hi));
        }

        foreach (var cutout in plate.Cutouts.Where(c => c.Vertices.Count >= 2))
            Remove(cutout.Vertices, cutout.Pocket);
        foreach (var opening in plate.MergedOpenings)
            Remove(opening.Contour, null);
        foreach (var hole in plate.Holes.Where(h => h.Diameter > 0 && h.Pin is null))
            Remove(PlanarGraph.Circle(hole.X, hole.Y, hole.Diameter).Take(2).ToList(), hole.Pocket);

        // A printed pin takes nothing away: its foot is only a border on the plate's face
        var pins = plate.Holes.Where(h => h.Pin is not null).ToList();
        foreach (var pin in pins)
            pieces.Add(PlanarGraph.Closed(PlanarGraph.Circle(pin.X, pin.Y, pin.Diameter).Take(2).ToList()));

        var graph = PlanarGraph.Build(pieces, 1e-4);
        var loops = graph.Loops();

        // The areas: each counterclockwise loop borders one; a clockwise loop is an inner border of the smallest area
        // around the points on its left, or of none (outside the plate)
        var areas = loops.Where(l => l.Area > 0).ToList();
        var holesOf = areas.Select(_ => new List<PlanarGraph.Loop>()).ToList();
        foreach (var loop in loops.Where(l => l.Area < 0))
        {
            if (PlanarGraph.PointOnLeft(loop.Points, q => !OutlineShape.Contains(loop.Points, q.X, q.Y)) is not { } p)
                continue;

            var around = -1;
            for (var i = 0; i < areas.Count; i++)
            {
                if (OutlineShape.Contains(areas[i].Points, p.X, p.Y) && (around < 0 || areas[i].Area < areas[around].Area))
                    around = i;
            }

            if (around >= 0)
                holesOf[around].Add(loop);
        }

        // The foot of each pin, an area of its own
        var pinOf = new PlateHole?[areas.Count];
        foreach (var pin in pins)
        {
            var foot = areas.FindIndex(a => a.HalfEdges.Count == 2 && a.HalfEdges.All(h =>
            {
                var (x, y) = graph.Nodes[graph.Origin(h)];
                return Math.Abs(Math.Sqrt((x - pin.X) * (x - pin.X) + (y - pin.Y) * (y - pin.Y)) - pin.Diameter / 2) < 1e-3;
            }));
            if (foot < 0)
                throw new InvalidOperationException($"The foot of {pin.Name} is cut by another shape of the plate.");
            pinOf[foot] = pin;
        }

        var footEdges = Enumerable.Range(0, areas.Count).Where(i => pinOf[i] is not null)
            .SelectMany(i => areas[i].HalfEdges.Select(h => h / 2)).ToHashSet();

        // The material over each area: the block less everything that covers it
        var spans = new Span[areas.Count];
        for (var i = 0; i < areas.Count; i++)
        {
            var area = areas[i];
            var holes = holesOf[i];
            var p = PlanarGraph.PointOnLeft(area.Points, q =>
                OutlineShape.Contains(area.Points, q.X, q.Y) && !holes.Any(h => OutlineShape.Contains(h.Points, q.X, q.Y)));
            if (p is not { } point || !OutlineShape.Contains(plate.Outline, point.X, point.Y))
            {
                spans[i] = Span.None;
                continue;
            }

            var (lo, hi) = (z0, z1);
            foreach (var removal in removals.Where(r => OutlineShape.Contains(r.Points, point.X, point.Y)))
            {
                if (removal.Lo <= z0 + 1e-9 && removal.Hi >= z1 - 1e-9)
                    (lo, hi) = (z1, z1);
                else if (removal.Lo <= z0 + 1e-9)
                    lo = Math.Max(lo, removal.Hi);
                else
                    hi = Math.Min(hi, removal.Lo);
            }

            spans[i] = new Span(lo, hi);
        }

        // The area on the left of each half edge; none for the outside and for lines with the same area on both sides
        var areaOf = Enumerable.Repeat(-1, 2 * graph.Edges.Count).ToArray();
        for (var i = 0; i < areas.Count; i++)
        {
            foreach (var h in holesOf[i].Prepend(areas[i]).SelectMany(l => l.HalfEdges))
                areaOf[h] = i;
        }

        Span SpanOf(int halfEdge) => areaOf[halfEdge] is var a and >= 0 ? spans[a] : Span.None;

        // ----- Vertices and edges, each made once and used by the two faces it joins -----

        static long Level(double z) => (long)Math.Round(z * 1e6);
        var vertices = new Dictionary<(int, long), int>();
        var horizontal = new Dictionary<(int, long), int>();
        var vertical = new Dictionary<(int, long, long), int>();

        int Vertex(int node, double z)
        {
            if (!vertices.TryGetValue((node, Level(z)), out var id))
                vertices[(node, Level(z))] = id = w.Vertex(graph.Nodes[node].X, graph.Nodes[node].Y, z);
            return id;
        }

        // An edge of the graph at height z, from A to B
        int Horizontal(int edge, double z)
        {
            if (horizontal.TryGetValue((edge, Level(z)), out var id))
                return id;

            var (a, b) = graph.Ends(edge);
            return horizontal[(edge, Level(z))] = w.ContourEdge(Vertex(graph.Edges[edge].A, z), Vertex(graph.Edges[edge].B, z), a, b, z);
        }

        // ----- Walls: where one side of an edge has material and the other not -----

        static IEnumerable<(double Lo, double Hi)> Minus(Span a, Span b)
        {
            if (a.IsEmpty)
                yield break;
            if (b.IsEmpty)
            {
                yield return (a.Lo, a.Hi);
                yield break;
            }

            if (a.Lo < b.Lo - 1e-9)
                yield return (a.Lo, Math.Min(a.Hi, b.Lo));
            if (a.Hi > b.Hi + 1e-9)
                yield return (Math.Max(a.Lo, b.Hi), a.Hi);
        }

        var walls = new List<(int Edge, double Lo, double Hi, bool MaterialLeft)>();
        for (var e = 0; e < graph.Edges.Count; e++)
        {
            var (left, right) = (SpanOf(2 * e), SpanOf(2 * e + 1));
            walls.AddRange(Minus(left, right).Select(s => (e, s.Lo, s.Hi, true)));
            walls.AddRange(Minus(right, left).Select(s => (e, s.Lo, s.Hi, false)));
        }

        // The heights the walls at each node end at: their vertical edges there are split at each
        var levels = new Dictionary<int, SortedSet<double>>();
        foreach (var (e, lo, hi, _) in walls)
        {
            foreach (var node in new[] { graph.Edges[e].A, graph.Edges[e].B })
            {
                if (!levels.TryGetValue(node, out var set))
                    levels[node] = set = [];
                set.Add(lo);
                set.Add(hi);
            }
        }

        // The vertical edges at a node from lo up to hi, each running upwards
        List<int> Upwards(int node, double lo, double hi)
        {
            var heights = levels[node].Where(z => z >= lo - 1e-9 && z <= hi + 1e-9).ToList();
            var edges = new List<int>();
            for (var i = 0; i + 1 < heights.Count; i++)
            {
                var key = (node, Level(heights[i]), Level(heights[i + 1]));
                if (!vertical.TryGetValue(key, out var id))
                {
                    var p = graph.Nodes[node];
                    vertical[key] = id = w.LineEdge(Vertex(node, heights[i]), Vertex(node, heights[i + 1]), p, p, heights[i], heights[i + 1]);
                }

                edges.Add(id);
            }

            return edges;
        }

        var faces = new List<int>();
        foreach (var (e, lo, hi, materialLeft) in walls)
        {
            var edge = graph.Edges[e];
            var (a, b) = graph.Ends(e);
            var (atA, atB) = (Upwards(edge.A, lo, hi), Upwards(edge.B, lo, hi));

            // Counterclockwise seen from outside the material: with the material on the left of A→B along the bottom,
            // up at B, back along the top and down at A; with it on the right the other way round
            (int, bool)[] sides = materialLeft
                ?
                [
                    (Horizontal(e, lo), true), .. atB.Select(v => (v, true)),
                    (Horizontal(e, hi), false), .. Enumerable.Reverse(atA).Select(v => (v, false))
                ]
                :
                [
                    .. atA.Select(v => (v, true)), (Horizontal(e, hi), true),
                    .. Enumerable.Reverse(atB).Select(v => (v, false)), (Horizontal(e, lo), false)
                ];
            var loop = w.Loop(sides);

            var (cx, cy, radius, start, sweep) = OutlineShape.ArcGeometry(a, b);
            if (radius > 0)
            {
                // The cylinder's normal points away from its axis. A counterclockwise arc has its center on the left:
                // with the material there, the wall faces away from the axis. The seam of the cylinder is put opposite
                // the arc, else CAD programs split the wall where it crosses the seam.
                var cylinder = w.Add($"CYLINDRICAL_SURFACE('',{w.Placement(cx, cy, lo, (0, 0, 1), AwayFrom(start, sweep))},{R(radius)})");
                faces.Add(w.SurfaceFace(loop, cylinder, sameSense: sweep > 0 == materialLeft));
                continue;
            }

            var (dx, dy) = (b.X - a.X, b.Y - a.Y);
            var length = Math.Sqrt(dx * dx + dy * dy);
            var normal = materialLeft ? (dy / length, -dx / length, 0.0) : (-dy / length, dx / length, 0.0);
            faces.Add(w.PlaneFace(loop, [], (a.X, a.Y, lo), normal, (dx / length, dy / length, 0)));
        }

        // ----- Top and bottom of each area with material -----
        // The borders have the area on their left: counterclockwise about +Z for the top face as they are, backwards
        // for the bottom face (its normal is −Z)
        for (var i = 0; i < areas.Count; i++)
        {
            if (spans[i].IsEmpty)
                continue;

            if (pinOf[i] is { } pin)
            {
                AddPin(areas[i], pin);
                continue;
            }

            var borders = holesOf[i].Prepend(areas[i]).ToList();
            var (lo, hi) = (spans[i].Lo, spans[i].Hi);
            var tops = borders.Select(l => w.Loop(l.HalfEdges.Select(h => (Horizontal(h / 2, hi), h % 2 == 0)).ToArray())).ToList();
            faces.Add(w.PlaneFace(tops[0], tops.Skip(1).ToList(), (plate.MinX, plate.MinY, hi), (0, 0, 1), (1, 0, 0)));

            // The far side runs on under the pins: their feet are no border there
            var bottoms = borders.Where(l => !l.HalfEdges.All(h => footEdges.Contains(h / 2)))
                .Select(l => w.Loop(Enumerable.Reverse(l.HalfEdges).Select(h => (Horizontal(h / 2, lo), h % 2 == 1)).ToArray())).ToList();
            faces.Add(w.PlaneFace(bottoms[0], bottoms.Skip(1).ToList(), (plate.MinX, plate.MinY, lo), (0, 0, -1), (1, 0, 0)));
        }

        return faces;

        // ----- Printed pins -----

        // The faces of a pin on its foot, each round face in two halves like the walls of a round hole. Every station of
        // its profile is a ring of two half circles between two points opposite each other; the foot's ring is the border
        // on the plate's face.
        void AddPin(PlanarGraph.Loop foot, PlateHole hole)
        {
            var profile = hole.Pin!;
            var (cx, cy) = (hole.X, hole.Y);
            var (h0, h1) = (foot.HalfEdges[0], foot.HalfEdges[1]);
            var (n0, n1) = (graph.Origin(h0), graph.Origin(h1));
            var angle = Math.Atan2(graph.Nodes[n0].Y - cy, graph.Nodes[n0].X - cx);
            var (ux, uy) = (Math.Cos(angle), Math.Sin(angle));
            (double X, double Y) Side(double radius, int sign) => (cx + sign * radius * ux, cy + sign * radius * uy);
            static (int, bool) Back((int Edge, bool Forward) e) => (e.Edge, !e.Forward);

            var rings = new List<PinRing> { new(Vertex(n0, z1), Vertex(n1, z1), (Horizontal(h0 / 2, z1), h0 % 2 == 0), (Horizontal(h1 / 2, z1), h1 % 2 == 0)) };
            foreach (var station in profile.Stations.Skip(1))
            {
                var z = z1 + station.Z;
                var (a, b) = (Side(station.Radius, 1), Side(station.Radius, -1));
                var (va, vb) = (w.Vertex(a.X, a.Y, z), w.Vertex(b.X, b.Y, z));
                rings.Add(new PinRing(va, vb,
                    (w.ContourEdge(va, vb, new OutlineVertex(a.X, a.Y, 1), new OutlineVertex(b.X, b.Y, 0), z), true),
                    (w.ContourEdge(vb, va, new OutlineVertex(b.X, b.Y, 1), new OutlineVertex(a.X, a.Y, 0), z), true)));
            }

            // The seam of a round surface is put opposite the half it carries, as for the walls
            var away = new[] { (uy, -ux, 0.0), (-uy, ux, 0.0) };
            for (var i = 0; i + 1 < rings.Count; i++)
            {
                var (s, t) = (profile.Stations[i], profile.Stations[i + 1]);
                var (lower, upper) = (rings[i], rings[i + 1]);
                var (zs, zt) = (z1 + s.Z, z1 + t.Z);
                if (Math.Abs(zs - zt) < 1e-9)
                {
                    faces.Add(w.PlaneFace(w.Loop(lower.Arc0, lower.Arc1), [w.Loop(Back(upper.Arc1), Back(upper.Arc0))], (cx, cy, zs), (0, 0, 1), (1, 0, 0)));
                    continue;
                }

                var m0 = w.LineEdge(lower.V0, upper.V0, Side(s.Radius, 1), Side(t.Radius, 1), zs, zt);
                var m1 = w.LineEdge(lower.V1, upper.V1, Side(s.Radius, -1), Side(t.Radius, -1), zs, zt);
                int Surface(int half) => Math.Abs(s.Radius - t.Radius) < 1e-9
                    ? w.Add($"CYLINDRICAL_SURFACE('',{w.Placement(cx, cy, zs, (0, 0, 1), away[half])},{R(s.Radius)})")
                    // A cone opens along its axis: from the narrow end down, so its normal points out and up
                    : w.Add($"CONICAL_SURFACE('',{w.Placement(cx, cy, zt, (0, 0, -1), away[half])},{R(t.Radius)},{R(Math.Atan2(s.Radius - t.Radius, zt - zs))})");

                faces.Add(w.SurfaceFace(w.Loop(lower.Arc0, (m1, true), Back(upper.Arc0), (m0, false)), Surface(0), sameSense: true));
                faces.Add(w.SurfaceFace(w.Loop(lower.Arc1, (m0, true), Back(upper.Arc1), (m1, false)), Surface(1), sameSense: true));
            }

            var last = rings[^1];
            var end = profile.Stations[^1];
            var endZ = z1 + end.Z;
            if (!profile.HasRoundTip)
            {
                faces.Add(w.PlaneFace(w.Loop(last.Arc0, last.Arc1), [], (cx, cy, endZ), (0, 0, 1), (1, 0, 0)));
                return;
            }

            // A half sphere in two quarters, parted by a half circle over the top. The sphere's axis is tilted so that
            // neither its poles nor its seam fall on the quarter it carries, which readers would have to split there.
            var (ex, ey) = ((ux, uy, 0.0), (-uy, ux, 0.0));
            var parting = w.Add($"CIRCLE('',{w.Placement(cx, cy, endZ, (uy, -ux, 0.0), ex)},{R(end.Radius)})");
            var over = w.Add($"EDGE_CURVE('',{Ref(last.V0)},{Ref(last.V1)},{Ref(parting)},.T.)");
            var (rt2, rt3) = (Math.Sqrt(2), Math.Sqrt(3));
            int Quarter(int sign)
            {
                var axis = (sign * (ex.Item1 + ey.Item1) / rt3, sign * (ex.Item2 + ey.Item2) / rt3, -1 / rt3);
                var reference = (-sign * ey.Item1 / rt2, -sign * ey.Item2 / rt2, -1 / rt2);
                return w.Add($"SPHERICAL_SURFACE('',{w.Placement(cx, cy, endZ, axis, reference)},{R(end.Radius)})");
            }

            faces.Add(w.SurfaceFace(w.Loop(last.Arc0, (over, false)), Quarter(1), sameSense: true));
            faces.Add(w.SurfaceFace(w.Loop(last.Arc1, (over, true)), Quarter(-1), sameSense: true));
        }
    }

    /// <summary>A circle of a pin: two points opposite each other and the half circles V0 to V1 and V1 to V0, both counterclockwise.</summary>
    private readonly record struct PinRing(int V0, int V1, (int Edge, bool Forward) Arc0, (int Edge, bool Forward) Arc1);

    /// <summary>
    /// Reference direction of an arc's circle and cylinder: from the center away from the middle of the arc, so the
    /// parameter seam at that direction lies outside the arc (any arc short of a full circle).
    /// </summary>
    private static (double, double, double) AwayFrom(double start, double sweep)
    {
        var opposite = start + sweep / 2 + Math.PI;
        return (Math.Cos(opposite), Math.Sin(opposite), 0);
    }

    public static void Save(Plate plate, double thickness, string name, string path) =>
        File.WriteAllText(path, Write(plate, thickness, name, DateTime.Now), new UTF8Encoding(false));

    /// <summary>STEP reals always have a decimal point.</summary>
    private static string R(double value)
    {
        if (Math.Abs(value) < 5e-10)
            value = 0;
        return value.ToString("0.0#########", CultureInfo.InvariantCulture);
    }

    private static string Ref(int id) => "#" + id.ToString(CultureInfo.InvariantCulture);

    /// <summary>Apostrophes are doubled; other characters outside ASCII are replaced, which every reader accepts.</summary>
    private static string Text(string text)
    {
        var builder = new StringBuilder();
        foreach (var c in text)
            builder.Append(c == '\'' ? "''" : c is >= ' ' and <= '~' && c != '\\' ? c.ToString() : "_");
        return builder.ToString();
    }

    /// <summary>Numbers the entities and writes them as #n=...;</summary>
    private sealed class EntityWriter
    {
        private readonly StringBuilder _data = new();
        private int _next = 1;

        public string Data => _data.ToString();

        public int Add(string entity)
        {
            var id = _next++;
            _data.Append('#').Append(id.ToString(CultureInfo.InvariantCulture)).Append('=').Append(entity).Append(";\n");
            return id;
        }

        private int Point(double x, double y, double z) => Add($"CARTESIAN_POINT('',({R(x)},{R(y)},{R(z)}))");

        private int Direction((double X, double Y, double Z) d) => Add($"DIRECTION('',({R(d.X)},{R(d.Y)},{R(d.Z)}))");

        public int Vertex(double x, double y, double z) => Add($"VERTEX_POINT('',{Ref(Point(x, y, z))})");

        public string Placement(double x, double y, double z, (double, double, double) axis, (double, double, double) reference) =>
            Ref(Add($"AXIS2_PLACEMENT_3D('',{Ref(Point(x, y, z))},{Ref(Direction(axis))},{Ref(Direction(reference))})"));

        /// <summary>Straight edge from one vertex to another.</summary>
        public int LineEdge(int from, int to, (double X, double Y) a, (double X, double Y) b, double za, double zb)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var dz = zb - za;
            var length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var vector = Add($"VECTOR('',{Ref(Direction((dx / length, dy / length, dz / length)))},{R(length)})");
            var line = Add($"LINE('',{Ref(Point(a.X, a.Y, za))},{Ref(vector)})");
            return Add($"EDGE_CURVE('',{Ref(from)},{Ref(to)},{Ref(line)},.T.)");
        }

        /// <summary>
        /// Edge of a contour in the plane z from vertex a to b: straight, or with a's bulge an arc, which runs
        /// counterclockwise about +Z for a positive bulge and clockwise for a negative one.
        /// </summary>
        public int ContourEdge(int from, int to, OutlineVertex a, OutlineVertex b, double z)
        {
            var (cx, cy, radius, start, sweep) = OutlineShape.ArcGeometry(a, b);
            if (radius <= 0)
                return LineEdge(from, to, (a.X, a.Y), (b.X, b.Y), z, z);

            var circle = Add($"CIRCLE('',{Placement(cx, cy, z, (0, 0, sweep > 0 ? 1 : -1), AwayFrom(start, sweep))},{R(radius)})");
            return Add($"EDGE_CURVE('',{Ref(from)},{Ref(to)},{Ref(circle)},.T.)");
        }

        /// <summary>Face bounded by one loop on a surface; sameSense false turns the face against the surface normal.</summary>
        public int SurfaceFace(int loop, int surface, bool sameSense) =>
            Add($"ADVANCED_FACE('',({Ref(Add($"FACE_OUTER_BOUND('',{Ref(loop)},.T.)"))}),{Ref(surface)},{(sameSense ? ".T." : ".F.")})");

        /// <summary>Loop of edges, each used forward (true) or backward.</summary>
        public int Loop(params (int Edge, bool Forward)[] edges) =>
            Add($"EDGE_LOOP('',({string.Join(",", edges.Select(e => Ref(Add($"ORIENTED_EDGE('',*,*,{Ref(e.Edge)},{(e.Forward ? ".T." : ".F.")})"))))}))");

        public int PlaneFace(int outerLoop, IReadOnlyList<int> innerLoops, (double X, double Y, double Z) origin,
            (double, double, double) normal, (double, double, double) reference)
        {
            var plane = Add($"PLANE('',{Placement(origin.X, origin.Y, origin.Z, normal, reference)})");
            var bounds = new List<int> { Add($"FACE_OUTER_BOUND('',{Ref(outerLoop)},.T.)") };
            bounds.AddRange(innerLoops.Select(l => Add($"FACE_BOUND('',{Ref(l)},.T.)")));
            return Add($"ADVANCED_FACE('',({string.Join(",", bounds.Select(Ref))}),{Ref(plane)},.T.)");
        }
    }
}
