using MechanicalCalculatorWeb.Services;

namespace MechanicalCalculatorWeb.Models;

/// <summary>
/// What the "add a bearing" form binds to. The library stores three different shapes
/// — <see cref="Bearing"/> (deep groove ball, cylindrical roller),
/// <see cref="TaperedBearing"/> and <see cref="AngularContactBearing"/> — so the form
/// carries the union of their fields and converts on save, with
/// <see cref="Type"/> as the discriminator.
/// </summary>
public class CustomBearingDraft
{
    public string? CustomId { get; set; }

    public string Designation { get; set; } = "";
    public string Type { get; set; } = BearingService.TypeDeepGroove;

    // Common to every family
    public double Bore { get; set; }
    public double Outer { get; set; }
    public double Width { get; set; }
    public double C { get; set; }          // dynamic load rating (kN)
    public double C0 { get; set; }         // static load rating (kN)
    public int LimitSpeed { get; set; }    // rpm

    // Deep groove ball only (ISO 76 factor; cylindrical rollers carry no axial load)
    public double f0 { get; set; }

    // Tapered roller and angular contact
    public double e { get; set; }
    public double Y { get; set; }
    public double Y0 { get; set; }

    // Angular contact only
    public double ContactAngle { get; set; }
    public double X { get; set; }
    public int LimitSpeedOil { get; set; }

    public bool IsDeepGroove => Type == BearingService.TypeDeepGroove;
    public bool IsCylindrical => Type == BearingService.TypeCylindrical;
    public bool IsTapered => Type == BearingService.TypeTapered;
    public bool IsAngular => Type == BearingService.TypeAngular;

    /// <summary>
    /// The object actually stored in `data`, in the shape the calculators expect.
    /// </summary>
    public object ToPayload() => Type switch
    {
        BearingService.TypeTapered => new TaperedBearing
        {
            Designation = Designation, Type = Type,
            Bore = Bore, Outer = Outer, Width = Width,
            C = C, C0 = C0, e = e, Y = Y, Y0 = Y0, LimitSpeed = LimitSpeed
        },
        BearingService.TypeAngular => new AngularContactBearing
        {
            Designation = Designation, Type = Type,
            Bore = Bore, Outer = Outer, Width = Width,
            C = C, C0 = C0, ContactAngle = ContactAngle,
            e = e, X = X, Y = Y, Y0 = Y0,
            LimitSpeed = LimitSpeed, LimitSpeedOil = LimitSpeedOil
        },
        _ => new Bearing
        {
            Designation = Designation, Type = Type,
            Bore = Bore, Outer = Outer, Width = Width,
            C = C, C0 = C0, f0 = f0, LimitSpeed = LimitSpeed
        }
    };

    /// <summary>
    /// Null when the draft is usable. The engines divide by C, C0 and the load
    /// factors, so a zero there surfaces much later as a nonsense bearing life
    /// rather than an error — reject it here instead.
    /// </summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Designation)) return "Give the bearing a designation.";
        if (Bore <= 0) return "Bore diameter must be greater than zero.";
        if (Outer <= Bore) return "Outside diameter must be larger than the bore.";
        if (Width <= 0) return "Width must be greater than zero.";
        if (C <= 0) return "Dynamic load rating C must be greater than zero.";
        if (C0 <= 0) return "Static load rating C₀ must be greater than zero.";
        if (LimitSpeed <= 0) return "Limiting speed must be greater than zero.";

        if (IsDeepGroove && f0 <= 0)
            return "f₀ must be greater than zero — the X/Y factors for combined loading are derived from it.";

        if (IsTapered)
        {
            if (e <= 0) return "e must be greater than zero.";
            if (Y <= 0) return "Y must be greater than zero.";
            if (Y0 <= 0) return "Y₀ must be greater than zero.";
        }

        if (IsAngular)
        {
            if (ContactAngle <= 0 || ContactAngle >= 90) return "Contact angle must be between 0° and 90°.";
            if (e <= 0) return "e must be greater than zero.";
            if (X <= 0) return "X must be greater than zero.";
            if (Y <= 0) return "Y must be greater than zero.";
            if (Y0 <= 0) return "Y₀ must be greater than zero.";
            if (LimitSpeedOil <= 0) LimitSpeedOil = LimitSpeed;
        }

        return null;
    }

    /// <summary>Rebuilds the form draft from a stored bearing, for editing.</summary>
    public static CustomBearingDraft From(Bearing b) => new()
    {
        CustomId = b.CustomId, Designation = b.Designation, Type = b.Type,
        Bore = b.Bore, Outer = b.Outer, Width = b.Width,
        C = b.C, C0 = b.C0, f0 = b.f0, LimitSpeed = b.LimitSpeed
    };

    public static CustomBearingDraft From(TaperedBearing b) => new()
    {
        CustomId = b.CustomId, Designation = b.Designation, Type = BearingService.TypeTapered,
        Bore = b.Bore, Outer = b.Outer, Width = b.Width,
        C = b.C, C0 = b.C0, e = b.e, Y = b.Y, Y0 = b.Y0, LimitSpeed = b.LimitSpeed
    };

    public static CustomBearingDraft From(AngularContactBearing b) => new()
    {
        CustomId = b.CustomId, Designation = b.Designation, Type = BearingService.TypeAngular,
        Bore = b.Bore, Outer = b.Outer, Width = b.Width,
        C = b.C, C0 = b.C0, ContactAngle = b.ContactAngle,
        e = b.e, X = b.X, Y = b.Y, Y0 = b.Y0,
        LimitSpeed = b.LimitSpeed, LimitSpeedOil = b.LimitSpeedOil
    };
}
