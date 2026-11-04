using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>
/// 3D model of a plate as a STEP file (AP214): a block with the plate outline and thickness, a cylindrical hole through
/// it for every receptacle and pin, and every cut-out through it. The side facing the board is at z = 0, the plate extends to
/// z = −thickness. Written as an exact B-rep, so CAD programs import it as a solid; arcs of the outline and the cut-outs
/// stay arcs (circle edges and cylinder walls), not short segments.
/// </summary>
public static class StepWriter
{
    public static string Write(Plate plate, double thickness, string name, DateTime timestamp)
    {
        var w = new EntityWriter();
        var z0 = -thickness;
        const double z1 = 0;

        var holeTopLoops = new List<int>();
        var holeBottomLoops = new List<int>();
        var faces = new List<int>();

        // ----- Block: the outline (counterclockwise) extruded by the thickness, its arcs as cylinder walls -----
        // Arcs split in the drawing are joined first, so each round edge is one wall without a seam
        var contour = OutlineShape.Joined(plate.Contour);
        var (topEdges, bottomEdges) = Walls(w, contour, z0, z1, outer: true, faces);
        var n = contour.Count;

        // ----- Holes -----

        foreach (var hole in plate.Holes)
        {
            var r = hole.Diameter / 2;
            var vTop = w.Vertex(hole.X + r, hole.Y, z1);
            var vBottom = w.Vertex(hole.X + r, hole.Y, z0);
            var circleTop = w.Add($"CIRCLE('',{w.Placement(hole.X, hole.Y, z1, (0, 0, 1), (1, 0, 0))},{R(r)})");
            var circleBottom = w.Add($"CIRCLE('',{w.Placement(hole.X, hole.Y, z0, (0, 0, 1), (1, 0, 0))},{R(r)})");

            // Circles run counterclockwise about +Z, the seam runs upwards
            var edgeTop = w.Add($"EDGE_CURVE('',{Ref(vTop)},{Ref(vTop)},{Ref(circleTop)},.T.)");
            var edgeBottom = w.Add($"EDGE_CURVE('',{Ref(vBottom)},{Ref(vBottom)},{Ref(circleBottom)},.T.)");
            var seam = w.LineEdge(vBottom, vTop, (hole.X + r, hole.Y), (hole.X + r, hole.Y), z0, z1);

            // Inner loops are clockwise about the face normal: top face normal +Z, bottom face normal −Z
            holeTopLoops.Add(w.Loop((edgeTop, false)));
            holeBottomLoops.Add(w.Loop((edgeBottom, true)));

            // The wall of the hole faces the axis, opposite to the normal of the cylinder surface
            var cylinder = w.Add($"CYLINDRICAL_SURFACE('',{w.Placement(hole.X, hole.Y, z0, (0, 0, 1), (1, 0, 0))},{R(r)})");
            var wallLoop = w.Loop((seam, true), (edgeTop, true), (seam, false), (edgeBottom, false));
            faces.Add(w.Add($"ADVANCED_FACE('',({Ref(w.Add($"FACE_OUTER_BOUND('',{Ref(wallLoop)},.T.)"))}),{Ref(cylinder)},.F.)"));
        }

        // ----- Cut-outs and merged openings: a counterclockwise loop each; the material is outside it -----
        foreach (var cutout in plate.CutContours.Where(c => OutlineShape.Flatten(c).Count >= 3).Select(OutlineShape.Joined))
        {
            var m = cutout.Count;
            var (cTopEdges, cBottomEdges) = Walls(w, cutout, z0, z1, outer: false, faces);

            // Inner loops run clockwise about the face normal: backwards on the top face (+Z), forwards on the bottom (−Z)
            holeTopLoops.Add(w.Loop(Enumerable.Range(0, m).Reverse().Select(i => (cTopEdges[i], false)).ToArray()));
            holeBottomLoops.Add(w.Loop(Enumerable.Range(0, m).Select(i => (cBottomEdges[i], true)).ToArray()));
        }

        // ----- Faces of the block, normals pointing out of the material -----
        // Top face counterclockwise about +Z, bottom face the other way round (its normal is −Z)
        var topLoop = w.Loop(Enumerable.Range(0, n).Select(i => (topEdges[i], true)).ToArray());
        faces.Add(w.PlaneFace(topLoop, holeTopLoops, (plate.MinX, plate.MinY, z1), (0, 0, 1), (1, 0, 0)));

        var bottomLoop = w.Loop(Enumerable.Range(0, n).Reverse().Select(i => (bottomEdges[i], false)).ToArray());
        faces.Add(w.PlaneFace(bottomLoop, holeBottomLoops, (plate.MinX, plate.MinY, z0), (0, 0, -1), (1, 0, 0)));

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

    /// <summary>
    /// A counterclockwise loop extruded from z0 to z1: its edges at the top and the bottom (edge i runs from vertex i to
    /// i+1, a line or, with a bulge, an arc) and a wall per edge, added to the faces. A straight edge gets a plane wall,
    /// an arc a cylinder wall. The walls of the outline face out of the block (the edge direction turned right), the
    /// walls of a cut-out into the cut-out (turned left).
    /// </summary>
    private static (int[] Top, int[] Bottom) Walls(EntityWriter w, IReadOnlyList<OutlineVertex> loop, double z0, double z1,
        bool outer, List<int> faces)
    {
        var n = loop.Count;
        var top = loop.Select(v => w.Vertex(v.X, v.Y, z1)).ToArray();
        var bottom = loop.Select(v => w.Vertex(v.X, v.Y, z0)).ToArray();

        var topEdges = new int[n];
        var bottomEdges = new int[n];
        var verticalEdges = new int[n];
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            topEdges[i] = w.ContourEdge(top[i], top[j], loop[i], loop[j], z1);
            bottomEdges[i] = w.ContourEdge(bottom[i], bottom[j], loop[i], loop[j], z0);
            verticalEdges[i] = w.LineEdge(bottom[i], top[i], (loop[i].X, loop[i].Y), (loop[i].X, loop[i].Y), z0, z1);
        }

        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            var (a, b) = (loop[i], loop[j]);

            // Counterclockwise about the wall's normal; a cut-out wall faces the other way, so its loop runs backwards
            var wallLoop = outer
                ? w.Loop((bottomEdges[i], true), (verticalEdges[j], true), (topEdges[i], false), (verticalEdges[i], false))
                : w.Loop((verticalEdges[i], true), (topEdges[i], true), (verticalEdges[j], false), (bottomEdges[i], false));

            var (cx, cy, radius, start, sweep) = OutlineShape.ArcGeometry(a, b);
            if (radius > 0)
            {
                // The cylinder's normal points away from its axis. A counterclockwise arc has its center on the left,
                // inside the loop: the outline's wall faces away from the axis, a cut-out's wall towards it. A clockwise
                // arc the other way round. The seam of the cylinder is put opposite the arc, else CAD programs split
                // the wall where it crosses the seam (e.g. a slot's end into two quarter circles).
                var cylinder = w.Add($"CYLINDRICAL_SURFACE('',{w.Placement(cx, cy, z0, (0, 0, 1), AwayFrom(start, sweep))},{R(radius)})");
                faces.Add(w.SurfaceFace(wallLoop, cylinder, sameSense: sweep > 0 == outer));
                continue;
            }

            var (dx, dy) = (b.X - a.X, b.Y - a.Y);
            var length = Math.Sqrt(dx * dx + dy * dy);
            var normal = outer ? (dy / length, -dx / length, 0.0) : (-dy / length, dx / length, 0.0);
            faces.Add(w.PlaneFace(wallLoop, [], (a.X, a.Y, z0), normal, (dx / length, dy / length, 0)));
        }

        return (topEdges, bottomEdges);
    }

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