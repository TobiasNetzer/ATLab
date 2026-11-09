using System;
using System.Collections.Generic;
using System.Globalization;
using ATLab.FixtureDesigner.Localization;
using ATLab.Pcb.Fixture;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>A point of a pin's side profile: its radius at the height Z above the plate, in mm.</summary>
public readonly record struct PinStation(double Radius, double Z);

/// <summary>
/// The shape of a pin or rod, round about its axis, from the plate up to its tip: the stations give its side, from one
/// to the next a cylinder, a flat step or a cone. A round tip is a half sphere on the last station.
/// </summary>
public sealed class PinProfile
{
    private const double Tolerance = 1e-6;

    public required IReadOnlyList<PinStation> Stations { get; init; }

    public required bool HasRoundTip { get; init; }

    /// <summary>Ø of the pin where it stands on the plate.</summary>
    public double BaseDiameter => 2 * Stations[0].Radius;

    /// <summary>The shape of a type <see cref="Problems"/> has nothing to report on.</summary>
    public static PinProfile Of(PartType type)
    {
        var (r, h) = (type.Diameter / 2, type.Height);
        var stations = new List<PinStation>();
        void Add(double radius, double z)
        {
            if (stations.Count == 0 || Math.Abs(stations[^1].Radius - radius) > Tolerance || Math.Abs(stations[^1].Z - z) > Tolerance)
                stations.Add(new PinStation(radius, z));
        }

        if (type.HasBody)
        {
            var (body, step) = (type.BodyDiameter / 2, h - type.ThinLength);
            Add(body, 0);
            Add(body, step);
            if (type.IsPushRod)
            {
                Add(r + type.StepChamfer, step);
                Add(r, step + type.StepChamfer);
            }
            else
            {
                Add(r, step);
            }
        }
        else
        {
            Add(r, 0);
        }

        switch (type.TipStyle)
        {
            case PartTipStyle.Chamfer:
                Add(r, h - type.TipSize);
                Add(r - type.TipSize, h);
                break;
            case PartTipStyle.Round:
                Add(r, h - r);
                break;
            default:
                Add(r, h);
                break;
        }

        return new PinProfile { Stations = stations, HasRoundTip = type.TipStyle == PartTipStyle.Round };
    }

    /// <summary>What makes the type's shape impossible, one text each; a part that is not printed needs no size yet.</summary>
    public static List<string> Problems(PartType type)
    {
        var problems = new List<string>();
        var (d, h) = (type.Diameter, type.Height);
        if (d <= 0 || h <= 0)
        {
            if (type.IsPrinted)
                problems.Add(Localizer.Format("Check_PrintNeedsSize", type.Name));
            return problems;
        }

        var r = d / 2;
        var straightFrom = 0.0;
        if (type.HasBody)
        {
            if (type.BodyDiameter <= d + Tolerance)
                problems.Add(Localizer.Format("Check_BodyNotWider", type.Name, Mm(type.BodyDiameter), Mm(d)));

            if (type.ThinLength <= 0 || type.ThinLength >= h - Tolerance)
            {
                problems.Add(Localizer.Format("Check_ThinLength", type.Name, Mm(h)));
            }
            else
            {
                straightFrom = h - type.ThinLength;
                if (type.IsPushRod && type.BodyDiameter > d + Tolerance)
                {
                    var most = Math.Min((type.BodyDiameter - d) / 2, type.ThinLength);
                    if (type.StepChamfer <= 0)
                        problems.Add(Localizer.Format("Check_StepChamferMissing", type.Name));
                    else if (type.StepChamfer > most + Tolerance)
                        problems.Add(Localizer.Format("Check_StepChamferTooBig", type.Name, Mm(most)));
                    else
                        straightFrom += type.StepChamfer;
                }
            }
        }

        var tip = type.TipStyle switch
        {
            PartTipStyle.Chamfer => type.TipSize,
            PartTipStyle.Round => r,
            _ => 0
        };

        if (type.TipStyle == PartTipStyle.Chamfer && (type.TipSize <= 0 || type.TipSize >= r - Tolerance))
            problems.Add(Localizer.Format("Check_TipChamfer", type.Name, Mm(r)));
        else if (h - tip < straightFrom - Tolerance)
            problems.Add(Localizer.Format("Check_TipTooLong", type.Name));

        return problems;
    }

    private static string Mm(double value) => value.ToString("0.###", CultureInfo.CurrentCulture);
}
