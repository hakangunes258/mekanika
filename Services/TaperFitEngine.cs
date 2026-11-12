using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Taper fit calculation engine based on DIN 7190
/// </summary>
public class TaperFitEngine
{
    // Input Parameters - Dimensions
    public double MeanDiameter { get; set; }          // mm
    public double FitLength { get; set; }             // mm
    public double ShaftInnerDiameter { get; set; }    // mm (hollow shaft)
    public double HubOuterDiameter { get; set; }      // mm
    public double TaperRatio { get; set; }            // 1:X (e.g. 10 for 1:10)

    // Input Parameters - Assembly
    public double AxialDisplacement { get; set; }     // mm
    
    // Input Parameters - Surface
    public double ShaftRoughnessRz { get; set; }        // Rz (μm)
    public double HubRoughnessRz { get; set; }          // Rz (μm)
    public double FrictionCoefficientStatic { get; set; } = 0.15;
    public double FrictionCoefficientDynamic { get; set; } = 0.12;

    // Input Parameters - Operating
    public double AppliedTorque { get; set; }         // Nm
    public double AppliedAxialForce { get; set; }     // N
    public double LoadFactor { get; set; } = 1.0;     // Application factor (shock/duty)

    // Materials
    public Material ShaftMaterial { get; set; } = new();
    public Material HubMaterial { get; set; } = new();

    // Calculated Values - Geometry
    public double TaperAngle { get; set; }            // degrees (full angle)
    public double HalfAngle { get; set; }             // degrees
    public double SmallDiameter { get; set; }         // mm
    public double LargeDiameter { get; set; }         // mm
    public double TaperSlopePercent { get; set; }     // %

    // Calculated Values - Interference
    public double RadialInterference { get; set; }    // mm
    public double DiametralInterference { get; set; } // μm
    public double RoughnessLoss { get; set; }         // μm
    public double EffectiveInterference { get; set; } // μm

    // Calculated Values - Pressure & Stress
    public double ContactPressure { get; set; }       // MPa
    public double ShaftTangentialStress { get; set; } // MPa
    public double ShaftRadialStress { get; set; }     // MPa
    public double ShaftVonMisesStress { get; set; }   // MPa
    public double HubTangentialStress { get; set; }   // MPa
    public double HubRadialStress { get; set; }       // MPa
    public double HubVonMisesStress { get; set; }     // MPa

    /// <summary>Where the shaft's governing stress sits — "bore" or "fit surface".</summary>
    public string ShaftStressPoint { get; private set; } = "";
    /// <summary>Where the hub's governing stress sits — "bore" or "outer surface".</summary>
    public string HubStressPoint { get; private set; } = "";
    /// <summary>Which section along the cone produced the governing stress.</summary>
    public string GoverningSection { get; private set; } = "";

    /// <summary>
    /// The three sections along the cone. The diametral interference is the same
    /// at every one of them (Δs/X), but the local pressure is not: it rises
    /// towards the SMALL end, where the diameter is least.
    /// </summary>
    public List<TaperSection> Sections { get; } = new();

    // Calculated Values - Capacity
    public double MaxTorqueCapacity { get; set; }     // Nm
    public double MaxAxialForceCapacity { get; set; } // N
    public double RequiredAssemblyForce { get; set; } // N
    public double RequiredDisplacement { get; set; }  // mm

    // Self-locking
    /// <summary>
    /// True when the cone holds itself: the friction component up the taper
    /// exceeds the wedge component pushing the hub back off, μ_s/cos φ > tan φ
    /// (equivalently μ_s > sin φ; the familiar tan φ &lt; μ_s is its small-angle form).
    /// </summary>
    public bool IsSelfLocking { get; private set; }
    /// <summary>
    /// Axial force needed to pull the hub OFF the taper, using the static
    /// coefficient. Negative when the cone ejects the hub by itself.
    /// </summary>
    public double ExtractionForce { get; private set; }   // N
    /// <summary>
    /// Retaining force a nut has to supply when the taper is not self-locking.
    /// Zero for a self-holding taper.
    /// </summary>
    public double RequiredNutForce { get; private set; }  // N

    // Combined loading — one friction interface, shared by both components
    public double TangentialForce { get; set; }          // N, from the applied torque
    public double ResultantForce { get; set; }           // N
    /// <summary>Torque capacity expressed as a tangential force, for the interaction check.</summary>
    public double TangentialForceCapacity { get; set; }  // N

    // Safety Factors
    public double SafetyFactorSliding { get; set; }      // Against sliding (torque alone)
    public double SafetyFactorAxial { get; set; }        // Against sliding (axial alone)
    public double SafetyFactorCombined { get; set; }     // Against sliding (resultant)
    public double SafetyFactorShaftStress { get; set; }  // Shaft yield safety
    public double SafetyFactorHubStress { get; set; }    // Hub yield safety

    // Standard taper ratios
    // Morse tapers are all close to 1:20 (nominally 1:19.002 to 1:20.047
    // depending on the number) - there is no 1:50 Morse taper. 1:50 is a
    // self-holding pin taper, and 1:30 is a non-self-holding tool taper.
    // The metric TOOL taper of ISO 296 is 1:20, not 1:16. DIN 254 is the general
    // taper series and 1:16 is one of its members (pipe threads, among others),
    // so calling 1:16 "Metric" pointed at the wrong thing. The steep 7:24
    // machine taper (ISO 7388, SK/BT) is not a self-holding taper at all and is
    // not offered: nothing in this module models the retaining mechanism it needs.
    public static readonly Dictionary<string, double> StandardTapers = new()
    {
        { "1:20 (Morse 0-6 and metric tool taper, approx.)", 20 },
        { "1:16 (DIN 254 general series)", 16 },
        { "1:10", 10 },
        { "1:5", 5 },
        { "1:30 (DIN 254, non-self-holding)", 30 },
        { "1:50 (Taper pin, DIN 1B)", 50 }
    };

    public void Calculate()
    {
        Sections.Clear();

        CalculateGeometry();
        CalculateInterference();
        CalculateContactPressure();
        CalculateStresses();
        CalculateCapacities();
        CalculateSafetyFactors();
    }

    private void CalculateGeometry()
    {
        // Taper angle: tan(α/2) = 1 / (2*C) where C = taper ratio
        HalfAngle = Math.Atan(1.0 / (2.0 * TaperRatio)) * 180.0 / Math.PI;
        TaperAngle = 2.0 * HalfAngle;
        TaperSlopePercent = 100.0 / TaperRatio;

        // Calculate small and large diameters from mean diameter
        double deltaD = FitLength / TaperRatio;
        SmallDiameter = MeanDiameter - deltaD / 2.0;
        LargeDiameter = MeanDiameter + deltaD / 2.0;
    }

    private void CalculateInterference()
    {
        // Radial interference from axial displacement
        // δr = Δs * tan(α/2)
        double halfAngleRad = HalfAngle * Math.PI / 180.0;
        RadialInterference = AxialDisplacement * Math.Tan(halfAngleRad);
        DiametralInterference = 2.0 * RadialInterference * 1000.0; // to μm

        // Surface roughness loss (DIN 7190)
        RoughnessLoss = 0.8 * (ShaftRoughnessRz + HubRoughnessRz);

        // Effective interference
        EffectiveInterference = Math.Max(0, DiametralInterference - RoughnessLoss);

        // Required displacement for this interference
        if (DiametralInterference > 0)
        {
            RequiredDisplacement = (DiametralInterference / 1000.0) / (2.0 * Math.Tan(halfAngleRad));
        }
    }

    /// <summary>Lamé pressure (MPa) at a local joint diameter, for the interference given in microns.</summary>
    private double PressureAt(double d, double interferenceMicrons)
    {
        double di = ShaftInnerDiameter;
        double da = HubOuterDiameter;

        if (d <= 0 || da <= d || di < 0 || di >= d) return 0;

        double Qi = di > 0 ? di / d : 0;
        double Qa = d / da;

        double E1 = ShaftMaterial.ElasticModulus * 1000; // GPa to MPa
        double E2 = HubMaterial.ElasticModulus * 1000;
        if (E1 <= 0 || E2 <= 0) return 0;

        double C1 = (1 + Qi * Qi) / (1 - Qi * Qi) - ShaftMaterial.PoissonRatio;
        double C2 = (1 + Qa * Qa) / (1 - Qa * Qa) + HubMaterial.PoissonRatio;

        double denominator = d * (C1 / E1 + C2 / E2);
        return denominator > 0 ? (interferenceMicrons / 1000.0) / denominator : 0;
    }

    private void CalculateContactPressure()
    {
        // Quoted at the mean diameter, which is the section the capacity formulae
        // are written for. The stress check looks along the whole cone.
        ContactPressure = PressureAt(MeanDiameter, EffectiveInterference);
    }

    private static double VonMises(double sigmaT, double sigmaR)
        => Math.Sqrt(sigmaT * sigmaT - sigmaT * sigmaR + sigmaR * sigmaR);

    /// <summary>
    /// Stresses at one section along the cone, at every candidate point in both
    /// members.
    ///
    /// A HOLLOW shaft is stressed hardest at its BORE, where σ_r has relaxed to
    /// zero and σ_t has risen to −2p/(1−Qi²); reading the fit surface alone
    /// understates it by a third at Qi = 0,8.
    /// </summary>
    private TaperSection SectionAt(string name, double d)
    {
        double di = ShaftInnerDiameter;
        double da = HubOuterDiameter;

        double p = PressureAt(d, EffectiveInterference);
        double Qi = di > 0 && di < d ? di / d : 0;
        double Qa = da > d ? d / da : 0;

        double shaftSurfaceT = Qi > 0 ? -p * (1 + Qi * Qi) / (1 - Qi * Qi) : -p;
        double shaftT = shaftSurfaceT, shaftR = -p, shaftV = VonMises(shaftSurfaceT, -p);
        string shaftPoint = "fit surface";

        if (Qi > 0)
        {
            double boreT = -2.0 * p / (1 - Qi * Qi);
            double boreV = VonMises(boreT, 0);
            if (boreV > shaftV) { shaftT = boreT; shaftR = 0; shaftV = boreV; shaftPoint = "bore"; }
        }

        double hubBoreT = Qa > 0 ? p * (1 + Qa * Qa) / (1 - Qa * Qa) : 0;
        double hubT = hubBoreT, hubR = -p, hubV = VonMises(hubBoreT, -p);
        string hubPoint = "bore";

        double hubRimT = Qa > 0 ? 2.0 * p * Qa * Qa / (1 - Qa * Qa) : 0;
        double hubRimV = VonMises(hubRimT, 0);
        if (hubRimV > hubV) { hubT = hubRimT; hubR = 0; hubV = hubRimV; hubPoint = "outer surface"; }

        return new TaperSection
        {
            Name = name,
            Diameter = d,
            Pressure = p,
            ShaftTangential = shaftT,
            ShaftRadial = shaftR,
            ShaftVonMises = shaftV,
            ShaftPoint = shaftPoint,
            HubTangential = hubT,
            HubRadial = hubR,
            HubVonMises = hubV,
            HubPoint = hubPoint
        };
    }

    /// <summary>
    /// Stresses along the whole cone, not just at the mean diameter.
    ///
    /// The diametral interference Δs/X is the same at every section, but the
    /// local pressure depends on the local diameter and wall ratio — so with a
    /// constant hub outside diameter the pressure RISES towards the small end.
    /// On the default Ø50 1:10 example that is 31,8 MPa at the small end against
    /// 28,4 at the mean, and the hub stress is 6 % higher than the mean value.
    /// (The page used to say the large end governs "where the wall is thinnest";
    /// with a constant D_a the opposite is true.)
    /// </summary>
    private void CalculateStresses()
    {
        Sections.Add(SectionAt("Small end", SmallDiameter));
        Sections.Add(SectionAt("Mean", MeanDiameter));
        Sections.Add(SectionAt("Large end", LargeDiameter));

        var shaftWorst = Sections[0];
        var hubWorst = Sections[0];
        foreach (var s in Sections)
        {
            if (s.ShaftVonMises > shaftWorst.ShaftVonMises) shaftWorst = s;
            if (s.HubVonMises > hubWorst.HubVonMises) hubWorst = s;
        }

        ShaftTangentialStress = shaftWorst.ShaftTangential;
        ShaftRadialStress = shaftWorst.ShaftRadial;
        ShaftVonMisesStress = shaftWorst.ShaftVonMises;
        ShaftStressPoint = shaftWorst.ShaftPoint;

        HubTangentialStress = hubWorst.HubTangential;
        HubRadialStress = hubWorst.HubRadial;
        HubVonMisesStress = hubWorst.HubVonMises;
        HubStressPoint = hubWorst.HubPoint;

        GoverningSection = hubWorst.HubVonMises >= shaftWorst.ShaftVonMises
            ? hubWorst.Name : shaftWorst.Name;
    }

    private void CalculateCapacities()
    {
        double dm = MeanDiameter / 1000.0; // m
        double L = FitLength / 1000.0;      // m
        double halfAngleRad = HalfAngle * Math.PI / 180.0;
        double p = ContactPressure * 1e6;   // Pa

        double muStatic = FrictionCoefficientStatic;
        double muDynamic = FrictionCoefficientDynamic;

        // Torque capacity: T = μ * p * π * dm² * L / 2
        MaxTorqueCapacity = muDynamic * p * Math.PI * dm * dm * L / 2.0;

        double area = Math.PI * dm * L;          // N/Pa, the conical contact term
        double tanPhi = Math.Tan(halfAngleRad);
        double cosPhi = Math.Cos(halfAngleRad);

        // Self-locking is a property of the friction coefficient and the angle,
        // not of a fixed angle threshold. The wedge pushes the hub back off with
        // p*A*tan(phi); friction resists with p*A*mu/cos(phi). A 7,5 degree cut-off
        // takes no account of mu at all: at mu_s = 0,10 a 1:5 taper (phi = 5,71)
        // is already at its limit and drew no warning.
        IsSelfLocking = muStatic / cosPhi > tanPhi;

        // Force needed to pull the hub OFF, using the STATIC coefficient: this is
        // a break-loose condition, not sliding in service. Negative means the
        // cone ejects the hub by itself.
        ExtractionForce = p * area * (muStatic / cosPhi - tanPhi);

        // Service holding capacity against an axial load, using the DYNAMIC
        // coefficient. When the taper is not self-locking there is NO holding
        // capacity: the old code fell back to the absolute magnitude here, so a
        // 1:2 cone that ejects its own hub reported +232 kN of axial capacity.
        double axialFactor = muDynamic / cosPhi - tanPhi;
        MaxAxialForceCapacity = axialFactor > 0 ? p * area * axialFactor : 0;

        // What a retaining nut has to hold when the taper cannot hold itself.
        RequiredNutForce = IsSelfLocking ? 0 : p * area * (tanPhi - muStatic / cosPhi);

        // Required assembly force (pushing ON: friction and wedge both oppose it)
        RequiredAssemblyForce = p * area * (muStatic / cosPhi + tanPhi);

        // Combined loading. One friction interface carries both components, so
        // they are not independent — but unlike a cylindrical joint the two
        // capacities are not the same: the wedge opposes an axial load and does
        // nothing to a torque. A single "resultant capacity" cannot represent
        // both, so the two are combined through an interaction ellipse instead,
        //     (F_t/F_t,cap)^2 + (F_a/F_a,cap)^2 <= 1,
        // which reduces exactly to each separate safety when the other load is
        // zero and is always below the lower of the two when both act.
        TangentialForce = MeanDiameter > 0
            ? 2000.0 * AppliedTorque * LoadFactor / MeanDiameter : 0;
        double axial = AppliedAxialForce * LoadFactor;
        ResultantForce = Math.Sqrt(TangentialForce * TangentialForce + axial * axial);
        TangentialForceCapacity = MeanDiameter > 0
            ? 2000.0 * MaxTorqueCapacity / MeanDiameter : 0;
    }

    private void CalculateSafetyFactors()
    {
        // Design loads include the application factor, consistent with the other modules
        double designTorque = AppliedTorque * LoadFactor;
        double designAxialForce = AppliedAxialForce * LoadFactor;

        // Sliding safety factor (against torque)
        SafetyFactorSliding = designTorque > 0 ? MaxTorqueCapacity / designTorque : 999;

        // Axial force safety factor
        SafetyFactorAxial = designAxialForce > 0 ? MaxAxialForceCapacity / designAxialForce : 999;

        // Interaction of torque and axial force, both drawing on one friction
        // interface. A zero capacity with a load on it gives a zero safety,
        // which is what a non-self-locking taper under axial load deserves.
        if (ResultantForce <= 0)
        {
            SafetyFactorCombined = 999;
        }
        else
        {
            double torqueUse = TangentialForceCapacity > 0
                ? TangentialForce / TangentialForceCapacity
                : (TangentialForce > 0 ? double.PositiveInfinity : 0);
            double axialUse = MaxAxialForceCapacity > 0
                ? designAxialForce / MaxAxialForceCapacity
                : (designAxialForce > 0 ? double.PositiveInfinity : 0);

            double demand = Math.Sqrt(torqueUse * torqueUse + axialUse * axialUse);
            SafetyFactorCombined = demand > 0 && !double.IsInfinity(demand) ? 1.0 / demand : 0;
        }

        // Stress safety factors (against yield) - Re / σ_vonMises
        SafetyFactorShaftStress = ShaftVonMisesStress > 0 ? ShaftMaterial.YieldStrength / ShaftVonMisesStress : 999;
        SafetyFactorHubStress = HubVonMisesStress > 0 ? HubMaterial.YieldStrength / HubVonMisesStress : 999;
    }
}

/// <summary>One section along the cone, with the stresses it carries.</summary>
public sealed class TaperSection
{
    public string Name { get; init; } = "";
    public double Diameter { get; init; }          // mm
    public double Pressure { get; init; }          // MPa

    public double ShaftTangential { get; init; }   // MPa
    public double ShaftRadial { get; init; }       // MPa
    public double ShaftVonMises { get; init; }     // MPa
    public string ShaftPoint { get; init; } = "";

    public double HubTangential { get; init; }     // MPa
    public double HubRadial { get; init; }         // MPa
    public double HubVonMises { get; init; }       // MPa
    public string HubPoint { get; init; } = "";
}
