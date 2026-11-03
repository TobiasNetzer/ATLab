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
/// z = −thickness. Written as an exact B-rep, so CAD programs import it as a solid.
/// </summary>
public static class StepWriter
{
    public static string Write(Plate plate, double thickness, string name, DateTime timestamp)
    {
        var w = new EntityWriter();
        var z0 = -thickness;
        const double z1 = 0;

        // ----- Block: the outline (counterclockwise) extruded by the thickness -----
        var corners = plate.Outline.Select(p => (p.X, p.Y)).ToArray();
        var n = corners.Length;
        var top = corners.Select(c => w.Vertex(c.Item1, c.Item2, z1)).ToArray();
        var bottom = corners.Select(c => w.Vertex(c.Item1, c.Item2, z0)).ToArray();

        // Edges along the outline (i to i+1) at the top and the bottom, and the vertical edges at the corners
        var topEdges = new int[n];
        var bottomEdges = new int[n];
        var verticalEdges = new int[n];
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            topEdges[i] = w.LineEdge(top[i], top[j], corners[i], corners[j], z1, z1);
            bottomEdges[i] = w.LineEdge(bottom[i], bottom[j], corners[i], corners[j], z0, z0);
            verticalEdges[i] = w.LineEdge(bottom[i], top[i], corners[i], corners[i], z0, z1);
        }

        // ----- Holes -----
        var holeTopLoops = new List<int>();
        var holeBottomLoops = new List<int>();
        var faces = new List<int>();

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

        // ----- Cut-outs: a counterclockwise loop each; the material is outside it -----
        foreach (var cutout in plate.CutoutOutlines.Where(c => c.Count >= 3))
        {
            var m = cutout.Count;
            var cTop = cutout.Select(c => w.Vertex(c.X, c.Y, z1)).ToArray();
            var cBottom = cutout.Select(c => w.Vertex(c.X, c.Y, z0)).ToArray();
            var cTopEdges = new int[m];
            var cBottomEdges = new int[m];
            var cVertical = new int[m];
            for (var i = 0; i < m; i++)
            {
                var j = (i + 1) % m;
                cTopEdges[i] = w.LineEdge(cTop[i], cTop[j], cutout[i], cutout[j], z1, z1);
                cBottomEdges[i] = w.LineEdge(cBottom[i], cBottom[j], cutout[i], cutout[j], z0, z0);
                cVertical[i] = w.LineEdge(cBottom[i], cTop[i], cutout[i], cutout[i], z0, z1);
            }

            // Inner loops run clockwise about the face normal: backwards on the top face (+Z), forwards on the bottom (−Z)
            holeTopLoops.Add(w.Loop(Enumerable.Range(0, m).Reverse().Select(i => (cTopEdges[i], false)).ToArray()));
            holeBottomLoops.Add(w.Loop(Enumerable.Range(0, m).Select(i => (cBottomEdges[i], true)).ToArray()));

            // A wall per edge, facing into the cut-out: the edge direction turned left, the loop the other way round
            // than on the outside of the block
            for (var i = 0; i < m; i++)
            {
                var j = (i + 1) % m;
                var loop = w.Loop((cVertical[i], true), (cTopEdges[i], true), (cVertical[j], false), (cBottomEdges[i], false));
                var (dx, dy) = (cutout[j].X - cutout[i].X, cutout[j].Y - cutout[i].Y);
                var length = Math.Sqrt(dx * dx + dy * dy);
                faces.Add(w.PlaneFace(loop, [], (cutout[i].X, cutout[i].Y, z0), (-dy / length, dx / length, 0),
                    (dx / length, dy / length, 0)));
            }
        }

        // ----- Faces of the block, normals pointing out of the material -----
        // Top face counterclockwise about +Z, bottom face the other way round (its normal is −Z)
        var topLoop = w.Loop(Enumerable.Range(0, n).Select(i => (topEdges[i], true)).ToArray());
        faces.Add(w.PlaneFace(topLoop, holeTopLoops, (plate.MinX, plate.MinY, z1), (0, 0, 1), (1, 0, 0)));

        var bottomLoop = w.Loop(Enumerable.Range(0, n).Reverse().Select(i => (bottomEdges[i], false)).ToArray());
        faces.Add(w.PlaneFace(bottomLoop, holeBottomLoops, (plate.MinX, plate.MinY, z0), (0, 0, -1), (1, 0, 0)));

        // A side face per edge; for a counterclockwise outline the outward normal is the edge direction turned right
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            var loop = w.Loop((bottomEdges[i], true), (verticalEdges[j], true), (topEdges[i], false), (verticalEdges[i], false));
            var (dx, dy) = (corners[j].Item1 - corners[i].Item1, corners[j].Item2 - corners[i].Item2);
            var length = Math.Sqrt(dx * dx + dy * dy);
            faces.Add(w.PlaneFace(loop, [], (corners[i].Item1, corners[i].Item2, z0), (dy / length, -dx / length, 0),
                (dx / length, dy / length, 0)));
        }

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