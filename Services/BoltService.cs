using System;
using System.Collections.Generic;
using System.Linq;

namespace MechanicalCalculatorWeb.Services
{
    /// <summary>
    /// Metric bolt data according to ISO 4014/4017 and ISO 262
    /// </summary>
    public class BoltDimension
    {
        public string Size { get; set; } = "";           // M8, M10, etc.
        public double d { get; set; }                     // Nominal diameter (mm)
        public double P_coarse { get; set; }              // Coarse pitch (mm)
        public double d2_coarse { get; set; }             // Pitch diameter - coarse (mm)
        public double d3_coarse { get; set; }             // Minor diameter - coarse (mm)
        public double s { get; set; }                     // Wrench size (mm)
        public double e { get; set; }                     // Corner dimension (mm)
        public double k { get; set; }                     // Head height (mm)
        public double dw { get; set; }                    // Bearing diameter under head (mm)
        public double m_nut { get; set; }                 // Nut height ISO 4032 (mm)
        public double dw_nut { get; set; }                // Nut bearing diameter (mm)
    }

    /// <summary>
    /// Bolt strength class according to ISO 898-1
    /// </summary>
    public class BoltStrengthClass
    {
        public string Class { get; set; } = "";          // 4.6, 8.8, 10.9, 12.9
        public double Rm { get; set; }                    // Tensile strength (MPa)
        public double Rp02 { get; set; }                  // 0.2% proof stress (MPa)
        public double Rp02_Rm_ratio { get; set; }         // Rp0.2/Rm ratio
        public double ElongationMin { get; set; }         // Min elongation (%)
        public string Material { get; set; } = "";        // Typical material
    }

    /// <summary>
    /// Nut strength class according to ISO 898-2
    /// </summary>
    public class NutStrengthClass
    {
        public string Class { get; set; } = "";          // 8, 10, 12
        public double ProofStress { get; set; }           // Proof load stress (MPa)
        public string MatchingBoltClass { get; set; } = ""; // Compatible bolt class
    }

    /// <summary>
    /// Washer dimensions according to ISO 7089/7090
    /// </summary>
    public class WasherDimension
    {
        public string Size { get; set; } = "";           // M8, M10, etc.
        public double d1 { get; set; }                    // Inner diameter (mm)
        public double d2 { get; set; }                    // Outer diameter (mm)
        public double h { get; set; }                     // Thickness (mm)
        public double Hardness_HV { get; set; }           // Hardness (HV)
    }

    /// <summary>
    /// VDI 2230 Friction Coefficient Classes (Table A5)
    /// </summary>
    public enum FrictionClass
    {
        None,    // No class selected (use specific surface condition)
        ClassA,  // μ = 0.05-0.08  (e.g., MoS2, graphite, high-performance lubricants)
        ClassB,  // μ = 0.08-0.12  (e.g., oils, waxes, phosphated+oiled)
        ClassC,  // μ = 0.12-0.18  (e.g., black oxide+oiled, lightly oiled)
        ClassD,  // μ = 0.18-0.28  (e.g., as-received, black oxide dry)
        ClassE   // μ = 0.28-0.40  (e.g., rough dry surfaces, heavily oxidized)
    }

    /// <summary>
    /// Friction coefficient class data according to VDI 2230 Table A5
    /// </summary>
    public class FrictionClassData
    {
        public FrictionClass Class { get; set; }
        public string Description { get; set; } = "";
        public double Mu_min { get; set; }      // Minimum friction coefficient
        public double Mu_max { get; set; }      // Maximum friction coefficient
        public double Mu_typical { get; set; }  // Typical (middle) value
        public string Examples { get; set; } = "";  // Example surface conditions
    }

    /// <summary>
    /// Friction coefficients for various surface conditions
    /// </summary>
    public class FrictionCoefficient
    {
        public string Condition { get; set; } = "";      // Surface condition description
        public FrictionClass Class { get; set; } = FrictionClass.None; // VDI 2230 friction class (if applicable)
        public double MuG_min { get; set; }               // Thread friction min
        public double MuG_max { get; set; }               // Thread friction max
        public double MuG_typical { get; set; }           // Thread friction typical
        public double MuK_min { get; set; }               // Head friction min
        public double MuK_max { get; set; }               // Head friction max
        public double MuK_typical { get; set; }           // Head friction typical
    }

    /// <summary>
    /// Tightening method factors
    /// </summary>
    public class TighteningMethod
    {
        public string Method { get; set; } = "";         // Method name
        public string Description { get; set; } = "";    // Description
        public double AlphaA_min { get; set; }            // Tightening factor min
        public double AlphaA_max { get; set; }            // Tightening factor max
        public double AlphaA_typical { get; set; }        // Tightening factor typical
        public double Scatter { get; set; }               // Scatter percentage (%)
    }

    public static class BoltService
    {
        /// <summary>
        /// Metric bolt dimensions M3 to M64 (ISO 4014/4017)
        /// </summary>
        public static List<BoltDimension> MetricBolts { get; } = new List<BoltDimension>
        {
            new BoltDimension { Size = "M3",   d = 3,    P_coarse = 0.5,  d2_coarse = 2.675,  d3_coarse = 2.387,  s = 5.5,  e = 6.01,  k = 2.0,  dw = 5.07,  m_nut = 2.4,  dw_nut = 5.07 },
            new BoltDimension { Size = "M4",   d = 4,    P_coarse = 0.7,  d2_coarse = 3.545,  d3_coarse = 3.141,  s = 7,    e = 7.66,  k = 2.8,  dw = 6.53,  m_nut = 3.2,  dw_nut = 6.53 },
            new BoltDimension { Size = "M5",   d = 5,    P_coarse = 0.8,  d2_coarse = 4.480,  d3_coarse = 4.019,  s = 8,    e = 8.79,  k = 3.5,  dw = 7.53,  m_nut = 4.7,  dw_nut = 7.53 },
            new BoltDimension { Size = "M6",   d = 6,    P_coarse = 1.0,  d2_coarse = 5.350,  d3_coarse = 4.773,  s = 10,   e = 10.89, k = 4.0,  dw = 9.38,  m_nut = 5.2,  dw_nut = 9.38 },
            new BoltDimension { Size = "M8",   d = 8,    P_coarse = 1.25, d2_coarse = 7.188,  d3_coarse = 6.466,  s = 13,   e = 14.20, k = 5.3,  dw = 12.33, m_nut = 6.8,  dw_nut = 12.33 },
            new BoltDimension { Size = "M10",  d = 10,   P_coarse = 1.5,  d2_coarse = 9.026,  d3_coarse = 8.160,  s = 16,   e = 17.59, k = 6.4,  dw = 15.33, m_nut = 8.4,  dw_nut = 15.33 },
            new BoltDimension { Size = "M12",  d = 12,   P_coarse = 1.75, d2_coarse = 10.863, d3_coarse = 9.853,  s = 18,   e = 19.85, k = 7.5,  dw = 17.23, m_nut = 10.8, dw_nut = 17.23 },
            new BoltDimension { Size = "M14",  d = 14,   P_coarse = 2.0,  d2_coarse = 12.701, d3_coarse = 11.546, s = 21,   e = 23.35, k = 8.8,  dw = 20.17, m_nut = 12.8, dw_nut = 20.17 },
            new BoltDimension { Size = "M16",  d = 16,   P_coarse = 2.0,  d2_coarse = 14.701, d3_coarse = 13.546, s = 24,   e = 26.75, k = 10.0, dw = 23.17, m_nut = 14.8, dw_nut = 23.17 },
            new BoltDimension { Size = "M18",  d = 18,   P_coarse = 2.5,  d2_coarse = 16.376, d3_coarse = 14.933, s = 27,   e = 29.56, k = 11.5, dw = 26.17, m_nut = 15.8, dw_nut = 26.17 },
            new BoltDimension { Size = "M20",  d = 20,   P_coarse = 2.5,  d2_coarse = 18.376, d3_coarse = 16.933, s = 30,   e = 32.95, k = 12.5, dw = 29.16, m_nut = 18.0, dw_nut = 29.16 },
            new BoltDimension { Size = "M22",  d = 22,   P_coarse = 2.5,  d2_coarse = 20.376, d3_coarse = 18.933, s = 32,   e = 35.03, k = 14.0, dw = 31.00, m_nut = 19.4, dw_nut = 31.00 },
            new BoltDimension { Size = "M24",  d = 24,   P_coarse = 3.0,  d2_coarse = 22.051, d3_coarse = 20.319, s = 36,   e = 39.55, k = 15.0, dw = 35.00, m_nut = 21.5, dw_nut = 35.00 },
            new BoltDimension { Size = "M27",  d = 27,   P_coarse = 3.0,  d2_coarse = 25.051, d3_coarse = 23.319, s = 41,   e = 45.20, k = 17.0, dw = 40.00, m_nut = 23.8, dw_nut = 40.00 },
            new BoltDimension { Size = "M30",  d = 30,   P_coarse = 3.5,  d2_coarse = 27.727, d3_coarse = 25.706, s = 46,   e = 50.85, k = 18.7, dw = 45.00, m_nut = 25.6, dw_nut = 45.00 },
            new BoltDimension { Size = "M33",  d = 33,   P_coarse = 3.5,  d2_coarse = 30.727, d3_coarse = 28.706, s = 50,   e = 55.37, k = 21.0, dw = 49.00, m_nut = 28.7, dw_nut = 49.00 },
            new BoltDimension { Size = "M36",  d = 36,   P_coarse = 4.0,  d2_coarse = 33.402, d3_coarse = 31.093, s = 55,   e = 60.79, k = 22.5, dw = 54.00, m_nut = 31.0, dw_nut = 54.00 },
            new BoltDimension { Size = "M39",  d = 39,   P_coarse = 4.0,  d2_coarse = 36.402, d3_coarse = 34.093, s = 60,   e = 66.44, k = 25.0, dw = 58.80, m_nut = 33.4, dw_nut = 58.80 },
            new BoltDimension { Size = "M42",  d = 42,   P_coarse = 4.5,  d2_coarse = 39.077, d3_coarse = 36.479, s = 65,   e = 71.30, k = 26.0, dw = 63.10, m_nut = 34.0, dw_nut = 63.10 },
            new BoltDimension { Size = "M45",  d = 45,   P_coarse = 4.5,  d2_coarse = 42.077, d3_coarse = 39.479, s = 70,   e = 76.95, k = 28.0, dw = 67.40, m_nut = 36.0, dw_nut = 67.40 },
            new BoltDimension { Size = "M48",  d = 48,   P_coarse = 5.0,  d2_coarse = 44.752, d3_coarse = 41.866, s = 75,   e = 82.60, k = 30.0, dw = 71.80, m_nut = 38.0, dw_nut = 71.80 },
            new BoltDimension { Size = "M52",  d = 52,   P_coarse = 5.0,  d2_coarse = 48.752, d3_coarse = 45.866, s = 80,   e = 88.25, k = 33.0, dw = 76.20, m_nut = 42.0, dw_nut = 76.20 },
            new BoltDimension { Size = "M56",  d = 56,   P_coarse = 5.5,  d2_coarse = 52.428, d3_coarse = 49.252, s = 85,   e = 93.56, k = 35.0, dw = 81.00, m_nut = 45.0, dw_nut = 81.00 },
            new BoltDimension { Size = "M60",  d = 60,   P_coarse = 5.5,  d2_coarse = 56.428, d3_coarse = 53.252, s = 90,   e = 99.21, k = 38.0, dw = 85.50, m_nut = 48.0, dw_nut = 85.50 },
            new BoltDimension { Size = "M64",  d = 64,   P_coarse = 6.0,  d2_coarse = 60.103, d3_coarse = 56.639, s = 95,   e = 104.86, k = 40.0, dw = 90.00, m_nut = 51.0, dw_nut = 90.00 },
        };

        /// <summary>
        /// Socket Head Cap Screws - DIN 912 / ISO 4762
        /// Note: 's' represents hex key (allen) size, 'dw' is head diameter
        /// </summary>
        public static List<BoltDimension> SocketHeadBolts { get; } = new List<BoltDimension>
        {
            new BoltDimension { Size = "M3",   d = 3,    P_coarse = 0.5,  d2_coarse = 2.675,  d3_coarse = 2.387,  s = 2.5,  e = 0,     k = 3.0,  dw = 5.5,  m_nut = 2.4,  dw_nut = 5.07 },
            new BoltDimension { Size = "M4",   d = 4,    P_coarse = 0.7,  d2_coarse = 3.545,  d3_coarse = 3.141,  s = 3.0,  e = 0,     k = 4.0,  dw = 7.0,  m_nut = 3.2,  dw_nut = 6.53 },
            new BoltDimension { Size = "M5",   d = 5,    P_coarse = 0.8,  d2_coarse = 4.480,  d3_coarse = 4.019,  s = 4.0,  e = 0,     k = 5.0,  dw = 8.5,  m_nut = 4.7,  dw_nut = 7.53 },
            new BoltDimension { Size = "M6",   d = 6,    P_coarse = 1.0,  d2_coarse = 5.350,  d3_coarse = 4.773,  s = 5.0,  e = 0,     k = 6.0,  dw = 10.0, m_nut = 5.2,  dw_nut = 9.38 },
            new BoltDimension { Size = "M8",   d = 8,    P_coarse = 1.25, d2_coarse = 7.188,  d3_coarse = 6.466,  s = 6.0,  e = 0,     k = 8.0,  dw = 13.0, m_nut = 6.8,  dw_nut = 12.33 },
            new BoltDimension { Size = "M10",  d = 10,   P_coarse = 1.5,  d2_coarse = 9.026,  d3_coarse = 8.160,  s = 8.0,  e = 0,     k = 10.0, dw = 16.0, m_nut = 8.4,  dw_nut = 15.33 },
            new BoltDimension { Size = "M12",  d = 12,   P_coarse = 1.75, d2_coarse = 10.863, d3_coarse = 9.853,  s = 10.0, e = 0,     k = 12.0, dw = 18.0, m_nut = 10.8, dw_nut = 17.23 },
            new BoltDimension { Size = "M14",  d = 14,   P_coarse = 2.0,  d2_coarse = 12.701, d3_coarse = 11.546, s = 12.0, e = 0,     k = 14.0, dw = 21.0, m_nut = 12.8, dw_nut = 20.17 },
            new BoltDimension { Size = "M16",  d = 16,   P_coarse = 2.0,  d2_coarse = 14.701, d3_coarse = 13.546, s = 14.0, e = 0,     k = 16.0, dw = 24.0, m_nut = 14.8, dw_nut = 23.17 },
            new BoltDimension { Size = "M18",  d = 18,   P_coarse = 2.5,  d2_coarse = 16.376, d3_coarse = 14.933, s = 14.0, e = 0,     k = 18.0, dw = 27.0, m_nut = 15.8, dw_nut = 26.17 },
            new BoltDimension { Size = "M20",  d = 20,   P_coarse = 2.5,  d2_coarse = 18.376, d3_coarse = 16.933, s = 17.0, e = 0,     k = 20.0, dw = 30.0, m_nut = 18.0, dw_nut = 29.16 },
            new BoltDimension { Size = "M22",  d = 22,   P_coarse = 2.5,  d2_coarse = 20.376, d3_coarse = 18.933, s = 17.0, e = 0,     k = 22.0, dw = 33.0, m_nut = 19.4, dw_nut = 31.00 },
            new BoltDimension { Size = "M24",  d = 24,   P_coarse = 3.0,  d2_coarse = 22.051, d3_coarse = 20.319, s = 19.0, e = 0,     k = 24.0, dw = 36.0, m_nut = 21.5, dw_nut = 35.00 },
            new BoltDimension { Size = "M27",  d = 27,   P_coarse = 3.0,  d2_coarse = 25.051, d3_coarse = 23.319, s = 19.0, e = 0,     k = 27.0, dw = 40.0, m_nut = 23.8, dw_nut = 40.00 },
            new BoltDimension { Size = "M30",  d = 30,   P_coarse = 3.5,  d2_coarse = 27.727, d3_coarse = 25.706, s = 22.0, e = 0,     k = 30.0, dw = 45.0, m_nut = 25.6, dw_nut = 45.00 },
            new BoltDimension { Size = "M36",  d = 36,   P_coarse = 4.0,  d2_coarse = 33.402, d3_coarse = 31.093, s = 27.0, e = 0,     k = 36.0, dw = 54.0, m_nut = 31.0, dw_nut = 54.00 },
        };

        /// <summary>
        /// Bolt strength classes according to ISO 898-1
        /// </summary>
        public static List<BoltStrengthClass> StrengthClasses { get; } = new List<BoltStrengthClass>
        {
            new BoltStrengthClass { Class = "4.6",  Rm = 400,  Rp02 = 240,  Rp02_Rm_ratio = 0.60, ElongationMin = 22, Material = "Low/medium carbon steel" },
            new BoltStrengthClass { Class = "4.8",  Rm = 420,  Rp02 = 340,  Rp02_Rm_ratio = 0.80, ElongationMin = 14, Material = "Low/medium carbon steel" },
            new BoltStrengthClass { Class = "5.6",  Rm = 500,  Rp02 = 300,  Rp02_Rm_ratio = 0.60, ElongationMin = 20, Material = "Low/medium carbon steel" },
            new BoltStrengthClass { Class = "5.8",  Rm = 520,  Rp02 = 420,  Rp02_Rm_ratio = 0.80, ElongationMin = 10, Material = "Low/medium carbon steel" },
            new BoltStrengthClass { Class = "6.8",  Rm = 600,  Rp02 = 480,  Rp02_Rm_ratio = 0.80, ElongationMin = 8,  Material = "Low/medium carbon steel" },
            new BoltStrengthClass { Class = "8.8",  Rm = 800,  Rp02 = 640,  Rp02_Rm_ratio = 0.80, ElongationMin = 12, Material = "Medium carbon steel, Q&T" },
            new BoltStrengthClass { Class = "9.8",  Rm = 900,  Rp02 = 720,  Rp02_Rm_ratio = 0.80, ElongationMin = 10, Material = "Medium carbon steel, Q&T" },
            new BoltStrengthClass { Class = "10.9", Rm = 1000, Rp02 = 900,  Rp02_Rm_ratio = 0.90, ElongationMin = 9,  Material = "Alloy steel, Q&T" },
            new BoltStrengthClass { Class = "12.9", Rm = 1200, Rp02 = 1080, Rp02_Rm_ratio = 0.90, ElongationMin = 8,  Material = "Alloy steel, Q&T" },
        };

        /// <summary>
        /// Nut strength classes according to ISO 898-2
        /// </summary>
        public static List<NutStrengthClass> NutClasses { get; } = new List<NutStrengthClass>
        {
            new NutStrengthClass { Class = "5",  ProofStress = 520,  MatchingBoltClass = "4.6, 4.8, 5.6, 5.8" },
            new NutStrengthClass { Class = "6",  ProofStress = 600,  MatchingBoltClass = "6.8" },
            new NutStrengthClass { Class = "8",  ProofStress = 800,  MatchingBoltClass = "8.8" },
            new NutStrengthClass { Class = "10", ProofStress = 1040, MatchingBoltClass = "10.9" },
            new NutStrengthClass { Class = "12", ProofStress = 1200, MatchingBoltClass = "12.9" },
        };

        /// <summary>
        /// Standard washer dimensions (ISO 7089 - normal series)
        /// </summary>
        public static List<WasherDimension> Washers { get; } = new List<WasherDimension>
        {
            new WasherDimension { Size = "M3",  d1 = 3.2,  d2 = 7,    h = 0.5,  Hardness_HV = 200 },
            new WasherDimension { Size = "M4",  d1 = 4.3,  d2 = 9,    h = 0.8,  Hardness_HV = 200 },
            new WasherDimension { Size = "M5",  d1 = 5.3,  d2 = 10,   h = 1.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M6",  d1 = 6.4,  d2 = 12,   h = 1.6,  Hardness_HV = 200 },
            new WasherDimension { Size = "M8",  d1 = 8.4,  d2 = 16,   h = 1.6,  Hardness_HV = 200 },
            new WasherDimension { Size = "M10", d1 = 10.5, d2 = 20,   h = 2.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M12", d1 = 13.0, d2 = 24,   h = 2.5,  Hardness_HV = 200 },
            new WasherDimension { Size = "M14", d1 = 15.0, d2 = 28,   h = 2.5,  Hardness_HV = 200 },
            new WasherDimension { Size = "M16", d1 = 17.0, d2 = 30,   h = 3.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M18", d1 = 19.0, d2 = 34,   h = 3.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M20", d1 = 21.0, d2 = 37,   h = 3.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M22", d1 = 23.0, d2 = 39,   h = 3.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M24", d1 = 25.0, d2 = 44,   h = 4.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M27", d1 = 28.0, d2 = 50,   h = 4.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M30", d1 = 31.0, d2 = 56,   h = 4.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M33", d1 = 34.0, d2 = 60,   h = 5.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M36", d1 = 37.0, d2 = 66,   h = 5.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M39", d1 = 40.0, d2 = 72,   h = 6.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M42", d1 = 43.0, d2 = 78,   h = 7.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M45", d1 = 46.0, d2 = 85,   h = 7.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M48", d1 = 50.0, d2 = 92,   h = 8.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M52", d1 = 54.0, d2 = 98,   h = 8.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M56", d1 = 58.0, d2 = 105,  h = 9.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M60", d1 = 62.0, d2 = 110,  h = 9.0,  Hardness_HV = 200 },
            new WasherDimension { Size = "M64", d1 = 66.0, d2 = 115,  h = 9.0,  Hardness_HV = 200 },
        };

        /// <summary>
        /// VDI 2230 Friction Coefficient Classes (Table A5)
        /// </summary>
        public static List<FrictionClassData> FrictionClasses { get; } = new List<FrictionClassData>
        {
            new FrictionClassData
            {
                Class = FrictionClass.ClassA,
                Description = "Class A - Very low friction",
                Mu_min = 0.05,
                Mu_max = 0.08,
                Mu_typical = 0.065,
                Examples = "MoS2, graphite, high-performance lubricants, special anti-seize compounds"
            },
            new FrictionClassData
            {
                Class = FrictionClass.ClassB,
                Description = "Class B - Low friction",
                Mu_min = 0.08,
                Mu_max = 0.12,
                Mu_typical = 0.10,
                Examples = "Oils, waxes, phosphated+oiled, good quality lubricants"
            },
            new FrictionClassData
            {
                Class = FrictionClass.ClassC,
                Description = "Class C - Medium friction",
                Mu_min = 0.12,
                Mu_max = 0.18,
                Mu_typical = 0.15,
                Examples = "Black oxide+oiled, standard lubricants, lightly oiled threads"
            },
            new FrictionClassData
            {
                Class = FrictionClass.ClassD,
                Description = "Class D - Medium-high friction",
                Mu_min = 0.18,
                Mu_max = 0.28,
                Mu_typical = 0.23,
                Examples = "As-received bolts, black oxide dry, phosphated dry, unlubricated"
            },
            new FrictionClassData
            {
                Class = FrictionClass.ClassE,
                Description = "Class E - High friction",
                Mu_min = 0.28,
                Mu_max = 0.40,
                Mu_typical = 0.34,
                Examples = "Hot-dip galvanized, rough surfaces, rusty threads, heavily oxidized, no lubrication"
            }
        };

        /// <summary>
        /// Friction coefficients for various surface conditions (VDI 2230)
        /// Now mapped to VDI 2230 friction classes where applicable
        /// </summary>
        public static List<FrictionCoefficient> FrictionCoefficients { get; } = new List<FrictionCoefficient>
        {
            // Class A - Very low friction (μ = 0.05-0.08)
            new FrictionCoefficient { Condition = "MoS2 (molybdenum disulfide)",    Class = FrictionClass.ClassA, MuG_min = 0.06, MuG_max = 0.10, MuG_typical = 0.08, MuK_min = 0.06, MuK_max = 0.10, MuK_typical = 0.08 },

            // Class B - Low friction (μ = 0.08-0.12)
            new FrictionCoefficient { Condition = "Phosphated, oiled",              Class = FrictionClass.ClassB, MuG_min = 0.08, MuG_max = 0.12, MuG_typical = 0.10, MuK_min = 0.08, MuK_max = 0.12, MuK_typical = 0.10 },
            new FrictionCoefficient { Condition = "Waxed threads",                  Class = FrictionClass.ClassB, MuG_min = 0.08, MuG_max = 0.12, MuG_typical = 0.10, MuK_min = 0.10, MuK_max = 0.14, MuK_typical = 0.12 },
            new FrictionCoefficient { Condition = "Stainless steel, lubricated",    Class = FrictionClass.ClassB, MuG_min = 0.10, MuG_max = 0.16, MuG_typical = 0.13, MuK_min = 0.10, MuK_max = 0.16, MuK_typical = 0.13 },

            // Class C - Medium friction (μ = 0.12-0.18)
            new FrictionCoefficient { Condition = "Black oxide, oiled",             Class = FrictionClass.ClassC, MuG_min = 0.10, MuG_max = 0.14, MuG_typical = 0.12, MuK_min = 0.10, MuK_max = 0.14, MuK_typical = 0.12 },
            new FrictionCoefficient { Condition = "Black oxide, dry",               Class = FrictionClass.ClassC, MuG_min = 0.12, MuG_max = 0.18, MuG_typical = 0.14, MuK_min = 0.12, MuK_max = 0.18, MuK_typical = 0.14 },
            new FrictionCoefficient { Condition = "Phosphated, dry",                Class = FrictionClass.ClassC, MuG_min = 0.12, MuG_max = 0.18, MuG_typical = 0.16, MuK_min = 0.12, MuK_max = 0.18, MuK_typical = 0.16 },

            // Class D - Medium-high friction (μ = 0.18-0.28)
            new FrictionCoefficient { Condition = "Stainless steel, dry",           Class = FrictionClass.ClassD, MuG_min = 0.15, MuG_max = 0.25, MuG_typical = 0.20, MuK_min = 0.15, MuK_max = 0.25, MuK_typical = 0.20 },

            // Class E - High friction (μ = 0.28-0.40)
            new FrictionCoefficient { Condition = "Hot-dip galvanized, dry",        Class = FrictionClass.ClassE, MuG_min = 0.14, MuG_max = 0.22, MuG_typical = 0.18, MuK_min = 0.14, MuK_max = 0.22, MuK_typical = 0.18 },
        };

        /// <summary>
        /// Tightening methods and their scatter factors (VDI 2230)
        /// </summary>
        public static List<TighteningMethod> TighteningMethods { get; } = new List<TighteningMethod>
        {
            new TighteningMethod { Method = "Torque wrench",            Description = "Standard torque-controlled tightening",                AlphaA_min = 1.4, AlphaA_max = 2.0, AlphaA_typical = 1.6, Scatter = 25 },
            new TighteningMethod { Method = "Torque wrench (precision)", Description = "Calibrated torque wrench with controlled conditions",  AlphaA_min = 1.2, AlphaA_max = 1.6, AlphaA_typical = 1.4, Scatter = 15 },
            new TighteningMethod { Method = "Angle controlled",          Description = "Torque + angle tightening",                           AlphaA_min = 1.1, AlphaA_max = 1.3, AlphaA_typical = 1.2, Scatter = 10 },
            new TighteningMethod { Method = "Yield controlled",          Description = "Tightening to yield point detection",                 AlphaA_min = 1.02, AlphaA_max = 1.10, AlphaA_typical = 1.06, Scatter = 4 },
            new TighteningMethod { Method = "Hydraulic tensioner",       Description = "Direct bolt elongation measurement",                  AlphaA_min = 1.02, AlphaA_max = 1.10, AlphaA_typical = 1.06, Scatter = 4 },
            new TighteningMethod { Method = "Ultrasonic",                Description = "Ultrasonic elongation measurement",                   AlphaA_min = 1.01, AlphaA_max = 1.05, AlphaA_typical = 1.03, Scatter = 2 },
            new TighteningMethod { Method = "Impact wrench",             Description = "Pneumatic/electric impact - NOT recommended for critical joints", AlphaA_min = 1.6, AlphaA_max = 2.5, AlphaA_typical = 2.0, Scatter = 40 },
        };

        /// <summary>
        /// Standard hole clearances according to ISO 273
        /// </summary>
        public static Dictionary<string, Dictionary<string, double>> HoleClearances { get; } = new Dictionary<string, Dictionary<string, double>>
        {
            // Format: Size -> { "fine", "medium", "coarse" }
            { "M3",  new Dictionary<string, double> { { "fine", 3.2 },  { "medium", 3.4 },  { "coarse", 3.6 } } },
            { "M4",  new Dictionary<string, double> { { "fine", 4.3 },  { "medium", 4.5 },  { "coarse", 4.8 } } },
            { "M5",  new Dictionary<string, double> { { "fine", 5.3 },  { "medium", 5.5 },  { "coarse", 5.8 } } },
            { "M6",  new Dictionary<string, double> { { "fine", 6.4 },  { "medium", 6.6 },  { "coarse", 7.0 } } },
            { "M8",  new Dictionary<string, double> { { "fine", 8.4 },  { "medium", 9.0 },  { "coarse", 10.0 } } },
            { "M10", new Dictionary<string, double> { { "fine", 10.5 }, { "medium", 11.0 }, { "coarse", 12.0 } } },
            { "M12", new Dictionary<string, double> { { "fine", 13.0 }, { "medium", 13.5 }, { "coarse", 14.5 } } },
            { "M14", new Dictionary<string, double> { { "fine", 15.0 }, { "medium", 15.5 }, { "coarse", 16.5 } } },
            { "M16", new Dictionary<string, double> { { "fine", 17.0 }, { "medium", 17.5 }, { "coarse", 18.5 } } },
            { "M18", new Dictionary<string, double> { { "fine", 19.0 }, { "medium", 20.0 }, { "coarse", 21.0 } } },
            { "M20", new Dictionary<string, double> { { "fine", 21.0 }, { "medium", 22.0 }, { "coarse", 24.0 } } },
            { "M22", new Dictionary<string, double> { { "fine", 23.0 }, { "medium", 24.0 }, { "coarse", 26.0 } } },
            { "M24", new Dictionary<string, double> { { "fine", 25.0 }, { "medium", 26.0 }, { "coarse", 28.0 } } },
            { "M27", new Dictionary<string, double> { { "fine", 28.0 }, { "medium", 30.0 }, { "coarse", 32.0 } } },
            { "M30", new Dictionary<string, double> { { "fine", 31.0 }, { "medium", 33.0 }, { "coarse", 35.0 } } },
            { "M33", new Dictionary<string, double> { { "fine", 34.0 }, { "medium", 36.0 }, { "coarse", 38.0 } } },
            { "M36", new Dictionary<string, double> { { "fine", 37.0 }, { "medium", 39.0 }, { "coarse", 42.0 } } },
            { "M39", new Dictionary<string, double> { { "fine", 40.0 }, { "medium", 42.0 }, { "coarse", 45.0 } } },
            { "M42", new Dictionary<string, double> { { "fine", 43.0 }, { "medium", 45.0 }, { "coarse", 48.0 } } },
            { "M45", new Dictionary<string, double> { { "fine", 46.0 }, { "medium", 48.0 }, { "coarse", 52.0 } } },
            { "M48", new Dictionary<string, double> { { "fine", 50.0 }, { "medium", 52.0 }, { "coarse", 56.0 } } },
            { "M52", new Dictionary<string, double> { { "fine", 54.0 }, { "medium", 56.0 }, { "coarse", 62.0 } } },
            { "M56", new Dictionary<string, double> { { "fine", 58.0 }, { "medium", 62.0 }, { "coarse", 66.0 } } },
            { "M60", new Dictionary<string, double> { { "fine", 62.0 }, { "medium", 66.0 }, { "coarse", 70.0 } } },
            { "M64", new Dictionary<string, double> { { "fine", 66.0 }, { "medium", 70.0 }, { "coarse", 74.0 } } },
        };

        /// <summary>
        /// Settling amounts per interface (VDI 2230, Table 5.4/1)
        /// Values in μm
        /// </summary>
        public static Dictionary<string, double> SettlingAmounts { get; } = new Dictionary<string, double>
        {
            { "Machined surface (Ra ≤ 10 μm), head bearing", 3.0 },
            { "Machined surface (Ra ≤ 10 μm), interface", 3.0 },
            { "Machined surface (Ra ≤ 10 μm), thread", 3.0 },
            { "Rolled/forged surface (Ra ≤ 40 μm), head bearing", 6.5 },
            { "Rolled/forged surface (Ra ≤ 40 μm), interface", 6.5 },
            { "As-cast surface, head bearing", 10.0 },
            { "As-cast surface, interface", 10.0 },
        };

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Get bolt dimensions by size
        /// </summary>
        public static BoltDimension? GetBoltBySize(string size)
        {
            return MetricBolts.FirstOrDefault(b => b.Size.Equals(size, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get strength class by name
        /// </summary>
        public static BoltStrengthClass? GetStrengthClass(string className)
        {
            return StrengthClasses.FirstOrDefault(s => s.Class == className);
        }

        /// <summary>
        /// Get washer by bolt size
        /// </summary>
        public static WasherDimension? GetWasherBySize(string size)
        {
            return Washers.FirstOrDefault(w => w.Size.Equals(size, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get friction coefficient by condition
        /// </summary>
        public static FrictionCoefficient? GetFrictionCoefficient(string condition)
        {
            return FrictionCoefficients.FirstOrDefault(f => f.Condition.Equals(condition, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get friction class data by class enum
        /// </summary>
        public static FrictionClassData? GetFrictionClassData(FrictionClass frictionClass)
        {
            return FrictionClasses.FirstOrDefault(f => f.Class == frictionClass);
        }

        /// <summary>
        /// Get friction coefficients by VDI 2230 friction class
        /// Returns a FrictionCoefficient object with values from the class definition
        /// </summary>
        public static FrictionCoefficient GetFrictionCoefficientFromClass(FrictionClass frictionClass)
        {
            var classData = GetFrictionClassData(frictionClass);
            if (classData == null)
            {
                // Default to Class C (medium friction) if not found
                classData = FrictionClasses.FirstOrDefault(f => f.Class == FrictionClass.ClassC)
                    ?? new FrictionClassData { Mu_min = 0.12, Mu_max = 0.18, Mu_typical = 0.15 };
            }

            return new FrictionCoefficient
            {
                Condition = classData.Description,
                Class = frictionClass,
                MuG_min = classData.Mu_min,
                MuG_max = classData.Mu_max,
                MuG_typical = classData.Mu_typical,
                MuK_min = classData.Mu_min,
                MuK_max = classData.Mu_max,
                MuK_typical = classData.Mu_typical
            };
        }

        /// <summary>
        /// Get tightening method by name
        /// </summary>
        public static TighteningMethod? GetTighteningMethod(string method)
        {
            return TighteningMethods.FirstOrDefault(t => t.Method.Equals(method, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get hole clearance for given size and fit type
        /// </summary>
        public static double GetHoleClearance(string size, string fitType = "medium")
        {
            if (HoleClearances.TryGetValue(size, out var clearances))
            {
                if (clearances.TryGetValue(fitType.ToLowerInvariant(), out var diameter))
                {
                    return diameter;
                }
            }
            return 0;
        }

        /// <summary>
        /// Calculate polar moment of resistance for thread (Wp)
        /// </summary>
        public static double CalculateWp(double d3)
        {
            return Math.PI * Math.Pow(d3, 3) / 16.0;
        }

        /// <summary>
        /// Calculate thread lead angle
        /// </summary>
        public static double CalculateLeadAngle(double P, double d2)
        {
            return Math.Atan(P / (Math.PI * d2)) * 180.0 / Math.PI; // degrees
        }

        /// <summary>
        /// Calculate friction angle
        /// </summary>
        public static double CalculateFrictionAngle(double mu)
        {
            return Math.Atan(mu / Math.Cos(30.0 * Math.PI / 180.0)) * 180.0 / Math.PI; // degrees, for 60° thread
        }

        /// <summary>
        /// Calculate mean bearing diameter under head
        /// </summary>
        public static double CalculateDKm(double dw, double dHole)
        {
            return (dw + dHole) / 2.0;
        }

        /// <summary>
        /// Get recommended nut class for bolt class
        /// </summary>
        public static string GetRecommendedNutClass(string boltClass)
        {
            return boltClass switch
            {
                "4.6" or "4.8" or "5.6" or "5.8" => "5",
                "6.8" => "6",
                "8.8" => "8",
                "10.9" => "10",
                "12.9" => "12",
                _ => "8"
            };
        }

        /// <summary>
        /// Get all available bolt sizes
        /// </summary>
        public static List<string> GetAllSizes()
        {
            return MetricBolts.Select(b => b.Size).ToList();
        }

        /// <summary>
        /// Get all strength class names
        /// </summary>
        public static List<string> GetAllStrengthClasses()
        {
            return StrengthClasses.Select(s => s.Class).ToList();
        }

        /// <summary>
        /// Get all surface conditions
        /// </summary>
        public static List<string> GetAllSurfaceConditions()
        {
            return FrictionCoefficients.Select(f => f.Condition).ToList();
        }

        /// <summary>
        /// Get all tightening method names
        /// </summary>
        public static List<string> GetAllTighteningMethods()
        {
            return TighteningMethods.Select(t => t.Method).ToList();
        }

        /// <summary>
        /// Get all inch bolt sizes
        /// </summary>
        public static List<string> GetAllInchBoltSizes()
        {
            return InchBolts.Select(b => b.Size).ToList();
        }

        /// <summary>
        /// Get inch bolt by size
        /// </summary>
        public static BoltDimension? GetInchBoltBySize(string size)
        {
            return InchBolts.FirstOrDefault(b => b.Size == size);
        }

        /// <summary>
        /// Inch bolt data according to ASME B1.1 (UNC/UNF)
        /// </summary>
        public static List<BoltDimension> InchBolts { get; } = new List<BoltDimension>
        {
            // Size format: "1/4-20" (UNC), "1/4-28" (UNF)
            // d = nominal diameter in mm, P = pitch in mm (25.4/TPI)
            // UNC = Unified National Coarse, UNF = Unified National Fine
            
            // #6 (0.138" = 3.505mm)
            new BoltDimension { Size = "#6",    d = 3.505,  P_coarse = 0.794,  d2_coarse = 3.073,  d3_coarse = 2.641,  s = 6.35,  e = 7.14,  k = 2.31,  dw = 5.82,  m_nut = 2.78,  dw_nut = 5.82 },
            
            // #8 (0.164" = 4.166mm)
            new BoltDimension { Size = "#8",    d = 4.166,  P_coarse = 0.794,  d2_coarse = 3.734,  d3_coarse = 3.302,  s = 6.35,  e = 7.14,  k = 2.74,  dw = 5.82,  m_nut = 3.30,  dw_nut = 5.82 },
            
            // #10 (0.190" = 4.826mm)
            new BoltDimension { Size = "#10",   d = 4.826,  P_coarse = 0.794,  d2_coarse = 4.394,  d3_coarse = 3.962,  s = 7.94,  e = 8.89,  k = 3.18,  dw = 7.24,  m_nut = 3.81,  dw_nut = 7.24 },
            
            // 1/4" (6.35mm) - 20 TPI UNC, 28 TPI UNF
            new BoltDimension { Size = "1/4\"", d = 6.35,   P_coarse = 1.270,  d2_coarse = 5.537,  d3_coarse = 4.724,  s = 11.11, e = 12.45, k = 4.09,  dw = 10.16, m_nut = 5.56,  dw_nut = 10.16 },
            
            // 5/16" (7.938mm) - 18 TPI UNC, 24 TPI UNF
            new BoltDimension { Size = "5/16\"", d = 7.938, P_coarse = 1.411,  d2_coarse = 7.034,  d3_coarse = 6.130,  s = 12.70, e = 14.22, k = 5.03,  dw = 11.61, m_nut = 6.88,  dw_nut = 11.61 },
            
            // 3/8" (9.525mm) - 16 TPI UNC, 24 TPI UNF
            new BoltDimension { Size = "3/8\"", d = 9.525,  P_coarse = 1.588,  d2_coarse = 8.509,  d3_coarse = 7.493,  s = 14.29, e = 16.00, k = 6.02,  dw = 13.06, m_nut = 8.26,  dw_nut = 13.06 },
            
            // 7/16" (11.112mm) - 14 TPI UNC, 20 TPI UNF
            new BoltDimension { Size = "7/16\"", d = 11.112, P_coarse = 1.814, d2_coarse = 9.934,  d3_coarse = 8.756,  s = 17.46, e = 19.56, k = 7.01,  dw = 15.93, m_nut = 9.53,  dw_nut = 15.93 },
            
            // 1/2" (12.7mm) - 13 TPI UNC, 20 TPI UNF
            new BoltDimension { Size = "1/2\"", d = 12.7,   P_coarse = 1.954,  d2_coarse = 11.430, d3_coarse = 10.160, s = 19.05, e = 21.34, k = 7.95,  dw = 17.42, m_nut = 11.11, dw_nut = 17.42 },
            
            // 9/16" (14.288mm) - 12 TPI UNC, 18 TPI UNF
            new BoltDimension { Size = "9/16\"", d = 14.288, P_coarse = 2.117, d2_coarse = 12.913, d3_coarse = 11.538, s = 22.23, e = 24.89, k = 8.89,  dw = 20.32, m_nut = 12.70, dw_nut = 20.32 },
            
            // 5/8" (15.875mm) - 11 TPI UNC, 18 TPI UNF
            new BoltDimension { Size = "5/8\"", d = 15.875, P_coarse = 2.309,  d2_coarse = 14.376, d3_coarse = 12.878, s = 23.81, e = 26.67, k = 9.84,  dw = 21.77, m_nut = 14.29, dw_nut = 21.77 },
            
            // 3/4" (19.05mm) - 10 TPI UNC, 16 TPI UNF
            new BoltDimension { Size = "3/4\"", d = 19.05,  P_coarse = 2.540,  d2_coarse = 17.399, d3_coarse = 15.748, s = 28.58, e = 32.00, k = 11.73, dw = 26.11, m_nut = 17.15, dw_nut = 26.11 },
            
            // 7/8" (22.225mm) - 9 TPI UNC, 14 TPI UNF
            new BoltDimension { Size = "7/8\"", d = 22.225, P_coarse = 2.822,  d2_coarse = 20.391, d3_coarse = 18.557, s = 33.34, e = 37.34, k = 13.67, dw = 30.48, m_nut = 20.24, dw_nut = 30.48 },
            
            // 1" (25.4mm) - 8 TPI UNC, 12 TPI UNF
            new BoltDimension { Size = "1\"",   d = 25.4,   P_coarse = 3.175,  d2_coarse = 23.338, d3_coarse = 21.276, s = 38.10, e = 42.67, k = 15.62, dw = 34.80, m_nut = 22.86, dw_nut = 34.80 },
            
            // 1-1/8" (28.575mm) - 7 TPI UNC, 12 TPI UNF
            new BoltDimension { Size = "1-1/8\"", d = 28.575, P_coarse = 3.629, d2_coarse = 26.211, d3_coarse = 23.847, s = 42.86, e = 48.01, k = 17.48, dw = 39.12, m_nut = 25.65, dw_nut = 39.12 },
            
            // 1-1/4" (31.75mm) - 7 TPI UNC, 12 TPI UNF
            new BoltDimension { Size = "1-1/4\"", d = 31.75, P_coarse = 3.629, d2_coarse = 29.386, d3_coarse = 27.022, s = 47.63, e = 53.34, k = 19.30, dw = 43.51, m_nut = 28.58, dw_nut = 43.51 },
            
            // 1-3/8" (34.925mm) - 6 TPI UNC, 12 TPI UNF
            new BoltDimension { Size = "1-3/8\"", d = 34.925, P_coarse = 4.233, d2_coarse = 32.174, d3_coarse = 29.422, s = 52.39, e = 58.67, k = 21.21, dw = 47.88, m_nut = 31.37, dw_nut = 47.88 },
            
            // 1-1/2" (38.1mm) - 6 TPI UNC, 12 TPI UNF
            new BoltDimension { Size = "1-1/2\"", d = 38.1,  P_coarse = 4.233, d2_coarse = 35.349, d3_coarse = 32.597, s = 57.15, e = 64.01, k = 23.11, dw = 52.20, m_nut = 34.29, dw_nut = 52.20 },
        };

        /// <summary>
        /// SAE Grade strength classes for inch bolts
        /// </summary>
        public static List<BoltStrengthClass> SAEGrades { get; } = new List<BoltStrengthClass>
        {
            new BoltStrengthClass { Class = "SAE 2",   Rm = 510,  Rp02 = 393,  Rp02_Rm_ratio = 0.77, ElongationMin = 18, Material = "Low/medium carbon steel" },
            new BoltStrengthClass { Class = "SAE 5",   Rm = 827,  Rp02 = 634,  Rp02_Rm_ratio = 0.77, ElongationMin = 14, Material = "Medium carbon steel, Q&T" },
            new BoltStrengthClass { Class = "SAE 8",   Rm = 1034, Rp02 = 896,  Rp02_Rm_ratio = 0.87, ElongationMin = 12, Material = "Medium carbon alloy steel, Q&T" },
            new BoltStrengthClass { Class = "SAE 8.2", Rm = 1034, Rp02 = 896,  Rp02_Rm_ratio = 0.87, ElongationMin = 10, Material = "Low carbon martensite steel" },
            new BoltStrengthClass { Class = "ASTM A325", Rm = 827, Rp02 = 634, Rp02_Rm_ratio = 0.77, ElongationMin = 14, Material = "Structural bolt, Type 1" },
            new BoltStrengthClass { Class = "ASTM A490", Rm = 1034, Rp02 = 896, Rp02_Rm_ratio = 0.87, ElongationMin = 14, Material = "Structural bolt, alloy steel" },
        };
    }
}
