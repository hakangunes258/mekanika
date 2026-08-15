namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// A small library of real, named lubricants, so the lubrication card can ask for a product
/// instead of six numbers.
///
/// WHY IT IS SMALL AND WHY IT NAMES PRODUCTS
/// Every field below is copied from the manufacturer's published technical data sheet, cited
/// per entry. That is the whole point: viscosity, density and especially the FZG stages are
/// product properties, not properties of a viscosity grade, and inventing "typical" values for
/// them would put invented numbers straight into a scuffing safety factor. Ten verified entries
/// are worth more than fifty plausible ones. If a value is not published, it is null here and
/// the user is asked for it — never filled in with a guess.
///
/// THE FZG TRAP. ISO/TR 13989 Eq. (99) is calibrated to the FZG A/8,3/90 test of ISO 14635-1.
/// Data sheets also quote A/16,6/90, and some quote only that one. The two are different tests
/// and their stage numbers are NOT interchangeable, so <see cref="FzgStageA8390"/> is only ever
/// filled from a line that names A/8,3/90 explicitly. Mobilgear's sheet prints both, which is
/// how the distinction became visible.
///
/// Micropitting is the FVA 54 / FZG GF-C test, a different rig again, and feeds
/// <see cref="GearPairEngine.MicropittingLoadStage"/>.
///
/// A grease is a base oil plus a thickener, so it still has a base oil type and base oil
/// viscosities — those are what is stored. The NLGI consistency number does not enter any
/// equation in this module and is not carried.
/// </summary>
public class LubricantPreset
{
    public string Name { get; set; } = "";

    /// <summary>Base oil chemistry — sets X_L in the scuffing calculation.</summary>
    public LubricantType Type { get; set; } = LubricantType.Mineral;

    /// <summary>True for greases; selects X_S = 1,2 and the out-of-scope warnings.</summary>
    public bool IsGrease { get; set; }

    public double Nu40 { get; set; }              // mm²/s
    public double Nu100 { get; set; }             // mm²/s
    public double Density15 { get; set; }         // kg/dm³

    /// <summary>
    /// FZG scuffing failure load stage from the A/8,3/90 test of ISO 14635-1 — and only from
    /// that test. Null when the sheet does not publish it, in which case the user must supply
    /// it: the scuffing temperature is derived from this number and there is no safe default.
    /// "&gt;12" and "12+" are recorded as 12, the highest stage the test resolves.
    /// </summary>
    public double? FzgStageA8390 { get; set; }

    /// <summary>FVA 54 (FZG GF-C/8,3/90) micropitting failure stage. Null when not published.</summary>
    public double? MicropittingStage { get; set; }

    /// <summary>Where every number above came from, shown in the dialog and the results.</summary>
    public string Source { get; set; } = "";

    public override string ToString() => Name;

    /// <summary>The sentinel the lubricant select uses for "not one of these".</summary>
    public const string CustomName = "Custom";

    /// <summary>
    /// Resolve by NAME, never by index — the same rule the material and bearing libraries
    /// follow. Share links carry the name, so inserting a row must not repoint an existing
    /// link at a different lubricant. Returns null for Custom or an unknown name.
    /// </summary>
    public static LubricantPreset? Find(string? name)
        => string.IsNullOrWhiteSpace(name) || name == CustomName
            ? null
            : All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));

    public static readonly IReadOnlyList<LubricantPreset> All = new List<LubricantPreset>
    {
        // ---- Mineral, ExxonMobil Mobilgear 600 XP series -------------------------------
        // One sheet covers the whole series and is unusually complete: it prints the FZG
        // scuffing stage for BOTH test variants separately, and the FVA 54 micropitting
        // stage. That is why six of the ten entries come from it.
        new() { Name = "Mobilgear 600 XP 100", Type = LubricantType.Mineral,
                Nu40 = 100, Nu100 = 11.2, Density15 = 0.88,
                FzgStageA8390 = 12, MicropittingStage = 10,
                Source = "ExxonMobil Mobilgear 600 XP Series TDS, 09-2020. FZG scuffing A/8,3/90 (ISO 14635-1) 12+; FVA 54 micropitting stage 10, GFT class High." },
        new() { Name = "Mobilgear 600 XP 150", Type = LubricantType.Mineral,
                Nu40 = 150, Nu100 = 14.7, Density15 = 0.89,
                FzgStageA8390 = 12, MicropittingStage = 10,
                Source = "ExxonMobil Mobilgear 600 XP Series TDS, 09-2020. FZG scuffing A/8,3/90 (ISO 14635-1) 12+; FVA 54 micropitting stage 10, GFT class High." },
        new() { Name = "Mobilgear 600 XP 220", Type = LubricantType.Mineral,
                Nu40 = 220, Nu100 = 19.0, Density15 = 0.89,
                FzgStageA8390 = 12, MicropittingStage = 10,
                Source = "ExxonMobil Mobilgear 600 XP Series TDS, 09-2020. FZG scuffing A/8,3/90 (ISO 14635-1) 12+; FVA 54 micropitting stage 10, GFT class High." },
        new() { Name = "Mobilgear 600 XP 320", Type = LubricantType.Mineral,
                Nu40 = 320, Nu100 = 24.1, Density15 = 0.90,
                FzgStageA8390 = 12, MicropittingStage = 10,
                Source = "ExxonMobil Mobilgear 600 XP Series TDS, 09-2020. FZG scuffing A/8,3/90 (ISO 14635-1) 12+; FVA 54 micropitting stage 10, GFT class High." },
        new() { Name = "Mobilgear 600 XP 460", Type = LubricantType.Mineral,
                Nu40 = 460, Nu100 = 30.6, Density15 = 0.90,
                FzgStageA8390 = 12, MicropittingStage = 10,
                Source = "ExxonMobil Mobilgear 600 XP Series TDS, 09-2020. FZG scuffing A/8,3/90 (ISO 14635-1) 12+; FVA 54 micropitting stage 10, GFT class High." },
        new() { Name = "Mobilgear 600 XP 680", Type = LubricantType.Mineral,
                Nu40 = 680, Nu100 = 39.2, Density15 = 0.91,
                FzgStageA8390 = 12, MicropittingStage = 10,
                Source = "ExxonMobil Mobilgear 600 XP Series TDS, 09-2020. FZG scuffing A/8,3/90 (ISO 14635-1) 12+; FVA 54 micropitting stage 10, GFT class High." },

        // ---- Mineral, Shell --------------------------------------------------------------
        new() { Name = "Shell Omala S2 G 220", Type = LubricantType.Mineral,
                Nu40 = 220, Nu100 = 19.4, Density15 = 0.899,
                FzgStageA8390 = null, MicropittingStage = null,
                Source = "Shell Omala S2 G 220 TDS: nu40 220, nu100 19,4 mm²/s (ISO 3104), density 899 kg/m³ at 15 °C (ISO 12185), VI 98. The sheet does not publish an FZG stage, so scuffing resistance must be entered." },

        // ---- Polyalphaolefin ------------------------------------------------------------
        new() { Name = "Mobil SHC 630", Type = LubricantType.Polyalphaolefin,
                Nu40 = 220, Nu100 = 25.2, Density15 = 0.866,
                FzgStageA8390 = null, MicropittingStage = null,
                Source = "ExxonMobil Mobil SHC 630 PDS: ISO VG 220, nu40 220, nu100 25,2 mm²/s, relative density 0,866 at 15,6 °C, VI 169. No FZG stage on the sheet consulted." },

        // ---- Polyglycol -----------------------------------------------------------------
        new() { Name = "Shell Omala S4 WE 220", Type = LubricantType.PolyglycolNonWaterSoluble,
                Nu40 = 222, Nu100 = 34.4, Density15 = 1.074,
                FzgStageA8390 = 12, MicropittingStage = null,
                Source = "Shell Omala S4 WE 220 TDS v4.1: nu40 222, nu100 34,4 mm²/s, density 1074 kg/m³ at 15 °C (ISO 12185), VI 203, FZG failure load stage >12 on DIN 51354-2 A/8,3/90. Polyalkylene glycol base." },

        // ---- Grease ---------------------------------------------------------------------
        // The lubricant of the KISSsoft tutorial this module is benchmarked against, which is
        // why it earns a place in a ten-entry library.
        new() { Name = "Klüber Microlube GB 00 (grease)", Type = LubricantType.Mineral,
                IsGrease = true,
                Nu40 = 700, Nu100 = 35, Density15 = 0.90,
                FzgStageA8390 = null, MicropittingStage = null,
                Source = "Klüber Microlube GB 00 product information 5.191: fluid gear grease, mineral base oil, base oil viscosity approx. 700 mm²/s at 40 °C and approx. 35 mm²/s at 100 °C, density approx. 0,90 g/cm³ at 20 °C. No FZG stage published; ISO/TR 13989 does not cover grease." },
    };
}
