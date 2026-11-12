using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Parallel key connection calculation engine based on DIN 6885
/// </summary>
public class KeyConnectionEngine
{
    // Input Parameters - Shaft
    public double ShaftDiameter { get; set; }         // mm
    
    // Input Parameters - Key Dimensions
    public double KeyWidth { get; set; }              // b (mm)
    public double KeyHeight { get; set; }             // h (mm)
    public double KeyLength { get; set; }             // l (mm)
    public double KeywayDepthShaft { get; set; }      // t1 (mm)
    public double KeywayDepthHub { get; set; }        // t2 (mm)
    public int NumberOfKeys { get; set; } = 1;

    // Input Parameters - Loading
    public double AppliedTorque { get; set; }         // Nm
    public double LoadFactor { get; set; } = 1.0;     // Application factor

    /// <summary>
    /// The safety factor <see cref="RequiredKeyLength"/> is sized for.
    ///
    /// It is 1.5 because that is the figure the page tells the reader to aim for
    /// and the threshold GetSafetyStatus draws the line at. It used to be sized
    /// for 1.0, so the key the calculator recommended came back "Marginal" -
    /// a design recommendation that its own verdict disagreed with.
    /// </summary>
    public double TargetSafetyFactor { get; set; } = 1.5;

    // Materials
    public Material ShaftMaterial { get; set; } = new();
    public Material HubMaterial { get; set; } = new();
    public Material KeyMaterial { get; set; } = new();

    // Calculated Values - Geometry
    public double EffectiveLength { get; set; }       // mm (l - b for round end keys)
    public double ContactAreaShaft { get; set; }      // mm²
    public double ContactAreaHub { get; set; }        // mm²
    public double BearingHeightShaft { get; set; }    // mm (t1)
    public double BearingHeightHub { get; set; }      // mm (t2)
    public double ShearArea { get; set; }             // mm²

    /// <summary>
    /// The part of the effective length that is counted as carrying load:
    /// min(l_eff, 1.3 d). See <see cref="LoadCarryingLengthFactor"/>.
    /// </summary>
    public double LoadCarryingLength { get; set; }    // mm

    /// <summary>1.3 d - the length beyond which extra key buys nothing.</summary>
    public double LoadCarryingLengthLimit { get; set; } // mm

    /// <summary>True when l_eff was longer than the limit and had to be cut back.</summary>
    public bool LoadCarryingLengthLimited { get; set; }

    // Calculated Values - Forces
    public double TangentialForce { get; set; }       // N
    public double ForcePerKey { get; set; }           // N

    // Calculated Values - Stresses
    public double SurfacePressureShaft { get; set; }  // MPa
    public double SurfacePressureHub { get; set; }    // MPa
    public double ShearStress { get; set; }           // MPa

    // Allowable Values
    public double AllowablePressureShaft { get; set; } // MPa
    public double AllowablePressureHub { get; set; }   // MPa
    public double AllowableShear { get; set; }         // MPa

    /// <summary>
    /// Which part supplied the allowable pressure on each flank pair, e.g.
    /// "key (S235JR)". Both flanks of a pair see the same pressure, so the
    /// weaker material governs and the results have to say which one it was -
    /// otherwise a soft key silently changes the answer with no visible cause.
    /// </summary>
    public string GoverningPartShaft { get; set; } = "";
    public string GoverningPartHub { get; set; } = "";

    // Safety Factors
    public double SafetyFactorShaft { get; set; }
    public double SafetyFactorHub { get; set; }
    public double SafetyFactorShear { get; set; }
    public double SafetyFactorMin { get; set; }

    // Required Length
    public double RequiredKeyLength { get; set; }     // mm

    /// <summary>
    /// The bearing length the three checks need at <see cref="TargetSafetyFactor"/>,
    /// before the key width is added back on and before rounding.
    /// </summary>
    public double RequiredBearingLength { get; set; } // mm

    /// <summary>
    /// True when that bearing length is longer than 1.3 d, i.e. no key length
    /// reaches the target safety factor and the joint itself has to change.
    /// </summary>
    public bool RequiredLengthExceedsLimit { get; set; }

    /// <summary>
    /// 1.3 - DIN 6892 does not credit key length past this multiple of the shaft
    /// diameter: the shaft twists along the key, so the far end stops taking its
    /// share. The page text already told the reader this; nothing enforced it, so
    /// a 2.5 d key was rated on its full length and came out "OK".
    /// </summary>
    public const double LoadCarryingLengthFactor = 1.3;

    // Standard key dimensions from DIN 6885
    public static readonly List<KeyDimension> StandardKeys = new()
    {
        new KeyDimension { ShaftDiaMin = 6, ShaftDiaMax = 8, Width = 2, Height = 2, DepthShaft = 1.2, DepthHub = 1.0 },
        new KeyDimension { ShaftDiaMin = 8, ShaftDiaMax = 10, Width = 3, Height = 3, DepthShaft = 1.8, DepthHub = 1.4 },
        new KeyDimension { ShaftDiaMin = 10, ShaftDiaMax = 12, Width = 4, Height = 4, DepthShaft = 2.5, DepthHub = 1.8 },
        new KeyDimension { ShaftDiaMin = 12, ShaftDiaMax = 17, Width = 5, Height = 5, DepthShaft = 3.0, DepthHub = 2.3 },
        new KeyDimension { ShaftDiaMin = 17, ShaftDiaMax = 22, Width = 6, Height = 6, DepthShaft = 3.5, DepthHub = 2.8 },
        new KeyDimension { ShaftDiaMin = 22, ShaftDiaMax = 30, Width = 8, Height = 7, DepthShaft = 4.0, DepthHub = 3.3 },
        new KeyDimension { ShaftDiaMin = 30, ShaftDiaMax = 38, Width = 10, Height = 8, DepthShaft = 5.0, DepthHub = 3.3 },
        new KeyDimension { ShaftDiaMin = 38, ShaftDiaMax = 44, Width = 12, Height = 8, DepthShaft = 5.0, DepthHub = 3.3 },
        new KeyDimension { ShaftDiaMin = 44, ShaftDiaMax = 50, Width = 14, Height = 9, DepthShaft = 5.5, DepthHub = 3.8 },
        new KeyDimension { ShaftDiaMin = 50, ShaftDiaMax = 58, Width = 16, Height = 10, DepthShaft = 6.0, DepthHub = 4.3 },
        new KeyDimension { ShaftDiaMin = 58, ShaftDiaMax = 65, Width = 18, Height = 11, DepthShaft = 7.0, DepthHub = 4.4 },
        new KeyDimension { ShaftDiaMin = 65, ShaftDiaMax = 75, Width = 20, Height = 12, DepthShaft = 7.5, DepthHub = 4.9 },
        new KeyDimension { ShaftDiaMin = 75, ShaftDiaMax = 85, Width = 22, Height = 14, DepthShaft = 9.0, DepthHub = 5.4 },
        new KeyDimension { ShaftDiaMin = 85, ShaftDiaMax = 95, Width = 25, Height = 14, DepthShaft = 9.0, DepthHub = 5.4 },
        new KeyDimension { ShaftDiaMin = 95, ShaftDiaMax = 110, Width = 28, Height = 16, DepthShaft = 10.0, DepthHub = 6.4 },
        new KeyDimension { ShaftDiaMin = 110, ShaftDiaMax = 130, Width = 32, Height = 18, DepthShaft = 11.0, DepthHub = 7.4 }
    };

    public static KeyDimension? GetStandardKey(double shaftDiameter)
    {
        return StandardKeys.FirstOrDefault(k =>
            shaftDiameter > k.ShaftDiaMin && shaftDiameter <= k.ShaftDiaMax);
    }

    public void Calculate()
    {
        CalculateGeometry();
        CalculateForces();
        CalculateStresses();
        CalculateAllowableValues();
        CalculateSafetyFactors();
        CalculateRequiredLength();
    }

    /// <summary>
    /// Divides only when the divisor is usable, so a zero or negative dimension
    /// yields 0 instead of propagating Infinity/NaN into the results.
    /// </summary>
    private static double SafeDivide(double numerator, double divisor)
        => divisor > 0 ? numerator / divisor : 0;

    /// <summary>
    /// A rectangle only has an area when both of its sides are real. A negative
    /// height times a negative length is a positive number and would otherwise
    /// sail through as a contact area.
    /// </summary>
    private static double Area(double length, double height)
        => length > 0 && height > 0 ? length * height : 0;

    private void CalculateGeometry()
    {
        // Effective length of a round-ended (form A) key: the rounded ends carry
        // nothing, so l - b bears.
        //
        // There is deliberately NO fallback here. l <= b was silently replaced by
        // 0.9 * l, which turned an impossible key into a plausible-looking result
        // (l = 10 with b = 12 reported l_eff = 9.0 and a safety factor to match).
        // The page refuses that geometry now; the engine reports it as the zero
        // area it is.
        EffectiveLength = KeyLength - KeyWidth;

        // DIN 6892 does not credit the whole length: past about 1.3 d the shaft
        // has twisted enough that the far end of the key stops taking its share.
        // The pressures are therefore computed on the limited length, and
        // LoadCarryingLengthLimited lets the page say when that bit.
        LoadCarryingLengthLimit = LoadCarryingLengthFactor * ShaftDiameter;
        LoadCarryingLengthLimited = EffectiveLength > LoadCarryingLengthLimit
                                    && LoadCarryingLengthLimit > 0;
        LoadCarryingLength = LoadCarryingLengthLimited
            ? LoadCarryingLengthLimit
            : EffectiveLength;

        // Load-bearing flank heights (DIN 6892).
        // These are the SINGLE source of truth for both the surface pressure
        // (CalculateStresses) and the required length (CalculateRequiredLength) -
        // previously the two used different heights on the shaft side, so the
        // required length contradicted the safety factor shown next to it.
        // Capped at the key's own height: t1 > h is a contradiction, not a deeper
        // contact. Entering t1 = 9 on an 8 mm key used to be read as 9 mm of
        // bearing height and *improved* the reported safety factor.
        BearingHeightShaft = Math.Min(KeywayDepthShaft, KeyHeight);

        // Hub side: the key can only bear on the part of itself that sticks OUT of
        // the shaft, which is h - t1. It is NOT t2. The hub keyway is cut slightly
        // deeper than the protrusion on purpose, to leave top clearance so the key
        // seats on its flanks rather than its top face - so t2 > h - t1 by design.
        //
        // Using t2 therefore overstated the hub contact area on every standard key
        // (DIN 6885 d=40, key 12x8: t2 = 3.3 vs h - t1 = 3.0) and reported a hub
        // pressure ~9% LOW - i.e. optimistic on the unsafe side, on the hub side,
        // which is normally the governing one.
        //
        // min() covers inconsistent input: if someone enters t2 shallower than the
        // protrusion the key bottoms out in the hub slot, and t2 is what bears.
        //
        // When t1 >= h there is no protrusion at all and the key cannot reach the
        // hub. That used to fall back to t2 and report a working joint for a key
        // buried flush in the shaft; it is now the zero (or negative) height it is,
        // and the page refuses it.
        BearingHeightHub = Math.Min(KeyHeight - KeywayDepthShaft, KeywayDepthHub);

        // Contact and shear areas, on the load-carrying length rather than the
        // full effective length.
        ContactAreaShaft = Area(LoadCarryingLength, BearingHeightShaft);
        ContactAreaHub = Area(LoadCarryingLength, BearingHeightHub);
        ShearArea = Area(LoadCarryingLength, KeyWidth);
    }

    private void CalculateForces()
    {
        // Tangential force from torque: F = 2T / d
        double radius = ShaftDiameter / 2000.0; // m
        TangentialForce = (AppliedTorque * LoadFactor) / radius;

        // Force per key
        double keyFactor = NumberOfKeys > 1 ? 0.75 * NumberOfKeys : 1.0;
        ForcePerKey = TangentialForce / keyFactor;
    }

    private void CalculateStresses()
    {
        // Every one of these is assigned unconditionally, via SafeDivide.
        //
        // They used to be written only inside `if (area > 0)`, which left the
        // PREVIOUS calculation's values in place when a later one came out
        // degenerate. Re-running the same engine with l = b showed l_eff = 0.0 and
        // A = 0.0 in the geometry table while the stresses and safety factors below
        // it were still those of the run before - and the page reuses one engine
        // instance across Back to Input, so that is the normal path, not a corner.
        SurfacePressureShaft = SafeDivide(ForcePerKey, ContactAreaShaft);
        SurfacePressureHub = SafeDivide(ForcePerKey, ContactAreaHub);
        ShearStress = SafeDivide(ForcePerKey, ShearArea);
    }

    private void CalculateAllowableValues()
    {
        // Allowable surface pressure depends on loading type
        // For static/light shock: ~0.9 × Re
        // For heavy shock: ~0.6 × Re
        double pressureFactor = 0.9; // Assuming normal operation

        // Both flanks of a contact pair carry the same pressure, so the weaker of
        // the two materials decides it (DIN 6892 and ordinary practice).
        //
        // The key material used to reach the shear check only. A C45 shaft and hub
        // with an S235 key reported exactly the same pressure safety factors as an
        // all-C45 joint and a verdict of "OK", while the key's own flank is at
        // 219 MPa against an allowable of 212 - i.e. wrong in the unsafe direction,
        // in the common case of a soft key in hard parts.
        var shaftPair = Weaker(ShaftMaterial, "shaft", KeyMaterial, "key");
        var hubPair = Weaker(HubMaterial, "hub", KeyMaterial, "key");

        AllowablePressureShaft = shaftPair.Material.YieldStrength * pressureFactor;
        AllowablePressureHub = hubPair.Material.YieldStrength * pressureFactor;
        GoverningPartShaft = shaftPair.Label;
        GoverningPartHub = hubPair.Label;

        // Allowable shear stress: ~0.6 × Re. Shear is carried by the key alone.
        AllowableShear = KeyMaterial.YieldStrength * 0.6;
    }

    /// <summary>
    /// The lower-yield material of a contact pair, with a label naming it, e.g.
    /// "key (S235JR)". Ties go to the first argument so an all-one-material joint
    /// is described by its part rather than by the key.
    /// </summary>
    private static (Material Material, string Label) Weaker(
        Material first, string firstPart, Material second, string secondPart)
    {
        var (material, part) = second.YieldStrength < first.YieldStrength
            ? (second, secondPart)
            : (first, firstPart);

        return (material, string.IsNullOrWhiteSpace(material.Name)
            ? part
            : $"{part} ({material.Name})");
    }

    private void CalculateSafetyFactors()
    {
        // A zero pressure here means a zero contact area, not an unloaded joint -
        // the torque is validated to be positive before Calculate is called. So it
        // is a key that carries nothing, and the honest safety factor is 0.
        //
        // It used to return the 999 sentinel, which painted all three rows green:
        // entering l = b on a fresh form produced an impossible key reported as
        // "✓ OK" three times over.
        SafetyFactorShaft = SurfacePressureShaft > 0
            ? AllowablePressureShaft / SurfacePressureShaft : 0;

        SafetyFactorHub = SurfacePressureHub > 0
            ? AllowablePressureHub / SurfacePressureHub : 0;

        SafetyFactorShear = ShearStress > 0
            ? AllowableShear / ShearStress : 0;

        // Minimum safety factor
        SafetyFactorMin = Math.Min(Math.Min(SafetyFactorShaft, SafetyFactorHub), SafetyFactorShear);
    }

    private void CalculateRequiredLength()
    {
        // Minimum required EFFECTIVE length, using the same bearing heights that
        // CalculateStresses used - so the required length is consistent with the
        // safety factors reported alongside it.
        //
        // Sized for TargetSafetyFactor, not for 1.0. Dividing the allowables by
        // the target is the same thing as multiplying the load by it. Sizing for
        // 1.0 meant the length this card recommended scored "⚠️ Marginal" in the
        // table above it - at T = 820 Nm it returned exactly the 50 mm key that
        // was already fitted, with a margin of 0, next to a hub factor of 1.08.
        double demand = ForcePerKey * TargetSafetyFactor;

        double requiredFromShaft = SafeDivide(SafeDivide(demand, AllowablePressureShaft), BearingHeightShaft);
        double requiredFromHub = SafeDivide(SafeDivide(demand, AllowablePressureHub), BearingHeightHub);
        double requiredFromShear = SafeDivide(SafeDivide(demand, AllowableShear), KeyWidth);

        RequiredBearingLength = Math.Max(Math.Max(requiredFromShaft, requiredFromHub), requiredFromShear);

        // Past 1.3 d the extra length is not counted, so if that is what the
        // checks need, no key length reaches the target and the joint has to
        // change instead. Saying "fit a 220 mm key" would be worse than useless.
        RequiredLengthExceedsLimit = LoadCarryingLengthLimit > 0
                                     && RequiredBearingLength > LoadCarryingLengthLimit;

        // Add key width for round-end key
        RequiredKeyLength = RequiredBearingLength + KeyWidth;

        // Round up to nearest 5mm. DIN 6885-1 has its own length series
        // (… 28, 32, 36, 40, 45, 50, 56 …) and this is not it; the results label
        // says "rounded up to 5 mm" rather than implying a standard length.
        RequiredKeyLength = Math.Ceiling(RequiredKeyLength / 5.0) * 5.0;
    }
}

public class KeyDimension
{
    public double ShaftDiaMin { get; set; }
    public double ShaftDiaMax { get; set; }
    public double Width { get; set; }      // b
    public double Height { get; set; }     // h
    public double DepthShaft { get; set; } // t1
    public double DepthHub { get; set; }   // t2

    public override string ToString() => $"{Width}×{Height} (b×h)";
}
