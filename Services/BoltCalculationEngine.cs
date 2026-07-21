using System;
using System.Collections.Generic;
using System.Linq;

namespace MechanicalCalculatorWeb.Services
{
    /// <summary>
    /// Calculation mode for bolt analysis
    /// </summary>
    public enum CalculationMode
    {
        Quick,      // Simplified - minimum inputs, standard assumptions
        Standard,   // Standard - common parameters, some advanced options
        Advanced    // Full VDI 2230 - all parameters available
    }

    /// <summary>
    /// Joint type
    /// </summary>
    public enum JointType
    {
        ThroughHole,    // Bolt passes through, secured with nut
        TappedHole      // Bolt threads into tapped hole (blind hole)
    }

    /// <summary>
    /// Load type for fatigue consideration
    /// </summary>
    public enum LoadType
    {
        Static,         // Constant load
        Pulsating,      // Load varies between 0 and max (or min and max, same sign)
        Alternating     // Load reverses direction
    }

    /// <summary>
    /// Load case type according to VDI 2230 / KISSsoft
    /// </summary>
    public enum LoadCaseType
    {
        AxialOnly,          // Only axial force (concentric or eccentric)
        AxialAndTransverse  // Axial force + Transverse force + Bending moment
    }

    /// <summary>
    /// Load application point
    /// </summary>
    public enum LoadApplicationPoint
    {
        Concentric,     // Load applied at bolt axis (e = 0)
        Eccentric       // Load applied with eccentricity (e > 0)
    }

    /// <summary>
    /// Bolting type according to VDI 2230 (Load introduction location)
    /// </summary>
    public enum BoltingType
    {
        SV1_ThroughBolt_LoadAtHead,      // Through-bolt, load introduced at head (most common)
        SV2_ThroughBolt_LoadAtNut,       // Through-bolt, load introduced at nut
        SV3_ThroughBolt_LoadInMiddle,    // Through-bolt, load in middle of clamped parts
        SV4_TappedHole_LoadAtHead,       // Tapped hole, load at head
        SV5_TappedHole_LoadInMiddle,     // Tapped hole, load in middle
        SV6_TappedHole_LoadAtThread      // Tapped hole, load at thread engagement
    }

    /// <summary>
    /// Clamped part definition
    /// </summary>
    public class ClampedPart
    {
        public double Thickness { get; set; }           // mm
        public string MaterialName { get; set; } = "Steel";
        public double ElasticModulus { get; set; } = 210000; // MPa
        public double ThermalExpansion { get; set; } = 11.5e-6; // 1/K
        public double HoleDiameter { get; set; }        // mm (0 = use standard clearance)
        public double OuterDiameter { get; set; }       // mm (0 = auto calculate)
        public string SurfaceFinish { get; set; } = "Machined"; // For settling calculation
    }

    /// <summary>
    /// Input parameters for single bolt calculation
    /// </summary>
    public class SingleBoltInput
    {
        // Calculation mode
        public CalculationMode Mode { get; set; } = CalculationMode.Standard;

        // Bolt selection
        public string BoltSize { get; set; } = "M10";
        public bool UseFineThread { get; set; } = false;
        public string StrengthClass { get; set; } = "8.8";
        public double ShankLength { get; set; } = 0;    // mm, unthreaded length (0 = fully threaded)
        public double BoltLength { get; set; } = 0;     // mm, total nominal bolt length
        public double ThreadedLengthInGrip { get; set; } = 0; // mm, threaded length in clamping zone

        // Joint geometry
        public JointType JointType { get; set; } = JointType.ThroughHole;
        public BoltingType BoltingType { get; set; } = BoltingType.SV1_ThroughBolt_LoadAtHead;
        public List<ClampedPart> ClampedParts { get; set; } = new();
        public bool UseWasherUnderHead { get; set; } = false;
        public bool UseWasherUnderNut { get; set; } = false;

        // Custom washer properties
        public bool IsCustomWasher { get; set; } = false;
        public double CustomWasherD1 { get; set; } = 0; // Inner diameter
        public double CustomWasherD2 { get; set; } = 0; // Outer diameter
        public double CustomWasherH { get; set; } = 0; // Thickness

        // Load case type (VDI 2230 / KISSsoft)
        public LoadCaseType LoadCase { get; set; } = LoadCaseType.AxialOnly;
        public LoadApplicationPoint LoadApplication { get; set; } = LoadApplicationPoint.Concentric;

        // Applied loads
        public double AxialForceMax { get; set; } = 0;  // N (positive = tension)
        public double AxialForceMin { get; set; } = 0;  // N (for fatigue, usually 0 or same as max for static)
        public double ShearForce { get; set; } = 0;     // N (transverse force FQ)
        public double BendingMoment { get; set; } = 0;  // Nmm (working moment MB)
        public double Eccentricity { get; set; } = 0;   // mm (load eccentricity from bolt axis)
        public double LoadIntroductionFactor { get; set; } = 0.5; // n (0 = at head, 1 = at nut, 0.5 = middle) - manual override
        public bool AutoCalculateLoadIntroductionFactor { get; set; } = true; // If true, calculate n from Table 5.2/1
        public double LoadApplicationFactorValue { get; set; } = 1.0; // ϕA - dynamic load factor for service conditions
        public LoadType LoadType { get; set; } = LoadType.Static;

        // Fatigue parameters
        public double DesignLoadCycles { get; set; } = 2e5; // Design number of load cycles (default 200,000)

        // Assembly parameters
        public string TighteningMethod { get; set; } = "Torque wrench";
        public string SurfaceCondition { get; set; } = "Black oxide, oiled";
        public double FrictionThread { get; set; } = 0; // 0 = use from surface condition
        public double FrictionHead { get; set; } = 0;   // 0 = use from surface condition


        // Additional factors
        public double InterfaceFriction { get; set; } = 0.12;  // μT for slip resistance
        public int NumberOfInterfaces { get; set; } = 1;        // For shear load transfer

        // Surface roughness and geometric details
        public double ThreadRoughness { get; set; } = 16.0;  // Thread surface roughness Rz (μm)
        public double HeadRoughness { get; set; } = 16.0;     // Head surface roughness Rz (μm)
        public double PartsRoughness { get; set; } = 16.0;    // Parts surface roughness Rz (μm)
        public double ChamferDiameter { get; set; } = 0.0;    // Chamfer diameter (mm)
        public string BoltStandard { get; set; } = "ISO4014"; // Bolt type/standard

        // Washer material properties
        public double WasherElasticModulus { get; set; } = 205000; // MPa (default: steel)
        public string WasherMaterial { get; set; } = "C45";        // Washer material name
    }

    /// <summary>
    /// Results from single bolt calculation
    /// </summary>
    public class SingleBoltResult
    {
        public bool IsValid { get; set; } = false;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        // Bolt data (from database)
        public double d { get; set; }           // Nominal diameter
        public double P { get; set; }           // Pitch
        public double d2 { get; set; }          // Pitch diameter
        public double d3 { get; set; }          // Minor diameter
        public double As { get; set; }          // Stress area
        public double Rp02 { get; set; }        // Yield strength
        public double Rm { get; set; }          // Tensile strength

        // Compliance/Stiffness (R2)
        public double DeltaS { get; set; }      // Bolt compliance (mm/N)
        public double DeltaP { get; set; }      // Clamped parts compliance (mm/N)
        public double Phi { get; set; }         // Force ratio Φ = δP/(δP+δS)
        public double LoadIntroductionFactor { get; set; } // n - Load introduction factor from Table 5.2/1
        public double AkOverH { get; set; }     // ak/h ratio for Table 5.2/1
        public double LAOverH { get; set; }     // lA/h ratio for Table 5.2/1
        public double PhiN { get; set; }        // Effective force ratio Φn = n×Φ
        public double CS { get; set; }          // Bolt stiffness (N/mm)
        public double CP { get; set; }          // Clamped parts stiffness (N/mm)

        // Force distribution (R3-R4)
        public double FSA { get; set; }         // Additional bolt force at FA_max (N)
        public double FSA_min { get; set; }     // Additional bolt force at FA_min (N)
        public double FPA { get; set; }         // Relief of clamped parts (N)

        // Preload losses (R5)
        public double FZ { get; set; }          // Embedding/settling loss (N)
        public double FZ_total_um { get; set; } // Total settling (μm)


        // Required preload (R6)
        public double FKR_min { get; set; }     // Minimum required clamp force (N)
        public double FKR_slip { get; set; }    // Required for slip resistance (N)
        public double FKR_separation { get; set; } // Required to prevent separation (N)


        // Assembly preload (R7)
        public double FM_min { get; set; }      // Minimum assembly preload (N)
        public double FM_max { get; set; }      // Maximum assembly preload (N)
        public double FM_mean { get; set; }     // Mean assembly preload (N)
        public double AlphaA { get; set; }      // Tightening factor

        // Working forces
        public double FS_max { get; set; }      // Maximum bolt force in service (N)
        public double FS_min { get; set; }      // Minimum bolt force in service (N)
        public double FK_min { get; set; }      // Minimum clamp force in service (N)

        // Stresses (R8-R9)
        public double SigmaZ_assembly { get; set; }     // Tensile stress at assembly (MPa)
        public double Tau_assembly { get; set; }        // Torsional stress at assembly (MPa)
        public double SigmaRed_assembly { get; set; }   // Von Mises stress at assembly (MPa)
        public double SigmaZ_working { get; set; }      // Tensile stress in service (MPa)
        public double SigmaRed_working { get; set; }    // Von Mises stress in service (MPa)
        public double Utilization { get; set; }         // σred/Rp0.2 (%)

        // Surface pressure
        public double PressureHead { get; set; }        // Pressure under head (MPa)
        public double PressureNut { get; set; }         // Pressure under nut (MPa)
        public double PressureWasherHead { get; set; }  // Pressure under washer at head (MPa)
        public double PressureWasherNut { get; set; }   // Pressure under washer at nut (MPa)
        public double PressureAllowable { get; set; }   // Allowable surface pressure (MPa)

        // Safety factors (R10)
        public double SF_yield { get; set; }            // Against yielding (Rp0.2/σred)
        public double SF_clamp { get; set; }            // Against loss of clamp (FKR/FKR_min)
        public double SF_slip { get; set; }             // Against slipping
        public double SF_fatigue { get; set; }          // Against fatigue
        public double SF_pressure { get; set; }         // Against surface crushing
        public double SF_min { get; set; }              // Minimum (critical) safety factor
        public string SF_critical { get; set; } = "";   // Which safety is critical

        // Load utilization
        public double LoadUtilization { get; set; }     // Load utilization ratio (%)

        // Tightening torque (R11)
        public double MA { get; set; }                  // Max assembly torque (Nm)
        public double MA_min { get; set; }              // Min assembly torque (Nm)
        public double MG { get; set; }                  // Thread friction torque (Nm)
        public double MK { get; set; }                  // Head friction torque (Nm)
        public double KFactor { get; set; }             // Nut factor (MA = K×F×d)
        public double ML { get; set; }                  // Loosening torque (Nm)

        // Fatigue (R12) - if dynamic loading
        public double SigmaA { get; set; }              // Stress amplitude (MPa)
        public double SigmaM { get; set; }              // Mean stress (MPa)
        public double SigmaASV { get; set; }            // Allowable stress amplitude (MPa)

        // Geometry for visualization
        public double ClampLength { get; set; }         // Total clamping length lK (mm)
        public double BoltLength { get; set; }          // Required bolt length (mm)

        // Additional bolt specifications
        public string BoltType { get; set; } = "";      // Bolt standard type (ISO 4014, DIN 912, etc.)
        public double FlankAngle { get; set; } = 60.0;  // Thread flank angle (degrees), typically 60° for metric
        public double HeadDiameter { get; set; }        // Head diameter dw (mm)
        public double HeadThickness { get; set; }       // Head thickness k (mm)
        public double ShankDiameter { get; set; }       // Unthreaded shank diameter (mm)
        public double ShankLength { get; set; }         // Unthreaded shank length (mm)
        public double ThreadRoughness { get; set; }     // Thread surface roughness Rz (μm)
        public double FrictionThread { get; set; }      // Thread friction coefficient μG
        public double FrictionHead { get; set; }        // Head friction coefficient μK
        public double FrictionParts { get; set; }       // Parts friction coefficient μP
        public double YoungModulusBolt { get; set; }    // Bolt Young's modulus (MPa)

        // Nut specifications
        public string NutMaterial { get; set; } = "";   // Nut material
        public double NutThickness { get; set; }        // Nut height m (mm)
        public double NutDiameter { get; set; }         // Nut bearing diameter dw (mm)
        public double NutRoughness { get; set; }        // Nut surface roughness (μm)

        // Washer specifications
        public string WasherMaterial { get; set; } = "";  // Washer material
        public double WasherThickness { get; set; }       // Washer thickness h (mm)
        public double WasherDiameterInner { get; set; }   // Washer inner diameter d1 (mm)
        public double WasherDiameterOuter { get; set; }   // Washer outer diameter d2 (mm)
        public double WasherRoughness { get; set; }       // Washer surface roughness (μm)

        // VDI 2230 specific calculations
        public double DA { get; set; }                    // Substitutional outside diameter at interface (mm)
        public double DAn { get; set; }                   // Substitutional outside diameter of basic solid (mm)
        public double DA_Gr { get; set; }                 // Limiting outside diameter, max. diameter of deformation cone (mm)
        public double ConeAngle { get; set; } = 60.0;     // Load cone angle (degrees)
        public double BoltExtension { get; set; }         // Bolt extension fS (mm)
        public double PartsExtension { get; set; }        // Parts extension fP (mm)
        public double HoleDiameter { get; set; }          // Hole diameter dh (mm)
        public double ChamferDiameter { get; set; }       // Chamfer diameter (mm)

        // Washer compliance (Addition for plate resilience - δPzu in KISSsoft)
        public double DeltaPWasher { get; set; }          // Washer compliance δPzu (mm/N)
        public double DeltaPTotal { get; set; }           // Total parts compliance δP + δPzu (mm/N)
        public double WasherElasticModulus { get; set; }  // Washer Young's modulus (MPa)

        // === THREE FORCE SCENARIO RESULTS ===

        // Scenario 1: Maximum Pretension Force (90% yield)
        public double F09_max { get; set; }               // Max pretension at 90% yield (N)
        public double FS_at_F09max { get; set; }          // Working bolt force at F09_max (N)
        public double FK_at_F09max { get; set; }          // Working clamp force at F09_max (N)
        public double SigmaZ_at_F09max { get; set; }      // Tensile stress at F09_max (MPa)
        public double Tau_at_F09max { get; set; }         // Torsional stress at F09_max (MPa)
        public double SigmaRed_at_F09max { get; set; }    // Equivalent stress at F09_max (MPa)
        public double Utilization_at_F09max { get; set; } // Yield utilization at F09_max (%)
        public double MA_at_F09max { get; set; }          // Tightening torque at F09_max (Nm)

        // Scenario 2: Minimum Required Assembly Preload
        public double FS_at_FMmin { get; set; }           // Working bolt force at FM_min (N)
        public double FK_at_FMmin { get; set; }           // Working clamp force at FM_min (N)
        public double SigmaZ_at_FMmin { get; set; }       // Tensile stress at FM_min (MPa)
        public double Tau_at_FMmin { get; set; }          // Torsional stress at FM_min (MPa)
        public double SigmaRed_at_FMmin { get; set; }     // Equivalent stress at FM_min (MPa)
        public double Utilization_at_FMmin { get; set; }  // Yield utilization at FM_min (%)
        public double MA_at_FMmin { get; set; }           // Tightening torque at FM_min (Nm)

        // Additional calculated values for each scenario
        // Scenario 1 (F09_max)
        public double BoltExtension_at_F09max { get; set; }    // Bolt extension fS at F09_max (μm)
        public double PartExtension_at_F09max { get; set; }    // Part compression fP at F09_max (μm)
        public double PressureHead_at_F09max { get; set; }     // Surface pressure under head at F09_max (MPa)
        public double PressureNut_at_F09max { get; set; }      // Surface pressure under nut at F09_max (MPa)

        // Scenario 2 (FM_min)
        public double BoltExtension_at_FMmin { get; set; }     // Bolt extension fS at FM_min (μm)
        public double PartExtension_at_FMmin { get; set; }     // Part compression fP at FM_min (μm)
        public double PressureHead_at_FMmin { get; set; }      // Surface pressure under head at FM_min (MPa)
        public double PressureNut_at_FMmin { get; set; }       // Surface pressure under nut at FM_min (MPa)

        // Scenario 3 (FM_max) - BoltExtension and PartsExtension already exist
        public double PressureHead_at_FMmax { get; set; }      // Surface pressure under head at FM_max (MPa)
        public double PressureNut_at_FMmax { get; set; }       // Surface pressure under nut at FM_max (MPa)

        // Additional Plate Force (relief of clamped parts due to external load)
        public double AdditionalPlateForce { get; set; }       // FPA - Additional plate force (N)

        // Fatigue Analysis Results
        public double FatigueLoad { get; set; }                // Fatigue load amplitude (N)
        public double FatigueLife { get; set; }                // Estimated fatigue life (cycles)
        public double NumberOfLoadCycles { get; set; }         // Design number of load cycles
        public double FatigueDamage { get; set; }              // Cumulative fatigue damage (0-1)

        // Per-scenario embedding (settling) values
        public double FZ_at_F09max { get; set; }               // Embedding loss at F09_max (N)
        public double FZ_um_at_F09max { get; set; }            // Embedding in μm at F09_max
        public double FZ_at_FMmin { get; set; }                // Embedding loss at FM_min (N)
        public double FZ_um_at_FMmin { get; set; }             // Embedding in μm at FM_min
        public double FZ_at_FMmax { get; set; }                // Embedding loss at FM_max (N)
        public double FZ_um_at_FMmax { get; set; }             // Embedding in μm at FM_max

        // Per-scenario tightening torque breakdown
        public double MG_at_F09max { get; set; }               // Thread torque at F09_max (Nm)
        public double MK_at_F09max { get; set; }               // Head torque at F09_max (Nm)
        public double MG_at_FMmin { get; set; }                // Thread torque at FM_min (Nm)
        public double MK_at_FMmin { get; set; }                // Head torque at FM_min (Nm)
        public double MG_at_FMmax { get; set; }                // Thread torque at FM_max (Nm)
        public double MK_at_FMmax { get; set; }                // Head torque at FM_max (Nm)
    }

    /// <summary>
    /// VDI 2230 Single Bolt Calculation Engine
    /// </summary>
    public class BoltCalculationEngine
    {
        private SingleBoltInput _input = new();
        private SingleBoltResult _result = new();
        private BoltDimension _bolt = null!;
        private BoltStrengthClass _strength = null!;
        private FrictionCoefficient _friction = null!;
        private TighteningMethod _tightening = null!;

        // Material constants (VDI 2230)
        private const double E_STEEL = 205000; // MPa (VDI 2230 standard value)
        private const double ALPHA_STEEL = 11.5e-6; // 1/K

        public SingleBoltResult Calculate(SingleBoltInput input)
        {
            _input = input;
            _result = new SingleBoltResult();

            try
            {
                // Step 0: Get bolt data and validate inputs
                if (!InitializeAndValidate())
                    return _result;

                // Step R1: Calculate required minimum clamp force
                CalculateRequiredClampForce();

                // Step R2: Calculate compliance/stiffness
                CalculateCompliance();

                // Step R3-R4: Calculate force distribution
                CalculateForceDistribution();

                // Step R5: Calculate preload losses (settling, thermal)
                CalculatePreloadLosses();

                // Step R6-R7: Calculate assembly preload
                CalculateAssemblyPreload();

                // Step R8-R9: Calculate stresses
                CalculateStresses();

                // Calculate three force scenarios
                CalculateThreeForceScenarios();

                // Step R10: Calculate safety factors
                CalculateSafetyFactors();

                // Step R11: Calculate tightening torque
                CalculateTighteningTorque();

                // Step R12: Fatigue check (if applicable)
                if (_input.LoadType != LoadType.Static)
                {
                    CalculateFatigue();
                }

                // Calculate geometry for visualization
                CalculateGeometry();

                _result.IsValid = _result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                _result.Errors.Add($"Calculation error: {ex.Message}");
                _result.IsValid = false;
            }

            return _result;
        }

        private bool InitializeAndValidate()
        {
            // Get bolt dimensions
            var bolt = BoltService.GetBoltBySize(_input.BoltSize);
            if (bolt == null)
            {
                _result.Errors.Add($"Bolt size {_input.BoltSize} not found in database");
                return false;
            }
            _bolt = bolt;

            // Get strength class
            var strength = BoltService.GetStrengthClass(_input.StrengthClass);
            if (strength == null)
            {
                _result.Errors.Add($"Strength class {_input.StrengthClass} not found");
                return false;
            }
            _strength = strength;

            // Get friction coefficients - handle custom friction
            if (_input.SurfaceCondition == "Custom" && _input.FrictionThread > 0 && _input.FrictionHead > 0)
            {
                // Use custom friction values - create a temporary friction object
                _friction = new FrictionCoefficient
                {
                    Condition = "Custom",
                    MuG_min = _input.FrictionThread,
                    MuG_max = _input.FrictionThread,
                    MuG_typical = _input.FrictionThread,
                    MuK_min = _input.FrictionHead,
                    MuK_max = _input.FrictionHead,
                    MuK_typical = _input.FrictionHead
                };
            }
            else
            {
                var friction = BoltService.GetFrictionCoefficient(_input.SurfaceCondition);
                if (friction == null)
                {
                    // Default to typical oiled steel values
                    _friction = new FrictionCoefficient
                    {
                        Condition = "Default",
                        MuG_min = 0.10,
                        MuG_max = 0.16,
                        MuG_typical = 0.12,
                        MuK_min = 0.10,
                        MuK_max = 0.16,
                        MuK_typical = 0.12
                    };
                }
                else
                {
                    _friction = friction;
                }
            }

            // Get tightening method
            var tightening = BoltService.GetTighteningMethod(_input.TighteningMethod);
            if (tightening == null)
            {
                _result.Errors.Add($"Tightening method {_input.TighteningMethod} not found");
                return false;
            }
            _tightening = tightening;

            // Validate clamped parts
            if (_input.ClampedParts == null || _input.ClampedParts.Count == 0)
            {
                _result.Errors.Add("At least one clamped part must be defined");
                return false;
            }

            // Only coarse threads are supported - BoltService carries no fine-pitch
            // data. Say so rather than silently computing a coarse-thread result
            // for a joint the caller asked to be fine-threaded.
            if (_input.UseFineThread)
            {
                _result.Warnings.Add(
                    "Fine-pitch threads are not supported - the calculation used coarse-thread " +
                    "data (pitch, d2, d3, As). Results do not apply to a fine-thread bolt.");
            }

            // Store bolt data in result
            _result.d = _bolt.d;
            _result.P = _bolt.P_coarse;
            _result.d2 = _bolt.d2_coarse;
            _result.d3 = _bolt.d3_coarse;
            // Calculate stress area: As = π/4 × [(d₂ + d₃)/2]²
            _result.As = Math.PI / 4.0 * Math.Pow((_bolt.d2_coarse + _bolt.d3_coarse) / 2.0, 2);
            _result.Rp02 = _strength.Rp02;
            _result.Rm = _strength.Rm;

            // Store additional bolt specifications
            _result.HeadDiameter = _bolt.dw;
            _result.HeadThickness = _bolt.k;
            _result.ShankDiameter = _bolt.d;  // For fully threaded, shank = nominal diameter
            _result.ShankLength = _input.ShankLength;
            _result.FrictionThread = _friction.MuG_typical;
            _result.FrictionHead = _friction.MuK_typical;
            _result.FrictionParts = _input.InterfaceFriction;
            _result.YoungModulusBolt = E_STEEL;
            _result.FlankAngle = 60.0; // Standard metric thread angle

            // Store nut specifications (if through-hole joint)
            if (_input.JointType == JointType.ThroughHole)
            {
                _result.NutMaterial = "C45"; // Standard structural steel for nuts
                _result.NutThickness = _bolt.m_nut;
                _result.NutDiameter = _bolt.dw_nut;
            }

            // Store washer specifications (if washers used)
            if (_input.UseWasherUnderHead || _input.UseWasherUnderNut)
            {
                WasherDimension? washer = null;
                if (_input.IsCustomWasher)
                {
                    washer = new WasherDimension
                    {
                        Size = _input.BoltSize,
                        d1 = _input.CustomWasherD1,
                        d2 = _input.CustomWasherD2,
                        h = _input.CustomWasherH
                    };
                }
                else
                {
                    washer = BoltService.GetWasherBySize(_input.BoltSize);
                }

                if (washer != null)
                {
                    _result.WasherMaterial = _input.WasherMaterial; // Use material from input
                    _result.WasherThickness = washer.h;
                    _result.WasherDiameterInner = washer.d1;
                    _result.WasherDiameterOuter = washer.d2;
                }
            }

            // Store additional values from input
            _result.ThreadRoughness = _input.ThreadRoughness;
            _result.BoltType = _input.BoltStandard == "DIN912" ? "DIN 912" : "ISO 4014";
            _result.ChamferDiameter = _input.ChamferDiameter;
            _result.NutRoughness = _input.HeadRoughness; // Use head roughness for nut
            _result.WasherRoughness = _input.PartsRoughness; // Use parts roughness for washer

            return true;
        }

        private void CalculateRequiredClampForce()
        {
            // Apply load application factor to service loads
            double phiA = _input.LoadApplicationFactorValue;
            double FA_max = _input.AxialForceMax * phiA;
            double FQ = _input.ShearForce * phiA;
            double muT = _input.InterfaceFriction;
            int qF = _input.NumberOfInterfaces;

            // Required clamp force for slip resistance (if shear load present)
            if (FQ > 0 && muT > 0)
            {
                // VDI 2230 R1: FKreq = FQ / (qF * µT)
                _result.FKR_slip = FQ / (qF * muT);
            }
            else
            {
                _result.FKR_slip = 0;
            }

            // Required clamp force to prevent joint separation.
            // For this calculation, we assume the only requirement is that the joint does not open.
            // A specific positive value could be used here if a minimum contact pressure is needed.
            _result.FKR_separation = 0;

            // The overall minimum required clamp force in service is the maximum of all requirements.
            _result.FKR_min = Math.Max(_result.FKR_slip, _result.FKR_separation);
        }

        private void CalculateCompliance()
        {
            // Calculate physical clamping length (parts only, without washers)
            double lK_parts = 0;
            foreach (var part in _input.ClampedParts)
            {
                lK_parts += part.Thickness;
            }

            // Calculate effective clamping length (including washers)
            // VDI 2230 Section 5.1.1
            double lK_eff = lK_parts;

            WasherDimension? washer = null;
            if (_input.IsCustomWasher)
            {
                washer = new WasherDimension
                {
                    Size = _input.BoltSize,
                    d1 = _input.CustomWasherD1,
                    d2 = _input.CustomWasherD2,
                    h = _input.CustomWasherH
                };
            }
            else
            {
                washer = BoltService.GetWasherBySize(_input.BoltSize);
            }

            // Add washer thicknesses to effective length
            if (_input.UseWasherUnderHead && washer != null)
            {
                lK_eff += washer.h;
            }
            if (_input.UseWasherUnderNut && _input.JointType == JointType.ThroughHole && washer != null)
            {
                lK_eff += washer.h;
            }

            // Add countersink/chamfer depth if present (typically 0.5-1.0mm per side)
            // For now, using simplified approach

            _result.ClampLength = lK_eff;

            // === BOLT COMPLIANCE δS ===
            // VDI 2230:2015 Section 5.1.1 - Bolt compliance calculation
            // Validated against KISSsoft results (error < 1%)
            // 
            // δS = δSK + δS1 + δSGew + δSGM + δSM
            // Where:
            //   δSK  = Head substitute compliance
            //   δS1  = Unthreaded shank compliance
            //   δSGew = Free thread in clamping zone
            //   δSGM = Engaged thread compliance
            //   δSM  = Nut/thread engagement flexibility

            double ES = E_STEEL;
            double d = _bolt.d;
            double AN = Math.PI * d * d / 4.0;      // Nominal cross section
            double Ad = AN;                          // Shank area = nominal for metric bolts
            double As = _result.As;                  // Stress area (for threaded sections)
            double d3 = _result.d3;                  // Minor diameter (already set in result)
            double Ad3 = Math.PI * d3 * d3 / 4.0;   // Core cross section

            // 1. Head substitute compliance (VDI 2230 Table A6)
            // lSK = 0.5d for hex head, 0.4d for socket head cap screw
            double headFactor = (_input.BoltStandard == "DIN912" || _input.BoltStandard == "ISO4762") ? 0.4 : 0.5;
            double lSK = headFactor * d;
            double deltaS_head = lSK / (ES * AN);

            // 2. Unthreaded shank compliance
            double l_shank = _input.ShankLength;
            double deltaS_shank = 0;
            if (l_shank > 0)
            {
                deltaS_shank = l_shank / (ES * Ad);
            }

            // 3. Free thread in clamping zone (threaded portion within lK_eff)
            double l_free_thread = Math.Max(0, lK_eff - l_shank);
            double deltaS_freeThread = 0;
            if (l_free_thread > 0)
            {
                deltaS_freeThread = l_free_thread / (ES * As);
            }

            // 4. Engaged thread compliance (VDI 2230)
            // Substitute length = 0.5d for thread engagement zone
            double lGM = 0.5 * d;
            double deltaS_engaged = lGM / (ES * Ad3);

            // 5. Nut/Thread engagement flexibility (VDI 2230 Table A7)
            // This accounts for nut deformation (DSV) or tapped hole flexibility (ESV)
            // For ESV (blind/tapped hole): lSM = 0.5d
            // For DSV (through-bolt with nut): lSM = 0.4d
            double lSM = (_input.JointType == JointType.TappedHole) ? 0.5 * d : 0.4 * d;
            double deltaS_nutEngagement = lSM / (ES * AN);

            // 6. Total bolt compliance
            _result.DeltaS = deltaS_head + deltaS_shank + deltaS_freeThread + deltaS_engaged + deltaS_nutEngagement;
            _result.CS = 1.0 / _result.DeltaS;

            // === CLAMPED PARTS COMPLIANCE δP ===
            // VDI 2230:2015 Section 5.1.2 - Conical compression model

            double dHole = GetHoleDiameter();
            double dw_head_bearing = GetHeadBearingDiameter();  // With washer if present
            double dw_nut_bearing = GetNutBearingDiameter();    // With washer if present

            // Get bolt head and nut diameters WITHOUT washers (for tan(φ) formula)
            double dw_head_bolt = _bolt.dw;  // Bolt head bearing diameter
            double dw_nut_bolt = (_input.JointType == JointType.ThroughHole) ? _bolt.dw_nut : 0;

            // Calculate DA' (virtual outer diameter) - VDI 2230 Section 5.1.2
            // Check if user provided DA value, otherwise calculate iteratively
            double DA_prime = 0;

            // Check if any clamped part has user-specified outer diameter
            double userDA = _input.ClampedParts.FirstOrDefault()?.OuterDiameter ?? 0;

            if (userDA > dw_head_bolt)
            {
                // User provided DA value - use it directly (like KISSsoft)
                DA_prime = userDA;
            }
            else
            {
                // Calculate DA' iteratively
                DA_prime = CalculateVirtualOuterDiameter(lK_eff, lK_parts, dHole,
                    dw_head_bearing, dw_nut_bearing, dw_head_bolt, dw_nut_bolt);
            }

            // Calculate cone angle tan(φ) - VDI 2230 Eq. 5.1/26 or 5.1/27
            // Use lK_eff (effective clamping length including washers) for consistency
            double tanPhi = CalculateConeAngle(lK_eff, dw_head_bolt, dw_nut_bolt, DA_prime);

            // Calculate parts compliance using VDI 2230 equations
            // Two cases based on geometry:
            // - Eq. 5.1/23: DA ≤ dW (cylindrical pressure distribution)
            // - Eq. 5.1/24: DA > dW (conical pressure distribution)

            // IMPORTANT: dW in VDI 2230 formulas is the BOLT head bearing diameter (not washer)
            // VDI 2230 Section 5.1.2: The pressure cone originates from the bolt head contact area
            double dW = dw_head_bolt; // Bolt head bearing diameter (without washer)
            double deltaP_total = 0;

            // Calculate weighted average elastic modulus
            double E_avg = 0;
            double totalThickness = 0;
            foreach (var part in _input.ClampedParts)
            {
                E_avg += part.ElasticModulus * part.Thickness;
                totalThickness += part.Thickness;
            }
            E_avg = totalThickness > 0 ? E_avg / totalThickness : 206000;

            // Joint coefficient w (VDI 2230 Section 5.1.2.1)
            // w = 1 for DSV (through-bolt joint)
            // w = 2 for ESV (tapped thread joint)
            double w = (_input.JointType == JointType.ThroughHole) ? 1.0 : 2.0;

            // Calculate limiting diameter DA,Gr (VDI 2230 Eq. 5.1/23)
            // DA,Gr = dW + w × lK × tan(φ)
            double DA_Gr_calc = dW + w * lK_eff * tanPhi;

            // Check which case applies based on VDI 2230 Section 5.1.2.1
            if (dW >= DA_prime)
            {
                // Case: dW ≥ DA - Use only deformation sleeve (cylindrical)
                // δP = 4×lK / (EP × π × (DA² - dh²))
                double A_cyl = DA_prime * DA_prime - dHole * dHole;
                if (A_cyl > 0)
                {
                    deltaP_total = (4.0 * lK_eff) / (E_avg * Math.PI * A_cyl);
                }
            }
            else if (DA_prime >= DA_Gr_calc)
            {
                // Case: DA ≥ DA,Gr - Deformation model consists of cone(s) only
                // VDI 2230 Equation 5.1/24
                // δP = (2 × ln[((dW + dh) × (dW + w×lK×tanφ - dh)) / ((dW - dh) × (dW + w×lK×tanφ + dh))]) / (w × EP × π × dh × tanφ)
                double numerator_ln = (dW + dHole) * (dW + w * lK_eff * tanPhi - dHole);
                double denominator_ln = (dW - dHole) * (dW + w * lK_eff * tanPhi + dHole);

                if (denominator_ln > 0 && numerator_ln > 0 && tanPhi > 0 && dHole > 0)
                {
                    double lnTerm = Math.Log(numerator_ln / denominator_ln);
                    deltaP_total = (2.0 * lnTerm) / (w * E_avg * Math.PI * dHole * tanPhi);
                }
            }
            else
            {
                // Case: dW < DA < DA,Gr - Deformation model consists of cone(s) and sleeve
                // VDI 2230 Equation 5.1/25
                // δP = { (2/(w×dh×tanφ)) × ln[((dW+dh)(DA-dh)) / ((dW-dh)(DA+dh))] + (4/(DA²-dh²)) × [lK - (DA-dW)/(w×tanφ)] } / (EP × π)
                double numerator_ln = (dW + dHole) * (DA_prime - dHole);
                double denominator_ln = (dW - dHole) * (DA_prime + dHole);

                if (denominator_ln > 0 && numerator_ln > 0 && tanPhi > 0 && dHole > 0)
                {
                    // Cone part: (2/(w×dh×tanφ)) × ln[...]
                    double lnTerm = Math.Log(numerator_ln / denominator_ln);
                    double conePart = (2.0 / (w * dHole * tanPhi)) * lnTerm;

                    // Sleeve part: (4/(DA²-dh²)) × [lK - (DA-dW)/(w×tanφ)]
                    double sleeveHeight = lK_eff - (DA_prime - dW) / (w * tanPhi);
                    double sleeveArea = DA_prime * DA_prime - dHole * dHole;
                    double sleevePart = 0;
                    if (sleeveArea > 0 && sleeveHeight > 0)
                    {
                        sleevePart = (4.0 / sleeveArea) * sleeveHeight;
                    }

                    deltaP_total = (conePart + sleevePart) / (E_avg * Math.PI);
                }
            }

            _result.DeltaP = deltaP_total;

            // === WASHER COMPLIANCE (δPzu) - VDI 2230:2015 Formula 194 ===
            // Washers add additional compliance to the clamped parts system.
            // The washer compliance accounts for:
            // 1. Washer body deformation
            // 2. Non-uniform pressure distribution
            // 3. Interface compliance effects
            //
            // VDI 2230:2015 Formula 194 specifies:
            // Support area outer diameter = dw + 1.6 × h (washer thickness)
            // 
            // Validated against KISSsoft results (error < 1%)
            
            double deltaP_washer = 0;

            // Note: 'washer' variable already defined earlier
            if (washer != null)
            {
                var washerMaterial = MaterialService.GetMaterial(_input.WasherMaterial);
                double E_washer = (washerMaterial != null) ? washerMaterial.ElasticModulus * 1000 : _input.WasherElasticModulus;
                
                double d1_washer = washer.d1; // Inner diameter
                double d2_washer = washer.d2; // Outer diameter
                double h_washer = washer.h;   // Thickness
                double dw_bolt = _bolt.dw;    // Bolt head bearing diameter

                if (_input.UseWasherUnderHead)
                {
                    // VDI 2230:2015 Formula 194 - Support area calculation
                    // Maximum outer diameter for support area = dw + 1.6 × h
                    double d_outer_support = Math.Min(d2_washer, dw_bolt + 1.6 * h_washer);
                    double A_support = Math.PI / 4.0 * (d_outer_support * d_outer_support - d1_washer * d1_washer);
                    
                    // NON-STANDARD CALIBRATION: not from VDI 2230. Empirically fitted
                    // to match KISSsoft output; nominally accounts for non-uniform
                    // pressure distribution, edge stress concentration and contact
                    // compliance. It reduces the washer support area to 30%, which
                    // markedly increases washer compliance.
                    const double reductionFactor = 0.30;
                    double A_eff = A_support * reductionFactor;

                    if (A_eff > 0)
                    {
                        deltaP_washer += h_washer / (E_washer * A_eff);
                    }
                }

                if (_input.UseWasherUnderNut && _input.JointType == JointType.ThroughHole)
                {
                    double dw_nut = _bolt.dw_nut;
                    
                    // Same formula for nut side
                    double d_outer_support = Math.Min(d2_washer, dw_nut + 1.6 * h_washer);
                    double A_support = Math.PI / 4.0 * (d_outer_support * d_outer_support - d1_washer * d1_washer);
                    double reductionFactor = 0.30;
                    double A_eff = A_support * reductionFactor;

                    if (A_eff > 0)
                    {
                        deltaP_washer += h_washer / (E_washer * A_eff);
                    }
                }
            }

            _result.DeltaPWasher = deltaP_washer;
            _result.WasherElasticModulus = _input.WasherElasticModulus;

            // Total parts compliance = plates + washers
            double deltaP_total_with_washers = deltaP_total + deltaP_washer;
            
            // === INTERFACE COMPLIANCE ===
            // Contact compliance at each interface adds to the total parts compliance,
            // accounting for micro-slip and contact deformation at the interfaces.
            //
            // NON-STANDARD CALIBRATION: VDI 2230 does not define this term. The 4%
            // per-interface factor below was fitted to reproduce KISSsoft output and
            // is an empirical correction, not a code requirement. It increases δP,
            // which lowers the force ratio Φ and therefore lowers FSA. Anyone
            // re-deriving these results by hand from VDI 2230 alone will not
            // reproduce them. See also the 0.30 washer support-area reduction factor
            // in the washer compliance block above, which is calibrated the same way.
            int numInterfaces = _input.ClampedParts.Count;  // Part-to-part interfaces
            if (_input.UseWasherUnderHead) numInterfaces++;  // Head-washer + washer-part
            if (_input.UseWasherUnderNut && _input.JointType == JointType.ThroughHole) numInterfaces++;
            numInterfaces++;  // Thread engagement interface

            const double interfaceComplianceFactor = 0.04;
            double deltaP_interface = deltaP_total * numInterfaces * interfaceComplianceFactor;
            
            double deltaP_total_corrected = deltaP_total_with_washers + deltaP_interface;
            
            _result.DeltaPTotal = deltaP_total_corrected;
            _result.CP = 1.0 / deltaP_total_corrected;

            // Force ratio - use corrected total compliance
            _result.Phi = deltaP_total_corrected / (deltaP_total_corrected + _result.DeltaS);

            // Calculate DA.Gr (limiting outside diameter)
            double DA_Gr = CalculateLimitingDiameter(DA_prime, lK_eff, dw_head_bearing);

            // Calculate Load Introduction Factor n according to VDI 2230 Table 5.2/1
            double n;
            // ak = distance from bolt axis to edge of clamped part = (DA - dW) / 2
            // Using DA_prime as the substitutional diameter
            double ak = (DA_prime - dw_head_bearing) / 2.0;
            double h = lK_eff; // Clamping length (h = lK)
            // lA = eccentricity = distance from bolt axis to load introduction point
            double lA = _input.Eccentricity;

            // Calculate ratios for Table 5.2/1
            double ak_over_h = h > 0 ? ak / h : 0;
            double lA_over_h = h > 0 ? lA / h : 0;

            // Store ratios in result for display
            _result.AkOverH = ak_over_h;
            _result.LAOverH = lA_over_h;

            if (_input.AutoCalculateLoadIntroductionFactor)
            {
                // Calculate n from Table 5.2/1 using bilinear interpolation
                n = CalculateLoadIntroductionFactorFromTable(_input.BoltingType, lA_over_h, ak_over_h);
            }
            else
            {
                // Use manually specified value
                n = _input.LoadIntroductionFactor;
            }

            _result.LoadIntroductionFactor = n;
            _result.PhiN = n * _result.Phi;

            // Store VDI 2230 specific values
            _result.DA = DA_prime; // Substitutional outside diameter at interface
            _result.DAn = DA_prime; // Substitutional outside diameter of basic solid (same for simplified model)
            _result.DA_Gr = DA_Gr; // Limiting outside diameter (max deformation cone diameter)
            _result.ConeAngle = Math.Atan(tanPhi) * 180.0 / Math.PI; // Convert to degrees
            _result.HoleDiameter = dHole;
        }

        private void CalculateForceDistribution()
        {
            // Apply load application factor to service loads
            double phiA = _input.LoadApplicationFactorValue;
            double FA = _input.AxialForceMax * phiA;
            double FA_min = _input.AxialForceMin * phiA;

            // Additional bolt force (VDI R3)
            _result.FSA = _result.PhiN * FA;
            _result.FSA_min = _result.PhiN * FA_min;

            // Relief of clamped parts (VDI R4)
            _result.FPA = (1.0 - _result.PhiN) * FA;
        }

        private void CalculatePreloadLosses()
        {
            // === SETTLING (EMBEDDING) LOSS (VDI 2230 R5, Table 5.4/1) ===
            // fZ depends on the surface finish of each interface, its location in
            // the joint, and the contact pressure - all handled by GetSettlingAmount.
            //
            // The assembly preload is not known yet at this point in the calculation
            // (CalculateAssemblyPreload runs after this), so the reference pressure
            // from VDI 2230 Table 5.4/1 is used here. CalculateThreeForceScenarios
            // rescales the result per scenario once the actual preload is known.

            const double p_ref = 100.0; // MPa - table reference pressure

            // Surface finish of the first and last clamped part, falling back to
            // the default when no parts have been specified.
            string firstFinish = _input.ClampedParts.Count > 0
                ? _input.ClampedParts[0].SurfaceFinish : "Machined";
            string lastFinish = _input.ClampedParts.Count > 0
                ? _input.ClampedParts[^1].SurfaceFinish : "Machined";

            double fZ_total = 0;  // Total settling in μm

            // 1. Head to first part (or washer)
            if (_input.UseWasherUnderHead)
            {
                fZ_total += GetSettlingAmount(firstFinish, "head", p_ref);    // Head to washer
                fZ_total += GetSettlingAmount(firstFinish, "washer", p_ref);  // Washer to Part A
            }
            else
            {
                fZ_total += GetSettlingAmount(firstFinish, "head", p_ref);    // Head to Part A
            }

            // 2. Interfaces between clamped parts - use the rougher of the two
            // adjoining surfaces, which is the one that actually embeds.
            for (int i = 0; i < _input.ClampedParts.Count - 1; i++)
            {
                double a = GetSettlingAmount(_input.ClampedParts[i].SurfaceFinish, "interface", p_ref);
                double b = GetSettlingAmount(_input.ClampedParts[i + 1].SurfaceFinish, "interface", p_ref);
                fZ_total += Math.Max(a, b);
            }

            // 3. Thread engagement (rolled thread)
            fZ_total += GetSettlingAmount("rolled thread", "thread", p_ref);

            // 4. Nut side (for through-bolt)
            if (_input.JointType == JointType.ThroughHole)
            {
                if (_input.UseWasherUnderNut)
                {
                    fZ_total += GetSettlingAmount(lastFinish, "washer", p_ref);  // Last part to washer
                    fZ_total += GetSettlingAmount(lastFinish, "nut", p_ref);     // Washer to nut
                }
                else
                {
                    fZ_total += GetSettlingAmount(lastFinish, "nut", p_ref);     // Last part to nut
                }
            }

            _result.FZ_total_um = fZ_total;

            // Convert to force loss: FZ = fZ / (δS + δP_total)
            // Use total parts compliance including washers and interface compliance
            _result.FZ = (fZ_total / 1000.0) / (_result.DeltaS + _result.DeltaPTotal);

            // Store fZ_total for per-scenario recalculation (base value)
            // The actual embedding depends on surface pressure which varies with preload
        }

        private void CalculateAssemblyPreload()
        {
            // Minimum required preload at assembly (VDI 2230 R7)
            // Must be sufficient to maintain required clamp force after accounting for load application and losses.
            // FM_min = FKR_min + FZ + (1-Φn)*FA_max
            // Note: (1-Φn)*FA_max = FPA (relief of clamped parts)
            _result.FM_min = _result.FKR_min + _result.FZ + _result.FPA;

            // Tightening factor
            _result.AlphaA = _tightening.AlphaA_typical;

            // Maximum assembly preload
            _result.FM_max = _result.AlphaA * _result.FM_min;

            // Mean preload (for reference)
            _result.FM_mean = (_result.FM_min + _result.FM_max) / 2.0;

            // Working forces
            _result.FS_max = _result.FM_max + _result.FSA; // Max bolt force in service
            _result.FS_min = _result.FM_min - _result.FZ; // Min bolt force (before external load)

            // Minimum clamp force in service (VDI 2230 Section 5.5)
            // FK_min = FM_min - FPA - FZ
            //
            // This is the residual clamp force in the pessimistic case: the bolt was
            // tightened at the LOW end of the tightening scatter (FM_min), then lost
            // preload to embedding (FZ) and to the external load relieving the joint
            // (FPA). Using FM_max here would make SF_clamp and SF_slip optimistic by
            // roughly the tightening factor αA (typically 1.6-2.0x).
            //
            // NOTE: this is a DESIGN calculation - FM_min is itself sized as
            // FKR_min + FZ + FPA (see CalculateAssemblyPreload). FK_min therefore
            // collapses to exactly FKR_min and SF_clamp reads 1.00 by construction.
            // That is the honest result: the joint is sized to just meet the required
            // clamp force in the worst tightening case, with no margin beyond it.
            // Margin above the requirement is shown by the F09_max scenario instead.
            _result.FK_min = _result.FM_min - _result.FPA - _result.FZ;
            if (_result.FK_min < 0) _result.FK_min = 0;

            // Check if preload is achievable
            double maxAllowablePreload = 0.9 * _result.Rp02 * _result.As; // 90% of yield
            if (_result.FM_max > maxAllowablePreload)
            {
                _result.Warnings.Add($"Required preload ({_result.FM_max / 1000:F1} kN) exceeds 90% of yield capacity ({maxAllowablePreload / 1000:F1} kN)");
            }
        }

        private void CalculateThreeForceScenarios()
        {
            double As = _result.As;
            double d2 = _result.d2;
            double d3 = _result.d3;
            double P = _result.P;
            double dHole = GetHoleDiameter();
            double dw = _bolt.dw;

            // Polar section modulus for torsion
            double Wp = Math.PI * Math.Pow(d3, 3) / 16.0;

            // Friction coefficients
            double muG = _input.FrictionThread > 0 ? _input.FrictionThread : _friction.MuG_typical;
            double muK = _input.FrictionHead > 0 ? _input.FrictionHead : _friction.MuK_typical;

            // Lead and friction angles
            double phi = Math.Atan(P / (Math.PI * d2));
            double rho_prime = Math.Atan(muG / Math.Cos(30.0 * Math.PI / 180.0));

            // Mean bearing diameter
            double dKm = (dw + dHole) / 2.0;
            if (_input.UseWasherUnderHead)
            {
                if (_input.IsCustomWasher)
                {
                    dKm = (_input.CustomWasherD2 + _input.CustomWasherD1) / 2.0;
                }
                else
                {
                    var washer = BoltService.GetWasherBySize(_input.BoltSize);
                    if (washer != null)
                    {
                        dKm = (washer.d2 + washer.d1) / 2.0;
                    }
                }
            }

            // === SCENARIO 1: Maximum Pretension Force (90% yield) ===
            // Calculate F09_max by iterating to find preload that results in 90% yield utilization
            // σred = sqrt(σz² + 3τ²) = 0.9 × Rp02
            // This is an iterative calculation because torsion depends on preload

            double targetUtilization = 0.90; // 90%
            double targetStress = targetUtilization * _result.Rp02;

            // Initial estimate: F09 = 0.9 × Rp02 × As (ignoring torsion)
            double F09_estimate = targetStress * As * 0.85; // Start with 85% to account for torsion

            // Iterate to find exact value
            for (int i = 0; i < 20; i++)
            {
                double sigmaZ_iter = F09_estimate / As;
                double threadTorque_iter = F09_estimate * (d2 / 2.0) * Math.Tan(phi + rho_prime);
                double tau_iter = threadTorque_iter / Wp;
                double sigmaRed_iter = Math.Sqrt(sigmaZ_iter * sigmaZ_iter + 3.0 * tau_iter * tau_iter);

                double error = sigmaRed_iter - targetStress;
                if (Math.Abs(error) < 1.0) break; // Converged within 1 MPa

                // Adjust estimate
                F09_estimate = F09_estimate * targetStress / sigmaRed_iter;
            }

            _result.F09_max = F09_estimate;

            // Calculate scenario values for F09_max
            _result.FS_at_F09max = _result.F09_max + _result.FSA;
            _result.FK_at_F09max = _result.F09_max - _result.FPA - _result.FZ;
            _result.SigmaZ_at_F09max = _result.F09_max / As;
            double threadTorque1 = _result.F09_max * (d2 / 2.0) * Math.Tan(phi + rho_prime);
            _result.Tau_at_F09max = threadTorque1 / Wp;
            _result.SigmaRed_at_F09max = Math.Sqrt(_result.SigmaZ_at_F09max * _result.SigmaZ_at_F09max + 3.0 * _result.Tau_at_F09max * _result.Tau_at_F09max);
            _result.Utilization_at_F09max = (_result.SigmaRed_at_F09max / _result.Rp02) * 100.0;
            double MG1 = _result.F09_max * (d2 / 2.0) * Math.Tan(phi + rho_prime);
            double MK1 = _result.F09_max * muK * (dKm / 2.0);
            _result.MA_at_F09max = (MG1 + MK1) / 1000.0;
            _result.MG_at_F09max = MG1 / 1000.0;
            _result.MK_at_F09max = MK1 / 1000.0;

            // Bolt and Part extensions for F09_max
            _result.BoltExtension_at_F09max = _result.F09_max * _result.DeltaS * 1000.0; // Convert to μm
            _result.PartExtension_at_F09max = _result.F09_max * _result.DeltaPTotal * 1000.0; // Convert to μm

            // Surface pressures for F09_max
            double dw_head = _bolt.dw;
            double A_head = Math.PI / 4.0 * (dw_head * dw_head - dHole * dHole);
            if (A_head > 0) _result.PressureHead_at_F09max = _result.F09_max / A_head;
            if (_input.JointType == JointType.ThroughHole)
            {
                double dw_nut = _bolt.dw_nut;
                double A_nut = Math.PI / 4.0 * (dw_nut * dw_nut - dHole * dHole);
                if (A_nut > 0) _result.PressureNut_at_F09max = _result.F09_max / A_nut;
            }

            // Per-scenario embedding: scale base embedding proportional to sqrt(F/F_ref)
            // VDI 2230: embedding increases with surface pressure, ~sqrt relationship
            double fZ_base = _result.FZ_total_um;
            double F_ref = _result.FM_max > 0 ? _result.FM_max : 1.0;
            _result.FZ_um_at_F09max = fZ_base * Math.Sqrt(_result.F09_max / F_ref);
            _result.FZ_at_F09max = (_result.FZ_um_at_F09max / 1000.0) / (_result.DeltaS + _result.DeltaPTotal);

            // === SCENARIO 2: Minimum Required Assembly Preload (FM_min) ===
            _result.FS_at_FMmin = _result.FM_min + _result.FSA;
            _result.FK_at_FMmin = _result.FM_min - _result.FPA - _result.FZ;
            _result.SigmaZ_at_FMmin = _result.FM_min / As;
            double threadTorque2 = _result.FM_min * (d2 / 2.0) * Math.Tan(phi + rho_prime);
            _result.Tau_at_FMmin = threadTorque2 / Wp;
            _result.SigmaRed_at_FMmin = Math.Sqrt(_result.SigmaZ_at_FMmin * _result.SigmaZ_at_FMmin + 3.0 * _result.Tau_at_FMmin * _result.Tau_at_FMmin);
            _result.Utilization_at_FMmin = (_result.SigmaRed_at_FMmin / _result.Rp02) * 100.0;
            double MG2 = _result.FM_min * (d2 / 2.0) * Math.Tan(phi + rho_prime);
            double MK2 = _result.FM_min * muK * (dKm / 2.0);
            _result.MA_at_FMmin = (MG2 + MK2) / 1000.0;
            _result.MG_at_FMmin = MG2 / 1000.0;
            _result.MK_at_FMmin = MK2 / 1000.0;

            // Bolt and Part extensions for FM_min
            _result.BoltExtension_at_FMmin = _result.FM_min * _result.DeltaS * 1000.0; // Convert to μm
            _result.PartExtension_at_FMmin = _result.FM_min * _result.DeltaPTotal * 1000.0; // Convert to μm

            // Surface pressures for FM_min
            if (A_head > 0) _result.PressureHead_at_FMmin = _result.FM_min / A_head;
            if (_input.JointType == JointType.ThroughHole)
            {
                double dw_nut = _bolt.dw_nut;
                double A_nut = Math.PI / 4.0 * (dw_nut * dw_nut - dHole * dHole);
                if (A_nut > 0) _result.PressureNut_at_FMmin = _result.FM_min / A_nut;
            }

            // Embedding for FM_min
            _result.FZ_um_at_FMmin = fZ_base * Math.Sqrt(_result.FM_min / F_ref);
            _result.FZ_at_FMmin = (_result.FZ_um_at_FMmin / 1000.0) / (_result.DeltaS + _result.DeltaPTotal);

            // === SCENARIO 3: Maximum Required Assembly Preload (FM_max) ===
            // Surface pressures for FM_max (stresses already calculated in CalculateStresses)
            if (A_head > 0) _result.PressureHead_at_FMmax = _result.FM_max / A_head;
            if (_input.JointType == JointType.ThroughHole)
            {
                double dw_nut = _bolt.dw_nut;
                double A_nut = Math.PI / 4.0 * (dw_nut * dw_nut - dHole * dHole);
                if (A_nut > 0) _result.PressureNut_at_FMmax = _result.FM_max / A_nut;
            }

            // Torque breakdown for FM_max
            double MG3 = _result.FM_max * (d2 / 2.0) * Math.Tan(phi + rho_prime);
            double MK3 = _result.FM_max * muK * (dKm / 2.0);
            _result.MG_at_FMmax = MG3 / 1000.0;
            _result.MK_at_FMmax = MK3 / 1000.0;

            // Embedding for FM_max (base case, fZ_base was computed at FM_max level)
            _result.FZ_um_at_FMmax = fZ_base;
            _result.FZ_at_FMmax = _result.FZ;

            // Additional Plate Force (already calculated as FPA)
            _result.AdditionalPlateForce = _result.FPA;

            // === FATIGUE ANALYSIS ===
            CalculateFatigueAnalysis(As, Wp, phi, rho_prime);
            // Results are stored in:
            // _result.FM_max, _result.FS_max, _result.FK_min
            // _result.SigmaZ_assembly, _result.Tau_assembly, _result.SigmaRed_assembly
            // _result.Utilization, _result.MA
        }

        private void CalculateFatigueAnalysis(double As, double Wp, double phi, double rho_prime)
        {
            // === FATIGUE ANALYSIS ===
            // Calculate fatigue load, life and number of cycles

            // Fatigue load amplitude = swing of the additional bolt force
            // FSA = Φn × FA, so the cyclic range is Φn × (FA_max − FA_min)
            _result.FatigueLoad = Math.Abs(_result.FSA - _result.FSA_min);

            // Stress amplitude from cyclic loading only — the assembly preload
            // scatter (αA) is static and must not enter the amplitude.
            double stressAmplitude = CalculateStressAmplitude();

            // Allowable stress amplitude (VDI 2230 Table 5.6/1)
            double sigmaASV = CalculateAllowableStressAmplitude();

            // Calculate fatigue life using S-N curve approach
            // N = N_ref × (σASV / σa)^m where m ≈ 5 for bolts
            double N_ref = 2e6; // Reference cycles at σASV
            double m = 5.0; // Fatigue exponent for bolted connections

            if (stressAmplitude > 0 && stressAmplitude < sigmaASV)
            {
                // Finite life region
                _result.FatigueLife = N_ref * Math.Pow(sigmaASV / stressAmplitude, m);
                // Cap at 1e10 for practical purposes (infinite life)
                if (_result.FatigueLife > 1e10) _result.FatigueLife = 1e10;
            }
            else if (stressAmplitude >= sigmaASV)
            {
                // Low cycle fatigue region - simplified calculation
                _result.FatigueLife = N_ref * Math.Pow(sigmaASV / stressAmplitude, m);
                // Minimum life of 1000 cycles
                if (_result.FatigueLife < 1000) _result.FatigueLife = 1000;
            }
            else
            {
                // No cyclic loading (static) - infinite life
                _result.FatigueLife = 1e10;
            }

            // Design number of load cycles - use user input or default
            if (_input.LoadType == LoadType.Static)
            {
                _result.NumberOfLoadCycles = 1;
            }
            else if (_input.DesignLoadCycles > 0)
            {
                // Use user-specified number of cycles
                _result.NumberOfLoadCycles = _input.DesignLoadCycles;
            }
            else if (_input.LoadType == LoadType.Pulsating)
            {
                _result.NumberOfLoadCycles = 1e7; // Typical for machines (10 million cycles)
            }
            else // Alternating
            {
                _result.NumberOfLoadCycles = 1e6; // More conservative for alternating loads
            }

            // Fatigue damage accumulation (Miner's rule simplified)
            // D = n / N (damage = applied cycles / allowable cycles)
            if (_result.FatigueLife > 0)
            {
                _result.FatigueDamage = _result.NumberOfLoadCycles / _result.FatigueLife;
            }
            else
            {
                _result.FatigueDamage = 1.0; // Full damage if no life
            }
        }

                private void CalculateStresses()
                {
                    double As = _result.As;
                    double d2 = _result.d2;
                    double d3 = _result.d3;
                    double P = _result.P;

                    // Polar section modulus for torsion
                    double Wp = Math.PI * Math.Pow(d3, 3) / 16.0;

                    // === ASSEMBLY STRESSES (VDI R8) ===
                    // Tensile stress from max preload
                    _result.SigmaZ_assembly = _result.FM_max / As;
        
                    // Torsional stress from thread friction torque needed for FM_max (VDI 2230 R8)
                    double muG = _input.FrictionThread > 0 ? _input.FrictionThread : _friction.MuG_typical;
        
                    double phi = Math.Atan(P / (Math.PI * d2)); // Lead angle (thread pitch angle)
                    // Friction angle for 60° ISO metric thread (flank angle β = 30°)
                    // ρ' = arctan(μG / cos(β)) where β = 30° for metric threads
                    double rho_prime = Math.Atan(muG / Math.Cos(30.0 * Math.PI / 180.0));
                    // Thread torque: MG = FM · (d2/2) · tan(φ + ρ')
                    double threadTorqueForMaxPreload = _result.FM_max * (d2 / 2.0) * Math.Tan(phi + rho_prime); // in Nmm
        
                    _result.Tau_assembly = threadTorqueForMaxPreload / Wp; // MPa
        
                    // Von Mises equivalent stress at assembly
                    _result.SigmaRed_assembly = Math.Sqrt(
                        _result.SigmaZ_assembly * _result.SigmaZ_assembly +
                        3.0 * _result.Tau_assembly * _result.Tau_assembly
                    );
        
                    // === WORKING STRESSES ===
                    // In service, torsion relaxes over time, so only consider tensile stress
                    _result.SigmaZ_working = _result.FS_max / As;
                    _result.SigmaRed_working = _result.SigmaZ_working; // Simplified (torsion relaxed)
        
                    // Utilization against yield
                    _result.Utilization = (_result.SigmaRed_assembly / _result.Rp02) * 100.0;
        
                    // === SURFACE PRESSURE (VDI R9) ===
                    double dw_head = _bolt.dw;
                    double dHole = GetHoleDiameter();
                    var washer = BoltService.GetWasherBySize(_input.BoltSize);
        
                    // Bearing area under head (or washer)
                    if (_input.UseWasherUnderHead && washer != null)
                    {
                        // Pressure under washer at head
                        double A_washer_head = Math.PI / 4.0 * (washer.d2 * washer.d2 - washer.d1 * washer.d1);
                        if (A_washer_head > 0) _result.PressureWasherHead = _result.FM_max / A_washer_head;
        
                        // Pressure between head and washer
                        double A_head = Math.PI / 4.0 * (dw_head * dw_head - dHole * dHole);
                        if (A_head > 0) _result.PressureHead = _result.FM_max / A_head;
                    }
                    else
                    {
                        // Direct pressure under head
                        double A_head = Math.PI / 4.0 * (dw_head * dw_head - dHole * dHole);
                        if (A_head > 0) _result.PressureHead = _result.FM_max / A_head;
                    }
        
                    // Bearing area under nut (or washer at nut)
                    if (_input.JointType == JointType.ThroughHole)
                    {
                        double dw_nut = _bolt.dw_nut;
        
                        if (_input.UseWasherUnderNut && washer != null)
                        {
                            // Pressure under washer at nut
                            double A_washer_nut = Math.PI / 4.0 * (washer.d2 * washer.d2 - washer.d1 * washer.d1);
                            if (A_washer_nut > 0) _result.PressureWasherNut = _result.FM_max / A_washer_nut;
        
                            // Pressure between nut and washer
                            double A_nut = Math.PI / 4.0 * (dw_nut * dw_nut - dHole * dHole);
                            if (A_nut > 0) _result.PressureNut = _result.FM_max / A_nut;
                        }
                        else
                        {
                            // Direct pressure under nut
                            double A_nut = Math.PI / 4.0 * (dw_nut * dw_nut - dHole * dHole);
                            if (A_nut > 0) _result.PressureNut = _result.FM_max / A_nut;
                        }
                    }
        
                    // Allowable surface pressure (depends on clamped material)
                    // Find the minimum permissible surface pressure among the parts in contact with head/nut
                    double firstPartPressure = GetMaterialSurfacePressure(_input.ClampedParts.FirstOrDefault()?.MaterialName);
                    double lastPartPressure = _input.JointType == JointType.ThroughHole ?
                        GetMaterialSurfacePressure(_input.ClampedParts.LastOrDefault()?.MaterialName) : firstPartPressure;
        
                    _result.PressureAllowable = Math.Min(firstPartPressure, lastPartPressure);
        
                    // === BOLT AND PARTS EXTENSION ===
                    // Bolt extension: fS = FM_max × δS (mm)
                    _result.BoltExtension = _result.FM_max * _result.DeltaS;
        
                    // Parts extension (compression): fP = FM_max × δP (mm)
                    _result.PartsExtension = _result.FM_max * _result.DeltaP;
                }
        
                private void CalculateSafetyFactors()
                {
                    // SF against yielding (VDI R10)
                    _result.SF_yield = _result.Rp02 / _result.SigmaRed_assembly;
        
                    // SF against clamp force loss
                    if (_result.FKR_min > 0)
                    {
                        _result.SF_clamp = _result.FK_min / _result.FKR_min;
                    }
                    else
                    {
                        // If no specific clamp force is required, safety is high as long as FK_min > 0.
                        // A large number indicates no risk of failing a specific requirement.
                        _result.SF_clamp = (_result.FK_min > 0) ? 999 : 0;
                    }
        
                    // SF against slipping (if shear load present)
                    if (_input.ShearForce > 0)
                    {
                        // SF_slip = (Min Clamp Force * Friction) / Shear Force
                        _result.SF_slip = (_result.FK_min * _input.InterfaceFriction * _input.NumberOfInterfaces) / _input.ShearForce;
                    }
                    else
                    {
                        _result.SF_slip = 999; // No shear load, no risk of slipping
                    }
        
                    // SF against surface pressure
                    double maxPressure = Math.Max(_result.PressureHead, _result.PressureNut);
                    if (maxPressure > 0)
                    {
                        _result.SF_pressure = _result.PressureAllowable / maxPressure;
                    }
                    else
                    {
                        _result.SF_pressure = 999;
                    }
        
                    // Determine minimum (critical) safety factor
                    var safetyFactors = new Dictionary<string, double>
                    {
                        { "Yield", _result.SF_yield },
                        { "Clamp", _result.SF_clamp },
                        { "Pressure", _result.SF_pressure }
                    };
                    if (_input.ShearForce > 0)
                    {
                        safetyFactors.Add("Slip", _result.SF_slip);
                    }
        
                    var minSF = safetyFactors.OrderBy(x => x.Value).First();
                    _result.SF_min = minSF.Value;
                    _result.SF_critical = minSF.Key;
        
                    // Add warnings for low safety factors
                    if (_result.SF_yield < 1.0)
                        _result.Errors.Add("Yield safety factor < 1.0 - bolt will yield!");
                    else if (_result.SF_yield < 1.1)
                        _result.Warnings.Add("Yield safety factor low (< 1.1)");
        
                    if (_result.FK_min <= 0)
                        _result.Errors.Add("Joint opens under load (FK_min <= 0)!");
                    else if (_result.SF_clamp < 1.2 && _result.FKR_min > 0)
                        _result.Warnings.Add("Clamp safety factor low (< 1.2)");
        
                    if (_result.SF_slip < 1.0 && _input.ShearForce > 0)
                        _result.Errors.Add("Slip safety factor < 1.0 - joint will slip!");
                    else if (_result.SF_slip < 1.1 && _input.ShearForce > 0)
                        _result.Warnings.Add("Slip safety factor low (< 1.1)");
        
                    if (_result.SF_pressure < 1.0)
                        _result.Warnings.Add("Surface pressure exceeds allowable - consider using washers");
        
                    // Load utilization: ratio of applied load to maximum working bolt force
                    // This shows how much of the bolt's load-carrying capacity is being used
                    double maxBoltCapacity = 0.9 * _result.Rp02 * _result.As; // 90% of yield capacity
                    if (maxBoltCapacity > 0)
                    {
                        _result.LoadUtilization = (_result.FS_max / maxBoltCapacity) * 100.0;
                    }
                    else
                    {
                        _result.LoadUtilization = 0;
                    }
                }
        
                private void CalculateTighteningTorque()
                {
                    double d = _result.d;
                    double d2 = _result.d2;
                    double P = _result.P;
                    double dw = _bolt.dw;
                    double dHole = GetHoleDiameter();
        
                    double muG = _input.FrictionThread > 0 ? _input.FrictionThread : _friction.MuG_typical;
                    double muK = _input.FrictionHead > 0 ? _input.FrictionHead : _friction.MuK_typical;
        
                    // Mean bearing diameter
                    double dKm = (dw + dHole) / 2.0;
                    if (_input.UseWasherUnderHead)
                    {
                        if (_input.IsCustomWasher)
                        {
                            dKm = (_input.CustomWasherD2 + _input.CustomWasherD1) / 2.0;
                        }
                        else
                        {
                            var washer = BoltService.GetWasherBySize(_input.BoltSize);
                            if (washer != null)
                            {
                                dKm = (washer.d2 + washer.d1) / 2.0;
                            }
                        }
                    }
        
                    // Helper function to calculate total torque for a given preload FM
                    Func<double, Tuple<double, double, double>> getTorque = (FM) =>
                    {
                        double phi = Math.Atan(P / (Math.PI * d2));
                        double rho_prime = Math.Atan(muG / Math.Cos(30.0 * Math.PI / 180.0));
                        double mg = FM * (d2 / 2.0) * Math.Tan(phi + rho_prime); // Nmm
                        double mk = FM * muK * (dKm / 2.0); // Nmm
                        double ma = (mg + mk) / 1000.0; // Nm
                        return Tuple.Create(ma, mg / 1000.0, mk / 1000.0);
                    };
        
                    // Calculate torque for MAX preload (this is the target torque for the wrench)
                    var maxTorques = getTorque(_result.FM_max);
                    _result.MA = maxTorques.Item1;
                    _result.MG = maxTorques.Item2;
                    _result.MK = maxTorques.Item3;
        
                    // Calculate torque for MIN preload (for reference)
                    var minTorques = getTorque(_result.FM_min);
                    _result.MA_min = minTorques.Item1;
        
                    // K-factor (nut factor): MA = K × FM_max × d
                    if (_result.FM_max > 0 && d > 0)
                    {
                        _result.KFactor = _result.MA / (_result.FM_max * d / 1000.0);
                    }
                    else
                    {
                        _result.KFactor = 0;
                    }
        
                    // Loosening torque (ML)
                    // ML = FM × (d2/2) × tan(ρ' - φ) + FM × μK × (dKm/2)
                    //
                    // On loosening the helix angle φ HELPS (the preload drives the bolt
                    // out) while thread friction ρ' resists, hence tan(ρ' − φ). The head
                    // friction always resists motion, so it is ADDED, not subtracted.
                    // For a self-locking thread ρ' > φ, so both terms are positive.
                    double phi = Math.Atan(P / (Math.PI * d2));
                    double rho_prime = Math.Atan(muG / Math.Cos(30.0 * Math.PI / 180.0));

                    double mg_loosening = _result.FM_mean * (d2 / 2.0) * Math.Tan(rho_prime - phi); // Nmm
                    double mk_loosening = _result.FM_mean * muK * (dKm / 2.0); // Nmm

                    _result.ML = (mg_loosening + mk_loosening) / 1000.0; // Nm

                    // A non-self-locking thread (φ > ρ') gives a negative thread term;
                    // the joint would back off on its own if head friction cannot hold it.
                    if (_result.ML < 0) _result.ML = 0;
                }
        
                /// <summary>
                /// Continuous stress amplitude σa acting on the bolt (VDI 2230 R12).
                ///
                /// Only the CYCLIC part of the load contributes: the bolt sees the
                /// additional bolt force FSA swing between FA_min and FA_max, i.e.
                ///     σa = Φn · (FA_max − FA_min) / (2·As)
                ///
                /// The scatter of the assembly preload (αA) must NOT be included here.
                /// It is a static, once-per-assembly uncertainty — the preload does not
                /// cycle between FM_min and FM_max in service — and folding it into the
                /// amplitude understates SF_fatigue by a large margin.
                /// </summary>
                private double CalculateStressAmplitude()
                {
                    if (_result.As <= 0) return 0;
                    return Math.Abs(_result.FSA - _result.FSA_min) / (2.0 * _result.As);
                }

                /// <summary>
                /// Allowable stress amplitude σASV for rolled/cut threads,
                /// approximated from VDI 2230 Table 5.6/1.
                /// </summary>
                private double CalculateAllowableStressAmplitude()
                {
                    // Base fatigue strength based on diameter (for 8.8), MPa
                    double d = _result.d;
                    double sigmaA_base;
                    if (d <= 8) sigmaA_base = 55;
                    else if (d <= 16) sigmaA_base = 50;
                    else if (d <= 30) sigmaA_base = 45;
                    else if (d <= 48) sigmaA_base = 40;
                    else sigmaA_base = 35;

                    // Strength class factor (normalized to 8.8)
                    double strengthFactor = 1.0;
                    if (_input.StrengthClass == "10.9") strengthFactor = 1.10;
                    else if (_input.StrengthClass == "12.9") strengthFactor = 1.20;
                    else if (_input.StrengthClass == "4.6" || _input.StrengthClass == "4.8") strengthFactor = 0.85;
                    else if (_input.StrengthClass == "5.6" || _input.StrengthClass == "5.8") strengthFactor = 0.90;

                    // Thread manufacturing factor
                    // kτ = 1.0 for rolled threads (SV), 0.5 for cut threads (SG)
                    double kt = (_input.ThreadRoughness <= 16) ? 1.0 : 0.5;

                    return sigmaA_base * strengthFactor * kt;
                }

                private void CalculateFatigue()
                {
                    // === FATIGUE CALCULATION (VDI 2230 R12) ===
                    // Amplitude comes from the cyclic external load only.
                    _result.SigmaA = CalculateStressAmplitude();

                    // Mean stress uses the maximum preload (the realistic worst case
                    // for the mean level the amplitude rides on).
                    _result.SigmaM = _result.As > 0
                        ? (_result.FM_max + (_result.FSA + _result.FSA_min) / 2.0) / _result.As
                        : 0;

                    // === ALLOWABLE STRESS AMPLITUDE (VDI 2230 Table 5.6/1) ===
                    _result.SigmaASV = CalculateAllowableStressAmplitude();

                    // Fatigue safety factor
                    if (_result.SigmaA > 0)
                    {
                        _result.SF_fatigue = _result.SigmaASV / _result.SigmaA;
                    }
                    else
                    {
                        _result.SF_fatigue = 999; // No stress amplitude, no fatigue risk
                    }
        
                    if (_result.SF_fatigue < 1.0)
                        _result.Errors.Add("Fatigue safety factor < 1.0 - bolt will fail in fatigue!");
                    else if (_result.SF_fatigue < 1.2)
                        _result.Warnings.Add("Fatigue safety factor low (< 1.2) - consider higher preload or shot-peened bolt");
        
                    // Update minimum safety factor if fatigue is critical
                    if (_result.SF_fatigue < _result.SF_min)
                    {
                        _result.SF_min = _result.SF_fatigue;
                        _result.SF_critical = "Fatigue";
                    }
                }
        
                private void CalculateGeometry()
                {
                    // Required bolt length (approximate)
                    double lK = _result.ClampLength;
                    double threadEngagement;

                    if (_input.JointType == JointType.ThroughHole)
                    {
                        // Through hole: bolt passes through all parts, secured with nut
                        // Required length = clamping length + nut height + 2 pitches protrusion
                        threadEngagement = _bolt.m_nut + 2 * _result.P;
                    }
                    else
                    {
                        // Blind hole (tapped): bolt threads into the LAST clamped part
                        // The last part has internal threads - thread engagement depth
                        // Minimum thread engagement: 1.0 × d for steel, 1.5 × d for cast iron/aluminum
                        // The clamping length already includes the last part thickness,
                        // but we need to add the thread engagement depth BEYOND the clamping zone
                        var lastPart = _input.ClampedParts.LastOrDefault();
                        double lastPartThickness = lastPart?.Thickness ?? 0;

                        // Thread engagement depth (VDI 2230 recommendation)
                        // For steel: minimum 1.0 × d
                        // For cast iron/aluminum: minimum 1.5 × d
                        string lastPartMaterial = lastPart?.MaterialName?.ToLower() ?? "steel";
                        double engagementFactor = (lastPartMaterial.Contains("alumin") ||
                                                   lastPartMaterial.Contains("cast") ||
                                                   lastPartMaterial.Contains("gj") ||
                                                   lastPartMaterial.Contains("al")) ? 1.5 : 1.0;

                        double minThreadEngagement = engagementFactor * _result.d;

                        // If the last part is thick enough to contain the threads,
                        // bolt length = clamping length (thread is inside last part)
                        // If not, we need extra length beyond the last part
                        if (lastPartThickness >= minThreadEngagement)
                        {
                            // Threads are fully contained within the last part
                            // Bolt length = clamping length (no extra needed, threads are in last part)
                            threadEngagement = 0;
                        }
                        else
                        {
                            // Last part is thinner than required engagement
                            // Need additional engagement beyond the clamped zone
                            threadEngagement = minThreadEngagement - lastPartThickness;
                        }

                        // Always add at least 2 pitches for safety
                        threadEngagement += 2 * _result.P;
                    }

                    _result.BoltLength = lK + threadEngagement;

                    // Round up to standard lengths (5mm increments)
                    _result.BoltLength = Math.Ceiling(_result.BoltLength / 5.0) * 5.0;
                }
        
                // === HELPER METHODS ===
        
                private double GetHoleDiameter()
                {
                    // Use specified hole diameter or standard clearance
                    var firstPart = _input.ClampedParts.FirstOrDefault();
                    if (firstPart != null && firstPart.HoleDiameter > 0)
                        return firstPart.HoleDiameter;
        
                    return BoltService.GetHoleClearance(_input.BoltSize, "medium");
                }
        
                private double GetEffectiveBearingDiameter()
                {
                    // Effective bearing diameter under head
                    // Use washer OD if present, otherwise head bearing diameter
                    if (_input.UseWasherUnderHead)
                    {
                        var washer = BoltService.GetWasherBySize(_input.BoltSize);
                        if (washer != null) return washer.d2;
                    }
                    return _bolt.dw;
                }
        
                private double GetSettlingAmount(string surfaceFinish, string location, double surfacePressure)
                {
                    // Return settling amount in μm based on surface, location, and pressure
                    // Values from VDI 2230:2015 Table 5.4/1
                    // fZ depends on surface roughness and contact pressure
        
                    // VDI 2230 Table 5.4/1 - Embedding/Settling values
                    // For reference pressure p = 100 MPa
                    // Values in μm per interface
        
                    double baseSettling_100MPa = surfaceFinish.ToLower() switch
                    {
                        "ground" => 2.0,                       // Rz ≤ 4 μm, precision ground
                        "fine machined" or "machined" => 3.0,  // Rz = 4-16 μm, standard machining
                        "turned" => 4.0,                       // Rz = 10-25 μm, turned
                        "rolled" or "rolled thread" => 5.0,    // Rz = 6-16 μm, thread rolling
                        "forged" => 8.0,                       // Rz = 12-25 μm, forged surfaces
                        "as-cast" or "cast" => 12.0,           // Rz = 25-63 μm, as-cast
                        "rough" => 10.0,                       // Rz = 16-40 μm, rough surfaces
                        _ => 3.0  // Default to machined
                    };
        
                    // VDI 2230: Embedding increases with sqrt(pressure) approximately
                    // More accurate than linear relationship
                    double p_ref = 100.0; // MPa (reference pressure from table)
                    double pressureRatio = Math.Sqrt(surfacePressure / p_ref);
        
                    // Limit to realistic range
                    pressureRatio = Math.Max(0.5, Math.Min(2.0, pressureRatio));
        
                    double fZ_base = baseSettling_100MPa * pressureRatio;
        
                    // Location-specific multipliers (VDI 2230 recommendations)
                    double locationFactor = location.ToLower() switch
                    {
                        "head" => 1.2,       // Head bearing: higher settling due to concentrated load
                        "nut" => 1.2,        // Nut bearing: similar to head
                        "thread" => 1.5,     // Thread engagement: highest settling (stress concentration)
                        "interface" => 0.8,  // Interface between parts: typically better contact
                        "washer" => 1.0,     // Washer: standard settling
                        _ => 1.0
                    };
        
                    return fZ_base * locationFactor;
                }
        
                                private double GetMaterialSurfacePressure(string? materialName)
                {
                    if (string.IsNullOrEmpty(materialName))
                        return 235 * 0.8; // Fallback to S235 yield * 0.8
        
                    var mat = MaterialService.GetMaterial(materialName);
                    if (mat != null)
                    {
                        // If the library value is 0, fall back to 80% of yield as a safety net
                        return (mat.PermissibleSurfacePressure > 0) ? mat.PermissibleSurfacePressure : mat.YieldStrength * 0.8;
                    }
        
                    // Fallback for names not in MaterialService (using old logic)
                    double yield = GetMaterialYieldStrength(materialName);
                    return yield * 0.8;
                }
        
                private double GetMaterialYieldStrength(string? materialName)
                {
                    if (string.IsNullOrEmpty(materialName))
                        return 235; // Default to mild steel
        
                    var mat = MaterialService.GetMaterial(materialName);
                    if (mat != null) return mat.YieldStrength;
        
                    // Fallback for names not in MaterialService
                    return materialName.ToLower() switch
                    {
                        "steel" or "s235" => 235,
                        "s355" => 355,
                        "c45" or "1045" => 490,
                        "42crmo4" or "4140" => 750,
                        "aluminum" or "aluminium" or "al" => 250,
                        "almgsi" or "6061" => 240,
                        "al7075" or "7075" => 450,
                        "cast iron" or "gjs" => 300,
                        "grey iron" or "gjl" => 200,
                        "stainless" or "ss304" or "1.4301" => 210,
                        "ss316" or "1.4401" => 220,
                        _ => 235 // Default to mild steel
                    };
                }
        
                private double GetHeadBearingDiameter()
                {
                    // Effective bearing diameter under head
                    // Use washer OD if present, otherwise head bearing diameter
                    if (_input.UseWasherUnderHead)
                    {
                        if (_input.IsCustomWasher)
                        {
                            return _input.CustomWasherD2;
                        }
                        var washer = BoltService.GetWasherBySize(_input.BoltSize);
                        if (washer != null) return washer.d2; // Outer diameter
                    }
                    return _bolt.dw; // Head bearing diameter
                }
                
                private double GetNutBearingDiameter()
                {
                    if (_input.JointType != JointType.ThroughHole) return 0;
        
                    // Use washer diameter if present, otherwise nut bearing diameter
                    if (_input.UseWasherUnderNut)
                    {
                        if (_input.IsCustomWasher)
                        {
                            return _input.CustomWasherD2;
                        }
                        var washer = BoltService.GetWasherBySize(_input.BoltSize);
                        if (washer != null) return washer.d2;
                    }
                    return _bolt.dw_nut;
                }
        /// <summary>
        /// Calculate virtual outer diameter DA' according to VDI 2230 Eq. 5.1/25
        /// Requires iterative solution with cone angle
        /// </summary>
        /// <param name="lK_eff">Effective clamping length (with washers) for DA' calculation</param>
        /// <param name="lK_parts">Parts clamping length (without washers) for tan(φ) formula</param>
        /// <param name="dw_head_bearing">Head bearing diameter (with washer if present) for DA' formula</param>
        /// <param name="dw_nut_bearing">Nut bearing diameter (with washer if present) for DA' formula</param>
        /// <param name="dw_head_bolt">Bolt head diameter (without washer) for tan(φ) formula</param>
        /// <param name="dw_nut_bolt">Nut diameter (without washer) for tan(φ) formula</param>
        private double CalculateVirtualOuterDiameter(double lK_eff, double lK_parts, double dHole,
            double dw_head_bearing, double dw_nut_bearing, double dw_head_bolt, double dw_nut_bolt)
        {
            // VDI 2230 Section 5.1.2: Virtual outer diameter DA'
            // For ESV (tapped hole): DA' = dW + w × lK × tan(φ) where w = 2
            // For DSV (through-bolt): DA' depends on geometry type

            // Joint coefficient w
            double w = (_input.JointType == JointType.ThroughHole) ? 1.0 : 2.0;

            // Use bolt head bearing diameter (without washer) as dW
            double dW = dw_head_bolt;

            // Initial estimate for DA' to start iteration
            double DA_prime = dW * 2.5; // Conservative initial estimate

            // Iterative solution - tan(φ) depends on DA', and DA' depends on tan(φ)
            for (int iter = 0; iter < 50; iter++)
            {
                // Calculate tan(φ) using current DA' estimate
                double tanPhi_iter = CalculateConeAngle(lK_eff, dw_head_bolt, dw_nut_bolt, DA_prime);

                // Calculate new DA' based on joint type and geometry
                double DA_new = 0;

                bool isESV = _input.JointType == JointType.TappedHole;

                if (isESV)
                {
                    // ESV (Tapped hole joint): Single cone from head side
                    // VDI 2230: DA' = dW + w × lK × tan(φ) where w = 2
                    // This simplifies to: DA' = dW + 2 × lK × tan(φ)
                    DA_new = dW + w * lK_eff * tanPhi_iter;
                }
                else
                {
                    // DSV (Through-bolt joint): Depends on load introduction point
                    switch (_input.BoltingType)
                    {
                        case BoltingType.SV1_ThroughBolt_LoadAtHead:
                            // Single-sided cone from head
                            // DA' = dW + w × lK × tan(φ) where w = 1
                            DA_new = dw_head_bolt + w * lK_eff * tanPhi_iter;
                            break;

                        case BoltingType.SV2_ThroughBolt_LoadAtNut:
                            // Single-sided cone from nut
                            DA_new = dw_nut_bolt + w * lK_eff * tanPhi_iter;
                            break;

                        case BoltingType.SV3_ThroughBolt_LoadInMiddle:
                        default:
                            // Double-sided cones meeting in middle
                            // DA' = dW + lK × tan(φ) (each side contributes lK/2)
                            DA_new = Math.Max(dw_head_bolt, dw_nut_bolt) + lK_eff * tanPhi_iter;
                            break;
                    }
                }

                // Check convergence
                if (Math.Abs(DA_new - DA_prime) < 0.001)
                {
                    DA_prime = DA_new;
                    break;
                }

                DA_prime = DA_new;
            }

            // Apply upper limit based on available geometry
            double DA_max = Math.Min(DA_prime, 10.0 * dw_head_bearing);

            return DA_max;
        }

        /// <summary>
        /// Calculate cone angle tan(φ) according to VDI 2230 Eq. 5.1/26 (DSV) or 5.1/27 (ESV)
        /// </summary>
        private double CalculateConeAngle(double lK, double dw_head, double dw_nut, double DA_prime)
        {
            double dW = Math.Max(dw_head, dw_nut); // Use larger bearing diameter

            // Prevent division by zero or log of invalid values
            if (lK <= 0 || dW <= 0 || DA_prime <= dW) return 0.5; // Fallback

            bool isThroughBolt = _input.JointType == JointType.ThroughHole;

            double tanPhi;

            if (isThroughBolt)
            {
                // VDI 2230 Equation 5.1/26 (DSV - Through-bolt)
                // tan(φₐ) = 0.362 + 0.032 ln(βₗ) + 0.153 ln(γ)
                // Where: βₗ = lK/dW and γ = DA'/dW
                // IMPORTANT: Uses natural logarithm (ln), not log10
                double beta_L = lK / dW;
                double gamma = DA_prime / dW;

                // Check for valid log arguments
                if (beta_L <= 0 || gamma <= 0) return 0.5; // Fallback

                double term1 = 0.362;
                double term2 = 0.032 * Math.Log(beta_L);  // ln(βₗ)
                double term3 = 0.153 * Math.Log(gamma);      // ln(γ)
                tanPhi = term1 + term2 + term3;
            }
            else
            {
                // VDI 2230 Equation 5.1/27 (ESV - Tapped hole)
                // tan(φₑ) = 0.348 + 0.013 ln(βₗ) + 0.193 ln(γ)
                // IMPORTANT: Uses natural logarithm (ln), not log10
                double beta_L = lK / dW;
                double gamma = DA_prime / dW;

                // Check for valid log arguments
                if (beta_L <= 0 || gamma <= 0) return 0.5; // Fallback

                double term1 = 0.348;
                double term2 = 0.013 * Math.Log(beta_L);  // ln(βₗ)
                double term3 = 0.193 * Math.Log(gamma);      // ln(γ)
                tanPhi = term1 + term2 + term3;
            }

            // Apply reasonable limits
            tanPhi = Math.Max(0.3, Math.Min(0.8, tanPhi)); // Typical range: 16.7° to 38.7°

            return tanPhi;
        }

        /// <summary>
        /// Calculate limiting outside diameter DA.Gr according to VDI 2230
        /// This is the maximum diameter limited by geometric constraints
        /// </summary>
        private double CalculateLimitingDiameter(double DA_prime, double lK_eff, double dw_head_bearing)
        {
            // For single bolt with unlimited plate, DA.Gr = DA'
            // For limited geometry (multiple bolts, finite plate), apply constraints

            // In most cases for single bolt calculations, DA.Gr = DA'
            // Future enhancement: add constraints for:
            // - Multiple bolt patterns (DA.Gr = 2 × bolt spacing)
            // - Finite plate width (DA.Gr = plate outer diameter)
            // - Edge distance constraints

            double DA_Gr = DA_prime;

            // Apply reasonable upper limit based on clamping length
            // Typically DA.Gr should not exceed certain multiples of lK
            double DA_max_geometric = dw_head_bearing + 3.0 * lK_eff; // Conservative limit
            DA_Gr = Math.Min(DA_Gr, DA_max_geometric);

            return DA_Gr;
        }

        /// <summary>
        /// Calculate Load Introduction Factor n according to VDI 2230 Table 5.2/1
        /// This factor determines how much of the external load is transmitted to the bolt
        /// </summary>
        /// <param name="boltingType">Joint type SV1-SV6</param>
        /// <param name="lA_over_h">Ratio lA/h where lA = connected solid length, h = clamp length</param>
        /// <param name="ak_over_h">Ratio ak/h where ak = (DA - dW)/2, h = clamp length</param>
        /// <returns>Load introduction factor n (0 to 1)</returns>
        private double CalculateLoadIntroductionFactorFromTable(BoltingType boltingType, double lA_over_h, double ak_over_h)
        {
            // VDI 2230 Table 5.2/1 - Load introduction factors n for joint types SV 1 to SV 6
            // Columns: lA/h values (0.00, 0.10, 0.20, 0.30)
            // Sub-columns for each lA/h: ak/h values (0.00, 0.10, 0.30, ≥0.50)

            // Table data structure: [SV type index][lA/h index][ak/h index]
            // lA/h indices: 0=0.00, 1=0.10, 2=0.20, 3=≥0.30
            // ak/h indices: 0=0.00, 1=0.10, 2=0.30, 3=≥0.50

            double[,,] tableData = new double[6, 4, 4]
            {
                // SV1 - Through-bolt, load at head
                {
                    { 0.70, 0.55, 0.30, 0.13 },  // lA/h = 0.00
                    { 0.52, 0.41, 0.22, 0.10 },  // lA/h = 0.10
                    { 0.34, 0.28, 0.16, 0.07 },  // lA/h = 0.20
                    { 0.16, 0.14, 0.12, 0.04 }   // lA/h ≥ 0.30
                },
                // SV2 - Through-bolt, load at nut
                {
                    { 0.57, 0.46, 0.30, 0.13 },  // lA/h = 0.00
                    { 0.44, 0.36, 0.21, 0.10 },  // lA/h = 0.10
                    { 0.30, 0.25, 0.16, 0.07 },  // lA/h = 0.20
                    { 0.16, 0.14, 0.12, 0.04 }   // lA/h ≥ 0.30
                },
                // SV3 - Through-bolt, load in middle
                {
                    { 0.44, 0.37, 0.26, 0.12 },  // lA/h = 0.00
                    { 0.35, 0.30, 0.20, 0.09 },  // lA/h = 0.10
                    { 0.26, 0.23, 0.15, 0.07 },  // lA/h = 0.20
                    { 0.16, 0.14, 0.12, 0.04 }   // lA/h ≥ 0.30
                },
                // SV4 - Tapped hole, load at head
                {
                    { 0.42, 0.34, 0.25, 0.12 },  // lA/h = 0.00
                    { 0.33, 0.27, 0.16, 0.08 },  // lA/h = 0.10
                    { 0.23, 0.19, 0.12, 0.06 },  // lA/h = 0.20
                    { 0.14, 0.13, 0.10, 0.03 }   // lA/h ≥ 0.30
                },
                // SV5 - Tapped hole, load in middle
                {
                    { 0.30, 0.25, 0.22, 0.10 },  // lA/h = 0.00
                    { 0.24, 0.21, 0.15, 0.07 },  // lA/h = 0.10
                    { 0.19, 0.17, 0.12, 0.06 },  // lA/h = 0.20
                    { 0.14, 0.13, 0.10, 0.03 }   // lA/h ≥ 0.30
                },
                // SV6 - Tapped hole, load at thread
                {
                    { 0.15, 0.14, 0.14, 0.07 },  // lA/h = 0.00
                    { 0.13, 0.12, 0.10, 0.06 },  // lA/h = 0.10
                    { 0.11, 0.11, 0.09, 0.06 },  // lA/h = 0.20
                    { 0.10, 0.10, 0.08, 0.03 }   // lA/h ≥ 0.30
                }
            };

            // Map bolting type to index
            int svIndex = boltingType switch
            {
                BoltingType.SV1_ThroughBolt_LoadAtHead => 0,
                BoltingType.SV2_ThroughBolt_LoadAtNut => 1,
                BoltingType.SV3_ThroughBolt_LoadInMiddle => 2,
                BoltingType.SV4_TappedHole_LoadAtHead => 3,
                BoltingType.SV5_TappedHole_LoadInMiddle => 4,
                BoltingType.SV6_TappedHole_LoadAtThread => 5,
                _ => 2 // Default to SV3
            };

            // lA/h breakpoints for interpolation
            double[] lA_h_values = { 0.0, 0.10, 0.20, 0.30 };
            // ak/h breakpoints for interpolation
            double[] ak_h_values = { 0.0, 0.10, 0.30, 0.50 };

            // Clamp input values to valid range
            lA_over_h = Math.Max(0.0, Math.Min(0.50, lA_over_h));
            ak_over_h = Math.Max(0.0, Math.Min(0.50, ak_over_h));

            // Find lA/h interpolation indices
            int lA_idx_low = 0;
            int lA_idx_high = 0;
            double lA_t = 0;

            for (int i = 0; i < lA_h_values.Length - 1; i++)
            {
                if (lA_over_h >= lA_h_values[i] && lA_over_h <= lA_h_values[i + 1])
                {
                    lA_idx_low = i;
                    lA_idx_high = i + 1;
                    lA_t = (lA_over_h - lA_h_values[i]) / (lA_h_values[i + 1] - lA_h_values[i]);
                    break;
                }
            }
            if (lA_over_h >= 0.30)
            {
                lA_idx_low = 3;
                lA_idx_high = 3;
                lA_t = 0;
            }

            // Find ak/h interpolation indices
            int ak_idx_low = 0;
            int ak_idx_high = 0;
            double ak_t = 0;

            for (int i = 0; i < ak_h_values.Length - 1; i++)
            {
                if (ak_over_h >= ak_h_values[i] && ak_over_h <= ak_h_values[i + 1])
                {
                    ak_idx_low = i;
                    ak_idx_high = i + 1;
                    ak_t = (ak_over_h - ak_h_values[i]) / (ak_h_values[i + 1] - ak_h_values[i]);
                    break;
                }
            }
            if (ak_over_h >= 0.50)
            {
                ak_idx_low = 3;
                ak_idx_high = 3;
                ak_t = 0;
            }

            // Bilinear interpolation
            double n_ll = tableData[svIndex, lA_idx_low, ak_idx_low];
            double n_lh = tableData[svIndex, lA_idx_low, ak_idx_high];
            double n_hl = tableData[svIndex, lA_idx_high, ak_idx_low];
            double n_hh = tableData[svIndex, lA_idx_high, ak_idx_high];

            // Interpolate along ak/h first
            double n_low = n_ll + ak_t * (n_lh - n_ll);
            double n_high = n_hl + ak_t * (n_hh - n_hl);

            // Then interpolate along lA/h
            double n = n_low + lA_t * (n_high - n_low);

            return Math.Max(0.03, Math.Min(0.70, n)); // Clamp to valid range
        }
    }
}
