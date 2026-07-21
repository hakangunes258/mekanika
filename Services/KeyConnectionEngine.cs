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

    // Safety Factors
    public double SafetyFactorShaft { get; set; }
    public double SafetyFactorHub { get; set; }
    public double SafetyFactorShear { get; set; }
    public double SafetyFactorMin { get; set; }

    // Required Length
    public double RequiredKeyLength { get; set; }     // mm

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

    private void CalculateGeometry()
    {
        // Effective length (assuming round-end key)
        EffectiveLength = KeyLength - KeyWidth;
        if (EffectiveLength < 0) EffectiveLength = KeyLength * 0.9;

        // Load-bearing flank heights (DIN 6885).
        // These are the SINGLE source of truth for both the surface pressure
        // (CalculateStresses) and the required length (CalculateRequiredLength) -
        // previously the two used different heights on the shaft side, so the
        // required length contradicted the safety factor shown next to it.
        BearingHeightShaft = KeywayDepthShaft;  // t1
        BearingHeightHub = KeywayDepthHub;      // t2

        // Contact areas
        ContactAreaShaft = EffectiveLength * BearingHeightShaft;
        ContactAreaHub = EffectiveLength * BearingHeightHub;

        // Shear area
        ShearArea = EffectiveLength * KeyWidth;
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
        // Surface pressure on shaft side
        if (ContactAreaShaft > 0)
        {
            SurfacePressureShaft = ForcePerKey / ContactAreaShaft;
        }

        // Surface pressure on hub side
        if (ContactAreaHub > 0)
        {
            SurfacePressureHub = ForcePerKey / ContactAreaHub;
        }

        // Shear stress in key
        if (ShearArea > 0)
        {
            ShearStress = ForcePerKey / ShearArea;
        }
    }

    private void CalculateAllowableValues()
    {
        // Allowable surface pressure depends on loading type
        // For static/light shock: ~0.9 × Re
        // For heavy shock: ~0.6 × Re
        double pressureFactor = 0.9; // Assuming normal operation

        AllowablePressureShaft = ShaftMaterial.YieldStrength * pressureFactor;
        AllowablePressureHub = HubMaterial.YieldStrength * pressureFactor;

        // Allowable shear stress: ~0.6 × Re
        AllowableShear = KeyMaterial.YieldStrength * 0.6;
    }

    private void CalculateSafetyFactors()
    {
        // Safety factors
        SafetyFactorShaft = SurfacePressureShaft > 0 
            ? AllowablePressureShaft / SurfacePressureShaft : 999;
        
        SafetyFactorHub = SurfacePressureHub > 0 
            ? AllowablePressureHub / SurfacePressureHub : 999;
        
        SafetyFactorShear = ShearStress > 0 
            ? AllowableShear / ShearStress : 999;

        // Minimum safety factor
        SafetyFactorMin = Math.Min(Math.Min(SafetyFactorShaft, SafetyFactorHub), SafetyFactorShear);
    }

    private void CalculateRequiredLength()
    {
        // Minimum required EFFECTIVE length, using the same bearing heights that
        // CalculateStresses used - so the required length is consistent with the
        // safety factors reported alongside it.
        double requiredFromShaft = SafeDivide(ForcePerKey / AllowablePressureShaft, BearingHeightShaft);
        double requiredFromHub = SafeDivide(ForcePerKey / AllowablePressureHub, BearingHeightHub);
        double requiredFromShear = SafeDivide(ForcePerKey / AllowableShear, KeyWidth);

        double maxRequired = Math.Max(Math.Max(requiredFromShaft, requiredFromHub), requiredFromShear);
        
        // Add key width for round-end key
        RequiredKeyLength = maxRequired + KeyWidth;
        
        // Round up to nearest 5mm
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
