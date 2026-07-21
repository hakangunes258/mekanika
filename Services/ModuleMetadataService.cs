namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Centralized module metadata and relationships service
/// Provides module information, descriptions, keywords, and related calculator suggestions
/// </summary>
public class ModuleMetadataService
{
    private readonly Dictionary<string, ModuleInfo> _modules;

    public ModuleMetadataService()
    {
        _modules = InitializeModules();
    }

    /// <summary>
    /// Get module information by key
    /// </summary>
    /// <param name="moduleKey">Module key (e.g., "key-connection")</param>
    /// <returns>Module information or empty ModuleInfo if not found</returns>
    public ModuleInfo GetModule(string moduleKey)
    {
        return _modules.GetValueOrDefault(moduleKey) ?? new ModuleInfo();
    }

    /// <summary>
    /// Get related calculators for a given module
    /// </summary>
    /// <param name="moduleKey">Current module key</param>
    /// <param name="count">Number of related modules to return</param>
    /// <returns>List of related modules</returns>
    public List<ModuleInfo> GetRelatedModules(string moduleKey, int count = 3)
    {
        var module = GetModule(moduleKey);
        return module.RelatedModules
            .Select(key => GetModule(key))
            .Where(m => !string.IsNullOrEmpty(m.Key))
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Get all calculator modules (excludes utility pages)
    /// </summary>
    /// <returns>List of all calculator modules</returns>
    public List<ModuleInfo> GetAllModules()
    {
        return _modules.Values
            .Where(m => m.Category != "Utility" && m.Category != "Information")
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToList();
    }

    /// <summary>
    /// Initialize all module definitions
    /// </summary>
    private Dictionary<string, ModuleInfo> InitializeModules()
    {
        return new Dictionary<string, ModuleInfo>
        {
            // ========== SHAFT-HUB CONNECTIONS ==========

            ["key-connection"] = new ModuleInfo
            {
                Key = "key-connection",
                Name = "Parallel Key Calculator",
                Route = "/key-connection",
                Category = "Shaft-Hub Connections",
                Icon = "🔑",
                Description = "Design and verify parallel key connections according to DIN 6885 standard. Calculate contact pressure, shear stress, safety factors, and required key dimensions for reliable torque transmission.",
                Keywords = "key connection, keyway, DIN 6885, parallel key, shaft key, power transmission, torque transmission, key design",
                RelatedModules = new[] { "interference-fit", "taper-fit", "clamp-connection" },
                IsVerified = true,
                VerificationStandards = new[] { "DIN 6885", "ISO 773" },
                HasVideo = false
            },

            ["interference-fit"] = new ModuleInfo
            {
                Key = "interference-fit",
                Name = "Interference Fit Calculator",
                Route = "/interference-fit",
                Category = "Shaft-Hub Connections",
                Icon = "🔧",
                Description = "Calculate press fits and shrink fits according to DIN 7190. Analyze contact pressure, assembly forces, stresses, and safety factors for cylindrical interference joints.",
                Keywords = "interference fit, press fit, shrink fit, hub shaft connection, contact pressure, DIN 7190, assembly force",
                RelatedModules = new[] { "taper-fit", "key-connection", "clamp-connection" },
                IsVerified = true,
                VerificationStandards = new[] { "DIN 7190", "ISO 286" },
                HasVideo = false
            },

            ["taper-fit"] = new ModuleInfo
            {
                Key = "taper-fit",
                Name = "Taper Fit Calculator",
                Route = "/taper-fit",
                Category = "Shaft-Hub Connections",
                Icon = "📐",
                Description = "Design conical taper connections with standard ratios (1:10, 1:20, Morse). Calculate contact pressure, axial force requirements, self-locking conditions, and safety factors.",
                Keywords = "taper fit, cone connection, taper joint, morse taper, locking assembly, contact pressure, self-locking",
                RelatedModules = new[] { "interference-fit", "key-connection", "clamp-connection" },
                IsVerified = true,
                VerificationStandards = new[] { "DIN 1448", "DIN 254" },
                HasVideo = false
            },

            ["clamp-connection"] = new ModuleInfo
            {
                Key = "clamp-connection",
                Name = "Clamp Connection Calculator",
                Route = "/clamp-connection",
                Category = "Shaft-Hub Connections",
                Icon = "🔒",
                Description = "Design shaft clamping hubs for frictional torque and axial load transmission. Calculate bolt preload, contact pressure, and transmission capacity.",
                Keywords = "clamp connection, shaft clamp, clamping hub, friction connection, bolt preload, torque transmission",
                RelatedModules = new[] { "single-bolt", "interference-fit", "key-connection" },
                // Not verified: the contact model assumes a SPLIT hub (both joints
                // bolted). A single-slit collar additionally acts as a lever hinged
                // at the slot root, which is not modelled - results are optimistic
                // for that case.
                IsVerified = false,
                VerificationStandards = new[] { "DIN 703", "VDI 2230", "Roloff/Matek" },
                HasVideo = false
            },

            // ========== FASTENERS ==========

            ["single-bolt"] = new ModuleInfo
            {
                Key = "single-bolt",
                Name = "Single Bolt Calculator",
                Route = "/single-bolt",
                Category = "Fasteners",
                Icon = "🔩",
                Description = "Analyze single bolt connections under axial and shear loads according to VDI 2230. Calculate preload, clamping force, stresses, and joint safety factors.",
                Keywords = "bolt calculation, fastener, preload, clamping force, VDI 2230, bolt joint, screw connection, bolt design",
                RelatedModules = new[] { "clamp-connection", "key-connection", "boltdatabase" },
                IsVerified = true,
                VerificationStandards = new[] { "VDI 2230", "ISO 898" },
                HasVideo = false
            },

            ["bolt"] = new ModuleInfo
            {
                Key = "bolt",
                Name = "General Bolt Calculator",
                Route = "/bolt",
                Category = "Fasteners",
                Icon = "🔧",
                Description = "General purpose bolt connection calculator for simplified bolt design. Calculate thread engagement, preload, and basic stresses for common bolted joints.",
                Keywords = "bolt calculator, bolt design, thread engagement, bolt preload, fastener design, bolt sizing",
                RelatedModules = new[] { "single-bolt", "boltdatabase", "clamp-connection" },
                IsVerified = false,
                VerificationStandards = new[] { "General Reference" },
                HasVideo = false
            },

            ["boltdatabase"] = new ModuleInfo
            {
                Key = "boltdatabase",
                Name = "Bolt Database",
                Route = "/boltdatabase",
                Category = "Reference",
                Icon = "🗄️",
                Description = "Comprehensive database of metric bolt grades, dimensions, and properties. ISO 4014 hex bolts, DIN 912 socket head cap screws, and strength grades.",
                Keywords = "bolt database, metric bolts, ISO 4014, DIN 912, bolt dimensions, bolt grades, fastener properties",
                RelatedModules = new[] { "single-bolt", "materials", "clamp-connection" },
                IsVerified = true,
                VerificationStandards = new[] { "ISO 4014", "DIN 912", "ISO 898" },
                HasVideo = false
            },

            // ========== BEARINGS ==========

            ["ball-bearing"] = new ModuleInfo
            {
                Key = "ball-bearing",
                Name = "Ball Bearing Life Calculator",
                Route = "/ball-bearing",
                Category = "Bearings",
                Icon = "🔘",
                Description = "Calculate ball bearing life (L10, L10h) according to ISO 281. Supports deep groove ball bearings and angular contact ball bearings. Analyze equivalent loads, bearing selection, and operating hours.",
                Keywords = "ball bearing, bearing life, L10 life, ISO 281, deep groove bearing, angular contact bearing, bearing calculation",
                RelatedModules = new[] { "roller-bearing", "bearings", "gear-pair" },
                IsVerified = true,
                VerificationStandards = new[] { "ISO 281", "DIN 26281", "DIN 628" },
                HasVideo = false
            },

            ["roller-bearing"] = new ModuleInfo
            {
                Key = "roller-bearing",
                Name = "Roller Bearing Life Calculator",
                Route = "/roller-bearing",
                Category = "Bearings",
                Icon = "🛞",
                Description = "Calculate roller bearing life and load capacity according to ISO 281. Supports cylindrical roller bearings (radial loads) and tapered roller bearings (combined radial and axial loads).",
                Keywords = "roller bearing, cylindrical bearing, tapered roller bearing, bearing life, ISO 281, bearing calculation, combined loads",
                RelatedModules = new[] { "ball-bearing", "bearings", "gear-pair" },
                IsVerified = true,
                VerificationStandards = new[] { "ISO 281", "DIN 26281", "DIN 720" },
                HasVideo = false
            },

            ["bearings"] = new ModuleInfo
            {
                Key = "bearings",
                Name = "Bearing Database",
                Route = "/bearings",
                Category = "Reference",
                Icon = "🗄️",
                Description = "Standard rolling bearing catalog with dimensions, load ratings, and specifications. Deep groove ball bearings, cylindrical roller bearings, and more.",
                Keywords = "bearing database, bearing catalog, bearing dimensions, load ratings, bearing specifications",
                RelatedModules = new[] { "ball-bearing", "roller-bearing", "materials" },
                IsVerified = true,
                VerificationStandards = new[] { "ISO 15", "DIN 625" },
                HasVideo = false
            },

            // ========== SPRINGS ==========

            ["compression-spring"] = new ModuleInfo
            {
                Key = "compression-spring",
                Name = "Compression Spring Calculator",
                Route = "/compression-spring",
                Category = "Springs",
                Icon = "🌀",
                Description = "Design helical compression springs according to EN 13906-1. Calculate spring rate, solid height, stresses, buckling stability, and fatigue life.",
                Keywords = "compression spring, helical spring, spring design, spring rate, coil spring, buckling, EN 13906",
                RelatedModules = new[] { "extension-spring", "torsion-spring", "materials" },
                IsVerified = true,
                VerificationStandards = new[] { "EN 13906-1", "DIN 2089" },
                HasVideo = false
            },

            ["extension-spring"] = new ModuleInfo
            {
                Key = "extension-spring",
                Name = "Extension Spring Calculator",
                Route = "/extension-spring",
                Category = "Springs",
                Icon = "🔗",
                Description = "Design helical extension springs with hooks. Calculate spring rate, initial tension, hook stresses, deflection, and fatigue life per EN 13906-2.",
                Keywords = "extension spring, tension spring, spring design, spring calculation, spring rate, hook stress",
                RelatedModules = new[] { "compression-spring", "torsion-spring", "materials" },
                IsVerified = true,
                VerificationStandards = new[] { "EN 13906-2", "DIN 2089" },
                HasVideo = false
            },

            ["torsion-spring"] = new ModuleInfo
            {
                Key = "torsion-spring",
                Name = "Torsion Spring Calculator",
                Route = "/torsion-spring",
                Category = "Springs",
                Icon = "🔄",
                Description = "Design helical torsion springs for rotational applications. Calculate torque-angle relationship, bending stress, and fatigue analysis per EN 13906-3.",
                Keywords = "torsion spring, rotational spring, spring torque, angular deflection, bending stress, EN 13906",
                RelatedModules = new[] { "compression-spring", "extension-spring", "materials" },
                IsVerified = true,
                VerificationStandards = new[] { "EN 13906-3", "DIN 2089" },
                HasVideo = false
            },

            // ========== GEARS ==========

            ["gear-pair"] = new ModuleInfo
            {
                Key = "gear-pair",
                Name = "Cylindrical Gear Pair Calculator",
                Route = "/gear-pair",
                Category = "Gears",
                Icon = "⚙️",
                Description = "Design spur and helical gear pairs. Calculate geometry, bending stress, contact stress, and safety factors according to ISO 6336 and DIN 3990.",
                Keywords = "spur gear, helical gear, gear design, gear calculation, ISO 6336, DIN 3990, gear strength",
                RelatedModules = new[] { "materials", "ball-bearing", "key-connection" },
                // Not verified: KV, KHβ, YF and YS are heavily simplified relative to
                // ISO 6336, and the load/life factors are approximations. Use for
                // preliminary sizing only.
                IsVerified = false,
                VerificationStandards = new[] { "ISO 6336 (simplified)", "DIN 3990 (simplified)" },
                HasVideo = false
            },

            // ========== SECTION PROPERTIES ==========

            ["moment-of-inertia"] = new ModuleInfo
            {
                Key = "moment-of-inertia",
                Name = "Moment of Inertia Calculator",
                Route = "/moment-of-inertia",
                Category = "Section Properties",
                Icon = "📊",
                Description = "Calculate section properties for various cross-sectional shapes. Determine moment of inertia, section modulus, radius of gyration, and centroid location.",
                Keywords = "moment of inertia, section modulus, radius of gyration, section properties, cross section, beam design",
                RelatedModules = new[] { "materials", "gear-pair", "compression-spring" },
                IsVerified = true,
                VerificationStandards = new[] { "Reference" },
                HasVideo = false
            },

            // ========== REFERENCE / UTILITY ==========

            ["materials"] = new ModuleInfo
            {
                Key = "materials",
                Name = "Material Database",
                Route = "/materials",
                Category = "Reference",
                Icon = "📋",
                Description = "Comprehensive engineering materials database with mechanical properties, thermal characteristics, and friction coefficients. Steels, aluminum alloys, and spring materials.",
                Keywords = "material database, material properties, steel grades, aluminum alloys, elastic modulus, yield strength",
                RelatedModules = new[] { "compression-spring", "extension-spring", "torsion-spring" },
                IsVerified = true,
                VerificationStandards = new[] { "EN 10027", "ASTM", "DIN" },
                HasVideo = false
            },

            // ========== INFORMATION PAGES ==========

            ["about"] = new ModuleInfo
            {
                Key = "about",
                Name = "About Mekanika",
                Route = "/about",
                Category = "Information",
                Icon = "ℹ️",
                Description = "Learn about Mekanika, our engineering calculation tools, technology stack, and standards used in calculations.",
                Keywords = "about mekanika, engineering tools, online calculators, mechanical engineering, free tools",
                RelatedModules = new[] { "contact", "materials", "bearings" },
                IsVerified = false,
                VerificationStandards = Array.Empty<string>(),
                HasVideo = false
            },

            ["contact"] = new ModuleInfo
            {
                Key = "contact",
                Name = "Contact Us",
                Route = "/contact",
                Category = "Information",
                Icon = "📧",
                Description = "Get in touch with the Mekanika team for questions, feedback, bug reports, or feature requests.",
                Keywords = "contact, feedback, bug report, feature request, support, email",
                RelatedModules = new[] { "about", "materials", "bearings" },
                IsVerified = false,
                VerificationStandards = Array.Empty<string>(),
                HasVideo = false
            }
        };
    }
}

/// <summary>
/// Module information model
/// </summary>
public class ModuleInfo
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Route { get; set; } = "";
    public string Category { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Description { get; set; } = "";
    public string Keywords { get; set; } = "";
    public string[] RelatedModules { get; set; } = Array.Empty<string>();
    public bool IsVerified { get; set; }
    public string[] VerificationStandards { get; set; } = Array.Empty<string>();
    public bool HasVideo { get; set; }
    public string? VideoId { get; set; }
    public string? VideoTitle { get; set; }

    // Feedback and rating properties
    public double? AverageRating { get; set; }
    public int? RatingCount { get; set; }
}
