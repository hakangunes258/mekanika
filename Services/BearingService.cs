using System.Text.Json.Serialization;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// The bearing library: the built-in catalogue data below, plus any bearings the
/// signed-in user has added (loaded by <see cref="CustomLibraryService"/>).
///
/// The four public lists are merged views — built-ins first, customs appended. The
/// order matters: <c>BallBearing.razor</c> and <c>RollerBearing.razor</c> bind their
/// selects by *index* into these lists, so a built-in changing position would
/// silently repoint a selection at a different bearing.
/// </summary>
public static class BearingService
{
    public const string TypeDeepGroove = "Deep Groove Ball";
    public const string TypeCylindrical = "Cylindrical Roller";
    public const string TypeTapered = "Tapered Roller";
    public const string TypeAngular = "Angular Contact Ball";

    // Deep Groove Ball Bearings (NACHI catalog) - Updated from official NACHI catalog
    // Contact angle α = 0° for deep groove ball bearings
    // Data source: Nachi-Deep-Groove Ball Bearings.pdf
    // f0 is calculated from ISO 76 Table 1 based on Dw*cos(α)/Dpw
    private static readonly List<Bearing> _deepGrooveBuiltIn = new()
    {
        // 6800 series - Extra-extra light series (Bore: 10-25mm)
        new Bearing { Designation = "6800", Bore = 10, Outer = 19, Width = 5, C = 2.12, C0 = 0.985, f0 = 14.2, LimitSpeed = 37000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6801", Bore = 12, Outer = 21, Width = 5, C = 2.08, C0 = 1.26, f0 = 15.8, LimitSpeed = 32000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6802", Bore = 15, Outer = 24, Width = 5, C = 2.63, C0 = 1.57, f0 = 16.1, LimitSpeed = 28000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6803", Bore = 17, Outer = 26, Width = 5, C = 4.00, C0 = 2.64, f0 = 15.5, LimitSpeed = 26000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6804", Bore = 20, Outer = 32, Width = 7, C = 4.30, C0 = 2.94, f0 = 16.0, LimitSpeed = 22000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6805", Bore = 25, Outer = 37, Width = 7, C = 5.35, C0 = 3.80, f0 = 16.4, LimitSpeed = 18000, Type = "Deep Groove Ball" },

        // 6900 series - Extra light series (Bore: 10-25mm)
        new Bearing { Designation = "6900", Bore = 10, Outer = 22, Width = 6, C = 2.49, C0 = 1.13, f0 = 14.0, LimitSpeed = 33000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6901", Bore = 12, Outer = 24, Width = 6, C = 2.70, C0 = 1.32, f0 = 14.5, LimitSpeed = 30000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6902", Bore = 15, Outer = 28, Width = 7, C = 4.30, C0 = 2.25, f0 = 14.3, LimitSpeed = 26000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6903", Bore = 17, Outer = 30, Width = 7, C = 4.60, C0 = 2.55, f0 = 14.7, LimitSpeed = 24000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6904", Bore = 20, Outer = 37, Width = 9, C = 6.35, C0 = 3.70, f0 = 14.8, LimitSpeed = 19000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6905", Bore = 25, Outer = 42, Width = 9, C = 7.00, C0 = 4.50, f0 = 15.3, LimitSpeed = 16000, Type = "Deep Groove Ball" },

        // 6000 series - Extra light series (Bore: 10-50mm)
        new Bearing { Designation = "6000", Bore = 10, Outer = 26, Width = 8, C = 4.55, C0 = 1.97, f0 = 12.4, LimitSpeed = 30000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6001", Bore = 12, Outer = 28, Width = 8, C = 5.10, C0 = 2.39, f0 = 13.2, LimitSpeed = 28000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6002", Bore = 15, Outer = 32, Width = 9, C = 5.60, C0 = 2.84, f0 = 13.9, LimitSpeed = 24000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6003", Bore = 17, Outer = 35, Width = 10, C = 6.00, C0 = 3.25, f0 = 14.3, LimitSpeed = 22000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6004", Bore = 20, Outer = 42, Width = 12, C = 9.40, C0 = 5.00, f0 = 13.9, LimitSpeed = 18000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6005", Bore = 25, Outer = 47, Width = 12, C = 10.1, C0 = 5.85, f0 = 14.5, LimitSpeed = 15000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6006", Bore = 30, Outer = 55, Width = 13, C = 13.2, C0 = 8.30, f0 = 14.8, LimitSpeed = 13000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6007", Bore = 35, Outer = 62, Width = 14, C = 16.0, C0 = 10.3, f0 = 14.8, LimitSpeed = 12000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6008", Bore = 40, Outer = 68, Width = 15, C = 16.8, C0 = 11.5, f0 = 15.3, LimitSpeed = 10000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6009", Bore = 45, Outer = 75, Width = 16, C = 20.9, C0 = 15.2, f0 = 15.3, LimitSpeed = 9200, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6010", Bore = 50, Outer = 80, Width = 16, C = 21.8, C0 = 16.6, f0 = 15.6, LimitSpeed = 8500, Type = "Deep Groove Ball" },

        // 6200 series - Light series (Bore: 10-80mm)
        new Bearing { Designation = "6200", Bore = 10, Outer = 30, Width = 9, C = 5.10, C0 = 2.39, f0 = 13.2, LimitSpeed = 25000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6201", Bore = 12, Outer = 32, Width = 10, C = 6.80, C0 = 3.05, f0 = 12.3, LimitSpeed = 22000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6202", Bore = 15, Outer = 35, Width = 11, C = 7.65, C0 = 3.70, f0 = 13.1, LimitSpeed = 20000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6203", Bore = 17, Outer = 40, Width = 12, C = 9.55, C0 = 4.80, f0 = 13.1, LimitSpeed = 18000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6204", Bore = 20, Outer = 47, Width = 14, C = 12.8, C0 = 6.60, f0 = 13.1, LimitSpeed = 16000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6205", Bore = 25, Outer = 52, Width = 15, C = 14.0, C0 = 7.90, f0 = 13.9, LimitSpeed = 13000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6206", Bore = 30, Outer = 62, Width = 16, C = 19.5, C0 = 11.3, f0 = 13.9, LimitSpeed = 11000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6207", Bore = 35, Outer = 72, Width = 17, C = 25.7, C0 = 15.3, f0 = 13.8, LimitSpeed = 9800, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6208", Bore = 40, Outer = 80, Width = 18, C = 29.1, C0 = 17.9, f0 = 14.0, LimitSpeed = 8700, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6209", Bore = 45, Outer = 85, Width = 19, C = 32.5, C0 = 20.5, f0 = 14.1, LimitSpeed = 7800, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6210", Bore = 50, Outer = 90, Width = 20, C = 35.0, C0 = 23.2, f0 = 14.4, LimitSpeed = 7100, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6211", Bore = 55, Outer = 100, Width = 21, C = 43.5, C0 = 29.3, f0 = 14.4, LimitSpeed = 6400, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6212", Bore = 60, Outer = 110, Width = 22, C = 52.5, C0 = 36.0, f0 = 14.3, LimitSpeed = 6000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6213", Bore = 65, Outer = 120, Width = 23, C = 57.0, C0 = 40.0, f0 = 14.4, LimitSpeed = 5500, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6214", Bore = 70, Outer = 125, Width = 24, C = 62.0, C0 = 44.0, f0 = 14.4, LimitSpeed = 5100, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6215", Bore = 75, Outer = 130, Width = 25, C = 66.0, C0 = 49.5, f0 = 14.7, LimitSpeed = 4800, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6216", Bore = 80, Outer = 140, Width = 26, C = 72.5, C0 = 53.0, f0 = 14.6, LimitSpeed = 4500, Type = "Deep Groove Ball" },

        // 62/22, 62/28, 62/32 series
        new Bearing { Designation = "62/22", Bore = 22, Outer = 50, Width = 14, C = 13.9, C0 = 6.95, f0 = 13.1, LimitSpeed = 14000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "62/28", Bore = 28, Outer = 58, Width = 16, C = 17.9, C0 = 9.75, f0 = 13.1, LimitSpeed = 12000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "62/32", Bore = 32, Outer = 65, Width = 17, C = 22.4, C0 = 13.1, f0 = 13.6, LimitSpeed = 11000, Type = "Deep Groove Ball" },

        // 6300 series - Medium series (Bore: 10-80mm)
        new Bearing { Designation = "6300", Bore = 10, Outer = 35, Width = 11, C = 8.10, C0 = 3.45, f0 = 11.3, LimitSpeed = 23000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6301", Bore = 12, Outer = 37, Width = 12, C = 9.75, C0 = 4.25, f0 = 11.2, LimitSpeed = 20000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6302", Bore = 15, Outer = 42, Width = 13, C = 11.4, C0 = 5.40, f0 = 12.3, LimitSpeed = 17000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6303", Bore = 17, Outer = 47, Width = 14, C = 13.6, C0 = 6.55, f0 = 12.3, LimitSpeed = 16000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6304", Bore = 20, Outer = 52, Width = 15, C = 15.9, C0 = 7.90, f0 = 12.4, LimitSpeed = 14000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6305", Bore = 25, Outer = 62, Width = 17, C = 23.6, C0 = 12.1, f0 = 12.2, LimitSpeed = 12000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6306", Bore = 30, Outer = 72, Width = 19, C = 26.7, C0 = 15.0, f0 = 13.2, LimitSpeed = 10000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6307", Bore = 35, Outer = 80, Width = 21, C = 33.5, C0 = 19.2, f0 = 13.2, LimitSpeed = 8800, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6308", Bore = 40, Outer = 90, Width = 23, C = 40.5, C0 = 24.1, f0 = 13.2, LimitSpeed = 7800, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6309", Bore = 45, Outer = 100, Width = 25, C = 53.0, C0 = 32.0, f0 = 13.1, LimitSpeed = 7000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6310", Bore = 50, Outer = 110, Width = 27, C = 62.0, C0 = 38.0, f0 = 13.1, LimitSpeed = 6400, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6311", Bore = 55, Outer = 120, Width = 29, C = 71.5, C0 = 44.5, f0 = 13.1, LimitSpeed = 5800, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6312", Bore = 60, Outer = 130, Width = 31, C = 82.0, C0 = 52.0, f0 = 13.2, LimitSpeed = 5400, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6313", Bore = 65, Outer = 140, Width = 33, C = 92.5, C0 = 59.5, f0 = 13.2, LimitSpeed = 4900, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6314", Bore = 70, Outer = 150, Width = 35, C = 104.0, C0 = 68.0, f0 = 13.2, LimitSpeed = 4600, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6315", Bore = 75, Outer = 160, Width = 37, C = 113.0, C0 = 77.0, f0 = 13.2, LimitSpeed = 4300, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6316", Bore = 80, Outer = 170, Width = 39, C = 123.0, C0 = 86.5, f0 = 13.2, LimitSpeed = 4000, Type = "Deep Groove Ball" },

        // 63/22, 63/28, 63/32 series
        new Bearing { Designation = "63/22", Bore = 22, Outer = 56, Width = 16, C = 18.4, C0 = 9.25, f0 = 12.4, LimitSpeed = 13000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "63/28", Bore = 28, Outer = 68, Width = 18, C = 26.8, C0 = 14.0, f0 = 12.4, LimitSpeed = 11000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "63/32", Bore = 32, Outer = 75, Width = 20, C = 30.0, C0 = 16.2, f0 = 12.7, LimitSpeed = 9500, Type = "Deep Groove Ball" },

        // 6800 series extended (Bore: 30-50mm)
        new Bearing { Designation = "6806", Bore = 30, Outer = 42, Width = 7, C = 5.35, C0 = 3.80, f0 = 16.4, LimitSpeed = 15000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6807", Bore = 35, Outer = 47, Width = 7, C = 4.75, C0 = 3.80, f0 = 16.4, LimitSpeed = 14000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6808", Bore = 40, Outer = 52, Width = 7, C = 5.95, C0 = 4.90, f0 = 16.2, LimitSpeed = 12000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6809", Bore = 45, Outer = 58, Width = 7, C = 5.35, C0 = 4.90, f0 = 16.1, LimitSpeed = 11000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6810", Bore = 50, Outer = 65, Width = 7, C = 6.40, C0 = 5.80, f0 = 16.1, LimitSpeed = 10000, Type = "Deep Groove Ball" },

        // 6900 series extended (Bore: 30-50mm)
        new Bearing { Designation = "6906", Bore = 30, Outer = 47, Width = 9, C = 9.95, C0 = 6.55, f0 = 15.4, LimitSpeed = 13000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6907", Bore = 35, Outer = 55, Width = 10, C = 10.4, C0 = 7.15, f0 = 15.6, LimitSpeed = 12000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6908", Bore = 40, Outer = 62, Width = 12, C = 13.7, C0 = 9.95, f0 = 15.8, LimitSpeed = 11000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6909", Bore = 45, Outer = 68, Width = 12, C = 14.1, C0 = 10.9, f0 = 16.1, LimitSpeed = 10000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6910", Bore = 50, Outer = 72, Width = 12, C = 14.5, C0 = 13.5, f0 = 16.2, LimitSpeed = 9500, Type = "Deep Groove Ball" },

        // 6000 series extended (Bore: 55-80mm)
        new Bearing { Designation = "6011", Bore = 55, Outer = 90, Width = 18, C = 28.3, C0 = 21.3, f0 = 15.4, LimitSpeed = 7700, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6012", Bore = 60, Outer = 95, Width = 18, C = 29.4, C0 = 23.2, f0 = 15.5, LimitSpeed = 7100, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6013", Bore = 65, Outer = 100, Width = 18, C = 30.5, C0 = 25.2, f0 = 15.7, LimitSpeed = 6700, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6014", Bore = 70, Outer = 110, Width = 20, C = 38.0, C0 = 31.0, f0 = 15.6, LimitSpeed = 6100, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6015", Bore = 75, Outer = 115, Width = 20, C = 39.5, C0 = 33.5, f0 = 15.7, LimitSpeed = 5700, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6016", Bore = 80, Outer = 125, Width = 22, C = 47.5, C0 = 43.0, f0 = 15.6, LimitSpeed = 5300, Type = "Deep Groove Ball" },

        // 6200 series extended (Bore: 85-110mm)
        new Bearing { Designation = "6217", Bore = 85, Outer = 150, Width = 28, C = 84.0, C0 = 62.0, f0 = 14.5, LimitSpeed = 4300, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6218", Bore = 90, Outer = 160, Width = 30, C = 96.0, C0 = 71.5, f0 = 14.5, LimitSpeed = 4000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6219", Bore = 95, Outer = 170, Width = 32, C = 109.0, C0 = 81.5, f0 = 14.4, LimitSpeed = 3800, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6220", Bore = 100, Outer = 180, Width = 34, C = 122.0, C0 = 93.0, f0 = 14.4, LimitSpeed = 3600, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6221", Bore = 105, Outer = 190, Width = 36, C = 133.0, C0 = 104.0, f0 = 14.3, LimitSpeed = 3400, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6222", Bore = 110, Outer = 200, Width = 38, C = 144.0, C0 = 117.0, f0 = 14.3, LimitSpeed = 3200, Type = "Deep Groove Ball" },

        // 6300 series extended (Bore: 85-110mm)
        new Bearing { Designation = "6317", Bore = 85, Outer = 180, Width = 41, C = 133.0, C0 = 96.5, f0 = 13.3, LimitSpeed = 3800, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6318", Bore = 90, Outer = 190, Width = 43, C = 143.0, C0 = 107.0, f0 = 13.3, LimitSpeed = 3600, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6319", Bore = 95, Outer = 200, Width = 45, C = 153.0, C0 = 118.0, f0 = 13.3, LimitSpeed = 3300, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6320", Bore = 100, Outer = 215, Width = 47, C = 173.0, C0 = 141.0, f0 = 13.2, LimitSpeed = 3200, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6321", Bore = 105, Outer = 225, Width = 49, C = 184.0, C0 = 153.0, f0 = 13.2, LimitSpeed = 3000, Type = "Deep Groove Ball" },
        new Bearing { Designation = "6322", Bore = 110, Outer = 240, Width = 50, C = 205.0, C0 = 179.0, f0 = 13.1, LimitSpeed = 2900, Type = "Deep Groove Ball" },
    };

    // Cylindrical Roller Bearings (NACHI catalog) - Updated from official NACHI catalog
    // Contact angle α = 0° for cylindrical roller bearings (pure radial load)
    // Data source: Nachi-Cylindrical Roller Bearings.pdf
    // f0 = 0 for roller bearings (axial load not supported by basic NU design)
    private static readonly List<Bearing> _cylindricalBuiltIn = new()
    {
        // NU2 series - Light series (Bore: 20-110mm)
        new Bearing { Designation = "NU203", Bore = 17, Outer = 40, Width = 12, C = 12.6, C0 = 7.95, f0 = 0, LimitSpeed = 16000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU204", Bore = 20, Outer = 47, Width = 14, C = 15.4, C0 = 12.7, f0 = 0, LimitSpeed = 15000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU204E", Bore = 20, Outer = 47, Width = 14, C = 25.7, C0 = 22.6, f0 = 0, LimitSpeed = 13000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU205", Bore = 25, Outer = 52, Width = 15, C = 17.7, C0 = 15.7, f0 = 0, LimitSpeed = 13000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU205E", Bore = 25, Outer = 52, Width = 15, C = 29.3, C0 = 27.7, f0 = 0, LimitSpeed = 12000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU206", Bore = 30, Outer = 62, Width = 16, C = 23.5, C0 = 21.5, f0 = 0, LimitSpeed = 11000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU206E", Bore = 30, Outer = 62, Width = 16, C = 39.0, C0 = 37.5, f0 = 0, LimitSpeed = 9500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU207", Bore = 35, Outer = 72, Width = 17, C = 33.5, C0 = 31.5, f0 = 0, LimitSpeed = 9500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU207E", Bore = 35, Outer = 72, Width = 17, C = 50.5, C0 = 50.0, f0 = 0, LimitSpeed = 8500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU208", Bore = 40, Outer = 80, Width = 18, C = 43.5, C0 = 43.0, f0 = 0, LimitSpeed = 8500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU208E", Bore = 40, Outer = 80, Width = 18, C = 55.5, C0 = 55.5, f0 = 0, LimitSpeed = 9500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU209", Bore = 45, Outer = 85, Width = 19, C = 46.0, C0 = 47.0, f0 = 0, LimitSpeed = 7500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU209E", Bore = 45, Outer = 85, Width = 19, C = 63.0, C0 = 66.5, f0 = 0, LimitSpeed = 7000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU210", Bore = 50, Outer = 90, Width = 20, C = 48.0, C0 = 51.0, f0 = 0, LimitSpeed = 7100, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU210E", Bore = 50, Outer = 90, Width = 20, C = 69.0, C0 = 76.5, f0 = 0, LimitSpeed = 6400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU211", Bore = 55, Outer = 100, Width = 21, C = 58.0, C0 = 62.5, f0 = 0, LimitSpeed = 6300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU211E", Bore = 55, Outer = 100, Width = 21, C = 86.5, C0 = 98.5, f0 = 0, LimitSpeed = 5800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU212", Bore = 60, Outer = 110, Width = 22, C = 68.5, C0 = 75.0, f0 = 0, LimitSpeed = 6000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU212E", Bore = 60, Outer = 110, Width = 22, C = 97.5, C0 = 107.0, f0 = 0, LimitSpeed = 5300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU213", Bore = 65, Outer = 120, Width = 23, C = 84.0, C0 = 94.5, f0 = 0, LimitSpeed = 5300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU213E", Bore = 65, Outer = 120, Width = 23, C = 108.0, C0 = 119.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU214", Bore = 70, Outer = 125, Width = 24, C = 83.5, C0 = 95.0, f0 = 0, LimitSpeed = 5000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU214E", Bore = 70, Outer = 125, Width = 24, C = 119.0, C0 = 137.0, f0 = 0, LimitSpeed = 4600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU215", Bore = 75, Outer = 130, Width = 25, C = 96.5, C0 = 111.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU215E", Bore = 75, Outer = 130, Width = 25, C = 130.0, C0 = 156.0, f0 = 0, LimitSpeed = 4300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU216", Bore = 80, Outer = 140, Width = 26, C = 106.0, C0 = 122.0, f0 = 0, LimitSpeed = 4500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU216E", Bore = 80, Outer = 140, Width = 26, C = 139.0, C0 = 167.0, f0 = 0, LimitSpeed = 4000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU217", Bore = 85, Outer = 150, Width = 28, C = 120.0, C0 = 140.0, f0 = 0, LimitSpeed = 4300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU217E", Bore = 85, Outer = 150, Width = 28, C = 167.0, C0 = 199.0, f0 = 0, LimitSpeed = 3800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU218", Bore = 90, Outer = 160, Width = 30, C = 152.0, C0 = 178.0, f0 = 0, LimitSpeed = 4000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU218E", Bore = 90, Outer = 160, Width = 30, C = 182.0, C0 = 217.0, f0 = 0, LimitSpeed = 3600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU219", Bore = 95, Outer = 170, Width = 32, C = 165.0, C0 = 195.0, f0 = 0, LimitSpeed = 3800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU219E", Bore = 95, Outer = 170, Width = 32, C = 222.0, C0 = 259.0, f0 = 0, LimitSpeed = 3400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU220", Bore = 100, Outer = 180, Width = 34, C = 183.0, C0 = 217.0, f0 = 0, LimitSpeed = 3600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU220E", Bore = 100, Outer = 180, Width = 34, C = 250.0, C0 = 305.0, f0 = 0, LimitSpeed = 3200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU221", Bore = 105, Outer = 190, Width = 36, C = 202.0, C0 = 241.0, f0 = 0, LimitSpeed = 3400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU222", Bore = 110, Outer = 200, Width = 38, C = 240.0, C0 = 290.0, f0 = 0, LimitSpeed = 3200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU222E", Bore = 110, Outer = 200, Width = 38, C = 293.0, C0 = 365.0, f0 = 0, LimitSpeed = 2800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU224", Bore = 120, Outer = 215, Width = 40, C = 260.0, C0 = 320.0, f0 = 0, LimitSpeed = 3000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU224E", Bore = 120, Outer = 215, Width = 40, C = 335.0, C0 = 420.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU226", Bore = 130, Outer = 230, Width = 40, C = 270.0, C0 = 340.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU226E", Bore = 130, Outer = 230, Width = 40, C = 365.0, C0 = 455.0, f0 = 0, LimitSpeed = 2400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU228", Bore = 140, Outer = 250, Width = 42, C = 310.0, C0 = 420.0, f0 = 0, LimitSpeed = 2400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU228E", Bore = 140, Outer = 250, Width = 42, C = 395.0, C0 = 515.0, f0 = 0, LimitSpeed = 2200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU230", Bore = 150, Outer = 270, Width = 45, C = 375.0, C0 = 490.0, f0 = 0, LimitSpeed = 2200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU230E", Bore = 150, Outer = 270, Width = 45, C = 450.0, C0 = 595.0, f0 = 0, LimitSpeed = 2000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU232", Bore = 160, Outer = 290, Width = 48, C = 430.0, C0 = 570.0, f0 = 0, LimitSpeed = 2200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU232E", Bore = 160, Outer = 290, Width = 48, C = 500.0, C0 = 665.0, f0 = 0, LimitSpeed = 1900, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU234", Bore = 170, Outer = 310, Width = 52, C = 475.0, C0 = 635.0, f0 = 0, LimitSpeed = 2000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU234E", Bore = 170, Outer = 310, Width = 52, C = 605.0, C0 = 800.0, f0 = 0, LimitSpeed = 1900, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU236", Bore = 180, Outer = 320, Width = 52, C = 495.0, C0 = 675.0, f0 = 0, LimitSpeed = 1900, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU236E", Bore = 180, Outer = 320, Width = 52, C = 625.0, C0 = 850.0, f0 = 0, LimitSpeed = 1800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU238", Bore = 190, Outer = 340, Width = 55, C = 555.0, C0 = 770.0, f0 = 0, LimitSpeed = 1800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU238E", Bore = 190, Outer = 340, Width = 55, C = 695.0, C0 = 955.0, f0 = 0, LimitSpeed = 1700, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU240", Bore = 200, Outer = 360, Width = 58, C = 620.0, C0 = 865.0, f0 = 0, LimitSpeed = 1500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU240E", Bore = 200, Outer = 360, Width = 58, C = 765.0, C0 = 1060.0, f0 = 0, LimitSpeed = 1600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU244", Bore = 220, Outer = 400, Width = 65, C = 760.0, C0 = 1080.0, f0 = 0, LimitSpeed = 1500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU248", Bore = 240, Outer = 440, Width = 72, C = 935.0, C0 = 1340.0, f0 = 0, LimitSpeed = 1400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU252", Bore = 260, Outer = 480, Width = 80, C = 1140.0, C0 = 1660.0, f0 = 0, LimitSpeed = 1300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU256", Bore = 280, Outer = 500, Width = 80, C = 1140.0, C0 = 1680.0, f0 = 0, LimitSpeed = 1200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU260", Bore = 300, Outer = 540, Width = 85, C = 1400.0, C0 = 2070.0, f0 = 0, LimitSpeed = 1100, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU264", Bore = 320, Outer = 580, Width = 92, C = 1600.0, C0 = 2390.0, f0 = 0, LimitSpeed = 1000, Type = "Cylindrical Roller" },

        // NU22 series - Heavy series (Bore: 20-110mm)
        new Bearing { Designation = "NU2204", Bore = 20, Outer = 47, Width = 18, C = 20.7, C0 = 18.4, f0 = 0, LimitSpeed = 14000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2204E", Bore = 20, Outer = 47, Width = 18, C = 30.5, C0 = 28.3, f0 = 0, LimitSpeed = 13000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2205", Bore = 25, Outer = 52, Width = 18, C = 24.3, C0 = 23.5, f0 = 0, LimitSpeed = 12000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2205E", Bore = 25, Outer = 52, Width = 18, C = 35.0, C0 = 34.5, f0 = 0, LimitSpeed = 12000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2206", Bore = 30, Outer = 62, Width = 20, C = 33.0, C0 = 33.0, f0 = 0, LimitSpeed = 10000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2206E", Bore = 30, Outer = 62, Width = 20, C = 49.0, C0 = 50.0, f0 = 0, LimitSpeed = 9500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2207", Bore = 35, Outer = 72, Width = 23, C = 49.0, C0 = 51.0, f0 = 0, LimitSpeed = 8500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2207E", Bore = 35, Outer = 72, Width = 23, C = 61.5, C0 = 65.0, f0 = 0, LimitSpeed = 8500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2208", Bore = 40, Outer = 80, Width = 23, C = 58.0, C0 = 62.0, f0 = 0, LimitSpeed = 7500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2208E", Bore = 40, Outer = 80, Width = 23, C = 72.5, C0 = 77.5, f0 = 0, LimitSpeed = 7500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2209", Bore = 45, Outer = 85, Width = 23, C = 61.5, C0 = 68.0, f0 = 0, LimitSpeed = 7400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2209E", Bore = 45, Outer = 85, Width = 23, C = 76.0, C0 = 84.5, f0 = 0, LimitSpeed = 7000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2210", Bore = 50, Outer = 90, Width = 23, C = 64.0, C0 = 73.5, f0 = 0, LimitSpeed = 6500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2210E", Bore = 50, Outer = 90, Width = 23, C = 83.5, C0 = 97.0, f0 = 0, LimitSpeed = 6400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2211", Bore = 55, Outer = 100, Width = 25, C = 75.5, C0 = 87.0, f0 = 0, LimitSpeed = 6200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2211E", Bore = 55, Outer = 100, Width = 25, C = 101.0, C0 = 122.0, f0 = 0, LimitSpeed = 5800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2212", Bore = 60, Outer = 110, Width = 28, C = 96.0, C0 = 116.0, f0 = 0, LimitSpeed = 5300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2212E", Bore = 60, Outer = 110, Width = 28, C = 131.0, C0 = 157.0, f0 = 0, LimitSpeed = 5300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2213", Bore = 65, Outer = 120, Width = 31, C = 120.0, C0 = 149.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2213E", Bore = 65, Outer = 120, Width = 31, C = 149.0, C0 = 181.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2214", Bore = 70, Outer = 125, Width = 31, C = 119.0, C0 = 151.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2214E", Bore = 70, Outer = 125, Width = 31, C = 156.0, C0 = 194.0, f0 = 0, LimitSpeed = 4600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2215", Bore = 75, Outer = 130, Width = 31, C = 130.0, C0 = 162.0, f0 = 0, LimitSpeed = 4500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2215E", Bore = 75, Outer = 130, Width = 31, C = 162.0, C0 = 207.0, f0 = 0, LimitSpeed = 4300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2216", Bore = 80, Outer = 140, Width = 33, C = 147.0, C0 = 186.0, f0 = 0, LimitSpeed = 4000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2216E", Bore = 80, Outer = 140, Width = 33, C = 186.0, C0 = 243.0, f0 = 0, LimitSpeed = 4000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2217", Bore = 85, Outer = 150, Width = 36, C = 170.0, C0 = 218.0, f0 = 0, LimitSpeed = 3800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2217E", Bore = 85, Outer = 150, Width = 36, C = 217.0, C0 = 279.0, f0 = 0, LimitSpeed = 3800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2218", Bore = 90, Outer = 160, Width = 40, C = 207.0, C0 = 265.0, f0 = 0, LimitSpeed = 3600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2218E", Bore = 90, Outer = 160, Width = 40, C = 242.0, C0 = 315.0, f0 = 0, LimitSpeed = 3600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2219", Bore = 95, Outer = 170, Width = 43, C = 230.0, C0 = 298.0, f0 = 0, LimitSpeed = 3400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2219E", Bore = 95, Outer = 170, Width = 43, C = 286.0, C0 = 370.0, f0 = 0, LimitSpeed = 3400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2220", Bore = 100, Outer = 180, Width = 46, C = 257.0, C0 = 335.0, f0 = 0, LimitSpeed = 3200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2220E", Bore = 100, Outer = 180, Width = 46, C = 335.0, C0 = 445.0, f0 = 0, LimitSpeed = 3200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2222", Bore = 110, Outer = 200, Width = 53, C = 320.0, C0 = 440.0, f0 = 0, LimitSpeed = 2800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2222E", Bore = 110, Outer = 200, Width = 53, C = 385.0, C0 = 515.0, f0 = 0, LimitSpeed = 2800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2224", Bore = 120, Outer = 215, Width = 58, C = 365.0, C0 = 490.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2224E", Bore = 120, Outer = 215, Width = 58, C = 450.0, C0 = 620.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2226", Bore = 130, Outer = 230, Width = 64, C = 380.0, C0 = 530.0, f0 = 0, LimitSpeed = 2400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2226E", Bore = 130, Outer = 230, Width = 64, C = 530.0, C0 = 735.0, f0 = 0, LimitSpeed = 2400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2228", Bore = 140, Outer = 250, Width = 68, C = 465.0, C0 = 670.0, f0 = 0, LimitSpeed = 2200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2228E", Bore = 140, Outer = 250, Width = 68, C = 570.0, C0 = 835.0, f0 = 0, LimitSpeed = 2200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2230", Bore = 150, Outer = 270, Width = 73, C = 545.0, C0 = 800.0, f0 = 0, LimitSpeed = 2000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2230E", Bore = 150, Outer = 270, Width = 73, C = 660.0, C0 = 990.0, f0 = 0, LimitSpeed = 2000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2232", Bore = 160, Outer = 290, Width = 80, C = 630.0, C0 = 940.0, f0 = 0, LimitSpeed = 1900, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2232E", Bore = 160, Outer = 290, Width = 80, C = 810.0, C0 = 1190.0, f0 = 0, LimitSpeed = 1900, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2236", Bore = 180, Outer = 320, Width = 86, C = 775.0, C0 = 1210.0, f0 = 0, LimitSpeed = 1700, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2236E", Bore = 180, Outer = 320, Width = 86, C = 1010.0, C0 = 1510.0, f0 = 0, LimitSpeed = 1800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2238", Bore = 190, Outer = 340, Width = 92, C = 830.0, C0 = 1290.0, f0 = 0, LimitSpeed = 1600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2238E", Bore = 190, Outer = 340, Width = 92, C = 1100.0, C0 = 1670.0, f0 = 0, LimitSpeed = 1700, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2240", Bore = 200, Outer = 360, Width = 98, C = 925.0, C0 = 1440.0, f0 = 0, LimitSpeed = 1500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2240E", Bore = 200, Outer = 360, Width = 98, C = 1220.0, C0 = 1870.0, f0 = 0, LimitSpeed = 1500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2244", Bore = 220, Outer = 400, Width = 108, C = 1140.0, C0 = 1810.0, f0 = 0, LimitSpeed = 1400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2248", Bore = 240, Outer = 440, Width = 120, C = 1440.0, C0 = 2320.0, f0 = 0, LimitSpeed = 1300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2252", Bore = 260, Outer = 480, Width = 130, C = 1780.0, C0 = 2930.0, f0 = 0, LimitSpeed = 1100, Type = "Cylindrical Roller" },

        // NU3 series - Medium series (Bore: 20-110mm)
        new Bearing { Designation = "NU304", Bore = 20, Outer = 52, Width = 15, C = 21.4, C0 = 17.3, f0 = 0, LimitSpeed = 12000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU304E", Bore = 20, Outer = 52, Width = 15, C = 31.5, C0 = 26.9, f0 = 0, LimitSpeed = 12000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU305", Bore = 25, Outer = 62, Width = 17, C = 29.3, C0 = 25.2, f0 = 0, LimitSpeed = 10000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU305E", Bore = 25, Outer = 62, Width = 17, C = 41.5, C0 = 37.5, f0 = 0, LimitSpeed = 10000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU306", Bore = 30, Outer = 72, Width = 19, C = 38.5, C0 = 35.0, f0 = 0, LimitSpeed = 8500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU306E", Bore = 30, Outer = 72, Width = 19, C = 53.0, C0 = 50.0, f0 = 0, LimitSpeed = 8500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU307", Bore = 35, Outer = 80, Width = 21, C = 49.5, C0 = 47.0, f0 = 0, LimitSpeed = 8000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU307E", Bore = 35, Outer = 80, Width = 21, C = 66.5, C0 = 65.5, f0 = 0, LimitSpeed = 7500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU308", Bore = 40, Outer = 90, Width = 23, C = 58.5, C0 = 57.0, f0 = 0, LimitSpeed = 6700, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU308E", Bore = 40, Outer = 90, Width = 23, C = 83.0, C0 = 81.5, f0 = 0, LimitSpeed = 6700, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU309", Bore = 45, Outer = 100, Width = 25, C = 78.5, C0 = 77.5, f0 = 0, LimitSpeed = 6300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU309E", Bore = 45, Outer = 100, Width = 25, C = 97.5, C0 = 98.5, f0 = 0, LimitSpeed = 6000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU310", Bore = 50, Outer = 110, Width = 27, C = 87.0, C0 = 86.0, f0 = 0, LimitSpeed = 5600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU310E", Bore = 50, Outer = 110, Width = 27, C = 110.0, C0 = 113.0, f0 = 0, LimitSpeed = 5400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU311", Bore = 55, Outer = 120, Width = 29, C = 111.0, C0 = 111.0, f0 = 0, LimitSpeed = 5000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU311E", Bore = 55, Outer = 120, Width = 29, C = 137.0, C0 = 143.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU312", Bore = 60, Outer = 130, Width = 31, C = 124.0, C0 = 126.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU312E", Bore = 60, Outer = 130, Width = 31, C = 150.0, C0 = 157.0, f0 = 0, LimitSpeed = 4300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU313", Bore = 65, Outer = 140, Width = 33, C = 135.0, C0 = 139.0, f0 = 0, LimitSpeed = 4500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU313E", Bore = 65, Outer = 140, Width = 33, C = 181.0, C0 = 191.0, f0 = 0, LimitSpeed = 4000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU314", Bore = 70, Outer = 150, Width = 35, C = 158.0, C0 = 220.0, f0 = 0, LimitSpeed = 4000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU314E", Bore = 70, Outer = 150, Width = 35, C = 205.0, C0 = 222.0, f0 = 0, LimitSpeed = 3600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU315", Bore = 75, Outer = 160, Width = 37, C = 190.0, C0 = 205.0, f0 = 0, LimitSpeed = 3800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU315E", Bore = 75, Outer = 160, Width = 37, C = 240.0, C0 = 263.0, f0 = 0, LimitSpeed = 3400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU316", Bore = 80, Outer = 170, Width = 39, C = 190.0, C0 = 207.0, f0 = 0, LimitSpeed = 3600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU316E", Bore = 80, Outer = 170, Width = 39, C = 256.0, C0 = 282.0, f0 = 0, LimitSpeed = 3200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU317", Bore = 85, Outer = 180, Width = 41, C = 224.0, C0 = 247.0, f0 = 0, LimitSpeed = 3400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU317E", Bore = 85, Outer = 180, Width = 41, C = 291.0, C0 = 330.0, f0 = 0, LimitSpeed = 3000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU318", Bore = 90, Outer = 190, Width = 43, C = 240.0, C0 = 265.0, f0 = 0, LimitSpeed = 3200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU318E", Bore = 90, Outer = 190, Width = 43, C = 335.0, C0 = 380.0, f0 = 0, LimitSpeed = 2800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU319", Bore = 95, Outer = 200, Width = 45, C = 259.0, C0 = 289.0, f0 = 0, LimitSpeed = 3000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU319E", Bore = 95, Outer = 200, Width = 45, C = 335.0, C0 = 385.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU320", Bore = 100, Outer = 215, Width = 47, C = 300.0, C0 = 335.0, f0 = 0, LimitSpeed = 2800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU320E", Bore = 100, Outer = 215, Width = 47, C = 380.0, C0 = 425.0, f0 = 0, LimitSpeed = 2400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU321", Bore = 105, Outer = 225, Width = 49, C = 340.0, C0 = 385.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU322", Bore = 110, Outer = 240, Width = 50, C = 380.0, C0 = 435.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU322E", Bore = 110, Outer = 240, Width = 50, C = 450.0, C0 = 525.0, f0 = 0, LimitSpeed = 2200, Type = "Cylindrical Roller" },

        // NU23 series - Heavy series (Bore: 20-140mm)
        new Bearing { Designation = "NU2304", Bore = 20, Outer = 52, Width = 21, C = 30.5, C0 = 27.2, f0 = 0, LimitSpeed = 11000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2304E", Bore = 20, Outer = 52, Width = 21, C = 42.0, C0 = 39.0, f0 = 0, LimitSpeed = 11000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2305", Bore = 25, Outer = 62, Width = 24, C = 42.5, C0 = 41.0, f0 = 0, LimitSpeed = 9300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2305E", Bore = 25, Outer = 62, Width = 24, C = 57.0, C0 = 56.0, f0 = 0, LimitSpeed = 9000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2306", Bore = 30, Outer = 72, Width = 27, C = 51.5, C0 = 51.0, f0 = 0, LimitSpeed = 8200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2306E", Bore = 30, Outer = 72, Width = 27, C = 74.5, C0 = 77.5, f0 = 0, LimitSpeed = 8000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2307", Bore = 35, Outer = 80, Width = 31, C = 60.5, C0 = 60.0, f0 = 0, LimitSpeed = 7200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2307E", Bore = 35, Outer = 80, Width = 31, C = 99.0, C0 = 109.0, f0 = 0, LimitSpeed = 6800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2308", Bore = 40, Outer = 90, Width = 33, C = 82.5, C0 = 88.0, f0 = 0, LimitSpeed = 6500, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2308E", Bore = 40, Outer = 90, Width = 33, C = 114.0, C0 = 122.0, f0 = 0, LimitSpeed = 6400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2309", Bore = 45, Outer = 100, Width = 36, C = 99.0, C0 = 104.0, f0 = 0, LimitSpeed = 6100, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2309E", Bore = 45, Outer = 100, Width = 36, C = 137.0, C0 = 153.0, f0 = 0, LimitSpeed = 6000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2310", Bore = 50, Outer = 110, Width = 40, C = 121.0, C0 = 131.0, f0 = 0, LimitSpeed = 5400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2310E", Bore = 50, Outer = 110, Width = 40, C = 163.0, C0 = 187.0, f0 = 0, LimitSpeed = 5400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2311", Bore = 55, Outer = 120, Width = 43, C = 148.0, C0 = 162.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2311E", Bore = 55, Outer = 120, Width = 43, C = 201.0, C0 = 233.0, f0 = 0, LimitSpeed = 4800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2312", Bore = 60, Outer = 130, Width = 46, C = 169.0, C0 = 188.0, f0 = 0, LimitSpeed = 4300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2312E", Bore = 60, Outer = 130, Width = 46, C = 222.0, C0 = 262.0, f0 = 0, LimitSpeed = 4300, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2313", Bore = 65, Outer = 140, Width = 48, C = 188.0, C0 = 212.0, f0 = 0, LimitSpeed = 4000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2313E", Bore = 65, Outer = 140, Width = 48, C = 247.0, C0 = 287.0, f0 = 0, LimitSpeed = 3800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2314", Bore = 70, Outer = 150, Width = 51, C = 223.0, C0 = 262.0, f0 = 0, LimitSpeed = 3800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2314E", Bore = 70, Outer = 150, Width = 51, C = 274.0, C0 = 325.0, f0 = 0, LimitSpeed = 3600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2315", Bore = 75, Outer = 160, Width = 55, C = 258.0, C0 = 300.0, f0 = 0, LimitSpeed = 3400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2315E", Bore = 75, Outer = 160, Width = 55, C = 330.0, C0 = 395.0, f0 = 0, LimitSpeed = 3400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2316", Bore = 80, Outer = 170, Width = 58, C = 274.0, C0 = 330.0, f0 = 0, LimitSpeed = 3200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2316E", Bore = 80, Outer = 170, Width = 58, C = 355.0, C0 = 430.0, f0 = 0, LimitSpeed = 3200, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2317", Bore = 85, Outer = 180, Width = 60, C = 315.0, C0 = 380.0, f0 = 0, LimitSpeed = 3000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2317E", Bore = 85, Outer = 180, Width = 60, C = 390.0, C0 = 485.0, f0 = 0, LimitSpeed = 3000, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2318", Bore = 90, Outer = 190, Width = 64, C = 325.0, C0 = 395.0, f0 = 0, LimitSpeed = 2800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2318E", Bore = 90, Outer = 190, Width = 64, C = 435.0, C0 = 535.0, f0 = 0, LimitSpeed = 2800, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2319", Bore = 95, Outer = 200, Width = 67, C = 370.0, C0 = 460.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2319E", Bore = 95, Outer = 200, Width = 67, C = 460.0, C0 = 585.0, f0 = 0, LimitSpeed = 2600, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2320", Bore = 100, Outer = 215, Width = 73, C = 435.0, C0 = 545.0, f0 = 0, LimitSpeed = 2400, Type = "Cylindrical Roller" },
        new Bearing { Designation = "NU2320E", Bore = 100, Outer = 215, Width = 73, C = 570.0, C0 = 715.0, f0 = 0, LimitSpeed = 2400, Type = "Cylindrical Roller" }
    };

    // Tapered Roller Bearings (NACHI catalog) - Updated from official NACHI catalog
    // Contact angle α typically 10-30 degrees
    // Data source: Nachi-Tapered Roller Bearings.pdf
    private static readonly List<TaperedBearing> _taperedBuiltIn = new()
    {
        // H-E302 series - Light series (Bore: 20-50mm)
        new TaperedBearing { Designation = "H-E30204", Bore = 20, Outer = 47, Width = 15.25, C = 27.0, C0 = 27.2, e = 0.37, Y = 1.60, Y0 = 0.88, LimitSpeed = 12000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30205", Bore = 25, Outer = 52, Width = 16.25, C = 31.5, C0 = 33.7, e = 0.37, Y = 1.60, Y0 = 0.88, LimitSpeed = 10000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30206", Bore = 30, Outer = 62, Width = 17.25, C = 41.5, C0 = 44.8, e = 0.37, Y = 1.60, Y0 = 0.88, LimitSpeed = 8700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30207", Bore = 35, Outer = 72, Width = 18.25, C = 55.1, C0 = 60.9, e = 0.37, Y = 1.60, Y0 = 0.88, LimitSpeed = 7400, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30208", Bore = 40, Outer = 80, Width = 19.75, C = 62.9, C0 = 69.2, e = 0.37, Y = 1.60, Y0 = 0.88, LimitSpeed = 6700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30209", Bore = 45, Outer = 85, Width = 20.75, C = 67.2, C0 = 77.4, e = 0.40, Y = 1.48, Y0 = 0.81, LimitSpeed = 6100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30210", Bore = 50, Outer = 90, Width = 21.75, C = 76.5, C0 = 91.7, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 5700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30211", Bore = 55, Outer = 100, Width = 22.75, C = 94.6, C0 = 113.0, e = 0.40, Y = 1.48, Y0 = 0.81, LimitSpeed = 5200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30212", Bore = 60, Outer = 110, Width = 23.75, C = 106.0, C0 = 127.0, e = 0.40, Y = 1.48, Y0 = 0.81, LimitSpeed = 4700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30213", Bore = 65, Outer = 120, Width = 24.75, C = 128.0, C0 = 156.0, e = 0.40, Y = 1.48, Y0 = 0.81, LimitSpeed = 4300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30214", Bore = 70, Outer = 125, Width = 26.25, C = 138.0, C0 = 173.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 4100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30215", Bore = 75, Outer = 130, Width = 27.25, C = 142.0, C0 = 181.0, e = 0.44, Y = 1.38, Y0 = 0.76, LimitSpeed = 3900, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30216", Bore = 80, Outer = 140, Width = 28.25, C = 161.0, C0 = 202.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 3600, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30217", Bore = 85, Outer = 150, Width = 30.5, C = 182.0, C0 = 231.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 3400, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30218", Bore = 90, Outer = 160, Width = 32.5, C = 204.0, C0 = 261.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 3200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30219", Bore = 95, Outer = 170, Width = 34.5, C = 231.0, C0 = 299.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 3000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30220", Bore = 100, Outer = 180, Width = 37.0, C = 258.0, C0 = 338.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 2800, Type = "Tapered Roller" },

        // E30230-E30260 series (Bore: 150-320mm)
        new TaperedBearing { Designation = "E30230", Bore = 150, Outer = 270, Width = 49.0, C = 466.0, C0 = 625.0, e = 0.43, Y = 1.39, Y0 = 0.77, LimitSpeed = 1800, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30232", Bore = 160, Outer = 290, Width = 52.0, C = 483.0, C0 = 637.0, e = 0.46, Y = 1.31, Y0 = 0.72, LimitSpeed = 1600, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30234", Bore = 170, Outer = 310, Width = 57.0, C = 544.0, C0 = 726.0, e = 0.46, Y = 1.31, Y0 = 0.72, LimitSpeed = 1500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30236", Bore = 180, Outer = 320, Width = 57.0, C = 615.0, C0 = 870.0, e = 0.45, Y = 1.33, Y0 = 0.73, LimitSpeed = 1400, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30256", Bore = 280, Outer = 500, Width = 89.0, C = 1260.0, C0 = 1920.0, e = 0.42, Y = 1.44, Y0 = 0.79, LimitSpeed = 810, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30260", Bore = 300, Outer = 540, Width = 96.0, C = 1510.0, C0 = 2360.0, e = 0.42, Y = 1.44, Y0 = 0.79, LimitSpeed = 730, Type = "Tapered Roller" },

        // H-E303 series - Medium series (Bore: 20-90mm)
        new TaperedBearing { Designation = "H-E30304", Bore = 20, Outer = 52, Width = 16.25, C = 36.4, C0 = 35.2, e = 0.30, Y = 2.00, Y0 = 1.10, LimitSpeed = 11000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30305", Bore = 25, Outer = 62, Width = 18.25, C = 48.2, C0 = 46.9, e = 0.30, Y = 2.00, Y0 = 1.10, LimitSpeed = 9000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30306", Bore = 30, Outer = 72, Width = 20.75, C = 59.6, C0 = 60.1, e = 0.31, Y = 1.90, Y0 = 1.05, LimitSpeed = 7700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30307", Bore = 35, Outer = 80, Width = 22.75, C = 76.2, C0 = 78.9, e = 0.31, Y = 1.90, Y0 = 1.05, LimitSpeed = 6900, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E30308", Bore = 40, Outer = 90, Width = 25.25, C = 90.6, C0 = 101.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 6100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30309", Bore = 45, Outer = 100, Width = 27.25, C = 113.0, C0 = 128.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 5400, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30310", Bore = 50, Outer = 110, Width = 29.25, C = 137.0, C0 = 152.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 4900, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30311", Bore = 55, Outer = 120, Width = 31.5, C = 149.0, C0 = 170.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 4500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30312", Bore = 60, Outer = 130, Width = 33.5, C = 173.0, C0 = 201.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 4100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30313", Bore = 65, Outer = 140, Width = 36.0, C = 204.0, C0 = 239.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 3800, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30314", Bore = 70, Outer = 150, Width = 38.0, C = 230.0, C0 = 273.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 3500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30315", Bore = 75, Outer = 160, Width = 40.0, C = 250.0, C0 = 297.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 3300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30316", Bore = 80, Outer = 170, Width = 42.5, C = 294.0, C0 = 355.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 3100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30317", Bore = 85, Outer = 180, Width = 44.5, C = 305.0, C0 = 367.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 2900, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E30318", Bore = 90, Outer = 190, Width = 46.5, C = 336.0, C0 = 407.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 2700, Type = "Tapered Roller" },

        // 30320-30348 series (Bore: 100-240mm)
        new TaperedBearing { Designation = "30320", Bore = 100, Outer = 215, Width = 51.5, C = 344.0, C0 = 400.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 2400, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30321", Bore = 105, Outer = 225, Width = 53.5, C = 371.0, C0 = 432.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 2300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30322", Bore = 110, Outer = 240, Width = 54.5, C = 481.0, C0 = 590.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 2100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30324", Bore = 120, Outer = 260, Width = 59.5, C = 505.0, C0 = 611.0, e = 0.35, Y = 1.73, Y0 = 0.96, LimitSpeed = 2000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30326", Bore = 130, Outer = 280, Width = 63.75, C = 563.0, C0 = 684.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1800, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30328", Bore = 140, Outer = 300, Width = 67.75, C = 626.0, C0 = 761.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30330", Bore = 150, Outer = 320, Width = 72.0, C = 717.0, C0 = 962.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30332", Bore = 160, Outer = 340, Width = 75.0, C = 793.0, C0 = 981.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1400, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30334", Bore = 170, Outer = 360, Width = 80.0, C = 828.0, C0 = 1020.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30336", Bore = 180, Outer = 380, Width = 83.0, C = 901.0, C0 = 1110.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30338", Bore = 190, Outer = 400, Width = 86.0, C = 1010.0, C0 = 1250.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30340", Bore = 200, Outer = 420, Width = 89.0, C = 1120.0, C0 = 1450.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 1100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30344", Bore = 220, Outer = 460, Width = 97.0, C = 1260.0, C0 = 1680.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 980, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "30348", Bore = 240, Outer = 500, Width = 105.0, C = 1520.0, C0 = 2100.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 890, Type = "Tapered Roller" },

        // H-E322 series - Heavy series (Bore: 20-50mm)
        new TaperedBearing { Designation = "H-E32204", Bore = 20, Outer = 47, Width = 19.25, C = 32.5, C0 = 34.8, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 12000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32205", Bore = 25, Outer = 52, Width = 19.25, C = 39.8, C0 = 44.8, e = 0.36, Y = 1.67, Y0 = 0.92, LimitSpeed = 11000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32206", Bore = 30, Outer = 62, Width = 21.25, C = 50.7, C0 = 57.9, e = 0.37, Y = 1.60, Y0 = 0.88, LimitSpeed = 8700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32207", Bore = 35, Outer = 72, Width = 24.25, C = 69.6, C0 = 82.4, e = 0.37, Y = 1.60, Y0 = 0.88, LimitSpeed = 7500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32208", Bore = 40, Outer = 80, Width = 24.75, C = 77.7, C0 = 90.8, e = 0.37, Y = 1.60, Y0 = 0.88, LimitSpeed = 6600, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32209", Bore = 45, Outer = 85, Width = 24.75, C = 78.3, C0 = 94.1, e = 0.40, Y = 1.48, Y0 = 0.81, LimitSpeed = 6100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32210", Bore = 50, Outer = 90, Width = 24.75, C = 85.0, C0 = 105.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 5700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32211", Bore = 55, Outer = 100, Width = 26.75, C = 107.0, C0 = 133.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 5200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32212", Bore = 60, Outer = 110, Width = 29.75, C = 132.0, C0 = 167.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 4700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32213", Bore = 65, Outer = 120, Width = 32.75, C = 169.0, C0 = 225.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 4300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32214", Bore = 70, Outer = 125, Width = 33.25, C = 169.0, C0 = 225.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 4100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32215", Bore = 75, Outer = 130, Width = 33.25, C = 174.0, C0 = 234.0, e = 0.44, Y = 1.38, Y0 = 0.76, LimitSpeed = 3900, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32216", Bore = 80, Outer = 140, Width = 35.25, C = 203.0, C0 = 271.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 3600, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32217", Bore = 85, Outer = 150, Width = 38.5, C = 232.0, C0 = 315.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 3300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32218", Bore = 90, Outer = 160, Width = 42.5, C = 263.0, C0 = 362.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 3200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32219", Bore = 95, Outer = 170, Width = 45.5, C = 311.0, C0 = 439.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 3000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32220", Bore = 100, Outer = 180, Width = 49.0, C = 347.0, C0 = 495.0, e = 0.42, Y = 1.43, Y0 = 0.79, LimitSpeed = 2800, Type = "Tapered Roller" },

        // E32230-E32260 series (Bore: 150-320mm)
        new TaperedBearing { Designation = "E32230", Bore = 150, Outer = 270, Width = 77.0, C = 704.0, C0 = 1070.0, e = 0.44, Y = 1.38, Y0 = 0.76, LimitSpeed = 1800, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32232", Bore = 160, Outer = 290, Width = 84.0, C = 795.0, C0 = 1210.0, e = 0.44, Y = 1.38, Y0 = 0.76, LimitSpeed = 1700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32234", Bore = 170, Outer = 310, Width = 91.0, C = 1000.0, C0 = 1610.0, e = 0.44, Y = 1.38, Y0 = 0.76, LimitSpeed = 1500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32236", Bore = 180, Outer = 320, Width = 91.0, C = 957.0, C0 = 1520.0, e = 0.45, Y = 1.33, Y0 = 0.73, LimitSpeed = 1500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32248", Bore = 240, Outer = 440, Width = 127.0, C = 1830.0, C0 = 3010.0, e = 0.44, Y = 1.38, Y0 = 0.76, LimitSpeed = 980, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32252", Bore = 260, Outer = 480, Width = 137.0, C = 1760.0, C0 = 2870.0, e = 0.43, Y = 1.39, Y0 = 0.77, LimitSpeed = 880, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32256", Bore = 280, Outer = 500, Width = 137.0, C = 1860.0, C0 = 3150.0, e = 0.43, Y = 1.39, Y0 = 0.77, LimitSpeed = 810, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32260", Bore = 300, Outer = 540, Width = 149.0, C = 2310.0, C0 = 4060.0, e = 0.47, Y = 1.27, Y0 = 0.70, LimitSpeed = 780, Type = "Tapered Roller" },

        // 32304-32318 series (Bore: 20-90mm)
        new TaperedBearing { Designation = "H-32303", Bore = 17, Outer = 47, Width = 20.25, C = 31.9, C0 = 29.9, e = 0.29, Y = 1.97, Y0 = 1.08, LimitSpeed = 13000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32304", Bore = 20, Outer = 52, Width = 22.25, C = 45.1, C0 = 46.7, e = 0.30, Y = 2.00, Y0 = 1.10, LimitSpeed = 11000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32305", Bore = 25, Outer = 62, Width = 25.25, C = 61.2, C0 = 64.1, e = 0.30, Y = 2.00, Y0 = 1.10, LimitSpeed = 9100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32306", Bore = 30, Outer = 72, Width = 28.75, C = 82.2, C0 = 91.6, e = 0.31, Y = 1.90, Y0 = 1.05, LimitSpeed = 7900, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32307", Bore = 35, Outer = 80, Width = 32.75, C = 101.0, C0 = 114.0, e = 0.31, Y = 1.90, Y0 = 1.05, LimitSpeed = 7000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "H-E32308", Bore = 40, Outer = 90, Width = 35.25, C = 116.0, C0 = 139.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 6200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32309", Bore = 45, Outer = 100, Width = 38.25, C = 146.0, C0 = 180.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 5500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32310", Bore = 50, Outer = 110, Width = 42.25, C = 176.0, C0 = 220.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 5000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32311", Bore = 55, Outer = 120, Width = 45.5, C = 200.0, C0 = 250.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 4500, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32312", Bore = 60, Outer = 130, Width = 48.5, C = 221.0, C0 = 275.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 4200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32313", Bore = 65, Outer = 140, Width = 51.0, C = 276.0, C0 = 357.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 3900, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32314", Bore = 70, Outer = 150, Width = 54.0, C = 317.0, C0 = 414.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 3600, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32315", Bore = 75, Outer = 160, Width = 58.0, C = 363.0, C0 = 481.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 3300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32316", Bore = 80, Outer = 170, Width = 61.5, C = 378.0, C0 = 497.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 3100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32317", Bore = 85, Outer = 180, Width = 63.5, C = 439.0, C0 = 587.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 3000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32318", Bore = 90, Outer = 190, Width = 67.5, C = 461.0, C0 = 614.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 2800, Type = "Tapered Roller" },

        // 32320-32348 series (Bore: 100-240mm)
        new TaperedBearing { Designation = "32320", Bore = 100, Outer = 215, Width = 77.5, C = 491.0, C0 = 637.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 2400, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32321", Bore = 105, Outer = 225, Width = 81.5, C = 635.0, C0 = 886.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 2300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32322", Bore = 110, Outer = 240, Width = 84.5, C = 607.0, C0 = 796.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 2200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32324", Bore = 120, Outer = 260, Width = 90.5, C = 800.0, C0 = 1110.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 2000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32326", Bore = 130, Outer = 280, Width = 98.75, C = 852.0, C0 = 1160.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1800, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32328", Bore = 140, Outer = 300, Width = 107.75, C = 958.0, C0 = 1320.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1700, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "E32330", Bore = 150, Outer = 320, Width = 114.0, C = 1240.0, C0 = 1790.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1600, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32332", Bore = 160, Outer = 340, Width = 121.0, C = 1220.0, C0 = 1720.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1400, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32334", Bore = 170, Outer = 360, Width = 127.0, C = 1310.0, C0 = 1830.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32336", Bore = 180, Outer = 380, Width = 134.0, C = 1410.0, C0 = 1980.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1300, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32338", Bore = 190, Outer = 400, Width = 140.0, C = 1550.0, C0 = 2190.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1200, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32340", Bore = 200, Outer = 420, Width = 146.0, C = 1790.0, C0 = 2580.0, e = 0.35, Y = 1.74, Y0 = 0.96, LimitSpeed = 1100, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32344", Bore = 220, Outer = 460, Width = 154.0, C = 2100.0, C0 = 3170.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 1000, Type = "Tapered Roller" },
        new TaperedBearing { Designation = "32348", Bore = 240, Outer = 500, Width = 165.0, C = 2510.0, C0 = 3870.0, e = 0.35, Y = 1.73, Y0 = 0.95, LimitSpeed = 890, Type = "Tapered Roller" },
    };

    /// <summary>
    /// ISO 281 Table 3 - X, Y, e values for radial contact ball bearings
    /// Based on "relative axial load" = f0 * Fa / C0
    /// </summary>
    public static readonly List<(double f0FaC0, double e, double Y)> ISO281_XY_Table = new()
    {
        (0.172, 0.19, 2.30),
        (0.345, 0.22, 1.99),
        (0.689, 0.26, 1.71),
        (1.03,  0.28, 1.55),
        (1.38,  0.30, 1.45),
        (2.07,  0.34, 1.31),
        (3.45,  0.38, 1.15),
        (5.17,  0.42, 1.04),
        (6.89,  0.44, 1.00)
    };

    /// <summary>
    /// Calculate e and Y from ISO 281 Table 3 using linear interpolation
    /// Input: f0 * Fa / C0 (relative axial load)
    /// </summary>
    public static (double e, double Y) GetEandY_ISO281(double f0FaC0)
    {
        if (f0FaC0 <= 0) 
            return (0.19, 2.30);

        // Below first value
        if (f0FaC0 <= ISO281_XY_Table[0].f0FaC0)
            return (ISO281_XY_Table[0].e, ISO281_XY_Table[0].Y);

        // Above last value
        if (f0FaC0 >= ISO281_XY_Table[^1].f0FaC0)
            return (ISO281_XY_Table[^1].e, ISO281_XY_Table[^1].Y);

        // Find bracket and interpolate
        for (int i = 0; i < ISO281_XY_Table.Count - 1; i++)
        {
            var lower = ISO281_XY_Table[i];
            var upper = ISO281_XY_Table[i + 1];

            if (f0FaC0 >= lower.f0FaC0 && f0FaC0 <= upper.f0FaC0)
            {
                // Linear interpolation
                double ratio = (f0FaC0 - lower.f0FaC0) / (upper.f0FaC0 - lower.f0FaC0);
                double e = lower.e + ratio * (upper.e - lower.e);
                double Y = lower.Y + ratio * (upper.Y - lower.Y);
                return (e, Y);
            }
        }

        return (0.44, 1.00);
    }

    /// <summary>
    /// Calculate X, Y, e for deep groove ball bearings per ISO 281
    /// </summary>
    public static (double X, double Y, double e, double f0FaC0) CalculateXYFactors(double Fa, double Fr, double C0, double f0)
    {
        if (Fr <= 0 || C0 <= 0 || f0 <= 0)
            return (1.0, 0, 0, 0);

        // Step 1: Calculate relative axial load = f0 * Fa / C0
        double f0FaC0 = f0 * Fa / C0;
        
        // Step 2: Get e and Y from ISO 281 Table 3 (with interpolation)
        var (e, Y) = GetEandY_ISO281(f0FaC0);
        
        // Step 3: Calculate Fa/Fr
        double faFr = Fa / Fr;
        
        // Step 4: Determine X and Y based on Fa/Fr vs e
        if (faFr <= e)
        {
            // Fa/Fr ≤ e: X = 1, Y = 0
            return (1.0, 0, e, f0FaC0);
        }
        else
        {
            // Fa/Fr > e: X = 0.56, Y from table
            return (0.56, Y, e, f0FaC0);
        }
    }

    // Reliability factors (a1) based on ISO 281
    public static readonly Dictionary<int, double> ReliabilityFactors = new()
    {
        { 90, 1.00 },
        { 95, 0.64 },
        { 96, 0.55 },
        { 97, 0.47 },
        { 98, 0.37 },
        { 99, 0.25 }
    };

    // Machine factors
    public static readonly List<(string Value, double Factor, string Description)> MachineFactors = new()
    {
        ("1.0", 1.0, "Smooth operation"),
        ("1.2", 1.2, "Light shock"),
        ("1.5", 1.5, "Moderate shock"),
        ("2.0", 2.0, "Heavy shock"),
        ("2.5", 2.5, "Very heavy shock")
    };

    // Angular Contact Ball Bearings - ISO 15 / ISO 281
    // Contact angles: 15° (C suffix), 25° (AC suffix), 30° (A suffix), 40° (B suffix)
    // X and Y factors based on ISO 281:2007
    // Data source: ISO standards and bearing catalogs
    private static readonly List<AngularContactBearing> _angularBuiltIn = new()
    {
        // 7200 series - Light series, 40° contact angle (suffix B)
        // Data source: NACHI Angular Contact Ball Bearings Catalog
        // 40° contact angle: e=1.14, X=0.35, Y=0.57, Y0=0.52
        new AngularContactBearing { Designation = "7200B", Bore = 10, Outer = 30, Width = 9, C = 5.15, C0 = 2.57, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 19000, LimitSpeedOil = 28000 },
        new AngularContactBearing { Designation = "7201B", Bore = 12, Outer = 32, Width = 10, C = 7.20, C0 = 3.80, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 17000, LimitSpeedOil = 24000 },
        new AngularContactBearing { Designation = "7202B", Bore = 15, Outer = 35, Width = 11, C = 8.60, C0 = 4.50, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 16000, LimitSpeedOil = 21000 },
        new AngularContactBearing { Designation = "7203B", Bore = 17, Outer = 40, Width = 12, C = 11.3, C0 = 6.30, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 14000, LimitSpeedOil = 19000 },
        new AngularContactBearing { Designation = "7204B", Bore = 20, Outer = 47, Width = 14, C = 13.7, C0 = 7.85, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 11000, LimitSpeedOil = 16000 },
        new AngularContactBearing { Designation = "7205B", Bore = 25, Outer = 52, Width = 15, C = 15.3, C0 = 9.70, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 9500, LimitSpeedOil = 14000 },
        new AngularContactBearing { Designation = "7206B", Bore = 30, Outer = 62, Width = 16, C = 21.2, C0 = 13.9, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 8500, LimitSpeedOil = 12000 },
        new AngularContactBearing { Designation = "7207B", Bore = 35, Outer = 72, Width = 17, C = 28.0, C0 = 19.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 7500, LimitSpeedOil = 10000 },
        new AngularContactBearing { Designation = "7208B", Bore = 40, Outer = 80, Width = 18, C = 33.0, C0 = 23.7, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 6700, LimitSpeedOil = 9200 },
        new AngularContactBearing { Designation = "7209B", Bore = 45, Outer = 85, Width = 19, C = 37.0, C0 = 27.1, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 6300, LimitSpeedOil = 8500 },
        new AngularContactBearing { Designation = "7210B", Bore = 50, Outer = 90, Width = 20, C = 38.5, C0 = 29.6, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 5600, LimitSpeedOil = 7500 },
        new AngularContactBearing { Designation = "7211B", Bore = 55, Outer = 100, Width = 21, C = 48.0, C0 = 37.5, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 5300, LimitSpeedOil = 7000 },
        new AngularContactBearing { Designation = "7212B", Bore = 60, Outer = 110, Width = 22, C = 58.0, C0 = 46.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 4800, LimitSpeedOil = 6300 },
        new AngularContactBearing { Designation = "7213B", Bore = 65, Outer = 120, Width = 23, C = 66.0, C0 = 54.5, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 4300, LimitSpeedOil = 5600 },
        new AngularContactBearing { Designation = "7214B", Bore = 70, Outer = 125, Width = 24, C = 71.5, C0 = 59.5, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 4300, LimitSpeedOil = 5600 },
        new AngularContactBearing { Designation = "7215B", Bore = 75, Outer = 130, Width = 25, C = 74.0, C0 = 64.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 4000, LimitSpeedOil = 5300 },
        new AngularContactBearing { Designation = "7216B", Bore = 80, Outer = 140, Width = 26, C = 83.5, C0 = 71.5, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 3600, LimitSpeedOil = 5000 },
        new AngularContactBearing { Designation = "7217B", Bore = 85, Outer = 150, Width = 28, C = 96.5, C0 = 83.5, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 3400, LimitSpeedOil = 4500 },
        new AngularContactBearing { Designation = "7218B", Bore = 90, Outer = 160, Width = 30, C = 110.0, C0 = 97.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 3200, LimitSpeedOil = 4300 },
        new AngularContactBearing { Designation = "7219B", Bore = 95, Outer = 170, Width = 32, C = 120.0, C0 = 105.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 3000, LimitSpeedOil = 4000 },
        new AngularContactBearing { Designation = "7220B", Bore = 100, Outer = 180, Width = 34, C = 135.0, C0 = 118.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.52, LimitSpeed = 2800, LimitSpeedOil = 3700 },

        // 7200 series - 30° contact angle (suffix A - standard)
        // Data source: NACHI Angular Contact Ball Bearings Catalog
        // 30° contact angle: e=0.80, X=0.39, Y=0.76, Y0=0.66
        new AngularContactBearing { Designation = "7200", Bore = 10, Outer = 30, Width = 9, C = 5.40, C0 = 2.71, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 22000, LimitSpeedOil = 30000 },
        new AngularContactBearing { Designation = "7201", Bore = 12, Outer = 32, Width = 10, C = 7.60, C0 = 3.96, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 20000, LimitSpeedOil = 27000 },
        new AngularContactBearing { Designation = "7202", Bore = 15, Outer = 35, Width = 11, C = 9.05, C0 = 4.70, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 17000, LimitSpeedOil = 23000 },
        new AngularContactBearing { Designation = "7203", Bore = 17, Outer = 40, Width = 12, C = 11.9, C0 = 6.60, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 16000, LimitSpeedOil = 21000 },
        new AngularContactBearing { Designation = "7204", Bore = 20, Outer = 47, Width = 14, C = 14.5, C0 = 8.30, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 13000, LimitSpeedOil = 18000 },
        new AngularContactBearing { Designation = "7205", Bore = 25, Outer = 52, Width = 15, C = 16.2, C0 = 10.2, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 12000, LimitSpeedOil = 15000 },
        new AngularContactBearing { Designation = "7206", Bore = 30, Outer = 62, Width = 16, C = 22.5, C0 = 14.8, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 10000, LimitSpeedOil = 13000 },
        new AngularContactBearing { Designation = "7207", Bore = 35, Outer = 72, Width = 17, C = 29.7, C0 = 20.0, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 8500, LimitSpeedOil = 11000 },
        new AngularContactBearing { Designation = "7208", Bore = 40, Outer = 80, Width = 18, C = 35.0, C0 = 25.2, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 7500, LimitSpeedOil = 10000 },
        new AngularContactBearing { Designation = "7209", Bore = 45, Outer = 85, Width = 19, C = 39.5, C0 = 28.8, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 7000, LimitSpeedOil = 9200 },
        new AngularContactBearing { Designation = "7210", Bore = 50, Outer = 90, Width = 20, C = 41.0, C0 = 31.5, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 6300, LimitSpeedOil = 8500 },
        new AngularContactBearing { Designation = "7211", Bore = 55, Outer = 100, Width = 21, C = 51.0, C0 = 39.5, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 6000, LimitSpeedOil = 7500 },
        new AngularContactBearing { Designation = "7212", Bore = 60, Outer = 110, Width = 22, C = 62.0, C0 = 48.5, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 5300, LimitSpeedOil = 7100 },
        new AngularContactBearing { Designation = "7213", Bore = 65, Outer = 120, Width = 23, C = 70.0, C0 = 57.8, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 4900, LimitSpeedOil = 6500 },
        new AngularContactBearing { Designation = "7214", Bore = 70, Outer = 125, Width = 24, C = 76.5, C0 = 63.5, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 4500, LimitSpeedOil = 6300 },
        new AngularContactBearing { Designation = "7215", Bore = 75, Outer = 130, Width = 25, C = 79.0, C0 = 68.5, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 4300, LimitSpeedOil = 5800 },
        new AngularContactBearing { Designation = "7216", Bore = 80, Outer = 140, Width = 26, C = 89.0, C0 = 76.5, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 4000, LimitSpeedOil = 5400 },
        new AngularContactBearing { Designation = "7217", Bore = 85, Outer = 150, Width = 28, C = 103.0, C0 = 89.5, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 3800, LimitSpeedOil = 5000 },
        new AngularContactBearing { Designation = "7218", Bore = 90, Outer = 160, Width = 30, C = 118.0, C0 = 103.0, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 3500, LimitSpeedOil = 4700 },
        new AngularContactBearing { Designation = "7219", Bore = 95, Outer = 170, Width = 32, C = 126.0, C0 = 112.0, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 3400, LimitSpeedOil = 4500 },
        new AngularContactBearing { Designation = "7220", Bore = 100, Outer = 180, Width = 34, C = 144.0, C0 = 126.0, ContactAngle = 30, e = 0.80, X = 0.39, Y = 0.76, Y0 = 0.66, LimitSpeed = 3200, LimitSpeedOil = 4200 },

        // 7300 series - Medium series, 40° contact angle (suffix B or BE)
        new AngularContactBearing { Designation = "7300B", Bore = 10, Outer = 35, Width = 11, C = 8.20, C0 = 4.30, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 26000, LimitSpeedOil = 34000 },
        new AngularContactBearing { Designation = "7301B", Bore = 12, Outer = 37, Width = 12, C = 9.75, C0 = 5.30, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 22000, LimitSpeedOil = 30000 },
        new AngularContactBearing { Designation = "7302B", Bore = 15, Outer = 42, Width = 13, C = 11.9, C0 = 6.55, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 20000, LimitSpeedOil = 26000 },
        new AngularContactBearing { Designation = "7303B", Bore = 17, Outer = 47, Width = 14, C = 14.6, C0 = 8.30, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 18000, LimitSpeedOil = 24000 },
        new AngularContactBearing { Designation = "7304B", Bore = 20, Outer = 52, Width = 15, C = 17.2, C0 = 10.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 16000, LimitSpeedOil = 20000 },
        new AngularContactBearing { Designation = "7305B", Bore = 25, Outer = 62, Width = 17, C = 24.0, C0 = 14.3, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 13000, LimitSpeedOil = 17000 },
        new AngularContactBearing { Designation = "7306B", Bore = 30, Outer = 72, Width = 19, C = 31.0, C0 = 19.3, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 11000, LimitSpeedOil = 14000 },
        new AngularContactBearing { Designation = "7307B", Bore = 35, Outer = 80, Width = 21, C = 38.0, C0 = 24.5, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 9500, LimitSpeedOil = 13000 },
        new AngularContactBearing { Designation = "7308B", Bore = 40, Outer = 90, Width = 23, C = 48.0, C0 = 32.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 8500, LimitSpeedOil = 11000 },
        new AngularContactBearing { Designation = "7309B", Bore = 45, Outer = 100, Width = 25, C = 58.5, C0 = 40.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 7500, LimitSpeedOil = 10000 },
        new AngularContactBearing { Designation = "7310B", Bore = 50, Outer = 110, Width = 27, C = 71.0, C0 = 50.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 6700, LimitSpeedOil = 9000 },
        new AngularContactBearing { Designation = "7311B", Bore = 55, Outer = 120, Width = 29, C = 82.0, C0 = 60.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 6000, LimitSpeedOil = 8000 },
        new AngularContactBearing { Designation = "7312B", Bore = 60, Outer = 130, Width = 31, C = 95.0, C0 = 71.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 5600, LimitSpeedOil = 7500 },
        new AngularContactBearing { Designation = "7313B", Bore = 65, Outer = 140, Width = 33, C = 108.0, C0 = 83.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 5000, LimitSpeedOil = 6700 },
        new AngularContactBearing { Designation = "7314B", Bore = 70, Outer = 150, Width = 35, C = 122.0, C0 = 96.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 4800, LimitSpeedOil = 6300 },
        new AngularContactBearing { Designation = "7315B", Bore = 75, Outer = 160, Width = 37, C = 137.0, C0 = 110.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 4500, LimitSpeedOil = 6000 },
        new AngularContactBearing { Designation = "7316B", Bore = 80, Outer = 170, Width = 39, C = 153.0, C0 = 125.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 4300, LimitSpeedOil = 5600 },
        new AngularContactBearing { Designation = "7317B", Bore = 85, Outer = 180, Width = 41, C = 170.0, C0 = 143.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 4000, LimitSpeedOil = 5300 },
        new AngularContactBearing { Designation = "7318B", Bore = 90, Outer = 190, Width = 43, C = 186.0, C0 = 160.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 3800, LimitSpeedOil = 5000 },
        new AngularContactBearing { Designation = "7319B", Bore = 95, Outer = 200, Width = 45, C = 200.0, C0 = 176.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 3600, LimitSpeedOil = 4800 },
        new AngularContactBearing { Designation = "7320B", Bore = 100, Outer = 215, Width = 47, C = 228.0, C0 = 204.0, ContactAngle = 40, e = 1.14, X = 0.35, Y = 0.57, Y0 = 0.76, LimitSpeed = 3400, LimitSpeedOil = 4500 },

        // 7300 series - 30° contact angle (suffix A or AW)
        new AngularContactBearing { Designation = "7300A", Bore = 10, Outer = 35, Width = 11, C = 9.00, C0 = 5.00, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 24000, LimitSpeedOil = 32000 },
        new AngularContactBearing { Designation = "7301A", Bore = 12, Outer = 37, Width = 12, C = 10.6, C0 = 6.10, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 20000, LimitSpeedOil = 28000 },
        new AngularContactBearing { Designation = "7302A", Bore = 15, Outer = 42, Width = 13, C = 13.0, C0 = 7.65, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 18000, LimitSpeedOil = 24000 },
        new AngularContactBearing { Designation = "7303A", Bore = 17, Outer = 47, Width = 14, C = 15.9, C0 = 9.65, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 16000, LimitSpeedOil = 22000 },
        new AngularContactBearing { Designation = "7304A", Bore = 20, Outer = 52, Width = 15, C = 18.6, C0 = 11.6, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 14000, LimitSpeedOil = 18000 },
        new AngularContactBearing { Designation = "7305A", Bore = 25, Outer = 62, Width = 17, C = 26.0, C0 = 16.6, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 12000, LimitSpeedOil = 15000 },
        new AngularContactBearing { Designation = "7306A", Bore = 30, Outer = 72, Width = 19, C = 34.0, C0 = 22.4, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 10000, LimitSpeedOil = 13000 },
        new AngularContactBearing { Designation = "7307A", Bore = 35, Outer = 80, Width = 21, C = 42.5, C0 = 29.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 8500, LimitSpeedOil = 11000 },
        new AngularContactBearing { Designation = "7308A", Bore = 40, Outer = 90, Width = 23, C = 53.0, C0 = 37.5, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 7500, LimitSpeedOil = 10000 },
        new AngularContactBearing { Designation = "7309A", Bore = 45, Outer = 100, Width = 25, C = 64.0, C0 = 47.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 6700, LimitSpeedOil = 9000 },
        new AngularContactBearing { Designation = "7310A", Bore = 50, Outer = 110, Width = 27, C = 78.0, C0 = 58.5, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 6000, LimitSpeedOil = 8000 },
        new AngularContactBearing { Designation = "7311A", Bore = 55, Outer = 120, Width = 29, C = 90.0, C0 = 69.5, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 5300, LimitSpeedOil = 7000 },
        new AngularContactBearing { Designation = "7312A", Bore = 60, Outer = 130, Width = 31, C = 104.0, C0 = 83.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 5000, LimitSpeedOil = 6700 },
        new AngularContactBearing { Designation = "7313A", Bore = 65, Outer = 140, Width = 33, C = 118.0, C0 = 96.5, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 4500, LimitSpeedOil = 6000 },
        new AngularContactBearing { Designation = "7314A", Bore = 70, Outer = 150, Width = 35, C = 134.0, C0 = 112.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 4300, LimitSpeedOil = 5600 },
        new AngularContactBearing { Designation = "7315A", Bore = 75, Outer = 160, Width = 37, C = 150.0, C0 = 128.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 4000, LimitSpeedOil = 5300 },
        new AngularContactBearing { Designation = "7316A", Bore = 80, Outer = 170, Width = 39, C = 166.0, C0 = 146.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 3800, LimitSpeedOil = 5000 },
        new AngularContactBearing { Designation = "7317A", Bore = 85, Outer = 180, Width = 41, C = 186.0, C0 = 166.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 3600, LimitSpeedOil = 4800 },
        new AngularContactBearing { Designation = "7318A", Bore = 90, Outer = 190, Width = 43, C = 204.0, C0 = 186.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 3400, LimitSpeedOil = 4500 },
        new AngularContactBearing { Designation = "7319A", Bore = 95, Outer = 200, Width = 45, C = 220.0, C0 = 204.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 3200, LimitSpeedOil = 4300 },
        new AngularContactBearing { Designation = "7320A", Bore = 100, Outer = 215, Width = 47, C = 250.0, C0 = 236.0, ContactAngle = 30, e = 0.68, X = 0.39, Y = 0.76, Y0 = 1.00, LimitSpeed = 3000, LimitSpeedOil = 4000 },

        // 7200C series - 15° contact angle (High Speed)
        // 15° contact angle: e=0.38, X=0.44, Y=1.18, Y0=1.63
        new AngularContactBearing { Designation = "7200C", Bore = 10, Outer = 30, Width = 9, C = 4.62, C0 = 2.36, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 34000, LimitSpeedOil = 45000 },
        new AngularContactBearing { Designation = "7201C", Bore = 12, Outer = 32, Width = 10, C = 5.53, C0 = 2.90, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 30000, LimitSpeedOil = 40000 },
        new AngularContactBearing { Designation = "7202C", Bore = 15, Outer = 35, Width = 11, C = 6.37, C0 = 3.45, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 28000, LimitSpeedOil = 36000 },
        new AngularContactBearing { Designation = "7203C", Bore = 17, Outer = 40, Width = 12, C = 8.52, C0 = 4.65, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 24000, LimitSpeedOil = 32000 },
        new AngularContactBearing { Designation = "7204C", Bore = 20, Outer = 47, Width = 14, C = 11.4, C0 = 6.30, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 22000, LimitSpeedOil = 28000 },
        new AngularContactBearing { Designation = "7205C", Bore = 25, Outer = 52, Width = 15, C = 12.7, C0 = 7.50, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 19000, LimitSpeedOil = 24000 },
        new AngularContactBearing { Designation = "7206C", Bore = 30, Outer = 62, Width = 16, C = 17.8, C0 = 10.8, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 16000, LimitSpeedOil = 20000 },
        new AngularContactBearing { Designation = "7207C", Bore = 35, Outer = 72, Width = 17, C = 23.8, C0 = 14.6, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 14000, LimitSpeedOil = 17000 },
        new AngularContactBearing { Designation = "7208C", Bore = 40, Outer = 80, Width = 18, C = 28.1, C0 = 17.6, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 12000, LimitSpeedOil = 15000 },
        new AngularContactBearing { Designation = "7209C", Bore = 45, Outer = 85, Width = 19, C = 30.7, C0 = 20.4, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 11000, LimitSpeedOil = 14000 },
        new AngularContactBearing { Designation = "7210C", Bore = 50, Outer = 90, Width = 20, C = 33.0, C0 = 22.8, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 10000, LimitSpeedOil = 12000 },
        new AngularContactBearing { Designation = "7211C", Bore = 55, Outer = 100, Width = 21, C = 40.5, C0 = 29.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 9000, LimitSpeedOil = 11000 },
        new AngularContactBearing { Designation = "7212C", Bore = 60, Outer = 110, Width = 22, C = 48.0, C0 = 35.5, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 8000, LimitSpeedOil = 10000 },
        new AngularContactBearing { Designation = "7213C", Bore = 65, Outer = 120, Width = 23, C = 52.0, C0 = 40.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 7500, LimitSpeedOil = 9500 },
        new AngularContactBearing { Designation = "7214C", Bore = 70, Outer = 125, Width = 24, C = 56.5, C0 = 44.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 7000, LimitSpeedOil = 9000 },
        new AngularContactBearing { Designation = "7215C", Bore = 75, Outer = 130, Width = 25, C = 61.0, C0 = 49.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 6700, LimitSpeedOil = 8500 },
        new AngularContactBearing { Designation = "7216C", Bore = 80, Outer = 140, Width = 26, C = 68.5, C0 = 56.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 6300, LimitSpeedOil = 8000 },
        new AngularContactBearing { Designation = "7217C", Bore = 85, Outer = 150, Width = 28, C = 78.0, C0 = 65.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 6000, LimitSpeedOil = 7500 },
        new AngularContactBearing { Designation = "7218C", Bore = 90, Outer = 160, Width = 30, C = 91.5, C0 = 78.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 5300, LimitSpeedOil = 6700 },
        new AngularContactBearing { Designation = "7219C", Bore = 95, Outer = 170, Width = 32, C = 104.0, C0 = 90.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 5000, LimitSpeedOil = 6300 },
        new AngularContactBearing { Designation = "7220C", Bore = 100, Outer = 180, Width = 34, C = 116.0, C0 = 102.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 4800, LimitSpeedOil = 6000 },

        // 7000 series - Super precision, 15° contact angle (High Speed)
        new AngularContactBearing { Designation = "7000C", Bore = 10, Outer = 26, Width = 8, C = 3.45, C0 = 1.76, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 40000, LimitSpeedOil = 53000 },
        new AngularContactBearing { Designation = "7001C", Bore = 12, Outer = 28, Width = 8, C = 3.80, C0 = 2.04, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 36000, LimitSpeedOil = 48000 },
        new AngularContactBearing { Designation = "7002C", Bore = 15, Outer = 32, Width = 9, C = 4.95, C0 = 2.75, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 32000, LimitSpeedOil = 43000 },
        new AngularContactBearing { Designation = "7003C", Bore = 17, Outer = 35, Width = 10, C = 5.85, C0 = 3.35, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 28000, LimitSpeedOil = 38000 },
        new AngularContactBearing { Designation = "7004C", Bore = 20, Outer = 42, Width = 12, C = 9.00, C0 = 5.20, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 24000, LimitSpeedOil = 32000 },
        new AngularContactBearing { Designation = "7005C", Bore = 25, Outer = 47, Width = 12, C = 9.75, C0 = 6.00, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 20000, LimitSpeedOil = 28000 },
        new AngularContactBearing { Designation = "7006C", Bore = 30, Outer = 55, Width = 13, C = 12.7, C0 = 8.15, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 18000, LimitSpeedOil = 24000 },
        new AngularContactBearing { Designation = "7007C", Bore = 35, Outer = 62, Width = 14, C = 16.0, C0 = 10.8, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 16000, LimitSpeedOil = 20000 },
        new AngularContactBearing { Designation = "7008C", Bore = 40, Outer = 68, Width = 15, C = 17.8, C0 = 12.5, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 14000, LimitSpeedOil = 18000 },
        new AngularContactBearing { Designation = "7009C", Bore = 45, Outer = 75, Width = 16, C = 20.8, C0 = 15.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 13000, LimitSpeedOil = 16000 },
        new AngularContactBearing { Designation = "7010C", Bore = 50, Outer = 80, Width = 16, C = 21.2, C0 = 16.3, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 12000, LimitSpeedOil = 15000 },
        new AngularContactBearing { Designation = "7011C", Bore = 55, Outer = 90, Width = 18, C = 29.6, C0 = 22.8, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 10000, LimitSpeedOil = 13000 },
        new AngularContactBearing { Designation = "7012C", Bore = 60, Outer = 95, Width = 18, C = 31.2, C0 = 25.5, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 9500, LimitSpeedOil = 12000 },
        new AngularContactBearing { Designation = "7013C", Bore = 65, Outer = 100, Width = 18, C = 32.5, C0 = 27.5, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 9000, LimitSpeedOil = 11000 },
        new AngularContactBearing { Designation = "7014C", Bore = 70, Outer = 110, Width = 20, C = 41.0, C0 = 35.5, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 8500, LimitSpeedOil = 10000 },
        new AngularContactBearing { Designation = "7015C", Bore = 75, Outer = 115, Width = 20, C = 43.0, C0 = 39.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 7500, LimitSpeedOil = 9500 },
        new AngularContactBearing { Designation = "7016C", Bore = 80, Outer = 125, Width = 22, C = 52.0, C0 = 48.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 7000, LimitSpeedOil = 8500 },
        new AngularContactBearing { Designation = "7017C", Bore = 85, Outer = 130, Width = 22, C = 53.0, C0 = 51.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 6700, LimitSpeedOil = 8000 },
        new AngularContactBearing { Designation = "7018C", Bore = 90, Outer = 140, Width = 24, C = 63.0, C0 = 62.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 6300, LimitSpeedOil = 7500 },
        new AngularContactBearing { Designation = "7019C", Bore = 95, Outer = 145, Width = 24, C = 63.5, C0 = 65.5, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 6000, LimitSpeedOil = 7000 },
        new AngularContactBearing { Designation = "7020C", Bore = 100, Outer = 150, Width = 24, C = 64.0, C0 = 68.0, ContactAngle = 15, e = 0.38, X = 0.44, Y = 1.18, Y0 = 1.63, LimitSpeed = 5600, LimitSpeedOil = 6700 },

        // 7000 series - 25° contact angle (Universal)
        // 25° contact angle: e=0.52, X=0.41, Y=0.87, Y0=1.17
        new AngularContactBearing { Designation = "7000AC", Bore = 10, Outer = 26, Width = 8, C = 3.75, C0 = 2.00, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 38000, LimitSpeedOil = 50000 },
        new AngularContactBearing { Designation = "7001AC", Bore = 12, Outer = 28, Width = 8, C = 4.15, C0 = 2.32, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 34000, LimitSpeedOil = 45000 },
        new AngularContactBearing { Designation = "7002AC", Bore = 15, Outer = 32, Width = 9, C = 5.40, C0 = 3.12, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 30000, LimitSpeedOil = 40000 },
        new AngularContactBearing { Designation = "7003AC", Bore = 17, Outer = 35, Width = 10, C = 6.37, C0 = 3.80, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 26000, LimitSpeedOil = 36000 },
        new AngularContactBearing { Designation = "7004AC", Bore = 20, Outer = 42, Width = 12, C = 9.75, C0 = 5.90, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 22000, LimitSpeedOil = 30000 },
        new AngularContactBearing { Designation = "7005AC", Bore = 25, Outer = 47, Width = 12, C = 10.6, C0 = 6.80, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 19000, LimitSpeedOil = 26000 },
        new AngularContactBearing { Designation = "7006AC", Bore = 30, Outer = 55, Width = 13, C = 13.8, C0 = 9.30, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 17000, LimitSpeedOil = 22000 },
        new AngularContactBearing { Designation = "7007AC", Bore = 35, Outer = 62, Width = 14, C = 17.3, C0 = 12.2, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 15000, LimitSpeedOil = 19000 },
        new AngularContactBearing { Designation = "7008AC", Bore = 40, Outer = 68, Width = 15, C = 19.3, C0 = 14.2, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 13000, LimitSpeedOil = 17000 },
        new AngularContactBearing { Designation = "7009AC", Bore = 45, Outer = 75, Width = 16, C = 22.5, C0 = 17.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 12000, LimitSpeedOil = 15000 },
        new AngularContactBearing { Designation = "7010AC", Bore = 50, Outer = 80, Width = 16, C = 23.2, C0 = 18.6, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 11000, LimitSpeedOil = 14000 },
        new AngularContactBearing { Designation = "7011AC", Bore = 55, Outer = 90, Width = 18, C = 32.0, C0 = 26.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 9500, LimitSpeedOil = 12000 },
        new AngularContactBearing { Designation = "7012AC", Bore = 60, Outer = 95, Width = 18, C = 33.5, C0 = 29.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 9000, LimitSpeedOil = 11000 },
        new AngularContactBearing { Designation = "7013AC", Bore = 65, Outer = 100, Width = 18, C = 35.5, C0 = 31.5, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 8500, LimitSpeedOil = 10000 },
        new AngularContactBearing { Designation = "7014AC", Bore = 70, Outer = 110, Width = 20, C = 44.5, C0 = 40.5, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 8000, LimitSpeedOil = 9500 },
        new AngularContactBearing { Designation = "7015AC", Bore = 75, Outer = 115, Width = 20, C = 46.5, C0 = 44.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 7000, LimitSpeedOil = 9000 },
        new AngularContactBearing { Designation = "7016AC", Bore = 80, Outer = 125, Width = 22, C = 56.0, C0 = 54.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 6700, LimitSpeedOil = 8000 },
        new AngularContactBearing { Designation = "7017AC", Bore = 85, Outer = 130, Width = 22, C = 58.0, C0 = 58.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 6300, LimitSpeedOil = 7500 },
        new AngularContactBearing { Designation = "7018AC", Bore = 90, Outer = 140, Width = 24, C = 68.5, C0 = 70.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 6000, LimitSpeedOil = 7000 },
        new AngularContactBearing { Designation = "7019AC", Bore = 95, Outer = 145, Width = 24, C = 70.0, C0 = 74.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 5600, LimitSpeedOil = 6700 },
        new AngularContactBearing { Designation = "7020AC", Bore = 100, Outer = 150, Width = 24, C = 71.0, C0 = 77.0, ContactAngle = 25, e = 0.52, X = 0.41, Y = 0.87, Y0 = 1.17, LimitSpeed = 5300, LimitSpeedOil = 6300 },
    };

    // ============ USER-ADDED BEARINGS ============

    private static List<Bearing> _customDeepGroove = new();
    private static List<Bearing> _customCylindrical = new();
    private static List<TaperedBearing> _customTapered = new();
    private static List<AngularContactBearing> _customAngular = new();

    private static List<Bearing>? _deepGrooveAll;
    private static List<Bearing>? _cylindricalAll;
    private static List<TaperedBearing>? _taperedAll;
    private static List<AngularContactBearing>? _angularAll;

    /// <summary>Built-ins then the user's own. Index-stable — see the class remarks.</summary>
    public static IReadOnlyList<Bearing> DeepGrooveBallBearings
        => _deepGrooveAll ??= _deepGrooveBuiltIn.Concat(_customDeepGroove).ToList();

    public static IReadOnlyList<Bearing> CylindricalRollerBearings
        => _cylindricalAll ??= _cylindricalBuiltIn.Concat(_customCylindrical).ToList();

    public static IReadOnlyList<TaperedBearing> TaperedRollerBearings
        => _taperedAll ??= _taperedBuiltIn.Concat(_customTapered).ToList();

    public static IReadOnlyList<AngularContactBearing> AngularContactBallBearings
        => _angularAll ??= _angularBuiltIn.Concat(_customAngular).ToList();

    /// <summary>The user's own bearings, whatever their type. Empty when signed out.</summary>
    public static IEnumerable<object> CustomBearings =>
        _customDeepGroove.Cast<object>()
            .Concat(_customCylindrical)
            .Concat(_customTapered)
            .Concat(_customAngular);

    /// <summary>
    /// Replaces the user's bearings with the ones just loaded from their account.
    /// The flat <paramref name="ballAndRoller"/> list is split by <c>Type</c>, since
    /// deep groove and cylindrical roller share one model class.
    /// </summary>
    public static void SetCustomBearings(
        IEnumerable<Bearing> ballAndRoller,
        IEnumerable<TaperedBearing> tapered,
        IEnumerable<AngularContactBearing> angular)
    {
        var split = ballAndRoller.ToList();
        _customDeepGroove = split.Where(b => b.Type == TypeDeepGroove).ToList();
        _customCylindrical = split.Where(b => b.Type == TypeCylindrical).ToList();
        _customTapered = tapered.ToList();
        _customAngular = angular.ToList();

        _deepGrooveAll = _cylindricalAll = null;
        _taperedAll = null;
        _angularAll = null;
    }

    /// <summary>
    /// True if the catalogue already ships a bearing under this designation. Checked
    /// across every family, because designation is the key saved calculations and
    /// share links resolve by — a shadowed built-in would resolve differently for
    /// different users.
    /// </summary>
    public static bool IsBuiltInDesignation(string designation)
    {
        var d = designation.Trim();
        return _deepGrooveBuiltIn.Any(b => string.Equals(b.Designation, d, StringComparison.OrdinalIgnoreCase))
            || _cylindricalBuiltIn.Any(b => string.Equals(b.Designation, d, StringComparison.OrdinalIgnoreCase))
            || _taperedBuiltIn.Any(b => string.Equals(b.Designation, d, StringComparison.OrdinalIgnoreCase))
            || _angularBuiltIn.Any(b => string.Equals(b.Designation, d, StringComparison.OrdinalIgnoreCase));
    }

    public static List<Bearing> GetAllBearings()
    {
        var all = new List<Bearing>();
        all.AddRange(DeepGrooveBallBearings);
        all.AddRange(CylindricalRollerBearings);

        // Convert TaperedBearing to Bearing for display
        foreach (var tb in TaperedRollerBearings)
        {
            all.Add(new Bearing
            {
                Designation = tb.Designation,
                Type = tb.Type,
                Bore = tb.Bore,
                Outer = tb.Outer,
                Width = tb.Width,
                C = tb.C,
                C0 = tb.C0,
                f0 = 0, // Tapered bearings use e and Y instead
                LimitSpeed = tb.LimitSpeed
            });
        }

        return all;
    }
}

public class Bearing
{
    public string Designation { get; set; } = "";
    public string Type { get; set; } = "";
    public double Bore { get; set; }        // d (mm)
    public double Outer { get; set; }       // D (mm)
    public double Width { get; set; }       // B (mm)
    public double C { get; set; }           // Dynamic load rating (kN)
    public double C0 { get; set; }          // Static load rating (kN)
    public double f0 { get; set; }          // Calculation factor from ISO 76
    public int LimitSpeed { get; set; }     // Limiting speed (rpm)

    /// <summary>Supabase library_items.id when the user added this bearing; null for catalogue data.</summary>
    [JsonIgnore] public string? CustomId { get; set; }
    [JsonIgnore] public bool IsCustom => CustomId != null;
}

public class TaperedBearing
{
    public string Designation { get; set; } = "";
    public string Type { get; set; } = "";
    public double Bore { get; set; }        // d (mm)
    public double Outer { get; set; }       // D (mm)
    public double Width { get; set; }       // T (mm) - total width
    public double C { get; set; }           // Dynamic load rating (kN)
    public double C0 { get; set; }          // Static load rating (kN)
    public double e { get; set; }           // Limit value for Fa/Fr (typically 0.3-0.4)
    public double Y { get; set; }           // Y1 - Dynamic axial load factor (for P calculation)
    public double Y0 { get; set; }          // Static axial load factor (for P0 calculation)
    public int LimitSpeed { get; set; }     // Limiting speed (rpm)

    // For clarity: Y property is actually Y1 from catalog (used in dynamic equivalent load)
    [JsonIgnore] public double Y1 => Y;

    [JsonIgnore] public string? CustomId { get; set; }
    [JsonIgnore] public bool IsCustom => CustomId != null;
}

/// <summary>
/// Angular Contact Ball Bearing data class
/// Supports contact angles: 15°, 25°, 30°, 40°
/// </summary>
public class AngularContactBearing
{
    public string Designation { get; set; } = "";
    public string Type { get; set; } = "Angular Contact Ball";
    public double Bore { get; set; }            // d (mm)
    public double Outer { get; set; }           // D (mm)
    public double Width { get; set; }           // B (mm)
    public double C { get; set; }               // Dynamic load rating (kN)
    public double C0 { get; set; }              // Static load rating (kN)
    public double ContactAngle { get; set; }    // α (degrees) - typically 15, 25, 30, or 40
    public double e { get; set; }               // Limit value for Fa/Fr
    public double X { get; set; }               // Radial load factor (when Fa/Fr > e)
    public double Y { get; set; }               // Axial load factor (when Fa/Fr > e)
    public double Y0 { get; set; }              // Static axial load factor
    public int LimitSpeed { get; set; }         // Limiting speed (rpm) - grease lubrication
    public int LimitSpeedOil { get; set; }      // Limiting speed (rpm) - oil lubrication

    // Calculated properties based on contact angle
    [JsonIgnore] public double CalculatedE => 1.5 * Math.Tan(ContactAngle * Math.PI / 180);

    [JsonIgnore] public string? CustomId { get; set; }
    [JsonIgnore] public bool IsCustom => CustomId != null;
}
