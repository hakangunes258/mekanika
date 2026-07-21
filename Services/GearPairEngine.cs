using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Cylindrical gear pair calculation engine based on ISO 6336 / DIN 3990
/// Calculates geometry, forces, stresses, and safety factors for spur and helical gears
/// </summary>
public class GearPairEngine
{
    // ============ INPUT PARAMETERS ============

    // Power Data
    public double Power { get; set; }                    // kW
    public double Speed1 { get; set; }                   // rpm (pinion)
    public double ApplicationFactor { get; set; } = 1.0; // KA
    public double RequiredServiceLife { get; set; }      // hours

    // Geometry - Basic
    public double NormalModule { get; set; }             // mn (mm)
    public double PressureAngle { get; set; } = 20.0;    // αn (degrees)
    public double HelixAngle { get; set; } = 0.0;        // β (degrees)
    public double CenterDistance { get; set; }           // a (mm)

    // Geometry - Gear 1 (Pinion)
    public int NumberOfTeeth1 { get; set; }              // z1
    public double FaceWidth1 { get; set; }               // b1 (mm)
    public double ProfileShiftCoeff1 { get; set; }       // x1

    // Geometry - Gear 2 (Wheel)
    public int NumberOfTeeth2 { get; set; }              // z2
    public double FaceWidth2 { get; set; }               // b2 (mm)
    public double ProfileShiftCoeff2 { get; set; }       // x2 (calculated)

    // Reference Profile (ISO 53)
    public double DedendumCoeff { get; set; } = 1.25;    // h*fP
    public double RootRadiusCoeff { get; set; } = 0.3;   // ρ*fP
    public double AddendumCoeff { get; set; } = 1.0;     // h*aP

    // Quality
    public int QualityGrade1 { get; set; } = 6;          // ISO 1328 / DIN 3961
    public int QualityGrade2 { get; set; } = 6;

    // Materials
    public GearMaterial Material1 { get; set; } = new();
    public GearMaterial Material2 { get; set; } = new();

    // ============ CALCULATED VALUES - GEOMETRY ============

    // Basic Geometry
    public double TransverseModule { get; set; }         // mt (mm)
    public double TransversePressureAngle { get; set; }  // αt (degrees)
    public double WorkingPressureAngle { get; set; }     // αwt (degrees)
    public double BaseHelixAngle { get; set; }           // βb (degrees)
    public double GearRatio { get; set; }                // u = z2/z1
    public double SumProfileShift { get; set; }          // Σx

    // Diameters - Gear 1
    public double ReferenceDiameter1 { get; set; }       // d1 (mm)
    public double BaseDiameter1 { get; set; }            // db1 (mm)
    public double TipDiameter1 { get; set; }             // da1 (mm)
    public double RootDiameter1 { get; set; }            // df1 (mm)
    public double WorkingPitchDiameter1 { get; set; }    // dw1 (mm)

    // Diameters - Gear 2
    public double ReferenceDiameter2 { get; set; }       // d2 (mm)
    public double BaseDiameter2 { get; set; }            // db2 (mm)
    public double TipDiameter2 { get; set; }             // da2 (mm)
    public double RootDiameter2 { get; set; }            // df2 (mm)
    public double WorkingPitchDiameter2 { get; set; }    // dw2 (mm)

    // Tooth Dimensions
    public double Addendum1 { get; set; }                // ha1 (mm)
    public double Addendum2 { get; set; }                // ha2 (mm)
    public double Dedendum1 { get; set; }                // hf1 (mm)
    public double Dedendum2 { get; set; }                // hf2 (mm)
    public double ToothHeight { get; set; }              // h (mm)
    public double TipClearance { get; set; }             // c (mm)

    // Contact Ratios
    public double TransverseContactRatio { get; set; }   // εα
    public double OverlapRatio { get; set; }             // εβ
    public double TotalContactRatio { get; set; }        // εγ

    // ============ CALCULATED VALUES - KINEMATICS ============

    public double Speed2 { get; set; }                   // rpm (wheel)
    public double Torque1 { get; set; }                  // Nm (pinion)
    public double Torque2 { get; set; }                  // Nm (wheel)
    public double PitchLineVelocity { get; set; }        // v (m/s)

    // ============ CALCULATED VALUES - FORCES ============

    public double TangentialForce { get; set; }          // Ft (N)
    public double RadialForce { get; set; }              // Fr (N)
    public double AxialForce { get; set; }               // Fa (N)
    public double NormalForce { get; set; }              // Fn (N)
    public double SpecificLoad { get; set; }             // w (N/mm)

    // ============ CALCULATED VALUES - LOAD FACTORS ============

    public double DynamicFactor { get; set; }            // KV
    public double FaceLoadFactorFlank { get; set; }      // KHβ
    public double FaceLoadFactorRoot { get; set; }       // KFβ
    public double TransverseLoadFactorFlank { get; set; }// KHα
    public double TransverseLoadFactorRoot { get; set; } // KFα

    // ============ CALCULATED VALUES - TOOTH ROOT (BENDING) ============

    public double ToothFormFactor1 { get; set; }         // YF1
    public double ToothFormFactor2 { get; set; }         // YF2
    public double StressCorrectionFactor1 { get; set; }  // YS1
    public double StressCorrectionFactor2 { get; set; }  // YS2
    public double HelixAngleFactorRoot { get; set; }     // Yβ
    public double ContactRatioFactorRoot { get; set; }   // Yε

    public double NominalRootStress1 { get; set; }       // σF0,1 (MPa)
    public double NominalRootStress2 { get; set; }       // σF0,2 (MPa)
    public double RootStress1 { get; set; }              // σF1 (MPa)
    public double RootStress2 { get; set; }              // σF2 (MPa)

    public double PermissibleRootStress1 { get; set; }   // σFP1 (MPa)
    public double PermissibleRootStress2 { get; set; }   // σFP2 (MPa)
    public double RootSafetyFactor1 { get; set; }        // SF1
    public double RootSafetyFactor2 { get; set; }        // SF2

    // ============ CALCULATED VALUES - TOOTH FLANK (PITTING) ============

    public double ZoneFactorH { get; set; }              // ZH
    public double ElasticityFactor { get; set; }         // ZE (√N/mm²)
    public double ContactRatioFactorFlank { get; set; }  // Zε
    public double HelixAngleFactorFlank { get; set; }    // Zβ

    public double NominalContactStress { get; set; }     // σH0 (MPa)
    public double ContactStress { get; set; }            // σH (MPa)

    public double SingleToothContactFactor1 { get; set; }// ZB
    public double SingleToothContactFactor2 { get; set; }// ZD
    public double ContactStress1 { get; set; }           // σHB (MPa)
    public double ContactStress2 { get; set; }           // σHD (MPa)

    public double PermissibleContactStress1 { get; set; }// σHP1 (MPa)
    public double PermissibleContactStress2 { get; set; }// σHP2 (MPa)
    public double FlankSafetyFactor1 { get; set; }       // SH1
    public double FlankSafetyFactor2 { get; set; }       // SH2

    // ============ CALCULATED VALUES - LIFE ============

    public double LoadCycles1 { get; set; }              // NL1 (millions)
    public double LoadCycles2 { get; set; }              // NL2 (millions)
    public double LifeFactorRoot1 { get; set; }          // YNT1
    public double LifeFactorRoot2 { get; set; }          // YNT2
    public double LifeFactorFlank1 { get; set; }         // ZNT1
    public double LifeFactorFlank2 { get; set; }         // ZNT2

    // ============ MINIMUM SAFETY FACTORS ============

    public double MinRootSafety { get; set; }
    public double MinFlankSafety { get; set; }

    // ============ STANDARD MODULES (ISO 54) ============

    public static readonly double[] StandardModules = {
        0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.125, 1.25, 1.375, 1.5, 1.75,
        2.0, 2.25, 2.5, 2.75, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0,
        7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 14.0, 16.0, 18.0, 20.0
    };

    // ============ MAIN CALCULATION METHOD ============

    public void Calculate()
    {
        CalculateBasicGeometry();
        CalculateDiameters();
        CalculateContactRatios();
        CalculateKinematics();
        CalculateForces();
        CalculateLoadFactors();
        CalculateToothRootStrength();
        CalculateToothFlankStrength();
        CalculateSafetyFactors();
    }

    // ============ CALCULATION STEPS ============

    private void CalculateBasicGeometry()
    {
        // Convert angles to radians for calculations
        double betaRad = HelixAngle * Math.PI / 180.0;
        double alphanRad = PressureAngle * Math.PI / 180.0;

        // Transverse module: mt = mn / cos(β)
        TransverseModule = NormalModule / Math.Cos(betaRad);

        // Transverse pressure angle: tan(αt) = tan(αn) / cos(β)
        double tanAlphaT = Math.Tan(alphanRad) / Math.Cos(betaRad);
        TransversePressureAngle = Math.Atan(tanAlphaT) * 180.0 / Math.PI;

        // Base helix angle: tan(βb) = tan(β) × cos(αt)
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;
        double tanBetaB = Math.Tan(betaRad) * Math.Cos(alphatRad);
        BaseHelixAngle = Math.Atan(tanBetaB) * 180.0 / Math.PI;

        // Gear ratio
        GearRatio = (double)NumberOfTeeth2 / NumberOfTeeth1;

        // Reference center distance
        double referenceCenter = TransverseModule * (NumberOfTeeth1 + NumberOfTeeth2) / 2.0;

        // Calculate sum of profile shift coefficients from actual center distance
        // a = mt × (z1 + z2) / 2 × cos(αt) / cos(αwt)
        // This gives us αwt, then we can find Σx
        double invAlphaT = Math.Tan(alphatRad) - alphatRad;

        // Working pressure angle from center distance
        double cosAlphaWT = referenceCenter * Math.Cos(alphatRad) / CenterDistance;
        if (cosAlphaWT > 1.0) cosAlphaWT = 1.0;
        if (cosAlphaWT < -1.0) cosAlphaWT = -1.0;
        double alphaWTRad = Math.Acos(cosAlphaWT);
        WorkingPressureAngle = alphaWTRad * 180.0 / Math.PI;

        // Involute function difference
        double invAlphaWT = Math.Tan(alphaWTRad) - alphaWTRad;
        double invDiff = invAlphaWT - invAlphaT;

        // Sum of profile shifts: Σx = (z1 + z2) × inv_diff / (2 × tan(αn))
        SumProfileShift = (NumberOfTeeth1 + NumberOfTeeth2) * invDiff / (2.0 * Math.Tan(alphanRad));

        // If profile shift of gear 1 is given, calculate gear 2
        ProfileShiftCoeff2 = SumProfileShift - ProfileShiftCoeff1;
    }

    private void CalculateDiameters()
    {
        double betaRad = HelixAngle * Math.PI / 180.0;
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;

        // Reference diameters: d = mt × z
        ReferenceDiameter1 = TransverseModule * NumberOfTeeth1;
        ReferenceDiameter2 = TransverseModule * NumberOfTeeth2;

        // Base diameters: db = d × cos(αt)
        BaseDiameter1 = ReferenceDiameter1 * Math.Cos(alphatRad);
        BaseDiameter2 = ReferenceDiameter2 * Math.Cos(alphatRad);

        // Working pitch diameters
        WorkingPitchDiameter1 = 2.0 * CenterDistance / (1.0 + GearRatio);
        WorkingPitchDiameter2 = 2.0 * CenterDistance * GearRatio / (1.0 + GearRatio);

        // Tip alteration coefficient (k*mn = 0 for standard design)
        double tipAlteration = 0.0;

        // Addenda: ha = mn × (haP* + x + k)
        Addendum1 = NormalModule * (AddendumCoeff + ProfileShiftCoeff1 + tipAlteration);
        Addendum2 = NormalModule * (AddendumCoeff + ProfileShiftCoeff2 + tipAlteration);

        // Dedenda: hf = mn × (hfP* - x)
        Dedendum1 = NormalModule * (DedendumCoeff - ProfileShiftCoeff1);
        Dedendum2 = NormalModule * (DedendumCoeff - ProfileShiftCoeff2);

        // Tip diameters: da = d + 2×ha
        TipDiameter1 = ReferenceDiameter1 + 2.0 * Addendum1;
        TipDiameter2 = ReferenceDiameter2 + 2.0 * Addendum2;

        // Root diameters: df = d - 2×hf
        RootDiameter1 = ReferenceDiameter1 - 2.0 * Dedendum1;
        RootDiameter2 = ReferenceDiameter2 - 2.0 * Dedendum2;

        // Tooth height and tip clearance
        ToothHeight = Addendum1 + Dedendum1;
        TipClearance = NormalModule * (DedendumCoeff - AddendumCoeff);
    }

    private void CalculateContactRatios()
    {
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;
        double alphaWTRad = WorkingPressureAngle * Math.PI / 180.0;
        double betaRad = HelixAngle * Math.PI / 180.0;
        double betabRad = BaseHelixAngle * Math.PI / 180.0;

        // Base pitch: pbt = π × mt × cos(αt)
        double basePitch = Math.PI * TransverseModule * Math.Cos(alphatRad);

        // Length of path of contact
        double term1 = Math.Sqrt(Math.Pow(TipDiameter1 / 2.0, 2) - Math.Pow(BaseDiameter1 / 2.0, 2));
        double term2 = Math.Sqrt(Math.Pow(TipDiameter2 / 2.0, 2) - Math.Pow(BaseDiameter2 / 2.0, 2));
        double term3 = CenterDistance * Math.Sin(alphaWTRad);

        double pathOfContact = term1 + term2 - term3;
        if (pathOfContact < 0) pathOfContact = 0;

        // Transverse contact ratio: εα = ga / pbt
        TransverseContactRatio = pathOfContact / basePitch;

        // Overlap ratio: εβ = b × sin(β) / (π × mn)
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);
        if (Math.Abs(HelixAngle) > 0.01)
        {
            OverlapRatio = effectiveFaceWidth * Math.Sin(betaRad) / (Math.PI * NormalModule);
        }
        else
        {
            OverlapRatio = 0.0;
        }

        // Total contact ratio: εγ = εα + εβ
        TotalContactRatio = TransverseContactRatio + OverlapRatio;
    }

    private void CalculateKinematics()
    {
        // Output speed
        Speed2 = Speed1 / GearRatio;

        // Torque from power: T = P × 9550 / n
        Torque1 = Power * 9550.0 / Speed1;
        Torque2 = Power * 9550.0 / Speed2;

        // Pitch line velocity: v = π × d1 × n1 / 60000
        PitchLineVelocity = Math.PI * ReferenceDiameter1 * Speed1 / 60000.0;
    }

    private void CalculateForces()
    {
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;
        double betaRad = HelixAngle * Math.PI / 180.0;
        double alphanRad = PressureAngle * Math.PI / 180.0;

        // Tangential force: Ft = 2000 × T1 / d1
        TangentialForce = 2000.0 * Torque1 / ReferenceDiameter1;

        // Radial force: Fr = Ft × tan(αt)
        RadialForce = TangentialForce * Math.Tan(alphatRad);

        // Axial force: Fa = Ft × tan(β)
        AxialForce = TangentialForce * Math.Tan(betaRad);

        // Normal force: Fn = Ft / (cos(αn) × cos(β))
        NormalForce = TangentialForce / (Math.Cos(alphanRad) * Math.Cos(betaRad));

        // Specific load (force per unit facewidth)
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);
        SpecificLoad = TangentialForce / effectiveFaceWidth;
    }

    private void CalculateLoadFactors()
    {
        // Dynamic factor KV (Method B, ISO 6336)
        // Simplified calculation based on pitch line velocity and quality
        int avgQuality = (QualityGrade1 + QualityGrade2) / 2;
        double Cv = 0.8; // Coefficient depending on accuracy grade

        // K = v × sqrt(z1/u) / 100 (simplified)
        double K = PitchLineVelocity * Math.Sqrt(NumberOfTeeth1 / GearRatio) / 100.0;

        if (K < 0.2)
        {
            DynamicFactor = 1.0 + Cv * K;
        }
        else
        {
            DynamicFactor = 1.0 + Cv * 0.2 + (K - 0.2) * 0.5;
        }

        // Limit KV
        if (DynamicFactor > 2.0) DynamicFactor = 2.0;
        if (DynamicFactor < 1.0) DynamicFactor = 1.0;

        // Face load factor KHβ (simplified - assuming good alignment)
        // Based on ISO 6336 Method C
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);
        double faceWidthRatio = effectiveFaceWidth / ReferenceDiameter1;

        // Simplified KHβ based on face width ratio and quality
        FaceLoadFactorFlank = 1.0 + 0.05 * faceWidthRatio * avgQuality;
        if (FaceLoadFactorFlank > 2.0) FaceLoadFactorFlank = 2.0;
        if (FaceLoadFactorFlank < 1.0) FaceLoadFactorFlank = 1.0;

        FaceLoadFactorRoot = Math.Pow(FaceLoadFactorFlank, 0.9); // Approximate relationship

        // Transverse load factor KHα (Method B)
        // Depends on contact ratio and accuracy
        if (TotalContactRatio < 2.0)
        {
            TransverseLoadFactorFlank = 1.0 + (TotalContactRatio - 1.0) * 0.1 * avgQuality / 6.0;
        }
        else
        {
            TransverseLoadFactorFlank = 1.0 + 0.1 * avgQuality / 6.0;
        }

        if (TransverseLoadFactorFlank > 1.5) TransverseLoadFactorFlank = 1.5;
        if (TransverseLoadFactorFlank < 1.0) TransverseLoadFactorFlank = 1.0;

        TransverseLoadFactorRoot = TransverseLoadFactorFlank;
    }

    private void CalculateToothRootStrength()
    {
        double betaRad = HelixAngle * Math.PI / 180.0;
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);

        // Tooth form factor YF (simplified formula)
        // Based on number of teeth and profile shift
        double zn1 = NumberOfTeeth1 / Math.Pow(Math.Cos(betaRad), 3);
        double zn2 = NumberOfTeeth2 / Math.Pow(Math.Cos(betaRad), 3);

        // Simplified YF based on virtual number of teeth
        ToothFormFactor1 = 2.5 - 0.02 * zn1 + 0.3 * ProfileShiftCoeff1;
        ToothFormFactor2 = 2.5 - 0.02 * zn2 + 0.3 * ProfileShiftCoeff2;

        // Limits
        if (ToothFormFactor1 < 1.2) ToothFormFactor1 = 1.2;
        if (ToothFormFactor1 > 3.0) ToothFormFactor1 = 3.0;
        if (ToothFormFactor2 < 1.2) ToothFormFactor2 = 1.2;
        if (ToothFormFactor2 > 3.0) ToothFormFactor2 = 3.0;

        // Stress correction factor YS
        // Depends on tooth root fillet radius
        double qs1 = TransverseModule / (RootRadiusCoeff * NormalModule);
        double qs2 = qs1; // Same for both gears with same reference profile

        StressCorrectionFactor1 = 1.2 + 0.13 * qs1;
        StressCorrectionFactor2 = 1.2 + 0.13 * qs2;

        // Limits
        if (StressCorrectionFactor1 < 1.5) StressCorrectionFactor1 = 1.5;
        if (StressCorrectionFactor1 > 2.5) StressCorrectionFactor1 = 2.5;
        if (StressCorrectionFactor2 < 1.5) StressCorrectionFactor2 = 1.5;
        if (StressCorrectionFactor2 > 2.5) StressCorrectionFactor2 = 2.5;

        // Helix angle factor Yβ
        HelixAngleFactorRoot = 1.0 - Math.Abs(HelixAngle) / 120.0;
        if (HelixAngleFactorRoot < 0.75) HelixAngleFactorRoot = 0.75;

        // Contact ratio factor Yε
        ContactRatioFactorRoot = 0.25 + 0.75 / TransverseContactRatio;
        if (ContactRatioFactorRoot > 1.0) ContactRatioFactorRoot = 1.0;

        // Nominal tooth root stress: σF0 = Ft / (b × mn) × YF × YS × Yβ × Yε
        NominalRootStress1 = (TangentialForce / (effectiveFaceWidth * NormalModule)) *
                            ToothFormFactor1 * StressCorrectionFactor1 * HelixAngleFactorRoot * ContactRatioFactorRoot;

        NominalRootStress2 = (TangentialForce / (effectiveFaceWidth * NormalModule)) *
                            ToothFormFactor2 * StressCorrectionFactor2 * HelixAngleFactorRoot * ContactRatioFactorRoot;

        // Actual tooth root stress: σF = σF0 × KA × KV × KFβ × KFα
        double loadFactorProduct = ApplicationFactor * DynamicFactor * FaceLoadFactorRoot * TransverseLoadFactorRoot;
        RootStress1 = NominalRootStress1 * loadFactorProduct;
        RootStress2 = NominalRootStress2 * loadFactorProduct;

        // Calculate life factors
        CalculateLifeFactors();

        // Permissible tooth root stress
        // σFP = σFlim × YST × YNT × YδrelT × YRrelT × YX
        //
        // NOTE: SFmin is deliberately NOT divided out here. σFP is the permissible
        // stress, and the safety factor reported to the user is SF = σFP/σF - the
        // TRUE safety factor. Dividing by 1.4 here (as this used to) meant a user
        // reading SF = 1.0 actually had 1.4, while the flank side divided by 1.0,
        // so the two sides were not comparable. Required minima are exposed
        // separately as MinSafetyFactorRoot / MinSafetyFactorFlank.
        double YST = 2.0; // Stress correction factor reference

        PermissibleRootStress1 = Material1.BendingFatigueLimit * LifeFactorRoot1 * YST * 0.95;
        PermissibleRootStress2 = Material2.BendingFatigueLimit * LifeFactorRoot2 * YST * 0.95;
    }

    private void CalculateToothFlankStrength()
    {
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;
        double alphaWTRad = WorkingPressureAngle * Math.PI / 180.0;
        double betaRad = HelixAngle * Math.PI / 180.0;
        double betabRad = BaseHelixAngle * Math.PI / 180.0;
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);

        // Zone factor ZH
        // ZH = sqrt(2 × cos(βb) × cos(αwt) / (cos²(αt) × sin(αwt)))
        double cosAlphaT = Math.Cos(alphatRad);
        double cosAlphaWT = Math.Cos(alphaWTRad);
        double sinAlphaWT = Math.Sin(alphaWTRad);
        double cosBetaB = Math.Cos(betabRad);

        ZoneFactorH = Math.Sqrt(2.0 * cosBetaB * cosAlphaWT / (cosAlphaT * cosAlphaT * sinAlphaWT));

        // Elasticity factor ZE
        // ZE = sqrt(1 / (π × ((1-ν1²)/E1 + (1-ν2²)/E2)))
        double E1 = Material1.ElasticModulus * 1000.0; // Convert GPa to MPa
        double E2 = Material2.ElasticModulus * 1000.0;
        double nu1 = Material1.PoissonRatio;
        double nu2 = Material2.PoissonRatio;

        double elasticTerm = (1.0 - nu1 * nu1) / E1 + (1.0 - nu2 * nu2) / E2;
        ElasticityFactor = Math.Sqrt(1.0 / (Math.PI * elasticTerm));

        // Contact ratio factor Zε
        // For εβ ≥ 1: Zε = sqrt(1/εα)
        // For εβ < 1: Zε = sqrt((4-εα)/3 × (1-εβ) + εβ/εα)
        if (OverlapRatio >= 1.0)
        {
            ContactRatioFactorFlank = Math.Sqrt(1.0 / TransverseContactRatio);
        }
        else
        {
            ContactRatioFactorFlank = Math.Sqrt((4.0 - TransverseContactRatio) / 3.0 * (1.0 - OverlapRatio) +
                                               OverlapRatio / TransverseContactRatio);
        }

        // Helix angle factor Zβ
        HelixAngleFactorFlank = Math.Sqrt(Math.Cos(betaRad));

        // Nominal contact stress: σH0 = ZH × ZE × Zε × Zβ × sqrt(Ft/(d1×b) × (u+1)/u)
        double loadTerm = TangentialForce / (ReferenceDiameter1 * effectiveFaceWidth) * (GearRatio + 1.0) / GearRatio;
        NominalContactStress = ZoneFactorH * ElasticityFactor * ContactRatioFactorFlank * HelixAngleFactorFlank *
                              Math.Sqrt(loadTerm);

        // Contact stress with load factors: σH = σH0 × sqrt(KA × KV × KHβ × KHα)
        double loadFactorRoot = Math.Sqrt(ApplicationFactor * DynamicFactor * FaceLoadFactorFlank * TransverseLoadFactorFlank);
        ContactStress = NominalContactStress * loadFactorRoot;

        // Single tooth contact factors ZB and ZD
        // Simplified - set to 1.0 for standard design
        SingleToothContactFactor1 = 1.0;
        SingleToothContactFactor2 = 1.0;

        // Individual contact stresses
        ContactStress1 = ContactStress * SingleToothContactFactor1;
        ContactStress2 = ContactStress * SingleToothContactFactor2;

        // Permissible contact stress
        // σHP = (σHlim × ZNT × ZL × ZV × ZR × ZW × ZX) / SHmin
        // Simplified calculation
        double ZL = 1.0;  // Lubrication factor (oil lubrication)
        double ZV = 0.97; // Speed factor (approximate)
        double ZR = 0.95; // Roughness factor (approximate)
        double ZW = 1.0;  // Work hardening factor
        double ZX = 1.0;  // Size factor

        // σHP = σHlim × ZNT × ZL × ZV × ZR × ZW × ZX (SHmin not divided out -
        // see the note on PermissibleRootStress in CalculateToothRootStrength)
        PermissibleContactStress1 = Material1.ContactFatigueLimit * LifeFactorFlank1 * ZL * ZV * ZR * ZW * ZX;
        PermissibleContactStress2 = Material2.ContactFatigueLimit * LifeFactorFlank2 * ZL * ZV * ZR * ZW * ZX;
    }

    private void CalculateLifeFactors()
    {
        // Number of load cycles: NL = 60 × n × H
        LoadCycles1 = 60.0 * Speed1 * RequiredServiceLife / 1e6; // In millions
        LoadCycles2 = 60.0 * Speed2 * RequiredServiceLife / 1e6;

        // Life factor for tooth root YNT
        // For case-hardened steel: YNT = 1.0 for NL > 3×10^6
        // Simplified - assume infinite life zone
        if (LoadCycles1 >= 3.0) // 3 million cycles
            LifeFactorRoot1 = 1.0;
        else
            LifeFactorRoot1 = Math.Pow(3.0 / LoadCycles1, 0.1);

        if (LoadCycles2 >= 3.0)
            LifeFactorRoot2 = 1.0;
        else
            LifeFactorRoot2 = Math.Pow(3.0 / LoadCycles2, 0.1);

        // Life factor for tooth flank ZNT
        // Similar approach
        if (LoadCycles1 >= 50.0) // 50 million cycles for contact
            LifeFactorFlank1 = 1.0;
        else
            LifeFactorFlank1 = Math.Pow(50.0 / Math.Max(LoadCycles1, 0.1), 0.05);

        if (LoadCycles2 >= 50.0)
            LifeFactorFlank2 = 1.0;
        else
            LifeFactorFlank2 = Math.Pow(50.0 / Math.Max(LoadCycles2, 0.1), 0.05);

        // Limit life factors
        if (LifeFactorRoot1 > 2.5) LifeFactorRoot1 = 2.5;
        if (LifeFactorRoot2 > 2.5) LifeFactorRoot2 = 2.5;
        if (LifeFactorFlank1 > 1.6) LifeFactorFlank1 = 1.6;
        if (LifeFactorFlank2 > 1.6) LifeFactorFlank2 = 1.6;
    }

    /// <summary>
    /// Recommended minimum tooth root safety factor SFmin (ISO 6336).
    /// The reported RootSafetyFactor must be compared against this, not against 1.0.
    /// </summary>
    public const double MinSafetyFactorRoot = 1.4;

    /// <summary>
    /// Recommended minimum tooth flank safety factor SHmin (ISO 6336).
    /// </summary>
    public const double MinSafetyFactorFlank = 1.0;

    private void CalculateSafetyFactors()
    {
        // Root safety factor: SF = σFP / σF (compare against MinSafetyFactorRoot)
        RootSafetyFactor1 = RootStress1 > 0 ? PermissibleRootStress1 / RootStress1 : 999;
        RootSafetyFactor2 = RootStress2 > 0 ? PermissibleRootStress2 / RootStress2 : 999;

        // Flank safety factor: SH = σHP / σH (should be ≥ 1.0)
        FlankSafetyFactor1 = ContactStress1 > 0 ? PermissibleContactStress1 / ContactStress1 : 999;
        FlankSafetyFactor2 = ContactStress2 > 0 ? PermissibleContactStress2 / ContactStress2 : 999;

        // Minimum safety factors
        MinRootSafety = Math.Min(RootSafetyFactor1, RootSafetyFactor2);
        MinFlankSafety = Math.Min(FlankSafetyFactor1, FlankSafetyFactor2);
    }

    // ============ HELPER METHODS ============

    /// <summary>
    /// Calculate profile shift coefficient for gear 2 based on center distance
    /// </summary>
    public void CalculateProfileShift2()
    {
        double betaRad = HelixAngle * Math.PI / 180.0;
        double alphanRad = PressureAngle * Math.PI / 180.0;

        TransverseModule = NormalModule / Math.Cos(betaRad);
        double tanAlphaT = Math.Tan(alphanRad) / Math.Cos(betaRad);
        double alphatRad = Math.Atan(tanAlphaT);

        double referenceCenter = TransverseModule * (NumberOfTeeth1 + NumberOfTeeth2) / 2.0;
        double invAlphaT = Math.Tan(alphatRad) - alphatRad;

        double cosAlphaWT = referenceCenter * Math.Cos(alphatRad) / CenterDistance;
        if (cosAlphaWT > 1.0) cosAlphaWT = 1.0;
        if (cosAlphaWT < -1.0) cosAlphaWT = -1.0;
        double alphaWTRad = Math.Acos(cosAlphaWT);

        double invAlphaWT = Math.Tan(alphaWTRad) - alphaWTRad;
        double invDiff = invAlphaWT - invAlphaT;

        SumProfileShift = (NumberOfTeeth1 + NumberOfTeeth2) * invDiff / (2.0 * Math.Tan(alphanRad));
        ProfileShiftCoeff2 = SumProfileShift - ProfileShiftCoeff1;
    }

    /// <summary>
    /// Get standard module closest to calculated value
    /// </summary>
    public static double GetStandardModule(double calculatedModule)
    {
        return StandardModules.OrderBy(m => Math.Abs(m - calculatedModule)).First();
    }
}

// ============ SUPPORTING CLASSES ============

/// <summary>
/// Gear material properties with fatigue limits
/// </summary>
public class GearMaterial
{
    public string Name { get; set; } = "18CrNiMo7-6";
    public string HeatTreatment { get; set; } = "Case-hardened";
    public double SurfaceHardness { get; set; } = 60;           // HRC
    public double BendingFatigueLimit { get; set; } = 430;      // σFlim (MPa)
    public double ContactFatigueLimit { get; set; } = 1500;     // σHlim (MPa)
    public double TensileStrength { get; set; } = 1200;         // σB (MPa)
    public double YieldStrength { get; set; } = 850;            // σS (MPa)
    public double ElasticModulus { get; set; } = 206;           // E (GPa)
    public double PoissonRatio { get; set; } = 0.3;             // ν

    public override string ToString() => $"{Name} ({HeatTreatment})";

    // Standard gear materials
    public static readonly List<GearMaterial> StandardMaterials = new()
    {
        new GearMaterial
        {
            Name = "C45",
            HeatTreatment = "Normalized",
            SurfaceHardness = 22,
            BendingFatigueLimit = 190,
            ContactFatigueLimit = 620,
            TensileStrength = 620,
            YieldStrength = 340,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        },
        new GearMaterial
        {
            Name = "C45",
            HeatTreatment = "Quenched & Tempered",
            SurfaceHardness = 30,
            BendingFatigueLimit = 260,
            ContactFatigueLimit = 750,
            TensileStrength = 800,
            YieldStrength = 500,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        },
        new GearMaterial
        {
            Name = "42CrMo4",
            HeatTreatment = "Quenched & Tempered",
            SurfaceHardness = 34,
            BendingFatigueLimit = 310,
            ContactFatigueLimit = 850,
            TensileStrength = 1000,
            YieldStrength = 750,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        },
        new GearMaterial
        {
            Name = "42CrMo4",
            HeatTreatment = "Induction Hardened",
            SurfaceHardness = 54,
            BendingFatigueLimit = 360,
            ContactFatigueLimit = 1200,
            TensileStrength = 1100,
            YieldStrength = 900,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        },
        new GearMaterial
        {
            Name = "16MnCr5",
            HeatTreatment = "Case-hardened",
            SurfaceHardness = 58,
            BendingFatigueLimit = 400,
            ContactFatigueLimit = 1400,
            TensileStrength = 1000,
            YieldStrength = 700,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        },
        new GearMaterial
        {
            Name = "18CrNiMo7-6",
            HeatTreatment = "Case-hardened",
            SurfaceHardness = 60,
            BendingFatigueLimit = 430,
            ContactFatigueLimit = 1500,
            TensileStrength = 1200,
            YieldStrength = 850,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        },
        new GearMaterial
        {
            Name = "15CrNi6",
            HeatTreatment = "Case-hardened",
            SurfaceHardness = 60,
            BendingFatigueLimit = 430,
            ContactFatigueLimit = 1500,
            TensileStrength = 1000,
            YieldStrength = 685,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        },
        new GearMaterial
        {
            Name = "34CrNiMo6",
            HeatTreatment = "Nitrided",
            SurfaceHardness = 62,
            BendingFatigueLimit = 380,
            ContactFatigueLimit = 1250,
            TensileStrength = 1100,
            YieldStrength = 900,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        }
    };
}
