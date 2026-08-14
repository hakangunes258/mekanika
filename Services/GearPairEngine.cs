using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>Where the shaft deflection component f_sh comes from.</summary>
public enum ShaftDeflectionSource
{
    /// <summary>Calculated from the shaft dimensions per ISO 6336-1 Eq. (57).</summary>
    Calculated,
    /// <summary>Entered directly, e.g. from a real shaft analysis.</summary>
    Manual,
    /// <summary>Taken as zero. A deliberate choice, not a default — a real shaft deflects.</summary>
    Neglected
}

/// <summary>Where the tip alteration (tip shortening) coefficient k comes from.</summary>
public enum TipAlterationSource
{
    /// <summary>k = 0. Full-depth teeth, whatever that does to the tip clearance.</summary>
    None,
    /// <summary>k = y - Σx, the value that restores the reference profile's tip clearance.</summary>
    Calculated,
    /// <summary>Entered directly, e.g. to match an existing drawing.</summary>
    Manual
}

/// <summary>
/// What the user specifies to fix the tooth thickness. Everything downstream is derived
/// from the allowances A_sne/A_sni, so each mode is converted to those on the way in.
///
/// New members are appended, never reordered: the mode rides in shared links by name and
/// an older link must keep meaning what it meant.
/// </summary>
public enum ToothThicknessAllowanceMode
{
    /// <summary>Derived from the ISO/TR 10064-2 recommended minimum backlash.</summary>
    Automatic,
    /// <summary>The allowances A_sne/A_sni entered directly.</summary>
    Manual,
    /// <summary>Normal backlash j_bn, minimum and maximum.</summary>
    NormalBacklash,
    /// <summary>Circumferential backlash j_wt at the working pitch circle.</summary>
    CircumferentialBacklash,
    /// <summary>Radial backlash j_r.</summary>
    RadialBacklash,
    /// <summary>Base tangent length W_k limits, per gear.</summary>
    SpanLimits,
    /// <summary>Dimension over balls / pins M_d limits, per gear.</summary>
    BallLimits,
    /// <summary>A DIN 3967 tolerance zone, e.g. 27cd.</summary>
    Din3967
}

/// <summary>
/// How a backlash target, which constrains only the SUM of the two gears' allowances, is
/// shared between them. W_k and M_d are measured per gear and need no such rule.
/// </summary>
public enum BacklashSplit
{
    /// <summary>Half the thinning on each gear. The usual choice.</summary>
    Even,
    /// <summary>All of it on the pinion — keeps the wheel at nominal thickness.</summary>
    PinionOnly,
    /// <summary>All of it on the wheel.</summary>
    WheelOnly
}

/// <summary>
/// Cylindrical gear pair calculation engine.
///
/// Geometry and inspection dimensions follow ISO 21771; flank tolerances follow
/// ISO 1328-1; backlash follows ISO/TR 10064-2; load capacity follows ISO 6336
/// (parts 1, 2, 3 and 5), Method B throughout unless noted.
///
/// The load-capacity chain is split across dedicated services so each part can be read
/// against the clause it implements:
///   <see cref="Iso6336DynamicFactor"/>   K_V              - ISO 6336-1 Clause 6
///   <see cref="Iso6336FaceLoadFactor"/>  K_Hbeta, K_Fbeta - ISO 6336-1 Clauses 7.5, 7.6
///   <see cref="Iso6336TransverseFactor"/>K_Halpha, K_Falpha - ISO 6336-1 Clause 8
///   <see cref="Iso6336ToothForm"/>       Y_F, Y_S         - ISO 6336-3 Method B
///   <see cref="Iso6336SurfaceFactors"/>  Z_B/Z_D, Z_L/Z_v/Z_R - ISO 6336-2 Clauses 6, 12
///   <see cref="Iso6336LifeFactors"/>     Y_NT/Z_NT, Y_deltarelT, Y_RrelT, Y_X, Z_X, Z_W
///   <see cref="Iso6336Material"/>        sigma_Flim, sigma_Hlim - ISO 6336-5 Table 1
///   <see cref="GearToothMeasurement"/>   tooth thickness, W_k, M_d, backlash
///
/// NOT covered: scuffing (ISO/TR 13989), micropitting (ISO/TR 15144), tooth flank
/// fracture, and planetary/internal arrangements.
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

    /// <summary>
    /// Where the tip alteration coefficient k comes from. Defaults to None (k = 0), which is
    /// what this module did before k existed — so a shared link written by an older build
    /// still reproduces its own results.
    /// </summary>
    public TipAlterationSource TipAlterationMode { get; set; } = TipAlterationSource.None;

    /// <summary>
    /// Tip alteration coefficient k. An input when <see cref="TipAlterationMode"/> is Manual,
    /// written by the engine when it is Calculated. Applied to both gears:
    /// h_a = m_n (h*_aP + x + k).
    /// </summary>
    public double TipAlterationCoeff { get; set; }

    /// <summary>How k was obtained, for the results card. Null when k is simply 0.</summary>
    public string? TipAlterationNote { get; set; }

    // Quality - ISO 1328-1 flank tolerance class
    public int QualityGrade1 { get; set; } = 6;
    public int QualityGrade2 { get; set; } = 6;

    // === Deviations for the ISO 6336-1 dynamic factor (Method B) ===
    // f_pb (base pitch deviation) and f_falpha (profile form deviation) of the WHEEL,
    // in micrometres. ISO 6336-1:2006 sources these from ISO 1328-1.
    /// <summary>When true, the deviations below are taken as entered; otherwise they are
    /// derived from the wheel's ISO 1328-1 tolerance class.</summary>
    public bool UseMeasuredDeviations { get; set; }
    public double BasePitchDeviation { get; set; }       // f_pb (µm)
    public double ProfileFormDeviation { get; set; }     // f_falpha (µm)
    public double RunningInAllowanceP { get; set; }      // y_p (µm), 0 = conservative
    public double RunningInAllowanceF { get; set; }      // y_f (µm), 0 = conservative
    public double BoreDiameter1 { get; set; }            // di1 (mm), 0 = solid
    public double BoreDiameter2 { get; set; }            // di2 (mm), 0 = solid
    public bool SolidDiscGears { get; set; } = true;

    // === Face load factor inputs (ISO 6336-1 Clause 7.5, Method C) ===
    /// <summary>Where f_sh comes from. Calculated is the default; Neglected must be chosen deliberately.</summary>
    public ShaftDeflectionSource FshSource { get; set; } = ShaftDeflectionSource.Calculated;

    /// <summary>
    /// Pinion/shaft deflection component f_sh (µm). Written by the engine when
    /// <see cref="FshSource"/> is Calculated, read as an input when it is Manual.
    /// </summary>
    public double ShaftDeflectionFsh { get; set; }

    // Shaft dimensions for the ISO 6336-1 Eq. (57) estimate of f_sh. This module does not
    // model shafts; these four numbers are what the standard's approximate method needs.
    /// <summary>Bearing span l (mm).</summary>
    public double ShaftBearingSpan { get; set; }
    /// <summary>Distance s from the pinion mid-plane to the middle of the bearing span (mm).</summary>
    public double PinionOffset { get; set; }
    /// <summary>Outside diameter of the pinion shaft, for bending, d_sh (mm).</summary>
    public double ShaftDiameter { get; set; }
    /// <summary>Bore of a hollow pinion shaft (mm); 0 = solid.</summary>
    public double ShaftBoreDiameter { get; set; }
    /// <summary>Mounting arrangement per ISO 6336-1 Figure 13.</summary>
    public Iso6336ShaftDeflection.Arrangement ShaftArrangement { get; set; }
        = Iso6336ShaftDeflection.Arrangement.A;
    /// <summary>
    /// False when the pinion body cannot stiffen the shaft span whatever the diameter ratio
    /// — a pinion sliding on a feather key, or a normal shrink fit.
    /// </summary>
    public bool PinionCanStiffenShaft { get; set; } = true;

    /// <summary>ISO 6336-1 Eq. (57) breakdown, when f_sh was calculated.</summary>
    public Iso6336ShaftDeflection.Result? ShaftDeflectionResult { get; set; }
    /// <summary>Mesh misalignment from manufacturing f_ma (µm); derived from the ISO 1328-1
    /// helix slope tolerance, which ISO 6336-1 7.4.4.2 explicitly permits.</summary>
    public double MeshMisalignmentFma { get; set; }
    /// <summary>Helix modification per ISO 6336-1 Table 8.</summary>
    public Iso6336FaceLoadFactor.HelixModification HelixMod { get; set; }
        = Iso6336FaceLoadFactor.HelixModification.None;
    /// <summary>Bypass the Method C calculation and use a directly supplied K_Hbeta.</summary>
    public bool UseDirectFaceLoadFactor { get; set; }
    public double DirectKHbeta { get; set; } = 1.15;

    // === Lubrication and surface finish (ISO 6336-2 Clause 12, ISO 6336-3 Clause 7) ===
    public double LubricantViscosity40 { get; set; } = 220;  // ν40 (mm²/s), ISO VG 220
    public double FlankRoughnessRz1 { get; set; } = 3.0;     // flank Rz after running-in (µm)
    public double FlankRoughnessRz2 { get; set; } = 3.0;
    public double RootRoughnessRz1 { get; set; } = 10.0;     // root fillet Rz (µm), Y_RrelT
    public double RootRoughnessRz2 { get; set; } = 10.0;

    // === Scuffing, ISO/TR 13989-1 (flash temperature method) ===

    /// <summary>Base oil type. Sets X_L, which reaches both the friction and the scuffing limit.</summary>
    public LubricantType OilType { get; set; } = LubricantType.Mineral;

    /// <summary>
    /// How the oil reaches the mesh. This is the term the pitting calculation has no place
    /// for: ISO 6336-2's film factors see only viscosity, while ISO/TR 13989-1 Eq. (22) uses
    /// the method through X_S to set how hot the teeth run.
    /// </summary>
    public LubricationMethod LubricationMethod { get; set; } = LubricationMethod.Dip;

    /// <summary>
    /// True to derive the oil temperature as ambient + rise instead of entering it. The rise
    /// is the user's own figure - this module does not model a thermal network, and inventing
    /// one would be worse than asking.
    /// </summary>
    public bool OilTemperatureFromAmbient { get; set; } = true;

    /// <summary>Ambient temperature (°C).</summary>
    public double AmbientTemperature { get; set; } = 20;

    /// <summary>Temperature rise of the oil above ambient (K).</summary>
    public double OilTemperatureRise { get; set; } = 50;

    /// <summary>Oil temperature (°C), used directly when <see cref="OilTemperatureFromAmbient"/> is false.</summary>
    public double OilTemperatureDirect { get; set; } = 70;

    /// <summary>Kinematic viscosity at 100 °C (mm²/s). 0 = estimate from ν40 for a mineral oil.</summary>
    public double LubricantViscosity100 { get; set; }

    /// <summary>Oil density at 15 °C (kg/dm³), to turn kinematic viscosity into dynamic.</summary>
    public double OilDensity { get; set; } = 0.89;

    /// <summary>FZG A/8,3/90 load stage at which the oil scuffs.</summary>
    public double FzgLoadStage { get; set; } = 12;

    /// <summary>Oils with anti-scuff additives gain from a short contact exposure, Clause 10.3.</summary>
    public bool AntiScuffAdditives { get; set; }

    /// <summary>Structural factor X_W, Table 2. 0 = derive from the pinion's material group.</summary>
    public double StructuralFactorOverride { get; set; }

    /// <summary>True when the pinion drives; decides which end of the path the approach factor hits.</summary>
    public bool PinionDrives { get; set; } = true;

    /// <summary>
    /// When true, Y_NT and Z_NT are held at 1.0 in the long-life range instead of following
    /// the descending branch. ISO 6336 allows this only with optimum material, manufacturing
    /// and lubrication backed by experience, so it defaults to false.
    /// </summary>
    public bool OptimumLifeConditions { get; set; }

    // === Tooth thickness allowances and backlash ===
    public ToothThicknessAllowanceMode AllowanceMode { get; set; } = ToothThicknessAllowanceMode.Automatic;
    public double Asne1 { get; set; }                    // upper allowance, gear 1 (mm, negative)
    public double Asni1 { get; set; }                    // lower allowance, gear 1 (mm, negative)
    public double Asne2 { get; set; }
    public double Asni2 { get; set; }
    /// <summary>
    /// Upper centre distance deviation A_a (mm). Leaving both deviations at 0 means
    /// "derive them from <see cref="CentreDistanceToleranceField"/>".
    /// </summary>
    public double CentreDistanceUpperDev { get; set; }

    /// <inheritdoc cref="CentreDistanceUpperDev"/>
    public double CentreDistanceLowerDev { get; set; }

    /// <summary>
    /// ISO 286 field applied to the centre distance when no deviations are entered.
    /// js7 is the usual machined housing bore centre; coarser housings run to js8/js9.
    /// </summary>
    public string CentreDistanceToleranceField { get; set; } = "js7";

    /// <summary>Where the centre distance deviations came from, for the results card.</summary>
    public string? CentreDistanceNote { get; set; }
    /// <summary>Ball / pin diameter for the over-pins measurement (mm). 0 = best size.</summary>
    public double BallDiameter1 { get; set; }
    public double BallDiameter2 { get; set; }

    /// <summary>
    /// Number of teeth to span for W_k. 0 lets the calculator choose. A span dimension only
    /// means anything against the k it was measured over, so a drawing that states k must be
    /// able to say so — especially when W_k limits are the tolerance input.
    /// </summary>
    public int SpanTeeth1 { get; set; }
    public int SpanTeeth2 { get; set; }

    /// <summary>
    /// Tip and root diameter deviations (mm). Nominal is 0/0 — these are blank-turning
    /// tolerances, not tooth thickness ones, and they do not enter the load capacity. They are
    /// carried so the drawing dimensions the results print are complete.
    /// </summary>
    public double TipDiameterUpperDev { get; set; }
    public double TipDiameterLowerDev { get; set; }
    public double RootDiameterUpperDev { get; set; }
    public double RootDiameterLowerDev { get; set; }

    /// <summary>How a pair-level backlash target is shared between the two gears.</summary>
    public BacklashSplit SplitRule { get; set; } = BacklashSplit.Even;

    /// <summary>
    /// DIN 3967 allowance series letter, shared by both gears. Clause 3.1 says a single
    /// series for pinion and wheel is the rule, though different ones are permitted; the
    /// standard's own example uses one letter with a different tolerance series per gear,
    /// which is what these three fields express.
    /// </summary>
    public string Din3967AllowanceSeries { get; set; } = "cd";

    /// <summary>DIN 3967 tolerance series (21-30) of the pinion. 24-27 are preferred.</summary>
    public int Din3967ToleranceSeries1 { get; set; } = 26;

    /// <inheritdoc cref="Din3967ToleranceSeries1"/>
    public int Din3967ToleranceSeries2 { get; set; } = 26;

    /// <summary>
    /// Target backlash, minimum and maximum (mm). Read in whichever quantity
    /// <see cref="AllowanceMode"/> selects — j_bn, j_wt or j_r.
    /// </summary>
    public double TargetBacklashMin { get; set; }

    /// <inheritdoc cref="TargetBacklashMin"/>
    public double TargetBacklashMax { get; set; }

    /// <summary>
    /// Base tangent length limits (mm), largest and smallest permitted, per gear.
    /// Both sit BELOW the nominal W_k, because the allowances are negative.
    /// </summary>
    public double SpanLimitUpper1 { get; set; }
    public double SpanLimitLower1 { get; set; }
    public double SpanLimitUpper2 { get; set; }
    public double SpanLimitLower2 { get; set; }

    /// <summary>Dimension over balls limits (mm), largest and smallest permitted, per gear.</summary>
    public double BallLimitUpper1 { get; set; }
    public double BallLimitLower1 { get; set; }
    public double BallLimitUpper2 { get; set; }
    public double BallLimitLower2 { get; set; }

    // Materials
    public GearMaterial Material1 { get; set; } = new();
    public GearMaterial Material2 { get; set; } = new();

    // ============ CALCULATED VALUES - GEOMETRY ============

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

    // Undercut limits (ISO 21771)
    public double MinProfileShift1 { get; set; }         // x_min,1
    public double MinProfileShift2 { get; set; }         // x_min,2

    // Contact Ratios and path of contact
    public double TransverseContactRatio { get; set; }   // εα
    public double OverlapRatio { get; set; }             // εβ
    public double TotalContactRatio { get; set; }        // εγ
    public double PathOfContact { get; set; }            // gα (mm)
    public double ApproachPath { get; set; }             // gf (mm)
    public double RecessPath { get; set; }               // ga (mm)
    public double ApproachContactRatio { get; set; }     // ε1
    public double RecessContactRatio { get; set; }       // ε2

    // Specific sliding at the ends of the path of contact
    public double SpecificSliding1 { get; set; }         // ζ1 at the pinion root (start of contact)
    public double SpecificSliding2 { get; set; }         // ζ2 at the wheel root (end of contact)
    public double ToothLossFactor { get; set; }          // HV (ISO/TR 14179-2 gear loss factor)

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
    /// <summary>Full ISO 6336-1 Method B dynamic factor breakdown.</summary>
    public Iso6336DynamicFactor.Result? DynamicResult { get; set; }
    public double FaceLoadFactorFlank { get; set; }      // KHβ
    public double FaceLoadFactorRoot { get; set; }       // KFβ
    /// <summary>ISO 6336-1 Clause 7.5 face load factor breakdown.</summary>
    public Iso6336FaceLoadFactor.Result? FaceLoadResult { get; set; }
    public double TransverseLoadFactorFlank { get; set; }// KHα
    public double TransverseLoadFactorRoot { get; set; } // KFα
    /// <summary>ISO 6336-1 Clause 8 transverse load factor breakdown.</summary>
    public Iso6336TransverseFactor.Result? TransverseResult { get; set; }

    /// <summary>Where the deviations feeding K_V and K_Hbeta came from.</summary>
    public string? DeviationNote { get; set; }

    // ============ CALCULATED VALUES - TOOTH ROOT (BENDING) ============

    public double ToothFormFactor1 { get; set; }         // YF1
    public double ToothFormFactor2 { get; set; }         // YF2
    public double StressCorrectionFactor1 { get; set; }  // YS1
    public double StressCorrectionFactor2 { get; set; }  // YS2

    // ISO 6336-3 Method B intermediate tooth form results (for reporting)
    public double RootChord1 { get; set; }               // sFn1 (mm)
    public double RootChord2 { get; set; }               // sFn2 (mm)
    public double MomentArm1 { get; set; }               // hFe1 (mm)
    public double MomentArm2 { get; set; }               // hFe2 (mm)
    public double RootFilletRadius1 { get; set; }        // ρF1 (mm)
    public double RootFilletRadius2 { get; set; }        // ρF2 (mm)
    public double NotchParameter1 { get; set; }          // qs1
    public double NotchParameter2 { get; set; }          // qs2
    public string? ToothFormWarning1 { get; set; }
    public string? ToothFormWarning2 { get; set; }

    /// <summary>False when the ISO 6336-3 tooth form could not be evaluated (geometry invalid).
    /// Root strength results are then meaningless and must not be reported as safe.</summary>
    public bool ToothFormValid1 { get; set; }
    public bool ToothFormValid2 { get; set; }

    public double HelixAngleFactorRoot { get; set; }     // Yβ
    public double RimThicknessFactor1 { get; set; }      // YB1
    public double RimThicknessFactor2 { get; set; }      // YB2
    public double DeepToothFactor { get; set; }          // YDT
    public double VirtualContactRatio { get; set; }      // εαn, drives YDT

    public double NominalRootStress1 { get; set; }       // σF0,1 (MPa)
    public double NominalRootStress2 { get; set; }       // σF0,2 (MPa)
    public double RootStress1 { get; set; }              // σF1 (MPa)
    public double RootStress2 { get; set; }              // σF2 (MPa)

    // Permissible-stress factors, tooth root
    public double NotchSensitivity1 { get; set; }        // YδrelT1
    public double NotchSensitivity2 { get; set; }        // YδrelT2
    public double SurfaceFactorRoot1 { get; set; }       // YRrelT1
    public double SurfaceFactorRoot2 { get; set; }       // YRrelT2
    public double SizeFactorRoot1 { get; set; }          // YX1
    public double SizeFactorRoot2 { get; set; }          // YX2

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
    public string? ContactFactorWarning { get; set; }

    // Lubricant film factors (ISO 6336-2 Clause 12)
    public double LubricationFactor { get; set; }        // ZL
    public double VelocityFactor { get; set; }           // Zv
    public double RoughnessFactor { get; set; }          // ZR
    public Iso6336SurfaceFactors.LubricantFilmFactors? LubricantFilmResult { get; set; }

    // Work hardening and size factors, flank
    public double WorkHardeningFactor1 { get; set; }     // ZW1
    public double WorkHardeningFactor2 { get; set; }     // ZW2
    public double SizeFactorFlank1 { get; set; }         // ZX1
    public double SizeFactorFlank2 { get; set; }         // ZX2

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

    // ============ CALCULATED VALUES - TOLERANCES & INSPECTION ============

    /// <summary>ISO 1328-1 flank tolerances of each gear.</summary>
    public Iso1328Tolerance.Tolerances? Tolerance1 { get; set; }
    public Iso1328Tolerance.Tolerances? Tolerance2 { get; set; }

    /// <summary>Tooth thickness, span measurement, over-balls and chordal dimensions.</summary>
    public GearToothMeasurement.Result? Measurement1 { get; set; }
    public GearToothMeasurement.Result? Measurement2 { get; set; }

    /// <summary>Backlash of the pair from the allowances and centre distance deviation.</summary>
    public GearToothMeasurement.BacklashResult? Backlash { get; set; }

    /// <summary>Where the tooth thickness allowances came from.</summary>
    public string? AllowanceNote { get; set; }

    /// <summary>Centre distance deviations actually used (mm).</summary>
    public double UsedCentreDistanceUpperDev { get; set; }
    public double UsedCentreDistanceLowerDev { get; set; }

    // ============ MINIMUM SAFETY FACTORS ============

    public double MinRootSafety { get; set; }
    public double MinFlankSafety { get; set; }

    /// <summary>Scuffing result, ISO/TR 13989-1 flash temperature method.</summary>
    public Iso13989FlashTemperature.Result? Scuffing { get; set; }

    /// <summary>Scuffing result, ISO/TR 13989-2 integral temperature method.</summary>
    public Iso13989IntegralTemperature.Result? ScuffingIntegral { get; set; }

    /// <summary>Oil temperature actually used (°C), whether entered or derived from ambient.</summary>
    public double OilTemperatureUsed { get; set; }

    /// <summary>Kinematic viscosity at the oil temperature (mm²/s).</summary>
    public double ScuffingViscosityAtOil { get; set; }

    /// <summary>Scuffing safety S_B. A temperature ratio, not a stress one — see the results note.</summary>
    public double MinScuffingSafety { get; set; }

    /// <summary>
    /// Recommended minimum tooth root safety factor SFmin (ISO 6336).
    /// The reported RootSafetyFactor must be compared against this, not against 1.0.
    /// </summary>
    public const double MinSafetyFactorRoot = 1.4;

    /// <summary>Recommended minimum tooth flank safety factor SHmin (ISO 6336).</summary>
    public const double MinSafetyFactorFlank = 1.0;

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
        CalculateTolerances();          // needs geometry; feeds the deviations into K_V
        CalculateLoadFactors();
        CalculateToothRootStrength();
        CalculateToothFlankStrength();
        CalculateSafetyFactors();
        CalculateMeasurements();        // needs α_wt and the allowances
        CalculateScuffing();            // needs the load factors and the geometry
    }

    // ============ CALCULATION STEPS ============

    private void CalculateBasicGeometry()
    {
        double betaRad = HelixAngle * Math.PI / 180.0;
        double alphanRad = PressureAngle * Math.PI / 180.0;

        // Transverse module: mt = mn / cos(β)
        TransverseModule = NormalModule / Math.Cos(betaRad);

        // Transverse pressure angle: tan(αt) = tan(αn) / cos(β)
        double tanAlphaT = Math.Tan(alphanRad) / Math.Cos(betaRad);
        TransversePressureAngle = Math.Atan(tanAlphaT) * 180.0 / Math.PI;

        // Base helix angle: sin(βb) = sin(β) × cos(αn)
        double betaB = Math.Asin(Math.Sin(betaRad) * Math.Cos(alphanRad));
        BaseHelixAngle = betaB * 180.0 / Math.PI;

        GearRatio = (double)NumberOfTeeth2 / NumberOfTeeth1;

        double alphatRad = TransversePressureAngle * Math.PI / 180.0;
        double referenceCenter = TransverseModule * (NumberOfTeeth1 + NumberOfTeeth2) / 2.0;
        double invAlphaT = Math.Tan(alphatRad) - alphatRad;

        // Working pressure angle from the actual centre distance
        double cosAlphaWT = referenceCenter * Math.Cos(alphatRad) / CenterDistance;
        if (cosAlphaWT > 1.0) cosAlphaWT = 1.0;
        if (cosAlphaWT < -1.0) cosAlphaWT = -1.0;
        double alphaWTRad = Math.Acos(cosAlphaWT);
        WorkingPressureAngle = alphaWTRad * 180.0 / Math.PI;

        double invAlphaWT = Math.Tan(alphaWTRad) - alphaWTRad;
        double invDiff = invAlphaWT - invAlphaT;

        // Sum of profile shifts: Σx = (z1 + z2) × inv_diff / (2 × tan(αn))
        SumProfileShift = (NumberOfTeeth1 + NumberOfTeeth2) * invDiff / (2.0 * Math.Tan(alphanRad));
        ProfileShiftCoeff2 = SumProfileShift - ProfileShiftCoeff1;
    }

    private void CalculateDiameters()
    {
        double betaRad = HelixAngle * Math.PI / 180.0;
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;

        ReferenceDiameter1 = TransverseModule * NumberOfTeeth1;
        ReferenceDiameter2 = TransverseModule * NumberOfTeeth2;

        BaseDiameter1 = ReferenceDiameter1 * Math.Cos(alphatRad);
        BaseDiameter2 = ReferenceDiameter2 * Math.Cos(alphatRad);

        WorkingPitchDiameter1 = 2.0 * CenterDistance / (1.0 + GearRatio);
        WorkingPitchDiameter2 = 2.0 * CenterDistance * GearRatio / (1.0 + GearRatio);

        CalculateTipAlteration();

        Addendum1 = NormalModule * (AddendumCoeff + ProfileShiftCoeff1 + TipAlterationCoeff);
        Addendum2 = NormalModule * (AddendumCoeff + ProfileShiftCoeff2 + TipAlterationCoeff);

        Dedendum1 = NormalModule * (DedendumCoeff - ProfileShiftCoeff1);
        Dedendum2 = NormalModule * (DedendumCoeff - ProfileShiftCoeff2);

        TipDiameter1 = ReferenceDiameter1 + 2.0 * Addendum1;
        TipDiameter2 = ReferenceDiameter2 + 2.0 * Addendum2;

        RootDiameter1 = ReferenceDiameter1 - 2.0 * Dedendum1;
        RootDiameter2 = ReferenceDiameter2 - 2.0 * Dedendum2;

        ToothHeight = Addendum1 + Dedendum1;

        // Working tip clearance: what is actually left between one tip and the mating
        // root at the operating centre distance. The nominal (mn(h*fP - h*aP)) ignores
        // the centre distance, and so hides the clearance loss that profile shift causes.
        TipClearance = CenterDistance - TipDiameter1 / 2.0 - RootDiameter2 / 2.0;

        // Undercut limit (ISO 21771): x_min = h*aP - z sin²(αt) / (2 cos β)
        MinProfileShift1 = AddendumCoeff
            - NumberOfTeeth1 * Math.Pow(Math.Sin(alphatRad), 2) / (2.0 * Math.Cos(betaRad));
        MinProfileShift2 = AddendumCoeff
            - NumberOfTeeth2 * Math.Pow(Math.Sin(alphatRad), 2) / (2.0 * Math.Cos(betaRad));
    }

    /// <summary>
    /// Tip alteration (tip shortening) coefficient k, applied equally to both gears.
    ///
    /// Profile shift moves the teeth outwards but the centre distance does not follow by the
    /// same amount, so the working tip clearance shrinks. k is the classical remedy: the tips
    /// of both gears are turned down by k·m_n.
    ///
    ///     y = (a - a_d) / m_n        centre distance modification coefficient
    ///     k = y - Σx                 ISO 21771 / DIN 3960
    ///
    /// Substituting that k into c = a - d_a1/2 - d_f2/2 collapses the whole expression to
    /// c = m_n(h*_fP - h*_aP) — the reference profile's own clearance, 0.25·m_n for ISO 53
    /// profile A. That identity is what <see cref="TipAlterationSource.Calculated"/> targets,
    /// and it is the anchor to re-check if this method is ever touched.
    ///
    /// k is normally negative (Σx &gt; y). A positive k lengthens the teeth instead, which is
    /// legitimate but pushes towards a pointed tip, so it is reported rather than applied
    /// silently.
    /// </summary>
    private void CalculateTipAlteration()
    {
        TipAlterationNote = null;

        switch (TipAlterationMode)
        {
            case TipAlterationSource.None:
                TipAlterationCoeff = 0.0;
                break;

            case TipAlterationSource.Manual:
                // Held as entered; nothing to derive.
                break;

            case TipAlterationSource.Calculated:
                double ad = ReferenceCentreDistance();
                if (ad <= 0 || NormalModule <= 0)
                {
                    TipAlterationCoeff = 0.0;
                    TipAlterationNote = "k could not be derived (module or tooth counts missing); 0 was used.";
                    break;
                }

                double y = (CenterDistance - ad) / NormalModule;
                TipAlterationCoeff = y - SumProfileShift;

                TipAlterationNote =
                    $"k = y - Σx = {y:F4} - {SumProfileShift:F4} = {TipAlterationCoeff:F4}, which restores the "
                  + $"reference profile's tip clearance of {(DedendumCoeff - AddendumCoeff):F2}·mn "
                  + $"= {NormalModule * (DedendumCoeff - AddendumCoeff):F3} mm.";

                if (TipAlterationCoeff > 0)
                {
                    TipAlterationNote +=
                        " k is positive here, so the teeth are lengthened rather than shortened — check the "
                      + "tip tooth thickness s_an before adopting it.";
                }
                break;
        }
    }

    private void CalculateContactRatios()
    {
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;
        double alphaWTRad = WorkingPressureAngle * Math.PI / 180.0;
        double betaRad = HelixAngle * Math.PI / 180.0;

        // Transverse base pitch: pbt = π × mt × cos(αt)
        double basePitch = Math.PI * TransverseModule * Math.Cos(alphatRad);

        // Distances from each base tangent point to the tip circles
        double tip1 = Math.Sqrt(Math.Max(0, Math.Pow(TipDiameter1 / 2.0, 2) - Math.Pow(BaseDiameter1 / 2.0, 2)));
        double tip2 = Math.Sqrt(Math.Max(0, Math.Pow(TipDiameter2 / 2.0, 2) - Math.Pow(BaseDiameter2 / 2.0, 2)));
        double lineOfAction = CenterDistance * Math.Sin(alphaWTRad);

        PathOfContact = tip1 + tip2 - lineOfAction;
        if (PathOfContact < 0) PathOfContact = 0;

        // Split at the pitch point: the recess path belongs to the pinion tip, the
        // approach path to the wheel tip.
        RecessPath = Math.Max(0, tip1 - BaseDiameter1 / 2.0 * Math.Tan(alphaWTRad));
        ApproachPath = Math.Max(0, tip2 - BaseDiameter2 / 2.0 * Math.Tan(alphaWTRad));

        TransverseContactRatio = basePitch > 0 ? PathOfContact / basePitch : 0;
        ApproachContactRatio = basePitch > 0 ? ApproachPath / basePitch : 0;
        RecessContactRatio = basePitch > 0 ? RecessPath / basePitch : 0;

        // Overlap ratio: εβ = b × sin(β) / (π × mn)
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);
        OverlapRatio = Math.Abs(HelixAngle) > 0.01
            ? effectiveFaceWidth * Math.Abs(Math.Sin(betaRad)) / (Math.PI * NormalModule)
            : 0.0;

        TotalContactRatio = TransverseContactRatio + OverlapRatio;

        CalculateSlidingAndLossFactor(alphaWTRad, basePitch);
    }

    /// <summary>
    /// Specific sliding at the two ends of the path of contact, and the ISO/TR 14179-2
    /// gear loss factor H_V.
    ///
    /// With ρ1, ρ2 the distances of a contact point from the two base tangent points,
    /// the specific sliding of gear 1 is ζ1 = 1 - ρ2/(u ρ1), and of gear 2
    /// ζ2 = 1 - u ρ1/ρ2. The extremes occur at the ends of the path: the pinion root
    /// (start of contact) and the wheel root (end of contact) - and these are the values
    /// that govern scuffing and wear risk.
    /// </summary>
    private void CalculateSlidingAndLossFactor(double alphaWTRad, double basePitch)
    {
        double rb1TanW = BaseDiameter1 / 2.0 * Math.Tan(alphaWTRad);
        double rb2TanW = BaseDiameter2 / 2.0 * Math.Tan(alphaWTRad);
        double u = GearRatio;

        // Start of contact A: the wheel tip drives, the pinion contacts near its root.
        double rho1A = rb1TanW - ApproachPath;
        double rho2A = rb2TanW + ApproachPath;
        SpecificSliding1 = rho1A > 1e-9 ? 1.0 - rho2A / (u * rho1A) : 0;

        // End of contact E: the pinion tip drives, the wheel contacts near its root.
        double rho1E = rb1TanW + RecessPath;
        double rho2E = rb2TanW - RecessPath;
        SpecificSliding2 = rho2E > 1e-9 ? 1.0 - u * rho1E / rho2E : 0;

        // Gear loss factor H_V (Niemann; ISO/TR 14179-2). Geometry only - multiplying it
        // by a mean coefficient of friction would give the mesh power loss, which needs
        // oil temperature data this module does not ask for.
        double cosBetaB = Math.Cos(BaseHelixAngle * Math.PI / 180.0);
        if (NumberOfTeeth1 > 0 && u > 0 && cosBetaB > 0)
        {
            double e1 = ApproachContactRatio;
            double e2 = RecessContactRatio;
            ToothLossFactor = (Math.PI * (u + 1.0)) / (NumberOfTeeth1 * u * cosBetaB)
                            * (1.0 - TransverseContactRatio + e1 * e1 + e2 * e2);
        }
    }

    private void CalculateKinematics()
    {
        Speed2 = Speed1 / GearRatio;
        Torque1 = Power * 9550.0 / Speed1;
        Torque2 = Power * 9550.0 / Speed2;
        PitchLineVelocity = Math.PI * ReferenceDiameter1 * Speed1 / 60000.0;
    }

    private void CalculateForces()
    {
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;
        double betaRad = HelixAngle * Math.PI / 180.0;
        double alphanRad = PressureAngle * Math.PI / 180.0;

        TangentialForce = 2000.0 * Torque1 / ReferenceDiameter1;
        RadialForce = TangentialForce * Math.Tan(alphatRad);
        AxialForce = TangentialForce * Math.Tan(betaRad);
        NormalForce = TangentialForce / (Math.Cos(alphanRad) * Math.Cos(betaRad));

        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);
        SpecificLoad = TangentialForce / effectiveFaceWidth;
    }

    // ============ TOLERANCES ============

    /// <summary>
    /// ISO 1328-1 flank tolerances for both gears, and the deviations they imply for the
    /// ISO 6336-1 dynamic and face load factors.
    ///
    /// Two caveats, surfaced in <see cref="DeviationNote"/>:
    ///  - ISO 6336-1:2006 references ISO 1328-1:1995 (accuracy grades 0-12) whereas this
    ///    implementation uses the 2013 edition (flank tolerance classes 1-11); the
    ///    numbering and the formulae differ between editions.
    ///  - ISO 1328-1:2013 tabulates the transverse single pitch tolerance f_pT, while
    ///    ISO 6336-1 needs the BASE pitch deviation f_pb. They are related by cos(αt),
    ///    since p_b = p_t cos(αt).
    /// </summary>
    private void CalculateTolerances()
    {
        Tolerance1 = Iso1328Tolerance.Calculate(QualityGrade1, ReferenceDiameter1, NormalModule,
                                                FaceWidth1, NumberOfTeeth1, HelixAngle);
        Tolerance2 = Iso1328Tolerance.Calculate(QualityGrade2, ReferenceDiameter2, NormalModule,
                                                FaceWidth2, NumberOfTeeth2, HelixAngle);

        double alphaTRad = TransversePressureAngle * Math.PI / 180.0;

        // ISO 6336-1 6.4.3 takes the deviations of the WHEEL.
        if (!UseMeasuredDeviations)
        {
            BasePitchDeviation = Tolerance2.SinglePitch * Math.Cos(alphaTRad);
            ProfileFormDeviation = Tolerance2.ProfileForm;
            RunningInAllowanceP = 0;   // conservative: no running-in benefit
            RunningInAllowanceF = 0;

            DeviationNote =
                $"Deviations for K_V taken from ISO 1328-1:2013 class {QualityGrade2} (wheel): " +
                $"f_pT = {Tolerance2.SinglePitch:F1} µm → f_pb = {BasePitchDeviation:F1} µm via cos(αt); " +
                $"f_fα = {ProfileFormDeviation:F1} µm. Running-in allowances taken as 0 (conservative). " +
                Iso1328Tolerance.EditionMismatchNote;
            if (Tolerance2.Warning != null) DeviationNote += " " + Tolerance2.Warning;
        }
        else
        {
            DeviationNote = "Deviations for K_V were entered as measured values.";
        }

        // f_ma always comes from the helix slope tolerance, which ISO 6336-1 7.4.4.2
        // explicitly permits as the estimate of total manufacturing misalignment. It is
        // deliberately NOT tied to the measured-deviation switch above: leaving it at 0
        // would silently make K_Hbeta = 1.
        MeshMisalignmentFma = Math.Max(Tolerance1.HelixSlope, Tolerance2.HelixSlope);
    }

    private void CalculateLoadFactors()
    {
        int avgQuality = (QualityGrade1 + QualityGrade2) / 2;
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);

        // === Dynamic factor KV - ISO 6336-1:2006 Clause 6, Method B ===
        var kvInput = new Iso6336DynamicFactor.Input
        {
            z1 = NumberOfTeeth1,
            z2 = NumberOfTeeth2,
            mn = NormalModule,
            b = effectiveFaceWidth,
            beta = HelixAngle,
            alphaN = PressureAngle,
            x1 = ProfileShiftCoeff1,
            x2 = ProfileShiftCoeff2,
            d1 = ReferenceDiameter1,
            d2 = ReferenceDiameter2,
            da1 = TipDiameter1,
            da2 = TipDiameter2,
            df1 = RootDiameter1,
            df2 = RootDiameter2,
            di1 = BoreDiameter1,
            di2 = BoreDiameter2,
            db1 = BaseDiameter1,
            epsilonAlpha = TransverseContactRatio,
            epsilonGamma = TotalContactRatio,
            hfP = DedendumCoeff * NormalModule,
            alphaPn = PressureAngle,
            Ft = TangentialForce,
            KA = ApplicationFactor,
            n1 = Speed1,
            rho1 = 7.85e-6,
            rho2 = 7.85e-6,
            E1 = Material1.ElasticModulus * 1000.0,
            E2 = Material2.ElasticModulus * 1000.0,
            SolidDiscGears = SolidDiscGears,
            sigmaHlim1 = Material1.ContactFatigueLimit,
            sigmaHlim2 = Material2.ContactFatigueLimit,
            fpb = BasePitchDeviation,
            ffalpha = ProfileFormDeviation,
            yp = RunningInAllowanceP,
            yf = RunningInAllowanceF,
            UseCayForCa = true,
            // Footnote 4 of 6.4.3: for accuracy grades 6 to 12, Bk = 1,0
            ForceBkUnity = avgQuality >= 6
        };

        DynamicResult = Iso6336DynamicFactor.Calculate(kvInput);
        DynamicFactor = DynamicResult.Valid ? DynamicResult.KV : 1.0;

        // === Face load factors KHβ / KFβ - ISO 6336-1 Clause 7.5 (Method C) and 7.6 ===
        double h1 = Addendum1 + Dedendum1;
        double h2 = Addendum2 + Dedendum2;
        double bOverH1 = h1 > 0 ? effectiveFaceWidth / h1 : 3.0;
        double bOverH2 = h2 > 0 ? effectiveFaceWidth / h2 : 3.0;

        // === Shaft deflection component f_sh (ISO 6336-1 7.5.2.4.1) ===
        // Needs F_m/b, so it runs after K_V and before the face load factor.
        double fmOverB = TangentialForce * ApplicationFactor * DynamicFactor / effectiveFaceWidth;

        switch (FshSource)
        {
            case ShaftDeflectionSource.Calculated:
            {
                // The shaft dimensions are the user's to supply, but leaving them blank
                // must not silently fall back to f_sh = 0 - that is the non-conservative
                // hole this whole path exists to close. Stand in a representative shaft
                // instead and say so, so the number is never mistaken for the real one.
                double span = ShaftBearingSpan;
                double dsh = ShaftDiameter;
                bool assumed = false;

                if (span <= 0) { span = 3.0 * effectiveFaceWidth; assumed = true; }
                if (dsh <= 0) { dsh = ReferenceDiameter1 / 1.15; assumed = true; }

                ShaftDeflectionResult = Iso6336ShaftDeflection.Calculate(
                    fmOverB, effectiveFaceWidth, 0, ReferenceDiameter1,
                    span, PinionOffset, dsh, ShaftBoreDiameter,
                    ShaftArrangement, PinionCanStiffenShaft);

                if (assumed)
                {
                    ShaftDeflectionResult.Notes.Insert(0,
                        $"Shaft dimensions were not given, so a representative shaft was assumed: " +
                        $"bearing span l = {span:F1} mm (3×b) and shaft diameter d_sh = {dsh:F1} mm " +
                        $"(d1/d_sh = 1.15, the stiffening boundary). Enter the real dimensions for a " +
                        $"result that describes your gearbox.");
                }

                ShaftDeflectionFsh = ShaftDeflectionResult.fsh;
                break;
            }

            case ShaftDeflectionSource.Neglected:
                ShaftDeflectionResult = null;
                ShaftDeflectionFsh = 0;
                break;

            default:   // Manual - ShaftDeflectionFsh is the user's input, left alone
                ShaftDeflectionResult = null;
                break;
        }

        // The determinant helix slope deviation, for the Eq. (56) floor on F_bx.
        double fHbetaDeterminant = Math.Max(Tolerance1?.HelixSlope ?? 0, Tolerance2?.HelixSlope ?? 0);

        if (UseDirectFaceLoadFactor)
        {
            FaceLoadFactorFlank = Math.Max(1.0, DirectKHbeta);

            // Eq. (69), (70): the same b/h relation the Method C path uses, so a directly
            // entered K_Hbeta produces the same K_Fbeta as a calculated one of equal value.
            double bOverH = Math.Max(3.0, Math.Min(bOverH1, bOverH2));
            double hOverB = 1.0 / bOverH;
            double nf = 1.0 / (1.0 + hOverB + hOverB * hOverB);
            FaceLoadFactorRoot = Math.Pow(FaceLoadFactorFlank, nf);
            FaceLoadResult = null;
        }
        else
        {
            double sigmaHlimSoftBeta = Math.Min(Material1.ContactFatigueLimit, Material2.ContactFatigueLimit);
            var betaGroup = Iso6336FaceLoadFactor.MapMaterialGroup(
                Material1.ContactFatigueLimit <= Material2.ContactFatigueLimit
                    ? Material1.Iso6336Type : Material2.Iso6336Type);

            var faceRes = Iso6336FaceLoadFactor.Calculate(
                TangentialForce, effectiveFaceWidth, ApplicationFactor, DynamicFactor,
                DynamicResult?.cGammaBeta ?? 0,
                ShaftDeflectionFsh, MeshMisalignmentFma,
                HelixMod, betaGroup, sigmaHlimSoftBeta, PitchLineVelocity,
                bOverH1, bOverH2, fHbetaDeterminant);

            FaceLoadResult = faceRes;
            FaceLoadFactorFlank = faceRes.KHbeta;
            FaceLoadFactorRoot = faceRes.KFbeta;
        }

        // === Transverse load factors KHα / KFα - ISO 6336-1 Clause 8, Method B ===
        // Z_eps is needed for the K_Halpha limit (Eq. 73); it is also computed in
        // CalculateToothFlankStrength, but that runs later, so derive it here.
        double zEps = OverlapRatio >= 1.0
            ? Math.Sqrt(1.0 / TransverseContactRatio)
            : Math.Sqrt((4.0 - TransverseContactRatio) / 3.0 * (1.0 - OverlapRatio)
                        + OverlapRatio / TransverseContactRatio);

        double sigmaHlimSoft = Math.Min(Material1.ContactFatigueLimit, Material2.ContactFatigueLimit);
        var runInGroup = Iso6336TransverseFactor.MapMaterialGroup(
            Material1.ContactFatigueLimit <= Material2.ContactFatigueLimit
                ? Material1.Iso6336Type : Material2.Iso6336Type);

        double yAlpha = Iso6336TransverseFactor.RunningInAllowance(
            runInGroup, BasePitchDeviation, sigmaHlimSoft, PitchLineVelocity);

        var transverse = Iso6336TransverseFactor.Calculate(
            TangentialForce, effectiveFaceWidth,
            ApplicationFactor, DynamicFactor, FaceLoadFactorFlank,
            DynamicResult?.cGammaAlpha ?? 0,
            BasePitchDeviation, ProfileFormDeviation, yAlpha,
            TransverseContactRatio, TotalContactRatio, zEps);

        TransverseResult = transverse;
        TransverseLoadFactorFlank = transverse.KHalpha;
        TransverseLoadFactorRoot = transverse.KFalpha;
    }

    private void CalculateToothRootStrength()
    {
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);

        // === Tooth form factor YF and stress correction factor YS ===
        // ISO 6336-3:2006 Method B - real tooth geometry (30° tangent construction),
        // load applied at the outer point of single pair tooth contact.
        double hfP = DedendumCoeff * NormalModule;      // dedendum of the basic rack (mm)
        double rhofP = RootRadiusCoeff * NormalModule;  // root fillet radius of the basic rack (mm)

        var form1 = Iso6336ToothForm.Calculate(
            z: NumberOfTeeth1, mn: NormalModule, alphaN: PressureAngle, beta: HelixAngle,
            x: ProfileShiftCoeff1, da: TipDiameter1, d: ReferenceDiameter1,
            epsilonAlpha: TransverseContactRatio, hfP: hfP, rhofP: rhofP);

        var form2 = Iso6336ToothForm.Calculate(
            z: NumberOfTeeth2, mn: NormalModule, alphaN: PressureAngle, beta: HelixAngle,
            x: ProfileShiftCoeff2, da: TipDiameter2, d: ReferenceDiameter2,
            epsilonAlpha: TransverseContactRatio, hfP: hfP, rhofP: rhofP);

        ToothFormFactor1 = form1.YF;
        ToothFormFactor2 = form2.YF;
        StressCorrectionFactor1 = form1.YS;
        StressCorrectionFactor2 = form2.YS;

        RootChord1 = form1.sFn;              RootChord2 = form2.sFn;
        MomentArm1 = form1.hFe;              MomentArm2 = form2.hFe;
        RootFilletRadius1 = form1.rhoF;      RootFilletRadius2 = form2.rhoF;
        NotchParameter1 = form1.qs;          NotchParameter2 = form2.qs;
        ToothFormWarning1 = form1.Warning;   ToothFormWarning2 = form2.Warning;

        // A failed tooth form evaluation yields YF = 0, which would silently produce zero
        // root stress and an "infinite" safety factor - an invalid design reported as
        // safe. Track validity explicitly instead.
        ToothFormValid1 = form1.YF > 0 && form1.YS > 0;
        ToothFormValid2 = form2.YF > 0 && form2.YS > 0;

        // Helix angle factor Yβ (ISO 6336-3 Clause 6): the overlap ratio is capped at 1
        // and the helix angle at 30° before entering the expression.
        double betaForY = Math.Min(Math.Abs(HelixAngle), 30.0);
        double epsBetaForY = Math.Min(OverlapRatio, 1.0);
        HelixAngleFactorRoot = 1.0 - epsBetaForY * betaForY / 120.0;
        if (HelixAngleFactorRoot < 0.75) HelixAngleFactorRoot = 0.75;
        if (HelixAngleFactorRoot > 1.0) HelixAngleFactorRoot = 1.0;

        // Virtual (normal section) contact ratio - drives the deep tooth factor
        double cosBetaB = Math.Cos(BaseHelixAngle * Math.PI / 180.0);
        VirtualContactRatio = cosBetaB > 0
            ? TransverseContactRatio / (cosBetaB * cosBetaB)
            : TransverseContactRatio;

        // Rim thickness factor YB (ISO 6336-3 Clause 8) and deep tooth factor YDT (Clause 9)
        RimThicknessFactor1 = RimThicknessFactor(RootDiameter1, BoreDiameter1, NormalModule);
        RimThicknessFactor2 = RimThicknessFactor(RootDiameter2, BoreDiameter2, NormalModule);
        DeepToothFactor = DeepToothFactorFor(VirtualContactRatio, Math.Max(QualityGrade1, QualityGrade2));

        // Nominal tooth root stress (ISO 6336-3 Eq. 2):
        //     σF0 = Ft / (b mn) × YF × YS × Yβ × YB × YDT
        //
        // There is deliberately NO Yε here. ISO 6336-3 Method B applies the load at the
        // OUTER POINT OF SINGLE PAIR TOOTH CONTACT, so the benefit of load sharing is
        // already built into YF. Yε belongs to the DIN 3990 Method C scheme, where the
        // load is applied at the tooth TIP (YFa, YSa) and Yε corrects for it afterwards.
        // Mixing the two counts the same effect twice: it used to cut σF0 by ~29 %, and
        // therefore inflated every tooth root safety factor by ~40 %.
        double baseStress = TangentialForce / (effectiveFaceWidth * NormalModule);
        NominalRootStress1 = baseStress * ToothFormFactor1 * StressCorrectionFactor1
                           * HelixAngleFactorRoot * RimThicknessFactor1 * DeepToothFactor;
        NominalRootStress2 = baseStress * ToothFormFactor2 * StressCorrectionFactor2
                           * HelixAngleFactorRoot * RimThicknessFactor2 * DeepToothFactor;

        // Actual tooth root stress: σF = σF0 × KA × KV × KFβ × KFα
        double loadFactorProduct = ApplicationFactor * DynamicFactor * FaceLoadFactorRoot * TransverseLoadFactorRoot;
        RootStress1 = NominalRootStress1 * loadFactorProduct;
        RootStress2 = NominalRootStress2 * loadFactorProduct;

        CalculateLifeFactors();

        // === Permissible tooth root stress (ISO 6336-3 Clause 5) ===
        //   σFP = σFlim × YST × YNT × YδrelT × YRrelT × YX
        // No minimum safety factor is divided out here, so the reported
        // SF = σFP/σF is the ACTUAL safety factor; compare it against MinSafetyFactorRoot.
        const double YST = 2.0;   // stress correction factor of the reference test gear

        NotchSensitivity1 = Iso6336LifeFactors.YdeltaRelT(
            NotchParameter1, Material1.Iso6336Type, Material1.YieldStrength);
        NotchSensitivity2 = Iso6336LifeFactors.YdeltaRelT(
            NotchParameter2, Material2.Iso6336Type, Material2.YieldStrength);

        SurfaceFactorRoot1 = Iso6336LifeFactors.YRrelT(
            RootRoughnessRz1, Iso6336LifeFactors.MapSurfaceGroup(Material1.Iso6336Type));
        SurfaceFactorRoot2 = Iso6336LifeFactors.YRrelT(
            RootRoughnessRz2, Iso6336LifeFactors.MapSurfaceGroup(Material2.Iso6336Type));

        SizeFactorRoot1 = Iso6336LifeFactors.YX(
            NormalModule, Iso6336LifeFactors.MapSurfaceGroup(Material1.Iso6336Type));
        SizeFactorRoot2 = Iso6336LifeFactors.YX(
            NormalModule, Iso6336LifeFactors.MapSurfaceGroup(Material2.Iso6336Type));

        PermissibleRootStress1 = Material1.BendingFatigueLimit * YST * LifeFactorRoot1
                               * NotchSensitivity1 * SurfaceFactorRoot1 * SizeFactorRoot1;
        PermissibleRootStress2 = Material2.BendingFatigueLimit * YST * LifeFactorRoot2
                               * NotchSensitivity2 * SurfaceFactorRoot2 * SizeFactorRoot2;
    }

    private void CalculateToothFlankStrength()
    {
        double alphatRad = TransversePressureAngle * Math.PI / 180.0;
        double alphaWTRad = WorkingPressureAngle * Math.PI / 180.0;
        double betaRad = HelixAngle * Math.PI / 180.0;
        double betabRad = BaseHelixAngle * Math.PI / 180.0;
        double effectiveFaceWidth = Math.Min(FaceWidth1, FaceWidth2);

        // Zone factor ZH = sqrt(2 cos(βb) cos(αwt) / (cos²(αt) sin(αwt)))
        double cosAlphaT = Math.Cos(alphatRad);
        double cosAlphaWT = Math.Cos(alphaWTRad);
        double sinAlphaWT = Math.Sin(alphaWTRad);
        double cosBetaB = Math.Cos(betabRad);

        ZoneFactorH = Math.Sqrt(2.0 * cosBetaB * cosAlphaWT / (cosAlphaT * cosAlphaT * sinAlphaWT));

        // Elasticity factor ZE - ISO 6336-2 Eq. (19)
        ElasticityFactor = Iso6336SurfaceFactors.CalculateZE(
            Material1.ElasticModulus * 1000.0, Material1.PoissonRatio,
            Material2.ElasticModulus * 1000.0, Material2.PoissonRatio);

        // Contact ratio factor Zε
        ContactRatioFactorFlank = OverlapRatio >= 1.0
            ? Math.Sqrt(1.0 / TransverseContactRatio)
            : Math.Sqrt((4.0 - TransverseContactRatio) / 3.0 * (1.0 - OverlapRatio)
                        + OverlapRatio / TransverseContactRatio);

        // Helix angle factor Zβ
        HelixAngleFactorFlank = Math.Sqrt(Math.Cos(betaRad));

        // Nominal contact stress: σH0 = ZH ZE Zε Zβ sqrt(Ft/(d1 b) × (u+1)/u)
        double loadTerm = TangentialForce / (ReferenceDiameter1 * effectiveFaceWidth)
                        * (GearRatio + 1.0) / GearRatio;
        NominalContactStress = ZoneFactorH * ElasticityFactor * ContactRatioFactorFlank
                             * HelixAngleFactorFlank * Math.Sqrt(loadTerm);

        // σH = σH0 × sqrt(KA KV KHβ KHα)
        double loadFactorRoot = Math.Sqrt(ApplicationFactor * DynamicFactor
                              * FaceLoadFactorFlank * TransverseLoadFactorFlank);
        ContactStress = NominalContactStress * loadFactorRoot;

        // === Single pair tooth contact factors ZB / ZD (ISO 6336-2 Clause 6) ===
        var zbzd = Iso6336SurfaceFactors.CalculateZBZD(
            NumberOfTeeth1, NumberOfTeeth2,
            TipDiameter1, BaseDiameter1, TipDiameter2, BaseDiameter2,
            WorkingPressureAngle, TransverseContactRatio, OverlapRatio);

        SingleToothContactFactor1 = zbzd.ZB;
        SingleToothContactFactor2 = zbzd.ZD;
        ContactFactorWarning = zbzd.Warning;

        ContactStress1 = ContactStress * SingleToothContactFactor1;
        ContactStress2 = ContactStress * SingleToothContactFactor2;

        // === Lubricant film factors ZL, Zv, ZR (ISO 6336-2 Clause 12, Method B) ===
        // Evaluated with sigma_Hlim of the SOFTER material, as required by 12.3.
        double sigmaHlimSofter = Math.Min(Material1.ContactFatigueLimit, Material2.ContactFatigueLimit);

        var film = Iso6336SurfaceFactors.CalculateLubricantFilm(
            sigmaHlimSofter, LubricantViscosity40, PitchLineVelocity,
            FlankRoughnessRz1, FlankRoughnessRz2,
            BaseDiameter1, BaseDiameter2, WorkingPressureAngle);

        LubricantFilmResult = film;
        LubricationFactor = film.ZL;
        VelocityFactor = film.Zv;
        RoughnessFactor = film.ZR;

        // === Work hardening ZW and size ZX (ISO 6336-2 Clauses 13, 14) ===
        bool hard1 = Iso6336LifeFactors.IsSurfaceHardened(Material1.Iso6336Type);
        bool hard2 = Iso6336LifeFactors.IsSurfaceHardened(Material2.Iso6336Type);

        WorkHardeningFactor1 = Iso6336LifeFactors.ZW(
            Material1.SurfaceHardnessIso, hard2, hard1, FlankRoughnessRz2);
        WorkHardeningFactor2 = Iso6336LifeFactors.ZW(
            Material2.SurfaceHardnessIso, hard1, hard2, FlankRoughnessRz1);

        SizeFactorFlank1 = Iso6336LifeFactors.ZX(NormalModule, Material1.Iso6336Type);
        SizeFactorFlank2 = Iso6336LifeFactors.ZX(NormalModule, Material2.Iso6336Type);

        // σHP = σHlim × ZNT × ZL × Zv × ZR × ZW × ZX  (SHmin not divided out)
        double film3 = film.ZL * film.Zv * film.ZR;
        PermissibleContactStress1 = Material1.ContactFatigueLimit * LifeFactorFlank1
                                  * film3 * WorkHardeningFactor1 * SizeFactorFlank1;
        PermissibleContactStress2 = Material2.ContactFatigueLimit * LifeFactorFlank2
                                  * film3 * WorkHardeningFactor2 * SizeFactorFlank2;
    }

    /// <summary>
    /// Rim thickness factor Y_B - ISO 6336-3:2006 Clause 8.
    ///
    /// A thin rim under the tooth lets the root bend as part of the rim rather than as a
    /// cantilever, raising the root stress. For a solid blank (no bore given) Y_B = 1.
    ///     s_R/m_n >= 1.2 : Y_B = 1
    ///     0.5 &lt; s_R/m_n &lt; 1.2 : Y_B = 1.6 ln(2.242 m_n / s_R)
    /// The two branches meet at s_R/m_n = 1.2, where 1.6 ln(2.242/1.2) = 1.000.
    /// </summary>
    private static double RimThicknessFactor(double rootDiameter, double boreDiameter, double mn)
    {
        if (boreDiameter <= 0 || mn <= 0) return 1.0;      // solid blank

        double sR = (rootDiameter - boreDiameter) / 2.0;
        if (sR <= 0) return 1.0;                            // inconsistent input; do not amplify

        double ratio = sR / mn;
        if (ratio >= 1.2) return 1.0;

        // Below 0.5 the standard requires a dedicated analysis; clamp at the limit rather
        // than extrapolating a formula that is no longer valid.
        if (ratio < 0.5) ratio = 0.5;
        return 1.6 * Math.Log(2.242 / ratio);
    }

    /// <summary>
    /// Deep tooth factor Y_DT - ISO 6336-3:2006 Clause 9.
    ///
    /// Only high precision gears (ISO 1328 class 4 or better) with a virtual contact
    /// ratio above 2.05 benefit; everything else keeps Y_DT = 1.
    ///     2.05 &lt; εαn &lt;= 2.5 : Y_DT = 2.366 - 0.666 εαn   (1.000 at 2.05, 0.701 at 2.5)
    ///     εαn &gt; 2.5         : Y_DT = 0.7
    /// </summary>
    private static double DeepToothFactorFor(double epsilonAlphaN, int worstQualityGrade)
    {
        if (epsilonAlphaN <= 2.05 || worstQualityGrade > 4) return 1.0;
        if (epsilonAlphaN > 2.5) return 0.7;
        return 2.366 - 0.666 * epsilonAlphaN;
    }

    /// <summary>
    /// Life factors Y_NT and Z_NT from the real ISO 6336 curves (see
    /// <see cref="Iso6336LifeFactors"/>), keyed on each gear's own material group.
    /// </summary>
    private void CalculateLifeFactors()
    {
        // Number of load cycles NL = 60 n H, for one mesh contact per revolution.
        double cycles1 = 60.0 * Speed1 * RequiredServiceLife;
        double cycles2 = 60.0 * Speed2 * RequiredServiceLife;

        LoadCycles1 = cycles1 / 1e6;   // reported in millions
        LoadCycles2 = cycles2 / 1e6;

        var group1 = Iso6336LifeFactors.MapLifeGroup(Material1.Iso6336Type);
        var group2 = Iso6336LifeFactors.MapLifeGroup(Material2.Iso6336Type);

        LifeFactorRoot1 = Iso6336LifeFactors.YNT(cycles1, group1, OptimumLifeConditions);
        LifeFactorRoot2 = Iso6336LifeFactors.YNT(cycles2, group2, OptimumLifeConditions);
        LifeFactorFlank1 = Iso6336LifeFactors.ZNT(cycles1, group1, OptimumLifeConditions);
        LifeFactorFlank2 = Iso6336LifeFactors.ZNT(cycles2, group2, OptimumLifeConditions);
    }

    private void CalculateSafetyFactors()
    {
        // If the tooth form could not be evaluated the root stress is not meaningful:
        // report 0 (which reads as "not OK") rather than an infinite safety factor.
        RootSafetyFactor1 = !ToothFormValid1 ? 0
            : RootStress1 > 0 ? PermissibleRootStress1 / RootStress1 : 999;
        RootSafetyFactor2 = !ToothFormValid2 ? 0
            : RootStress2 > 0 ? PermissibleRootStress2 / RootStress2 : 999;

        FlankSafetyFactor1 = ContactStress1 > 0 ? PermissibleContactStress1 / ContactStress1 : 999;
        FlankSafetyFactor2 = ContactStress2 > 0 ? PermissibleContactStress2 / ContactStress2 : 999;

        MinRootSafety = Math.Min(RootSafetyFactor1, RootSafetyFactor2);
        MinFlankSafety = Math.Min(FlankSafetyFactor1, FlankSafetyFactor2);
    }

    // ============ TOOTH THICKNESS, CONTROL DIMENSIONS AND BACKLASH ============

    private void CalculateMeasurements()
    {
        // --- Centre distance deviation ---
        // Anything the caller supplies explicitly wins; otherwise the deviations come from
        // the chosen ISO 286 field on the centre distance. js7 is the usual housing bore
        // tolerance for an industrial gearbox and stays the default.
        if (Math.Abs(CentreDistanceUpperDev) > 0 || Math.Abs(CentreDistanceLowerDev) > 0)
        {
            UsedCentreDistanceUpperDev = CentreDistanceUpperDev;
            UsedCentreDistanceLowerDev = CentreDistanceLowerDev;
            CentreDistanceNote = "Centre distance deviations as entered.";
        }
        else
        {
            var field = Iso286.TryGetDeviations(CentreDistanceToleranceField, CenterDistance);
            if (field is { } dev)
            {
                UsedCentreDistanceUpperDev = dev.upper / 1000.0;   // µm → mm
                UsedCentreDistanceLowerDev = dev.lower / 1000.0;
                CentreDistanceNote =
                    $"Centre distance deviations from ISO 286 {CentreDistanceToleranceField} at "
                  + $"a = {CenterDistance:F3} mm: {dev.upper:+0;-0;0} / {dev.lower:+0;-0;0} µm.";
            }
            else
            {
                UsedCentreDistanceUpperDev = 0.030;
                UsedCentreDistanceLowerDev = -0.030;
                CentreDistanceNote =
                    $"ISO 286 {CentreDistanceToleranceField} is not covered at a = {CenterDistance:F3} mm "
                  + "(the tables run to 500 mm); ±30 µm was assumed. Enter the deviations from the "
                  + "housing drawing instead.";
            }
        }

        // --- Tooth thickness allowances ---
        double asne1 = Asne1, asni1 = Asni1, asne2 = Asne2, asni2 = Asni2;

        switch (AllowanceMode)
        {
            case ToothThicknessAllowanceMode.Automatic:
                ResolveAutomaticAllowances(ref asne1, ref asni1, ref asne2, ref asni2);
                break;

            case ToothThicknessAllowanceMode.Manual:
                AllowanceNote = "Tooth thickness allowances were entered directly.";
                break;

            case ToothThicknessAllowanceMode.NormalBacklash:
            case ToothThicknessAllowanceMode.CircumferentialBacklash:
            case ToothThicknessAllowanceMode.RadialBacklash:
                ResolveAllowancesFromBacklash(ref asne1, ref asni1, ref asne2, ref asni2);
                break;

            case ToothThicknessAllowanceMode.SpanLimits:
            case ToothThicknessAllowanceMode.BallLimits:
                ResolveAllowancesFromMeasurement(ref asne1, ref asni1, ref asne2, ref asni2);
                break;

            case ToothThicknessAllowanceMode.Din3967:
                ResolveAllowancesFromDin3967(ref asne1, ref asni1, ref asne2, ref asni2);
                break;
        }

        // Keep the resolved values on the engine so the UI and share state agree with
        // what was actually used.
        Asne1 = asne1; Asni1 = asni1;
        Asne2 = asne2; Asni2 = asni2;

        Measurement1 = GearToothMeasurement.Calculate(MeasurementInput(1, asne1, asni1));
        Measurement2 = GearToothMeasurement.Calculate(MeasurementInput(2, asne2, asni2));

        Backlash = GearToothMeasurement.CalculateBacklash(
            asne1, asni1, asne2, asni2,
            UsedCentreDistanceUpperDev, UsedCentreDistanceLowerDev,
            PressureAngle, WorkingPressureAngle, BaseHelixAngle,
            CenterDistance, NormalModule);
    }

    /// <summary>
    /// Scuffing by the flash temperature method, ISO/TR 13989-1.
    ///
    /// Runs last because it consumes almost everything else: the geometry, the load factors
    /// K_A K_V K_Hβ K_Hα, and the surface roughness. It is reported separately from the
    /// pitting and bending safeties because it is a different kind of limit - a temperature
    /// one, which a single overload can breach.
    /// </summary>
    private void CalculateScuffing()
    {
        OilTemperatureUsed = OilTemperatureFromAmbient
            ? AmbientTemperature + OilTemperatureRise
            : OilTemperatureDirect;

        // Viscosity at the oil temperature, from the two datasheet points. nu100 is estimated
        // for a mineral oil when it is not given; a synthetic of the same VG grade has a
        // markedly flatter curve, so entering it matters there.
        double nu100 = LubricantViscosity100 > 0
            ? LubricantViscosity100
            : Iso13989FlashTemperature.TypicalNu100(LubricantViscosity40);

        double nuAtOil = Iso13989FlashTemperature.ViscosityAt(
            LubricantViscosity40, nu100, OilTemperatureUsed);

        // eta [mPa.s] = nu [mm2/s] * rho [kg/dm3]
        double etaOil = nuAtOil * OilDensity;

        double xw = StructuralFactorOverride > 0
            ? StructuralFactorOverride
            : Iso13989FlashTemperature.StructuralFactor(Material1.Iso6336Type);

        Scuffing = Iso13989FlashTemperature.Calculate(new Iso13989FlashTemperature.Input
        {
            mn = NormalModule,
            alphaN = PressureAngle,
            beta = HelixAngle,
            alphaT = TransversePressureAngle,
            alphaWt = WorkingPressureAngle,
            betaB = BaseHelixAngle,
            a = CenterDistance,
            u = GearRatio,
            z1 = NumberOfTeeth1,
            z2 = NumberOfTeeth2,
            d1 = ReferenceDiameter1,
            d2 = ReferenceDiameter2,
            da1 = TipDiameter1,
            da2 = TipDiameter2,
            b = Math.Min(FaceWidth1, FaceWidth2),
            epsilonAlpha = TransverseContactRatio,
            epsilonBeta = OverlapRatio,
            epsilonGamma = TotalContactRatio,

            Ft = TangentialForce,
            vt = PitchLineVelocity,
            KA = ApplicationFactor,
            KV = DynamicFactor,
            KHbeta = FaceLoadFactorFlank,
            KHalpha = TransverseLoadFactorFlank,
            Kmp = 1.0,
            PinionDrives = PinionDrives,

            E1 = Material1.ElasticModulus * 1000.0,     // GPa -> N/mm²
            E2 = Material2.ElasticModulus * 1000.0,
            nu1 = Material1.PoissonRatio,
            nu2 = Material2.PoissonRatio,
            XW = xw,

            // ISO/TR 13989-1 works in Ra; the rest of this module works in Rz. The standard's
            // own note in Eq. (28) is that these are the roughnesses of newly manufactured
            // gears, and Rz ~ 6 Ra is the conversion ISO 6336-2 uses.
            Ra1 = FlankRoughnessRz1 / 6.0,
            Ra2 = FlankRoughnessRz2 / 6.0,
            QualityGrade = Math.Max(QualityGrade1, QualityGrade2),

            cGamma = DynamicResult?.cGammaAlpha ?? 20.0,

            OilTemperature = OilTemperatureUsed,
            EtaOil = etaOil,
            Lubricant = OilType,
            Method = LubricationMethod,
            FzgLoadStage = FzgLoadStage,
            AntiScuffAdditives = AntiScuffAdditives
        });

        ScuffingViscosityAtOil = nuAtOil;
        MinScuffingSafety = Scuffing is { Valid: true } s ? s.SafetyFactor : 0;

        // The second route. ISO/TR 13989's introduction says the two methods give about the
        // same assessment of scuffing risk, so running both is a cross-check rather than
        // duplication - and the integral method is the less sensitive of the two where the
        // flash temperature has a local peak.
        ScuffingIntegral = Iso13989IntegralTemperature.Calculate(new Iso13989IntegralTemperature.Input
        {
            alphaN = PressureAngle,
            alphaT = TransversePressureAngle,
            alphaWt = WorkingPressureAngle,
            beta = HelixAngle,
            betaB = BaseHelixAngle,
            a = CenterDistance,
            u = GearRatio,
            z1 = NumberOfTeeth1,
            z2 = NumberOfTeeth2,
            da1 = TipDiameter1,
            da2 = TipDiameter2,
            db1 = BaseDiameter1,
            db2 = BaseDiameter2,
            b = Math.Min(FaceWidth1, FaceWidth2),
            epsilonGamma = TotalContactRatio,

            Ft = TangentialForce,
            v = PitchLineVelocity,
            KA = ApplicationFactor,
            KV = DynamicFactor,
            KBbeta = FaceLoadFactorFlank,
            KBalpha = TransverseLoadFactorFlank,
            PinionDrives = PinionDrives,

            E1 = Material1.ElasticModulus * 1000.0,
            E2 = Material2.ElasticModulus * 1000.0,
            nu1 = Material1.PoissonRatio,
            nu2 = Material2.PoissonRatio,
            XW = StructuralFactorOverride > 0
                ? StructuralFactorOverride
                : Iso13989IntegralTemperature.WeldingFactor(Material1.Iso6336Type),

            // Part 2 asks for the roughness of the flanks AS MANUFACTURED, and carries the
            // run-in state separately in X_E. Part 1 asks for the run-in roughness. The form
            // collects the run-in value, so it is scaled back up by the standard's own
            // Ra_run-in ~ 0,6 Ra_new and the gear is then declared fully run in.
            Ra1 = FlankRoughnessRz1 / 6.0 / 0.6,
            Ra2 = FlankRoughnessRz2 / 6.0 / 0.6,
            PhiE = 1.0,

            cGamma = DynamicResult?.cGammaAlpha ?? 20.0,
            cPrime = DynamicResult?.cPrime ?? 14.0,
            QualityGrade = Math.Max(QualityGrade1, QualityGrade2),

            OilTemperature = OilTemperatureUsed,
            EtaOil = etaOil,
            Nu40 = LubricantViscosity40,
            Lubricant = OilType,
            Method = LubricationMethod,
            FzgLoadStage = FzgLoadStage
        });
    }

    /// <summary>The measurement input for one gear at a given pair of allowances.</summary>
    private GearToothMeasurement.GearInput MeasurementInput(int gear, double asne, double asni)
        => new()
        {
            z = gear == 1 ? NumberOfTeeth1 : NumberOfTeeth2,
            mn = NormalModule,
            alphaN = PressureAngle,
            beta = HelixAngle,
            x = gear == 1 ? ProfileShiftCoeff1 : ProfileShiftCoeff2,
            d = gear == 1 ? ReferenceDiameter1 : ReferenceDiameter2,
            db = gear == 1 ? BaseDiameter1 : BaseDiameter2,
            da = gear == 1 ? TipDiameter1 : TipDiameter2,
            df = gear == 1 ? RootDiameter1 : RootDiameter2,
            b = gear == 1 ? FaceWidth1 : FaceWidth2,
            Asne = asne,
            Asni = asni,
            BallDiameter = gear == 1 ? BallDiameter1 : BallDiameter2,
            SpanTeeth = gear == 1 ? SpanTeeth1 : SpanTeeth2
        };

    /// <summary>
    /// The default: allowances that just reach the ISO/TR 10064-2 recommended minimum backlash.
    /// </summary>
    private void ResolveAutomaticAllowances(ref double asne1, ref double asni1,
                                            ref double asne2, ref double asni2)
    {
        double target = GearToothMeasurement.RecommendedMinimumBacklash(CenterDistance, NormalModule);

        // The tolerance width has to come from somewhere; the cumulative pitch tolerance F_p
        // of each gear is a common practical choice and keeps the allowance tied to the
        // tolerance class the user already selected.
        double tsn1 = (Tolerance1?.CumulativePitch ?? 0) / 1000.0;
        double tsn2 = (Tolerance2?.CumulativePitch ?? 0) / 1000.0;

        var (upper, _) = GearToothMeasurement.AllowancesForTargetBacklash(
            target, 0, UsedCentreDistanceLowerDev,
            PressureAngle, WorkingPressureAngle, BaseHelixAngle);

        asne1 = asne2 = upper;
        asni1 = upper - tsn1;
        asni2 = upper - tsn2;

        AllowanceNote =
            $"Allowances derived from the ISO/TR 10064-2 recommended minimum normal backlash " +
            $"({target * 1000:F0} µm), split evenly between the two gears, with the tolerance width " +
            $"T_sn taken as each gear's ISO 1328-1 cumulative pitch tolerance F_p " +
            $"({Tolerance1?.CumulativePitch ?? 0:F0} / {Tolerance2?.CumulativePitch ?? 0:F0} µm).";
    }

    /// <summary>
    /// Backlash target → allowances.
    ///
    /// Backlash belongs to the pair, so each limit fixes only the SUM A_sn1 + A_sn2 and
    /// <see cref="SplitRule"/> decides how it is shared. The minimum backlash happens with
    /// the thickest teeth (A_sne) at the smallest centre distance, the maximum with the
    /// thinnest teeth (A_sni) at the largest — so the two limits pair with opposite centre
    /// distance deviations, which is easy to get backwards.
    /// </summary>
    private void ResolveAllowancesFromBacklash(ref double asne1, ref double asni1,
                                               ref double asne2, ref double asni2)
    {
        string quantity;
        double jbnMin, jbnMax;

        switch (AllowanceMode)
        {
            case ToothThicknessAllowanceMode.CircumferentialBacklash:
                quantity = "circumferential backlash j_wt";
                jbnMin = GearToothMeasurement.NormalBacklashFromCircumferential(
                    TargetBacklashMin, WorkingPressureAngle, BaseHelixAngle);
                jbnMax = GearToothMeasurement.NormalBacklashFromCircumferential(
                    TargetBacklashMax, WorkingPressureAngle, BaseHelixAngle);
                break;

            case ToothThicknessAllowanceMode.RadialBacklash:
                quantity = "radial backlash j_r";
                jbnMin = GearToothMeasurement.NormalBacklashFromRadial(
                    TargetBacklashMin, WorkingPressureAngle, BaseHelixAngle);
                jbnMax = GearToothMeasurement.NormalBacklashFromRadial(
                    TargetBacklashMax, WorkingPressureAngle, BaseHelixAngle);
                break;

            default:
                quantity = "normal backlash j_bn";
                jbnMin = TargetBacklashMin;
                jbnMax = TargetBacklashMax;
                break;
        }

        var extraNotes = new List<string>();

        // Without a usable upper limit there is no tolerance width, only a single target.
        // Fall back to the same F_p width the automatic mode uses rather than collapsing
        // the tolerance to zero, which no gear can be made to.
        if (jbnMax <= jbnMin)
        {
            double tsn = ((Tolerance1?.CumulativePitch ?? 0) + (Tolerance2?.CumulativePitch ?? 0)) / 1000.0;
            double cosAlphaN = Math.Cos(PressureAngle * Math.PI / 180.0);
            jbnMax = jbnMin + tsn * cosAlphaN
                   + (UsedCentreDistanceUpperDev - UsedCentreDistanceLowerDev)
                     * 2.0 * Math.Sin(WorkingPressureAngle * Math.PI / 180.0)
                     * Math.Cos(BaseHelixAngle * Math.PI / 180.0);
            extraNotes.Add(
                "No maximum was given, so the tolerance width was taken from the two gears' "
              + "ISO 1328-1 cumulative pitch tolerances F_p.");
        }

        double sumUpper = GearToothMeasurement.AllowanceSumForNormalBacklash(
            jbnMin, UsedCentreDistanceLowerDev, PressureAngle, WorkingPressureAngle, BaseHelixAngle);
        double sumLower = GearToothMeasurement.AllowanceSumForNormalBacklash(
            jbnMax, UsedCentreDistanceUpperDev, PressureAngle, WorkingPressureAngle, BaseHelixAngle);

        (asne1, asne2) = SplitAllowance(sumUpper);
        (asni1, asni2) = SplitAllowance(sumLower);

        if (sumUpper > 0)
        {
            extraNotes.Add(
                $"The requested minimum backlash needs teeth THICKER than nominal "
              + $"(ΣA_sne = {sumUpper * 1000:+0.0} µm). Either the target is too large for this "
              + "centre distance tolerance, or the nominal tooth thickness should be raised.");
        }

        string split = SplitRule switch
        {
            BacklashSplit.PinionOnly => "all of it taken on the pinion",
            BacklashSplit.WheelOnly => "all of it taken on the wheel",
            _ => "split evenly between the two gears"
        };

        AllowanceNote =
            $"Allowances derived from the requested {quantity} of "
          + $"{TargetBacklashMin * 1000:F0}–{TargetBacklashMax * 1000:F0} µm "
          + $"(j_bn {jbnMin * 1000:F1}–{jbnMax * 1000:F1} µm), {split}."
          + (extraNotes.Count > 0 ? " " + string.Join(" ", extraNotes) : "");
    }

    /// <summary>
    /// W_k or M_d limits → allowances. Both are measured on a single gear, so each gear is
    /// inverted independently and no split rule applies.
    /// </summary>
    private void ResolveAllowancesFromMeasurement(ref double asne1, ref double asni1,
                                                  ref double asne2, ref double asni2)
    {
        bool span = AllowanceMode == ToothThicknessAllowanceMode.SpanLimits;
        var failed = new List<string>();

        double? Invert(int gear, double target)
        {
            var g = MeasurementInput(gear, 0, 0);
            return span
                ? GearToothMeasurement.AllowanceForSpan(g, target)
                : GearToothMeasurement.AllowanceForBallDimension(g, target);
        }

        void Apply(int gear, double upperTarget, double lowerTarget,
                   ref double asne, ref double asni)
        {
            double? e = Invert(gear, upperTarget);
            double? i = Invert(gear, lowerTarget);

            if (e is { } ev) asne = ev; else failed.Add($"gear {gear} upper limit");
            if (i is { } iv) asni = iv; else failed.Add($"gear {gear} lower limit");

            // The largest permitted dimension must give the least thinning. If the two are
            // the wrong way round the drawing was read upside down; swapping silently would
            // hide that, so it is reported.
            if (asni > asne) failed.Add($"gear {gear}: the upper limit is smaller than the lower one");
        }

        Apply(1, SpanOrBall(1, true), SpanOrBall(1, false), ref asne1, ref asni1);
        Apply(2, SpanOrBall(2, true), SpanOrBall(2, false), ref asne2, ref asni2);

        AllowanceNote =
            (span
                ? "Allowances back-calculated from the base tangent length limits (A_sn = A_W / cos α_n)."
                : "Allowances back-calculated from the dimension over balls limits, by re-solving the "
                + "involute for the thinned tooth rather than through a sensitivity factor.")
          + (failed.Count > 0
                ? " Could not be resolved for: " + string.Join(", ", failed)
                + ". Those allowances were left unchanged — check the entered limits against the "
                + "nominal value shown in the control dimensions table."
                : "");
    }

    /// <summary>
    /// A DIN 3967 tolerance zone → allowances. Straight table lookup on each gear's own
    /// reference diameter, with A_sni = A_sne − T_sn (Clause 3.2).
    ///
    /// The allowances are what the standard tabulates; the backlash that follows is the
    /// theoretical one. DIN 3967 Clause A.1.2 is explicit that acceptance backlash also
    /// depends on temperature, housing tolerance, bore parallelism, tooth deviations and
    /// elasticity — so the number this produces is a starting point, not an acceptance limit.
    /// </summary>
    private void ResolveAllowancesFromDin3967(ref double asne1, ref double asni1,
                                              ref double asne2, ref double asni2)
    {
        var zone1 = Din3967.Allowances(Din3967ToleranceSeries1, Din3967AllowanceSeries, ReferenceDiameter1);
        var zone2 = Din3967.Allowances(Din3967ToleranceSeries2, Din3967AllowanceSeries, ReferenceDiameter2);

        var notes = new List<string>();

        if (zone1 is { } z1) { asne1 = z1.AsneMm; asni1 = z1.AsniMm; }
        else notes.Add($"gear 1 (d = {ReferenceDiameter1:F1} mm)");

        if (zone2 is { } z2) { asne2 = z2.AsneMm; asni2 = z2.AsniMm; }
        else notes.Add($"gear 2 (d = {ReferenceDiameter2:F1} mm)");

        if (notes.Count > 0)
        {
            AllowanceNote =
                "DIN 3967 does not cover " + string.Join(" or ", notes)
              + " — the tables run from 0 to 10000 mm reference diameter. Those allowances were "
              + "left unchanged.";
            return;
        }

        string preferred =
            Din3967.IsPreferredToleranceSeries(Din3967ToleranceSeries1) &&
            Din3967.IsPreferredToleranceSeries(Din3967ToleranceSeries2)
                ? ""
                : " Clause 3.3 names series 24 to 27 as the preferred ones.";

        AllowanceNote =
            $"Allowances from DIN 3967 tolerance zones {zone1!.Value.Designation} (gear 1) and "
          + $"{zone2!.Value.Designation} (gear 2): "
          + $"A_sne = {zone1.Value.AsneMicron:F0} / {zone2.Value.AsneMicron:F0} µm, "
          + $"T_sn = {zone1.Value.TsnMicron:F0} / {zone2.Value.TsnMicron:F0} µm."
          + preferred
          + " DIN 3962 Part 1 additionally requires T_sn to be at least twice the permissible "
          + "tooth thickness fluctuation R_s — check that against the manufacturing route.";
    }

    private double SpanOrBall(int gear, bool upper)
    {
        if (AllowanceMode == ToothThicknessAllowanceMode.SpanLimits)
            return gear == 1 ? (upper ? SpanLimitUpper1 : SpanLimitLower1)
                             : (upper ? SpanLimitUpper2 : SpanLimitLower2);

        return gear == 1 ? (upper ? BallLimitUpper1 : BallLimitLower1)
                         : (upper ? BallLimitUpper2 : BallLimitLower2);
    }

    /// <summary>Shares a total allowance between the two gears per <see cref="SplitRule"/>.</summary>
    private (double gear1, double gear2) SplitAllowance(double total) => SplitRule switch
    {
        BacklashSplit.PinionOnly => (total, 0.0),
        BacklashSplit.WheelOnly => (0.0, total),
        _ => (total / 2.0, total / 2.0)
    };

    // ============ HELPER METHODS ============

    /// <summary>
    /// Calculate the profile shift coefficient of gear 2 from the centre distance.
    /// Used by the UI to keep x2 live while the user is still typing.
    /// </summary>
    public void CalculateProfileShift2()
    {
        double betaRad = HelixAngle * Math.PI / 180.0;
        double alphanRad = PressureAngle * Math.PI / 180.0;

        TransverseModule = NormalModule / Math.Cos(betaRad);
        double alphatRad = Math.Atan(Math.Tan(alphanRad) / Math.Cos(betaRad));

        double referenceCenter = TransverseModule * (NumberOfTeeth1 + NumberOfTeeth2) / 2.0;
        double invAlphaT = Math.Tan(alphatRad) - alphatRad;

        double cosAlphaWT = referenceCenter * Math.Cos(alphatRad) / CenterDistance;
        if (cosAlphaWT > 1.0) cosAlphaWT = 1.0;
        if (cosAlphaWT < -1.0) cosAlphaWT = -1.0;
        double alphaWTRad = Math.Acos(cosAlphaWT);

        double invDiff = (Math.Tan(alphaWTRad) - alphaWTRad) - invAlphaT;

        SumProfileShift = (NumberOfTeeth1 + NumberOfTeeth2) * invDiff / (2.0 * Math.Tan(alphanRad));
        ProfileShiftCoeff2 = SumProfileShift - ProfileShiftCoeff1;
    }

    /// <summary>Reference centre distance for the current teeth, module and helix angle (Σx = 0).</summary>
    public double ReferenceCentreDistance()
    {
        if (NormalModule <= 0 || NumberOfTeeth1 <= 0 || NumberOfTeeth2 <= 0) return 0;
        double mt = NormalModule / Math.Cos(HelixAngle * Math.PI / 180.0);
        return mt * (NumberOfTeeth1 + NumberOfTeeth2) / 2.0;
    }

    /// <summary>Get the standard module (ISO 54) closest to a calculated value.</summary>
    public static double GetStandardModule(double calculatedModule)
        => StandardModules.OrderBy(m => Math.Abs(m - calculatedModule)).First();
}

// ============ SUPPORTING CLASSES ============

/// <summary>
/// Gear material properties. The allowable stress numbers are derived from
/// ISO 6336-5:2016 Table 1 rather than being hard-coded, so they always match the
/// material group, quality grade and surface hardness stated for the grade.
/// </summary>
public class GearMaterial
{
    public string Name { get; set; } = "18CrNiMo7-6";
    public string HeatTreatment { get; set; } = "Case-hardened";
    public double SurfaceHardness { get; set; } = 60;           // HRC (informational)

    // ISO 6336-5 classification - used to derive the allowable stress numbers
    public GearMaterialType Iso6336Type { get; set; } = GearMaterialType.CaseHardened;
    public GearQualityGrade Quality { get; set; } = GearQualityGrade.MQ;
    public double SurfaceHardnessIso { get; set; } = 700;       // HV or HBW per Table 1

    public double BendingFatigueLimit { get; set; } = 430;      // σFlim (MPa)
    public double ContactFatigueLimit { get; set; } = 1500;     // σHlim (MPa)
    public double TensileStrength { get; set; } = 1200;         // σB (MPa)
    public double YieldStrength { get; set; } = 850;            // σS (MPa)
    public double ElasticModulus { get; set; } = 206;           // E (GPa)
    public double PoissonRatio { get; set; } = 0.3;             // ν

    /// <summary>Row id when this grade came from the user's own library; null for built-ins.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? CustomId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsCustom => CustomId != null;

    /// <summary>
    /// The label every stored reference resolves by - share links, saved calculations and
    /// the library's uniqueness index all use it. "C45" alone is ambiguous because the same
    /// grade appears twice with different heat treatments, so the pair is the key.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Label => $"{Name} - {HeatTreatment}";

    /// <summary>
    /// Recalculates σFlim / σHlim from ISO 6336-5 Table 1 for the current type, quality
    /// grade and surface hardness. Returns any range warning.
    /// </summary>
    public (string? contactWarning, string? bendingWarning) ApplyIso6336Strength()
    {
        var h = Iso6336Material.Get(Iso6336Type, GearStressType.Contact, Quality, SurfaceHardnessIso);
        var f = Iso6336Material.Get(Iso6336Type, GearStressType.Bending, Quality, SurfaceHardnessIso);
        ContactFatigueLimit = h.SigmaLim;
        BendingFatigueLimit = f.SigmaLim;
        return (h.Warning, f.Warning);
    }

    public override string ToString() => $"{Name} ({HeatTreatment})";

    static GearMaterial()
    {
        foreach (var m in _builtIn)
        {
            m.ApplyIso6336Strength();
        }
    }

    // ============ BUILT-IN + CUSTOM LIBRARY ============

    private static List<GearMaterial> _custom = new();
    private static List<GearMaterial>? _all;

    /// <summary>
    /// Every gear material the user can pick: built-ins first, then their own.
    ///
    /// The order matters. GearPair.razor binds its material selects by INDEX, so a custom
    /// entry appearing before a built-in would silently repoint an in-progress calculation
    /// at a different grade. Customs always go last.
    /// </summary>
    public static List<GearMaterial> StandardMaterials
        => _all ??= _builtIn.Concat(_custom).ToList();

    public static IReadOnlyList<GearMaterial> BuiltInMaterials => _builtIn;
    public static IReadOnlyList<GearMaterial> CustomMaterials => _custom;

    /// <summary>
    /// Replaces the user's own grades and rebuilds the merged list. Called by
    /// CustomLibraryService at startup and on every auth state change; signed out it is
    /// called with an empty sequence so a shared browser does not keep the previous user's
    /// grades in the dropdowns.
    /// </summary>
    public static void SetCustomGearMaterials(IEnumerable<GearMaterial> materials)
    {
        _custom = materials.ToList();

        // A custom grade is stored by its ISO 6336-5 classification, not by its stress
        // numbers, so σFlim/σHlim have to be derived on the way in exactly as the
        // built-ins are in the static constructor. Skipping this leaves them at 0 and
        // every safety factor for that grade comes out 0 - the engine divides by them.
        foreach (var m in _custom)
        {
            m.ApplyIso6336Strength();
        }

        _all = null;
    }

    /// <summary>True when a "Name - HeatTreatment" label is already taken by a built-in.</summary>
    public static bool IsBuiltInLabel(string label)
        => _builtIn.Any(m => string.Equals(m.Label, label.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Resolves a grade by its <see cref="Label"/>, or null when unknown.</summary>
    public static GearMaterial? ByLabel(string label)
        => StandardMaterials.FirstOrDefault(
            m => string.Equals(m.Label, label?.Trim(), StringComparison.OrdinalIgnoreCase));

    // Built-in gear materials
    private static readonly List<GearMaterial> _builtIn = new()
    {
        new GearMaterial
        {
            Name = "C45",
            HeatTreatment = "Normalized",
            SurfaceHardness = 22,
            Iso6336Type = GearMaterialType.NormalizedLowCarbonSteel,
            Quality = GearQualityGrade.MQ,
            SurfaceHardnessIso = 190,
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
            Iso6336Type = GearMaterialType.ThroughHardenedCarbonSteel,
            Quality = GearQualityGrade.MQ,
            SurfaceHardnessIso = 210,
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
            Iso6336Type = GearMaterialType.ThroughHardenedAlloySteel,
            Quality = GearQualityGrade.MQ,
            SurfaceHardnessIso = 300,
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
            Iso6336Type = GearMaterialType.FlameOrInductionHardened,
            Quality = GearQualityGrade.MQ,
            SurfaceHardnessIso = 550,
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
            Iso6336Type = GearMaterialType.CaseHardened,
            Quality = GearQualityGrade.MQ,
            SurfaceHardnessIso = 700,
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
            Iso6336Type = GearMaterialType.CaseHardened,
            Quality = GearQualityGrade.MQ,
            SurfaceHardnessIso = 700,
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
            Iso6336Type = GearMaterialType.CaseHardened,
            Quality = GearQualityGrade.MQ,
            SurfaceHardnessIso = 700,
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
            Iso6336Type = GearMaterialType.NitridedThroughHardeningSteel,
            Quality = GearQualityGrade.MQ,
            SurfaceHardnessIso = 600,
            TensileStrength = 1100,
            YieldStrength = 900,
            ElasticModulus = 206,
            PoissonRatio = 0.3
        }
    };
}
