# Mekanika - Mechanical Engineering Calculator Web Application

## Project Overview
Mekanika is a web-based mechanical engineering calculator built with Blazor WebAssembly (.NET 8). It provides various calculation modules for mechanical engineers including interference fits, taper fits, key connections, bolt calculations, bearing life calculations, and spring design.

**Technology Stack:**
- Frontend: Blazor WebAssembly (C#)
- Styling: Custom CSS
- PDF Generation: JavaScript (jsPDF)
- Target Framework: .NET 8.0

---

## Standard Module Format

All calculation modules should follow the **Keyway (Key Connection) Module** format as the standard template. This ensures consistency across all modules.

### Module Structure Template

Based on `Pages/KeyConnection.razor` and `Services/KeyConnectionEngine.cs`:

#### 1. **Razor Component Structure** (`Pages/[ModuleName].razor`)

**REAL EXAMPLE: KeyConnection.razor** - Use this as your exact template!

```razor
@page "/key-connection"
@inject MaterialService MaterialService
@inject IJSRuntime JSRuntime

<PageTitle>Parallel Key Calculator - Mekanika</PageTitle>

<div class="page-header">
    <h1><span>🔑</span> Parallel Key Calculator</h1>
    <p>Parallel key connection design according to DIN 6885</p>
</div>

@if (!showResults)
{
    <!-- INPUT FORM -->
    <div class="form-grid">
        <!-- Shaft & Key Card -->
        <div class="card">
            <div class="card-header">
                <span>📏</span>
                <h2>Shaft & Key Dimensions</h2>
            </div>

            <div class="form-group">
                <label>Shaft Diameter (d)</label>
                <div class="input-group">
                    <input type="number" step="0.1" @bind="engine.ShaftDiameter" @bind:after="OnShaftDiameterChanged" placeholder="e.g. 40" />
                    <span class="unit">mm</span>
                </div>
            </div>

            @if (recommendedKey != null)
            {
                <div class="alert alert-success" style="margin: 12px 0;">
                    <span>💡</span>
                    <div>
                        <strong>Recommended:</strong> Key @recommendedKey.Width×@recommendedKey.Height mm
                        <button class="btn btn-primary" style="margin-left: 12px; padding: 4px 12px; font-size: 12px;" @onclick="ApplyRecommendedKey">Apply</button>
                    </div>
                </div>
            }

            <div class="form-group">
                <label>Key Width (b)</label>
                <div class="input-group">
                    <input type="number" step="0.1" @bind="engine.KeyWidth" />
                    <span class="unit">mm</span>
                </div>
            </div>

            <div class="form-group">
                <label>Key Height (h)</label>
                <div class="input-group">
                    <input type="number" step="0.1" @bind="engine.KeyHeight" />
                    <span class="unit">mm</span>
                </div>
            </div>

            <div class="form-group">
                <label>Key Length - Total (l)</label>
                <div class="input-group">
                    <input type="number" step="1" @bind="engine.KeyLength" placeholder="e.g. 50" />
                    <span class="unit">mm</span>
                </div>
                <small style="color: #6c757d; font-size: 12px;">Effective length = l - b</small>
            </div>
        </div>

        <!-- Keyway Depths Card -->
        <div class="card">
            <div class="card-header">
                <span>📐</span>
                <h2>Keyway Depths</h2>
            </div>

            <div class="form-group">
                <label>Keyway Depth in Shaft (t₁)</label>
                <div class="input-group">
                    <input type="number" step="0.1" @bind="engine.KeywayDepthShaft" />
                    <span class="unit">mm</span>
                </div>
            </div>

            <div class="form-group">
                <label>Keyway Depth in Hub (t₂)</label>
                <div class="input-group">
                    <input type="number" step="0.1" @bind="engine.KeywayDepthHub" />
                    <span class="unit">mm</span>
                </div>
            </div>

            <div class="form-group">
                <label>Number of Keys</label>
                <select @bind="engine.NumberOfKeys">
                    <option value="1">1 Key</option>
                    <option value="2">2 Keys (180°)</option>
                    <option value="3">3 Keys (120°)</option>
                </select>
            </div>
        </div>

        <!-- Loading Card -->
        <div class="card">
            <div class="card-header">
                <span>⚡</span>
                <h2>Loading</h2>
            </div>

            <div class="form-group">
                <label>Applied Torque (T)</label>
                <div class="input-group">
                    <input type="number" step="1" @bind="engine.AppliedTorque" placeholder="e.g. 500" />
                    <span class="unit">Nm</span>
                </div>
            </div>

            <div class="form-group">
                <label>Load Factor (φ)</label>
                <select @bind="selectedLoadFactor" @bind:after="OnLoadFactorChanged">
                    <option value="1.0">1.0 - Static</option>
                    <option value="1.25">1.25 - Moderate shock</option>
                    <option value="1.5">1.5 - Heavy shock</option>
                    <option value="1.75">1.75 - Alternating</option>
                    <option value="2.0">2.0 - Impact</option>
                </select>
            </div>
        </div>

        <!-- Materials Card -->
        <div class="card">
            <div class="card-header">
                <span>🧱</span>
                <h2>Materials</h2>
            </div>

            <div class="form-group">
                <label>Shaft Material</label>
                <select @bind="selectedShaftMaterialIndex">
                    @for (int i = 0; i < materials.Count; i++)
                    {
                        <option value="@i">@materials[i].Name</option>
                    }
                </select>
            </div>

            <div class="form-group">
                <label>Hub Material</label>
                <select @bind="selectedHubMaterialIndex">
                    @for (int i = 0; i < materials.Count; i++)
                    {
                        <option value="@i">@materials[i].Name</option>
                    }
                </select>
            </div>

            <div class="form-group">
                <label>Key Material</label>
                <select @bind="selectedKeyMaterialIndex">
                    @for (int i = 0; i < materials.Count; i++)
                    {
                        <option value="@i">@materials[i].Name</option>
                    }
                </select>
            </div>
        </div>
    </div>

    <div class="button-group">
        <button class="btn btn-success btn-lg" @onclick="Calculate">
            <span>▶</span> Calculate
        </button>
        <button class="btn btn-secondary" @onclick="ClearForm">
            <span>🧹</span> Clear
        </button>
    </div>
}
else
{
    <!-- RESULTS -->
    <div class="button-group" style="margin-bottom: 24px;">
        <button class="btn btn-primary" @onclick="BackToInput">
            <span>←</span> Back to Input
        </button>
        <button class="btn btn-secondary" @onclick="ClearForm">
            <span>🆕</span> New Calculation
        </button>
        <button class="btn btn-info" @onclick="SaveAsPdf">
            <span>📄</span> Save as PDF
        </button>
    </div>

    <!-- Detailed Results Container -->
    <div id="results-content">
        <!-- Geometric Properties -->
        <div class="card">
            <div class="card-header"><span>📏</span><h2>Geometric & Physical Properties</h2></div>
            <table class="results-table">
                <thead><tr><th>Description</th><th>Symbol</th><th>Value</th><th>Unit</th></tr></thead>
                <tbody>
                    <tr><td>Shaft Diameter</td><td>d</td><td>@engine.ShaftDiameter.ToString("F1")</td><td>mm</td></tr>
                    <tr><td>Key Width</td><td>b</td><td>@engine.KeyWidth.ToString("F1")</td><td>mm</td></tr>
                    <tr><td>Key Height</td><td>h</td><td>@engine.KeyHeight.ToString("F1")</td><td>mm</td></tr>
                    <tr><td>Total Key Length</td><td>l</td><td>@engine.KeyLength.ToString("F1")</td><td>mm</td></tr>
                    <tr class="highlight"><td>Effective Key Length</td><td>l<sub>eff</sub></td><td>@engine.EffectiveLength.ToString("F1")</td><td>mm</td></tr>
                    <tr><td>Keyway Depth (Shaft)</td><td>t₁</td><td>@engine.KeywayDepthShaft.ToString("F2")</td><td>mm</td></tr>
                    <tr><td>Keyway Depth (Hub)</td><td>t₂</td><td>@engine.KeywayDepthHub.ToString("F2")</td><td>mm</td></tr>
                    <tr><td>Contact Area (Shaft)</td><td>A<sub>shaft</sub></td><td>@engine.ContactAreaShaft.ToString("F1")</td><td>mm²</td></tr>
                    <tr><td>Contact Area (Hub)</td><td>A<sub>hub</sub></td><td>@engine.ContactAreaHub.ToString("F1")</td><td>mm²</td></tr>
                    <tr><td>Number of Keys</td><td>n</td><td>@engine.NumberOfKeys</td><td>-</td></tr>
                </tbody>
            </table>
        </div>

        <!-- Forces -->
        <div class="card">
            <div class="card-header"><span>⚡</span><h2>Forces & Moments</h2></div>
            <table class="results-table">
                <thead><tr><th>Description</th><th>Symbol</th><th>Value</th><th>Unit</th></tr></thead>
                <tbody>
                    <tr><td>Applied Torque</td><td>T</td><td>@engine.AppliedTorque.ToString("F0")</td><td>Nm</td></tr>
                    <tr><td>Load Factor</td><td>φ</td><td>@engine.LoadFactor.ToString("F2")</td><td>-</td></tr>
                    <tr class="highlight"><td>Tangential Force (Total)</td><td>F<sub>t</sub></td><td>@engine.TangentialForce.ToString("F0")</td><td>N</td></tr>
                    <tr><td>Force per Key</td><td>F<sub>key</sub></td><td>@engine.ForcePerKey.ToString("F0")</td><td>N</td></tr>
                </tbody>
            </table>
        </div>

        <!-- Stresses -->
        <div class="card">
            <div class="card-header"><span>💪</span><h2>Stresses</h2></div>
            <table class="results-table">
                <thead><tr><th>Description</th><th>Symbol</th><th>Value</th><th>Unit</th></tr></thead>
                <tbody>
                    <tr><td>Surface Pressure (Shaft)</td><td>p<sub>shaft</sub></td><td>@engine.SurfacePressureShaft.ToString("F1")</td><td>MPa</td></tr>
                    <tr><td>Surface Pressure (Hub)</td><td>p<sub>hub</sub></td><td>@engine.SurfacePressureHub.ToString("F1")</td><td>MPa</td></tr>
                    <tr><td>Key Shear Stress</td><td>τ<sub>key</sub></td><td>@engine.ShearStress.ToString("F1")</td><td>MPa</td></tr>
                </tbody>
            </table>
        </div>

        <!-- Safety Factors -->
        <div class="card">
            <div class="card-header"><span>🛡️</span><h2>Safety Factors</h2></div>
            <table class="results-table">
                <thead><tr><th>Description</th><th>Symbol</th><th>Value</th><th>Status</th></tr></thead>
                <tbody>
                    <tr class="@GetSafetyRowClass(engine.SafetyFactorShaft)"><td>Safety Factor (Shaft)</td><td>SF<sub>shaft</sub></td><td>@engine.SafetyFactorShaft.ToString("F2")</td><td>@GetSafetyStatus(engine.SafetyFactorShaft)</td></tr>
                    <tr class="@GetSafetyRowClass(engine.SafetyFactorHub)"><td>Safety Factor (Hub)</td><td>SF<sub>hub</sub></td><td>@engine.SafetyFactorHub.ToString("F2")</td><td>@GetSafetyStatus(engine.SafetyFactorHub)</td></tr>
                    <tr class="@GetSafetyRowClass(engine.SafetyFactorShear)"><td>Safety Factor (Shear)</td><td>SF<sub>shear</sub></td><td>@engine.SafetyFactorShear.ToString("F2")</td><td>@GetSafetyStatus(engine.SafetyFactorShear)</td></tr>
                </tbody>
            </table>
        </div>

        <!-- Design Recommendation -->
        <div class="card">
            <div class="card-header"><span>📐</span><h2>Design Recommendation</h2></div>
            <table class="results-table">
                <thead><tr><th>Description</th><th>Symbol</th><th>Value</th><th>Unit</th></tr></thead>
                <tbody>
                    <tr class="highlight"><td>Required Key Length (min)</td><td>l<sub>req</sub></td><td>@engine.RequiredKeyLength.ToString("F0")</td><td>mm</td></tr>
                    <tr><td>Provided Key Length</td><td>l</td><td>@engine.KeyLength.ToString("F0")</td><td>mm</td></tr>
                    <tr><td>Length Margin</td><td>Δl</td><td>@((engine.KeyLength - engine.RequiredKeyLength).ToString("F0"))</td><td>mm</td></tr>
                </tbody>
            </table>

            @if (engine.KeyLength < engine.RequiredKeyLength)
            {
                <div class="alert alert-danger" style="margin-top: 16px;">⚠️ <strong>Warning:</strong> Key length is insufficient! Increase to at least @engine.RequiredKeyLength.ToString("F0") mm.</div>
            }
            else if (engine.SafetyFactorMin < 1.0)
            {
                <div class="alert alert-danger" style="margin-top: 16px;">⚠️ <strong>Warning:</strong> Safety factor below 1.0! Design is unsafe.</div>
            }
            else if (engine.SafetyFactorMin < 1.5)
            {
                <div class="alert alert-warning" style="margin-top: 16px;">⚡ <strong>Note:</strong> Safety factor is marginal. Consider increasing key length.</div>
            }
            else
            {
                <div class="alert alert-success" style="margin-top: 16px;">✓ <strong>OK:</strong> Design meets safety requirements.</div>
            }
        </div>
    </div>
}

<style>
    /* Results Tables - CRITICAL: Copy all these styles exactly */
    .results-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 14px;
    }

    .results-table th {
        background: #f8f9fa;
        padding: 12px 16px;
        text-align: left;
        font-weight: 600;
        color: #2c3e50;
        border-bottom: 2px solid #dee2e6;
    }

    .results-table td {
        padding: 10px 16px;
        border-bottom: 1px solid #eee;
    }

    .results-table tr:hover {
        background: #f8f9fa;
    }

    /* Row state classes - CRITICAL for visual feedback */
    .results-table tr.highlight {
        background: #e3f2fd;
        font-weight: 500;
    }

    .results-table tr.warning {
        background: #fff8e1;
    }

    .results-table tr.danger {
        background: #ffebee;
    }

    .results-table tr.success {
        background: #e8f5e9;
    }

    /* Column widths - CRITICAL for proper layout */
    .results-table th:nth-child(1) { width: 40%; }
    .results-table th:nth-child(2) { width: 15%; text-align: center; }
    .results-table th:nth-child(3) { width: 25%; text-align: right; }
    .results-table th:nth-child(4) { width: 20%; text-align: center; }

    /* Column alignment and styling */
    .results-table td:nth-child(2) {
        text-align: center;
        font-family: 'Times New Roman', serif;
        font-style: italic;
        color: #666;
    }

    .results-table td:nth-child(3) {
        text-align: right;
        font-weight: 500;
        font-family: 'Consolas', monospace;
    }

    .results-table td:nth-child(4) {
        text-align: center;
        color: #666;
    }

    /* PDF Button styling */
    .btn-info {
        background: linear-gradient(135deg, #17a2b8 0%, #138496 100%);
        color: white;
    }

    .btn-info:hover {
        background: linear-gradient(135deg, #138496 0%, #117a8b 100%);
    }
</style>

@code {
    private KeyConnectionEngine engine = new();
    private List<Material> materials = new();
    private bool showResults = false;
    private KeyDimension? recommendedKey = null;
    private string selectedLoadFactor = "1.0";
    private int selectedShaftMaterialIndex = 2;
    private int selectedHubMaterialIndex = 2;
    private int selectedKeyMaterialIndex = 2;

    protected override void OnInitialized()
    {
        materials = MaterialService.GetMaterials();
        engine.LoadFactor = 1.0;
        engine.NumberOfKeys = 1;
    }

    private void OnShaftDiameterChanged() => recommendedKey = KeyConnectionEngine.GetStandardKey(engine.ShaftDiameter);

    private void OnLoadFactorChanged()
    {
        if (double.TryParse(selectedLoadFactor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double factor))
            engine.LoadFactor = factor;
    }

    private void ApplyRecommendedKey()
    {
        if (recommendedKey != null)
        {
            engine.KeyWidth = recommendedKey.Width;
            engine.KeyHeight = recommendedKey.Height;
            engine.KeywayDepthShaft = recommendedKey.DepthShaft;
            engine.KeywayDepthHub = recommendedKey.DepthHub;
        }
    }

    private string GetSafetyRowClass(double sf) => sf < 1.0 ? "danger" : sf < 1.5 ? "warning" : "success";
    private string GetSafetyStatus(double sf) => sf < 1.0 ? "❌ FAIL" : sf < 1.5 ? "⚠️ Marginal" : "✓ OK";

    private void Calculate()
    {
        if (engine.ShaftDiameter <= 0 || engine.KeyWidth <= 0 || engine.KeyHeight <= 0 || engine.KeyLength <= 0 || engine.AppliedTorque <= 0) return;
        engine.ShaftMaterial = materials[selectedShaftMaterialIndex];
        engine.HubMaterial = materials[selectedHubMaterialIndex];
        engine.KeyMaterial = materials[selectedKeyMaterialIndex];
        engine.Calculate();
        showResults = true;
    }

    private void BackToInput() => showResults = false;

    private void ClearForm()
    {
        engine = new KeyConnectionEngine { LoadFactor = 1.0, NumberOfKeys = 1 };
        recommendedKey = null;
        selectedLoadFactor = "1.0";
        showResults = false;
    }

    private async Task SaveAsPdf()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("generatePdf", "results-content", $"KeyConnection-Report-{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("console.error", $"PDF generation error: {ex.Message}");
        }
    }
}
```

#### 2. **Calculation Engine Structure** (`Services/[ModuleName]Engine.cs`)

**REAL EXAMPLE: KeyConnectionEngine.cs** - Use this as your exact template!

```csharp
using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Parallel key connection calculation engine based on DIN 6885
/// </summary>
public class KeyConnectionEngine
{
    // ============ INPUT PARAMETERS ============

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

    // ============ CALCULATED VALUES ============

    // Calculated Values - Geometry
    public double EffectiveLength { get; set; }       // mm (l - b for round end keys)
    public double ContactAreaShaft { get; set; }      // mm²
    public double ContactAreaHub { get; set; }        // mm²
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

    // ============ STANDARD DATA (DIN 6885) ============

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

    // ============ MAIN CALCULATION METHOD ============

    public void Calculate()
    {
        CalculateGeometry();
        CalculateForces();
        CalculateStresses();
        CalculateAllowableValues();
        CalculateSafetyFactors();
        CalculateRequiredLength();
    }

    // ============ CALCULATION STEPS ============

    private void CalculateGeometry()
    {
        // Effective length (assuming round-end key)
        EffectiveLength = KeyLength - KeyWidth;
        if (EffectiveLength < 0) EffectiveLength = KeyLength * 0.9;

        // Contact areas (DIN 6885)
        // Shaft contact: effective length × keyway depth in shaft (t1)
        ContactAreaShaft = EffectiveLength * KeywayDepthShaft;

        // Hub contact: effective length × keyway depth in hub (t2)
        ContactAreaHub = EffectiveLength * KeywayDepthHub;

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
        // Calculate minimum required key length based on surface pressure
        double heightShaft = KeyHeight - KeywayDepthShaft;
        double requiredFromShaft = (ForcePerKey / AllowablePressureShaft) / heightShaft;
        double requiredFromHub = (ForcePerKey / AllowablePressureHub) / KeywayDepthHub;
        double requiredFromShear = (ForcePerKey / AllowableShear) / KeyWidth;

        double maxRequired = Math.Max(Math.Max(requiredFromShaft, requiredFromHub), requiredFromShear);

        // Add key width for round-end key
        RequiredKeyLength = maxRequired + KeyWidth;

        // Round up to nearest 5mm
        RequiredKeyLength = Math.Ceiling(RequiredKeyLength / 5.0) * 5.0;
    }
}

// ============ SUPPORTING CLASSES ============

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
```

---

## Key Design Principles

### 1. **Two-Phase Interface**
- **Input Phase**: User enters all parameters in organized cards
- **Results Phase**: Comprehensive results presented in detailed tables
- Clear navigation between phases with "Back to Input", "New Calculation", and "Save as PDF" buttons

### 2. **Results Organization**
Results are presented in categorized tables:

- **Geometric & Physical Properties**
- **Forces & Moments**
- **Stresses**
- **Safety Factors**
- **Design Recommendations**

Each category is displayed in a standardized 4-column table format with proper highlighting for important values.

### 3. **Visual Feedback System**

**Table Row Classes:**
- `.highlight` - Important calculated values (light blue background)
- `.warning` - Marginal/concerning values (yellow background)
- `.danger` - Failed/critical values (red background)
- `.success` - Passed/safe values (green background)

**Safety Factor Color Coding:**
- `SF < 1.0` → Red (Danger) → "❌ FAIL"
- `1.0 ≤ SF < 1.5` → Yellow (Warning) → "⚠️ Marginal"
- `SF ≥ 1.5` → Green (Success) → "✓ OK"

**Alert Messages:**
- Danger alerts for design failures
- Warning alerts for marginal designs
- Success alerts for adequate designs

### 4. **Table Structure**
All results tables follow this format:
- **Column 1 (40%)**: Description in plain language
- **Column 2 (15%)**: Mathematical symbol (italic, centered)
- **Column 3 (25%)**: Numerical value (right-aligned, monospace font)
- **Column 4 (20%)**: Unit or status (centered)

### 5. **Calculation Engine Pattern**
```
Input Validation → Calculate() → {
    CalculateGeometry()
    CalculateForces()
    CalculateStresses()
    CalculateAllowableValues()
    CalculateSafetyFactors()
    CalculateRequiredDimensions()
}
```

### 6. **PDF Export**
- Simplified HTML/CSS for PDF generation
- Consistent branding (footer with Mekanika.org)
- Timestamp and standard reference
- All relevant results included

---

## Naming Conventions

### Files
- Razor pages: `PascalCase.razor` (e.g., `KeyConnection.razor`)
- Services: `PascalCaseEngine.cs` (e.g., `KeyConnectionEngine.cs`)
- Models: `PascalCase.cs` (e.g., `Material.cs`)

### CSS Classes
- Component classes: `kebab-case` (e.g., `.result-card`, `.form-grid`)
- State classes: `lowercase` (e.g., `.highlight`, `.warning`, `.danger`)

### Variables
- C# properties: `PascalCase` (e.g., `ShaftDiameter`)
- C# local variables: `camelCase` (e.g., `selectedMaterialIndex`)
- HTML attributes: `kebab-case` (e.g., `@bind:after`)

---

## Icons Used
Standard emoji icons for consistency:
- 📏 Geometry/Dimensions
- ⚡ Forces/Loading
- 💪 Stresses
- 🛡️ Safety Factors
- 📐 Design/Recommendations
- 🧱 Materials
- 🔬 Surface Properties
- 📚 Education/Theory
- 📄 PDF Export
- 🆕 New/Clear
- ▶ Calculate/Start
- ← Back
- ✓ Success/OK
- ⚠️ Warning
- ❌ Fail/Error

---

## Current Modules Status

### Calculation Modules

| Module | File | showResults | results-table | SaveAsPdf | GetSafetyRowClass | Status |
|--------|------|:-----------:|:-------------:|:---------:|:-----------------:|--------|
| **Key Connection** | KeyConnection.razor | ✅ | ✅ | ✅ | ✅ | ✅ **Full Standard** |
| **Interference Fit** | InterferenceFit.razor | ✅ | ✅ | ✅ | ✅ | ✅ **Full Standard** |
| **Taper Fit** | TaperFit.razor | ✅ | ✅ | ✅ | ✅ | ✅ **Full Standard** |
| **Single Bolt** | SingleBolt.razor | ✅ | ✅ | ✅ | ✅ | ✅ **Full Standard** |
| **Clamp Connection** | ClampConnection.razor | ✅ | ✅ | ✅ | ✅ | ✅ **Full Standard** |
| **Extension Spring** | ExtensionSpring.razor | ✅ | ✅ | ✅ | ✅ | ✅ **Full Standard** |
| **Torsion Spring** | TorsionSpring.razor | ✅ | ✅ | ✅ | ✅ | ✅ **Full Standard** |
| **Gear Pair** | GearPair.razor | ✅ | ✅ | ✅ | ✅ | ✅ **Full Standard** |
| **Ball Bearing** | BallBearing.razor | ✅ | ✅ | ✅ | ❌ | ⚠️ **Missing GetSafetyRowClass** |
| **Roller Bearing** | RollerBearing.razor | ✅ | ✅ | ✅ | ❌ | ⚠️ **Missing GetSafetyRowClass** |
| **Compression Spring** | CompressionSpring.razor | ✅ | ✅ | ✅ | ❌ | ⚠️ **Missing GetSafetyRowClass** |
| **Moment of Inertia** | MomentOfInertia.razor | ✅ | ✅ | ✅ | ❌ | ⚠️ **Missing GetSafetyRowClass** |

### Info / Database Pages (Standard format not required)

| Page | File | Notes |
|------|------|-------|
| Home | Index.razor | Landing page |
| About | About.razor | Project info |
| Contact | Contact.razor | Contact form |
| Materials DB | Materials.razor | Material database |
| Bearings DB | Bearings.razor | Bearing database |
| Bolt DB | BoltDatabase.razor | Bolt database |

### Summary

- ✅ **Full Standard (8/12 calc modules):** KeyConnection, InterferenceFit, TaperFit, SingleBolt, ClampConnection, ExtensionSpring, TorsionSpring, GearPair
- ⚠️ **Partial Standard (4/12 calc modules):** BallBearing, RollerBearing, CompressionSpring, MomentOfInertia — all missing `GetSafetyRowClass` + `GetSafetyStatus` helper methods

---

## Next Steps

When updating other modules to match the keyway format:

1. **Update Results Section Structure:**
   - Organize detailed results into categorized cards
   - Use standardized table structure with 4 columns
   - Apply consistent row highlighting (`.highlight`, `.warning`, `.danger`, `.success`)
   - Remove any old summary cards (results-header)

2. **Add Safety Factor Sections:**
   - Create dedicated safety factors table
   - Show individual safety factors for each failure mode
   - Use color-coded status (❌ FAIL, ⚠️ Marginal, ✓ OK)

3. **Add Design Recommendation Card (if applicable):**
   - Show required vs. provided dimensions
   - Calculate margins
   - Display context-aware alerts (danger/warning/success)

4. **Update Styling:**
   - Copy all style definitions from KeyConnection.razor
   - Include results-table styles with proper column widths
   - Ensure responsive design for mobile

5. **Add PDF Export:**
   - Add IJSRuntime injection
   - Create `SaveAsPdf()` method
   - Implement `GeneratePdfHtml()` with all critical results
   - Add "Save as PDF" button to results section

6. **Add Helper Methods:**
   - `GetSafetyRowClass(double sf)` for row coloring
   - `GetSafetyStatus(double sf)` for status text

---

## Developer Notes

- All calculations should include proper input validation
- Use meaningful variable names that match engineering terminology
- Comment complex calculations with formulas
- Always provide units in the UI
- Material properties are injected via `MaterialService`
- PDF generation uses JavaScript interop with jsPDF library (`Pages/pdfGenerator.js`)
- **Every calculator shows why Calculate refused.** All twelve fill a `List<string>` and
  render `Shared/ValidationAlert.razor` above the button (moment-of-inertia uses its own
  `validationError` string). A bare `return` in a Calculate guard is a bug: the button
  appears dead and the visitor cannot tell the app from a broken page. gear-pair was the
  last one still doing it and was fixed in Aug 2026.

### **Result-table column rules are duplicated in eleven pages — keep them identical**

Every single-shape calculator declares the same `.results-table` column widths and
alignments in its own Razor `<style>` block. `tools/check-table-css.mjs` (run by the deploy
workflow) fails the build if they stop matching. The canonical block is 40/15/25/20 with
`'Consolas', 'Courier New', monospace` for the value column.

They had drifted: eight pages used 40/15/25/20 and three (key-connection, clamp-connection,
moment-of-inertia) used 45/15/25/15. Nothing surfaces that — each page looks fine alone and
they only disagree side by side, which is how it was eventually caught (Aug 2026).

**gear-pair is excluded from the check, deliberately.** It is the only page with more than
one table shape — 12 tables of Description | Symbol | Gear 1 | Gear 2 | Unit, 8 of
Description | Symbol | Value | Unit, and 2 of six columns — and a single 5-column rule set
was being applied to all of them. In the 4-column tables that styled the Unit column as if
it were "Gear 2": right-aligned monospace instead of centred grey, with the declared widths
summing to 83%. Its rules are now selected by `:nth-child(n):nth-last-child(m)`, which
matches a cell only in a row of exactly n+m-1 cells, so each shape gets its own set and a
new column is picked up automatically.

**Two 6-column tables there are structurally identical and semantically are not** —
`Description | Symbol | Gear 1 | Gear 2 | Required | Status` versus the micropitting sweep
`Point | gY | ρn,Y | X_Y | Θfl,Y | λGF,Y`, a label followed by five numbers. No selector can
tell them apart, so the second carries an explicit `.results-table-numeric`.

**Why the eleven duplicates have not simply been merged into `modern-icons.css`:** two pages
use `.results-table` for something else — `MyCalculations` for the saved-calculations list and
`Account` for a two-column key/value table — and a shared rule would restyle both. Scoping it
with the obvious `#results-content` prefix then out-specifies gear-pair's rules, because an id
beats any number of pseudo-classes, and would silently re-lay-out its 5-column tables. Merging
means untangling that first; until then, one canonical copy and a build check.

### **Numbers are formatted in the invariant culture, app-wide**

`Program.cs` pins `CultureInfo.DefaultThreadCurrentCulture`/`CurrentCulture` (and the UI
pair) to `InvariantCulture` before the host is built, so the result tables read the same
for every visitor. The UI is English throughout; the numbers match it.

**The input boxes are not covered by this and cannot be.** The 204
`<input type="number">` fields hold an invariant value in the DOM — the HTML spec fixes
that — but the browser *draws* them in the operating system's locale. Under a Turkish
system locale Chromium renders `value="40.5"` as **`40,5`**. A `lang="en"` attribute on the
document or on the field itself was tested and changes nothing. So on a non-English machine
the entry field shows `40,5` while the results show `40.5`.

That mismatch is known, was chosen deliberately (Aug 2026) over the alternative of letting
the results follow the browser too, and is the one thing to remember before "fixing" the
number formatting again:

- **Before the pin the two sides agreed** — both followed the browser, both showed `40,5`
  on a Turkish machine. The pin did not remove an inconsistency, it introduced one on
  non-English machines in exchange for a stable results format. Reverting it is a real
  option; it is a presentation choice, not a correctness one.
- **Never move the fields to `type="text"` to force a dot.** Blazor would parse them with
  the invariant culture, and a visitor typing `40,5` — which the number field accepts today
  and converts correctly — would have the value silently read as nothing. A cosmetic
  mismatch is cheaper than losing an input.
- **Nothing that round-trips depends on the pin.** `CalculationState` writes and reads every
  number with an explicit `InvariantCulture`, so share links and cloud-saved calculations are
  unaffected either way.

**Lookup keys are lowercased with `ToLowerInvariant`, never `ToLower`** — and this is
independent of the pin, which is why it is a separate rule. Turkish lowercases `I` to
dotless `ı`, so under a Turkish culture `"Cast Iron"` became `"cast ıron"`, matched no arm
of `BoltCalculationEngine`'s material fallback, and silently returned the 235 MPa
mild-steel default in place of 300. The same applies to its surface-finish and location
switches, to the `Contains("alumin")` thread-engagement tests in `BoltCalculationEngine`
and `SingleBolt.razor`, and to `BoltService.GetHoleClearance`. A key that a human reads as
a fixed string must not change meaning with the visitor's language.

---

## Important Notes for Implementation

### When Creating New Modules:

1. **Copy the EXACT structure** from KeyConnection.razor and KeyConnectionEngine.cs
2. **Preserve the table structure** - 4 columns with correct widths
3. **Use the exact CSS classes** - .results-table, .highlight, .warning, .danger, .success
4. **Follow the calculation flow** - Geometry → Forces → Stresses → Allowables → Safety Factors → Requirements
5. **Keep the helper methods** - GetSafetyRowClass() and GetSafetyStatus()
6. **Maintain the alert logic** - Three-level alerts (danger/warning/success)
7. **Include PDF functionality** - SaveAsPdf() calling generatePdf with "results-content" div

### Key Differences from Old Template:

- ✅ **Detailed results tables** instead of summary cards
- ✅ **Four distinct result categories** (Geometry, Forces, Stresses, Safety Factors)
- ✅ **Color-coded safety factor rows** with status text
- ✅ **Design recommendation card** with margin calculations
- ✅ **Context-aware alerts** based on actual design status
- ✅ **Simplified PDF export** (no HTML generation in code)

---

---

## 🚀 Growth & Marketing Strategy

### **Current Analytics (as of Feb 2025)**
```
Monthly Statistics:
- Active Users: 54
- Total Events: 374
- Avg Session: 42 seconds
- Traffic: ~70% organic (Google)
- Geographic: Global (Ankara, Berlin, NYC, Anyang-si, etc.)

Top Modules (by usage):
1. Interference Fit Calculator (8 uses)
2. Taper Fit Calculator (11 uses)
3. Single Bolt Calculator (16 uses)

Growth Target (3 months):
- Conservative: 500 users/month (10x)
- Aggressive: 1,000 users/month (20x)
```

### **Contact & Communication**
```
Email Structure:
- contact@mekanika.org (Primary - General inquiries)
- info@mekanika.org (Alternative)
- support@mekanika.org (User support)
- youtube@mekanika.org (YouTube channel management)
- noreply@mekanika.org (Automated emails)

Future:
- api@mekanika.org (API inquiries)
- business@mekanika.org (B2B partnerships)

Provider: Zoho Mail (Free tier) → Migrate to Google Workspace when scaling
```

### **YouTube Content Strategy**

**Channel Information:**
- Channel Name: Mekanika
- Email: youtube@mekanika.org
- Target: Engineering professionals & students
- Language: English (primary), Turkish (future)

**Video Content Plan:**

**A. Channel Trailer (2-3 min) - General Introduction**
```
Script Structure:
[0:00-0:15] Hook: "Engineering calculations in seconds, not hours"
[0:15-0:45] Problem statement: Manual calculations are slow and error-prone
[0:45-1:30] Solution demo: Quick showcase of calculators
[1:30-2:00] Key features: 15+ calculators, standards-based, 100% free
[2:00-2:30] Call to action: Visit mekanika.org
[2:30-2:45] Subscribe prompt

Production: Screen recording + AI voiceover
```

**B. Module Tutorial Videos (5-7 min each)**
```
Standard Tutorial Template:
[0:00-0:20] Intro + Learning objectives
[0:20-1:00] Theory basics (relevant standard explanation)
[1:00-5:00] Step-by-step calculator demonstration (real example)
[5:00-6:00] Results interpretation & validation
[6:00-6:30] Tips & best practices
[6:30-7:00] Related calculators + CTA

Production Method:
- Screen recording with annotations
- AI-generated voiceover (natural, professional)
- Background music (royalty-free)
- Branded intro/outro (5 seconds)
```

**C. Video Priority Order (Phase 1 - First 5 videos)**
```
Based on current analytics and strategic importance:

1. "Interference Fit Calculator Tutorial - DIN 7190"
   - Most complex, high professional value
   - Keywords: press fit, shrink fit, shaft hub connection

2. "Single Bolt Connection Calculator - VDI 2230"
   - Most used module (16 uses)
   - High practical application

3. "Taper Fit Calculator - Complete Guide"
   - Second most used (11 uses)
   - Unique offering (fewer online alternatives)

4. "Ball Bearing Life Calculation - ISO 281"
   - Universal application
   - High search volume

5. "Parallel Key Connection - DIN 6885"
   - Standard format example
   - Good educational value

Phase 2 (Next 5-10 videos):
- Remaining calculators
- "Common Mistakes" series
- "Material Selection Guide"
- "Understanding Safety Factors"
```

**D. Video SEO Optimization**
```
Title Format:
"[Calculator Name] Tutorial - [Standard] | Free Online Tool"

Example:
"Interference Fit Calculator Tutorial - DIN 7190 | Free Engineering Tool"

Description Template:
---
Learn how to use the [Calculator Name] on Mekanika.org for professional engineering calculations.

🔗 Try it now: https://mekanika.org/[module-url]

⏱️ Timestamps:
0:00 Introduction
0:20 Theory & Standards
1:00 Calculator Demo
5:00 Results Interpretation
6:00 Tips & Best Practices

📚 Standards Covered:
- [DIN/ISO/EN Standard]
- Key equations explained
- Safety factor guidelines

🔧 Related Calculators:
- [Module 1]: https://mekanika.org/...
- [Module 2]: https://mekanika.org/...

📧 Contact: contact@mekanika.org
🌐 Website: https://mekanika.org

#MechanicalEngineering #EngineeringCalculator #[ModuleName] #DIN #ISO #FreeTools
---

Tags (15-20):
mechanical engineering, calculator, free tool, [module specific],
[standard name], engineering design, calculations, online tool,
shaft design, bearing selection, etc.

Thumbnail:
- Calculator screenshot (clean, high-res)
- Bold text: "[Calculator Name]"
- Subtitle: "Free Tutorial"
- Mekanika logo
- Bright colors (high CTR)
```

### **SEO & Analytics Implementation**

**A. Google Analytics 4 Migration (URGENT)**
```html
<!-- Add to wwwroot/index.html <head> section -->
<!-- Google tag (gtag.js) -->
<script async src="https://www.googletagmanager.com/gtag/js?id=G-XXXXXXXXXX"></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', 'G-XXXXXXXXXX');
</script>
```

**B. Enhanced Event Tracking**
```csharp
// Create: Services/AnalyticsService.cs
public class AnalyticsService
{
    private readonly IJSRuntime _js;

    public async Task TrackCalculation(string moduleName, Dictionary<string, object> parameters)
    {
        await _js.InvokeVoidAsync("gtag", "event", "calculation_completed", new
        {
            module_name = moduleName,
            timestamp = DateTime.UtcNow,
            user_inputs = parameters.Count
        });
    }

    public async Task TrackPdfDownload(string moduleName)
    {
        await _js.InvokeVoidAsync("gtag", "event", "pdf_download", new
        {
            module_name = moduleName,
            file_type = "pdf"
        });
    }

    public async Task TrackError(string moduleName, string errorType)
    {
        await _js.InvokeVoidAsync("gtag", "event", "calculation_error", new
        {
            module_name = moduleName,
            error_type = errorType
        });
    }

    public async Task TrackVideoClick(string videoUrl)
    {
        await _js.InvokeVoidAsync("gtag", "event", "video_click", new
        {
            video_url = videoUrl,
            source = "module_page"
        });
    }
}

// Register in Program.cs:
builder.Services.AddScoped<AnalyticsService>();
```

**C. SEO Meta Tags (Per Page)**
```razor
<!-- Add to each module page -->
<HeadContent>
    <!-- Primary Meta Tags -->
    <meta name="title" content="@Title" />
    <meta name="description" content="@Description" />
    <meta name="keywords" content="@Keywords" />

    <!-- Open Graph / Facebook -->
    <meta property="og:type" content="website" />
    <meta property="og:url" content="@CurrentUrl" />
    <meta property="og:title" content="@Title" />
    <meta property="og:description" content="@Description" />
    <meta property="og:image" content="@ImageUrl" />

    <!-- Twitter -->
    <meta property="twitter:card" content="summary_large_image" />
    <meta property="twitter:url" content="@CurrentUrl" />
    <meta property="twitter:title" content="@Title" />
    <meta property="twitter:description" content="@Description" />
    <meta property="twitter:image" content="@ImageUrl" />

    <!-- Canonical URL -->
    <link rel="canonical" href="@CurrentUrl" />
</HeadContent>

@code {
    private string Title => "Interference Fit Calculator - DIN 7190 | Mekanika";
    private string Description => "Free online interference fit calculator based on DIN 7190. Calculate contact pressure, assembly forces, and safety factors for press-fit and shrink-fit connections.";
    private string Keywords => "interference fit, press fit, shrink fit, DIN 7190, shaft hub connection, contact pressure calculator";
    private string CurrentUrl => "https://mekanika.org/interference-fit";
    private string ImageUrl => "https://mekanika.org/images/og-interference-fit.png";
}
```

**D. Structured Data (Schema.org)**
```html
<!-- Add to each calculator page -->
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  "name": "@ModuleName Calculator",
  "applicationCategory": "EngineeringApplication",
  "operatingSystem": "Web Browser",
  "browserRequirements": "Requires JavaScript, WebAssembly support",
  "offers": {
    "@type": "Offer",
    "price": "0",
    "priceCurrency": "USD"
  },
  "aggregateRating": {
    "@type": "AggregateRating",
    "ratingValue": "4.8",
    "ratingCount": "@UserCount"
  },
  "featureList": [
    "Standards-based calculations",
    "PDF export",
    "No registration required",
    "Privacy-focused (client-side)"
  ],
  "screenshot": "https://mekanika.org/screenshots/@module-name.png",
  "video": {
    "@type": "VideoObject",
    "name": "@ModuleName Tutorial",
    "description": "Complete tutorial for using the @ModuleName calculator",
    "thumbnailUrl": "https://img.youtube.com/vi/@videoId/maxresdefault.jpg",
    "uploadDate": "@UploadDate",
    "contentUrl": "https://www.youtube.com/watch?v=@videoId"
  }
}
</script>
```

### **Monetization Strategy**

**Current Status:**
- Google AdSense: Rejected (low traffic)
- Revenue: $0/month

**Recommended Approach:**

**Phase 1: Alternative Ads (Now - 3 months)**
```
Carbon Ads (Recommended):
- Target: Developers & technical professionals
- Format: Minimal, non-intrusive
- CPM: $2-5
- Setup: Quick (1 week)
- Requirements: Quality technical content ✓

Placement:
- Sidebar (desktop)
- Between results sections (mobile)
- Maximum 1 ad per page
```

**Phase 2: Freemium Model (3-6 months, 500+ users)**
```
Free Tier (Always Free):
✅ All calculation modules
✅ PDF export (with Mekanika branding)
✅ Basic material database
✅ 10 calculations/day
✅ Standard support (email)

Premium Tier ($9/month or $79/year):
✅ Unlimited calculations
✅ Calculation history (cloud storage)
✅ PDF export (unbranded, custom logo)
✅ Batch calculations
✅ Export to Excel/CSV
✅ Advanced material database
✅ Priority email support
✅ Early access to new modules
✅ API access (100 calls/day)

Target: 5% conversion rate → 25 paying users at 500 total users
Revenue: ~$200/month
```

**Phase 3: B2B Licensing (6+ months)**
```
Company License ($299/year):
- 10 user accounts
- White-label option
- Custom material database
- Dedicated support channel
- Training sessions (2 hours)
- Custom module development (additional)
- Invoice billing

Target: 3-5 companies
Revenue: $900-1,500/year
```

**Phase 4: Affiliate & Sponsorships**
```
Affiliate Partners:
- McMaster-Carr (components)
- Amazon Associates (engineering books)
- Engineering software (CAD tools)
- Bearing suppliers (SKF, FAG, Timken)

Sponsored Content:
- "Featured Material: [Company Steel Grade]"
- "Bearing Spotlight: SKF 6205"
- Native advertising
- Newsletter sponsorship (future)

Target: $50-100/month passive income
```

### **Module Verification Status**

> **Note:** An automated test-case framework (`Services/VerificationService.cs` and
> a `/verification` page) previously existed but was removed — the test cases were
> never actually executed against the engines, so they provided no real assurance.
> Do not reintroduce it without wiring the cases up to run for real.

**Nothing renders `IsVerified` today.** Grep it: the flag and `VerificationStandards` are set
on every `ModuleInfo`, and no page reads either. It is a source-level record of what has been
checked and against what, not a badge the visitor sees. Treat it as documentation until
something is built to display it — and if a badge is ever added, re-read each module's comment
first, because a stale one shipped for over a year saying gear-pair's scuffing was out of scope
after scuffing had been implemented.

**gear-pair is benchmarked against four KISSsoft reports** (Aug 2026): three generated with
ISO 6336:2006 Method B, and Tutorial 8, whose printed report is DIN 3990:1987 but whose
Figure 19 prints the ISO result. ~120 quantities diffed per case. Geometry, kinematics, forces,
tooth thickness, W_k, M_d and the DIN 3967 allowances agree to under 0,05 %; the strength chain
lands within 1 % of the ISO references when they are fed their own ISO 1328-1:1995 deviations,
and −1,8 % / −3,8 % on Figure 19 as run. The reports live outside the repo (they are the user's
own files, and `*.pdf` is gitignored) — the harness that drove them was a throwaway, per the
"verifying a change" note further down. Thirteen calculation errors came out of this exercise;
each is recorded above with the anchor that caught it.

**The fourth report (KISSsoft 2022 SP3, ISO 6336:2006; z 22/45, m_n 3, β 22,5°, a 110, x₁ 0,3087,
50 kW, ISO VG 320 at 70 °C) is the first run at an accuracy grade our own tolerances also speak —
ISO 1328-1:2013 grade 6 — so the edition mismatch that dominated the earlier comparisons is absent
here.** Everything upstream of the K factors agrees to under 0,5 %: x₂, every diameter, α_t, α_wt,
β_b, ε_α/ε_β/ε_γ, F_t/F_a/F_r, v, Y_F, Y_S, Y_β, Z_H, Z_E, Z_ε, Z_β, σ_F0 and σ_H0. It caught one
error and explained the whole of the rest:

- **K_Hα: q_α belongs outside the square root of Eq. (72), not inside it.** For ε_γ > 2 the
  standard reads `0,9 + 0,4·√(2(ε_γ−1)/ε_γ)·q_α`; it was implemented with q_α under the root.
  **The branches must meet at ε_γ = 2**, where the root is exactly 1 and Eq. (71) gives
  `0,9 + 0,4·q_α` — with q_α inside, the seam jumps (0,994 to 1,094 on this pair). That
  continuity is the anchor, and it needs no reference report. On this gear the rooted form gave
  1,111 against the report's 1,016; corrected, and fed the report's own K_Hβ, it gives 1,017.
  **Direction: √q > q below 1 and √q < q above it, so the old form was conservative for a heavily
  loaded pair and *not* conservative for a lightly loaded or coarse one** — where K_Hα weighs most.
- **The residual −8,7 % on S_F and −5,7 % on S_H is K_Hβ, and it is the deliberate omission.**
  The report declares "Tooth trace: with end relief" and "Position of contact pattern: favorable",
  which turn on B₁ = B₂ = 0,70 *and* the compensatory Eq. (53). Fed the report's own f_sh our f_ma
  comes out 12,73 µm against its 12,73 — identical — and our F_βx is then 1,33·f_sh + f_ma =
  19,19 µm against its 4,50 ≈ |1,33·B₁·f_sh − B₂·f_ma|. So the difference is entirely those two
  credits, both of which need a verified contact pattern that a web calculator cannot see. We stay
  on the additive Eq. (52) with B = 1, and the result is conservative.

Verification status is a single flag on `ModuleInfo` in
`Services/ModuleMetadataService.cs`:

```csharp
IsVerified = true,                                   // drives the module badge
VerificationStandards = new[] { "DIN 6885", "ISO 773" },
```

Set `IsVerified = false` for any module whose engine uses a simplified or
uncalibrated model, and record why in a comment next to the flag. Currently
`false` for: **clamp-connection** (split-hub model; single-slit lever effect not
modelled).

The **bolt** module (`/bolt`, "General Bolt Calculator") was **deleted in Aug 2026**. It was a
369-line quick torque/preload tool that no menu, no home-page card and no sitemap entry ever
linked to — unreachable since it was written — and it carried no standard reference, so it sat
next to the VDI 2230 `single-bolt` module as an unverifiable second answer. Do not restore it
without deciding first what it is *for* that `single-bolt` is not.

**gear-pair was promoted to `true` in Aug 2026** — see the gear module section below.

**Badge CSS:**
```razor
<style>
.badge-success {
    background: #28a745;
    color: white;
    padding: 4px 12px;
    border-radius: 12px;
    font-size: 12px;
    font-weight: 500;
}

.badge-warning {
    background: #ffc107;
    color: #333;
    padding: 4px 12px;
    border-radius: 12px;
    font-size: 12px;
    font-weight: 500;
}

.badge-danger {
    background: #dc3545;
    color: white;
    padding: 4px 12px;
    border-radius: 12px;
    font-size: 12px;
    font-weight: 500;
}
</style>
```


### **3D Geometry Viewer**

A full-screen viewer that shows the geometry of a completed calculation, driven
by the user's own inputs. Live on: **interference-fit**, **taper-fit**,
**key-connection**, **compression-spring**, **gear-pair**.

**`mekanika3d.inspect(moduleKey, params)`** builds a module's geometry without opening the
overlay and returns each layer's bounding box in millimetres. This is how a new builder gets
checked — run it from the browser console and compare the boxes against the diameters and
face widths that went in. It does **not** use `Box3.setFromObject`: that transforms the eight
corners of each mesh's *local* box, so any rotated part reports the axis-aligned box of a
rotated box. The gear wheel, phased by −90° − 180/z, came back 7 % oversize and looked like a
geometry bug until the inspector was rewritten to walk real vertices. Whatever you do, do not
"fix" a builder against a number that tool produced without checking the tool first.

**Files:**
- `wwwroot/js/viewer3d.js` — scene, orbit, explode, transparency, legend + the
  per-module geometry builders. Loaded on every page (small); **lazy-loads
  three.js r128 from cdnjs only when a viewer is actually opened**, so the
  ~600 KB never lands on the initial page load.
- `Shared/Viewer3D.razor` — the button component.
- `.btn-3d` styles live in `wwwroot/css/modern-icons.css`.

**To add a module:**

1. Add a builder to `BUILDERS` in `viewer3d.js`, keyed by module url:
   ```js
   BUILDERS['taper-fit'] = function (THREE, p, H) {
       return {
           title: '...', legend: [{ color: '#9EA3AB', label: 'Shaft' }],
           layers:  { shaft: group },        // groups added to the scene
           explode: { hub: [0, 40, 0] },     // offset at full explode
           ghosts:  [hubMaterial],           // toggled by the transparency button
           camera:  { radius: 250, target: [0, 0, 0] },
           extra:   null                     // optional extra state button
       };
   };
   ```
   Shared helpers on `H`: `mat`, `ghost`, `annulus`, `annulusSector`,
   `shaftWithKeyways`, `prism` (extrudes a constant Shape along **X**),
   `lathe` (surface of revolution about **X**, for tapers/steps where the radius
   varies), `box`, `cylX`, `helix`. **All modules use X as the shaft axis.**

   **Angular conventions — the main footgun.** The two construction routes place
   angle 0 on opposite sides, and mixing them within one assembly silently
   rotates a part:

   | route | shape/theta angle 0 lands at | radial direction at angle a |
   |---|---|---|
   | `prism` (ExtrudeGeometry) | world **−Z** | `(0, sin a, −cos a)` |
   | `lathe` (LatheGeometry) | world **+Z** | `(0, −sin a, cos a)` |

   A bare `CylinderGeometry` matches `lathe` after `rotateZ(-pi/2)`, and matches
   `prism` after `rotateZ(pi/2)` then `rotateY(pi)`. Build every part of one
   assembly through the same route, or convert explicitly.

   The default camera looks from the **+Y/+Z** quadrant, so a cutaway should
   remove that quadrant to be open to the viewer: keep `[pi, 2.5pi]` with `prism`,
   or `[0, 1.5pi]` with `lathe`.

2. Drop the button into the results button-group:
   ```razor
   <Viewer3D Module="taper-fit" Parameters="@Build3dParameters()" />
   ```

3. Add `Build3dParameters()` returning an anonymous object whose keys match what
   the builder reads. Feed it real engine values — the viewer is meant to show
   *this* calculation, not a generic picture.

   **Parameter names are read case-insensitively, on purpose.** `IJSRuntime`
   serialises with `JsonSerializerDefaults.Web`, so every key is camelCased on the
   way out: `L`→`l`, `Da`→`da`, `Dm`→`dm`, `L0`→`l0`, `Dhub`→`dhub`. Builders read
   `p.L`, `p.Da`… so an uppercase key arrived as `undefined` and `num()` silently
   substituted a hardcoded default — the viewer drew a plausible model that was
   **not** the user's calculation. It shipped that way on all four viewers, worst on
   compression-spring where almost every key is uppercase. `caseInsensitiveParams`
   in `viewer3d.js` now bridges both spellings, so neither side has to remember.
   Two keys differing only in case are indistinguishable after serialisation — the
   viewer logs a console warning if a builder is ever given such a pair.

   (Third time this serialiser-defaults trap has bitten; see also the `jsonb`
   round-trip rule in the custom-library section.)

**Measurement tool:** the "Measure" button lets the user click two points on the
model and reads out the straight distance plus its **axial (ΔX)** and **radial**
components. Model units are millimetres — builders are fed engine values directly,
so no scaling is applied and hit points are real dimensions. Measurements are
cleared whenever the geometry moves or changes (explode toggle, spring state).

The cursor **snaps** to real corners first, then to the nearest point along an
edge, and only falls back to the plain surface point — a hover marker shows what
will be picked (teal = corner, yellow = edge, grey = surface). The snap targets
come from the same `EdgesGeometry` overlays that draw the visible outlines, so
"what you can see" and "what you can snap to" are always the same set. `buildEdges`
rebuilds both after any geometry change; keep `EDGE_ANGLE` (24°) and mesh facet
counts consistent — a cross-section with facets coarser than the threshold (e.g.
a tube with <16 sides) floods the snap list with tessellation seams. Highlight
bands (the contact-interface meshes) are tagged `userData.noEdges` so they neither
draw outlines nor act as snap targets.

**Rules:**
- Never draw something the calculation does not know. Where a dimension is not an
  engine input (e.g. hub OD in key-connection), use a clearly representative value
  and say so in the legend.
- Interference and clearance are microns — do not fake a visible gap. Show the
  interface as a highlighted band with the pressure in the legend instead.
- **Build every part of an assembly through the same construction route.** This
  has bitten twice: the key ended up 90° from its keyway because it was placed as
  a plain `Box` instead of an extruded profile, and the interference-fit contact
  band sat 90° from the cutaway because it used a raw `CylinderGeometry` rotation
  while the shaft and hub used `prism`. See the convention table above.
- **Verify a new builder numerically**, since the geometry cannot be eyeballed from
  a terminal: run `mekanika3d.inspect`, check layer bounding boxes against the inputs,
  and confirm every layer of a cutaway leaves the *same* quadrant empty.
- **`num(v, dflt)` rejects anything ≤ 0 and substitutes the default.** That is right for a
  diameter and wrong for every signed input — a profile shift of −0.2 would silently become
  0 and draw a different gear. The gear builder carries a local `snum` for x and β; any new
  builder taking signed values needs the same.
- **The gear pair's meshing phase is a tooth centre against a space centre** on the line of
  centres. A tooth is symmetric about its own centre, so the neighbouring space centre sits
  exactly half a pitch away *at every radius* — which is what makes one construction correct
  for a profile-shifted pair too, where the reference circles are not the ones in contact.
  Check it by confirming the wheel's tooth 0 lands 180/z₂ degrees off the line of centres.

### **Shareable Calculation Links**

Every calculation module can produce a link that reopens it with the same inputs.
Live on **all twelve calculators**. No account and no backend — the inputs ride in
the URL fragment and the recipient's browser re-runs the engine.

**Files:**
- `Services/CalculationState.cs` — a module's inputs as a neutral string map, plus
  the compact wire format (`<version>~<module>~key=value;key=value`). Deliberately
  not JSON: ~half the size once base64'd, and no serialiser/trimming concerns.
- `Services/CalculationShareService.cs` — builds the link, reads it back, base64url,
  clipboard. Payload lives in the **fragment** (`#s=…`), never the query string, so
  it does not hit server logs / GA, does not disturb the 404.html SPA rewrite, and
  leaves the canonical URL intact for SEO.
- `Shared/ShareCalculation.razor` — the "Share Link" button + copy panel.
- `Shared/SharedCalculationLoader.razor` — headless; owns the restore plumbing so it
  is not repeated in twelve pages. Restores on first load **and** on a fragment-only
  navigation (a link pasted while the page is already open — the SPA does not reload).
- `wwwroot/js/share.js` — clipboard + fragment clearing only.
- `wwwroot/404.html` + the restore script in `wwwroot/index.html` — the deep-link
  handshake every shared link depends on. **See the rule below before touching either.**

**The GitHub Pages deep-link handshake.** GitHub Pages has no SPA rewrite: it serves
`404.html` with an HTTP 404 for `/key-connection`, `/key-connection#s=…` and
`/auth/callback#access_token=…` alike. `404.html` stashes `location.href` in
`sessionStorage.redirect` and sends the browser to `/`; `index.html` restores it with
`history.replaceState` **before Blazor boots**, so the router and the auth callback see
the original path and fragment.

- **The redirect must be script-driven (`location.replace`).** It shipped once relying
  on `<meta http-equiv="refresh">` alone. Browsers may delay or ignore that hint: deep
  links intermittently stalled on "Redirecting…" without ever requesting `/`, which
  silently broke *every* Share Link and *every* Google sign-in for weeks. `<noscript>`
  meta refresh is the fallback, not the mechanism. The deploy workflow greps for both
  halves of the handshake.
- **Both `sessionStorage` accesses stay in `try/catch`.** Storage throws outright when
  it is blocked; in `index.html` that exception would run before Blazor boots and take
  the whole site down for that visitor.
- **`404.html` must not redirect when `location.pathname` is already `/`** — otherwise a
  misconfigured root would spin forever.
- **The dev server hides all of this.** `dotnet run` rewrites unknown paths to
  `index.html`, so `404.html` never executes locally and deep links appear to work no
  matter how broken the handshake is. Test against a server that actually 404s.
- **Closed (Aug 2026): module URLs no longer 404.** `tools/generate-static-pages.mjs`
  runs after `dotnet publish` and writes `<route>/index.html` for every URL in
  sitemap.xml, so Pages answers `/interference-fit/` with 200 and the slash-less form
  only 301s to it. Each page carries its own title, description, canonical, OG/Twitter
  tags and a `SoftwareApplication` JSON-LD, none of which need JavaScript — which is
  what social and non-Google crawlers see. See the section below before touching it.
  `404.html` still serves everything not in the sitemap (`/login`, `/account`,
  `/my-calculations`, `/auth/callback`, `/spring`), and its `noindex` is
  correct for exactly those.

### **Static Page Generation (the 404 fix)**

`tools/generate-static-pages.mjs`, run by the deploy workflow straight after
`dotnet publish`. Four rules, each of which is the reason something is written the
way it is:

- **The shell is the *published* `index.html`, transformed — not a second template.**
  A parallel copy of that `<head>` would drift the first time a script or a `?v=`
  changes, and nothing would show it. Every replacement asserts it matched exactly
  once, so a restructured `index.html` fails the build instead of quietly emitting
  pages that have lost their canonical.
- **Metadata comes from `ModuleMetadataService.cs`, the route list from `sitemap.xml`.**
  One source of truth each, and they check each other: a sitemap URL with no module
  entry is a hard error. A hand-maintained JSON copy with a "keep in sync" comment is
  precisely the drift this repo keeps getting bitten by. Metadata with no sitemap entry
  is only *reported*, never silently dropped — an unreachable page is a decision for a
  human, not something a build step should bury. `/bolt` was the one entry this caught, and
  it was deleted rather than published.
- **Canonical, `og:url` and sitemap all use the trailing-slash form.** That is the URL
  Pages answers with 200. Pointing the canonical at the slash-less form makes a crawler
  follow a 301 and then be told the page it came from is the canonical one — a mixed
  signal for nothing.
- **The trailing-slash normaliser must keep `location.search` and `location.hash`.**
  Blazor's router does not match `/interference-fit/`, so each generated page rewrites
  the URL with `history.replaceState` before `blazor.webassembly.js` loads. A shared
  calculation is `/key-connection#s=<payload>` and the fragment *is* the payload: it
  rides through the 301 (browsers carry a fragment when the target has none) and has to
  ride through the rewrite too. Verified in a real browser, not just by reading it.

`<base href="/" />` must stay `/`. The generated files live one directory down, so
every relative asset path resolves through it; rewriting it per directory would 404
the whole framework.

**This is the same state layer cloud-saved calculations will use** (`inputs` jsonb).
Build/apply once per module, reuse for links, localStorage, and Supabase.

**To add a module:**

1. Drop the loader in at the top level (not inside an `@if`, or it won't init on the
   input-form view):
   ```razor
   <SharedCalculationLoader Module="taper-fit" OnRestore="ApplyShareState" />
   ```
2. Add the button to the results button-group:
   ```razor
   <ShareCalculation Module="taper-fit" State="@BuildShareState()" />
   ```
3. Add an `openedFromSharedLink` flag (shows the "Opened from a shared link" banner),
   reset it in `ClearForm`, and write `BuildShareState()` / `ApplyShareState()`.
   `ApplyShareState` **must be `async Task`** and end by calling the module's own
   `Calculate()` so the recipient lands on the results. `OnRestore` is an
   `EventCallback`, so Blazor re-renders automatically — do not add `StateHasChanged`.

**Rules — these are the ones that actually bit:**
- **Store by name/designation, never by list index.** Materials go in by `Name`,
  bearings by `Designation`, gears by `"Name - HeatTreatment"`, shapes by enum name.
  Inserting one row into a lookup would otherwise silently repoint every existing
  link at the wrong item. Resolve back to an index on restore, falling back to the
  current value when the name is unknown.
- **Store inputs only, never results.** The engine re-runs, so a months-old link
  reflects the current (possibly corrected) engine — important while several are
  still `IsVerified = false`.
- **Every getter takes the current value as its fallback**, so a link written by an
  older build that lacked a key leaves that field at its default, not zero.
- **Order matters when a handler resets fields.** Call the driving handler *first*,
  then reapply stored values it would have cleared: hook type before raise/thread
  (extension spring), load type before working lengths (compression spring), bearing
  type + contact-angle filter before locating the bearing, `OnShapeChanged` before
  the section dimensions, `UpdateTolerances` / `OnTaperChanged` before custom
  deviations / ratio. For values a lookup overwrites (bearing ratings, material G/Rm,
  gear x2), reapply the stored value afterwards so a manual override survives.
- **Validate dropdown values against their option set** on restore, or an unknown
  value leaves the `<select>` rendering blank.
- **Adding the loader after the page-header `</div>` has twice produced a stray
  duplicate `</div>`** (TaperFit, TorsionSpring) — check the header still balances.

### **Custom Library Entries**

Signed-in users can add their own entries to the reference libraries. Live for
**materials** and **bearings**; the table and service are already shaped for bolts.

**Files:**
- `supabase/library_items.sql` — one table for every library, kind-tagged
  (`material` | `bearing` | `bolt` | `gear-material`), `data` jsonb in the shape of the C# model,
  `name` duplicated into its own column purely to carry a case-insensitive unique
  index per (user, kind). 4 RLS policies, same pattern as `calculations`.
  **Run it once in the SQL editor after `schema.sql`.**
- `Services/CustomLibraryService.cs` — PostgREST CRUD + `RefreshAsync()`, which
  pushes the loaded items into the static providers.
- `Services/MaterialService.cs` — `SetCustomMaterials()`, `CustomMaterials`,
  `IsBuiltInName()`; `GetMaterials()` now returns built-ins **then** customs.
- `Services/BearingService.cs` — the four catalogue lists are now merged views over
  private `_…BuiltIn` fields; `SetCustomBearings()`, `IsBuiltInDesignation()`, and
  the `Type…` constants.
- `Models/LibraryItem.cs` (the row), `Models/Material.cs` and the three bearing
  classes (`CustomId` / `IsCustom`), `Models/CustomBearingDraft.cs`.
- `Pages/Materials.razor`, `Pages/Bearings.razor` — add / edit / delete, "Mine"
  badge on custom rows. Shared styles (`.entry-form*`, `.badge-custom`,
  `.row-actions`) live in `modern-icons.css`, **not** in a page `<style>` block —
  a Razor `<style>` only reaches the DOM once that page has been visited, so a
  user landing straight on `/bearings` would get an unstyled form.

**Bearings are three model classes, not one.** `Bearing` (deep groove ball +
cylindrical roller), `TaperedBearing` (+e, Y, Y0) and `AngularContactBearing`
(+contact angle, X, oil-lubricated speed). `CustomBearingDraft` is the form's union
of all three and converts on save; the stored payload's own `type` field is the
discriminator `RefreshAsync` reads back. The `Bearing` shape covers two families, so
its `Type` is what splits them.

**Load timing is the whole design.** `Program.cs` awaits `library.RefreshAsync()`
*before* `RunAsync()`, and re-runs it on `AuthStateChanged`. That is what lets every
consumer stay synchronous — pages call `MaterialService.GetMaterials()` in
`OnInitialized` and the list is already complete. Do not make the calculators await
a library load.

**Rules:**
- **Pin one `JsonSerializerOptions` to both ends of the `data` round-trip.** The two
  defaults disagree: `JsonContent.Create` writes with `JsonSerializerDefaults.Web`
  (camelCase), while `JsonElement.Deserialize<T>()` with no options matches property
  names case-*sensitively* against PascalCase. Nothing throws — every property just
  reads back as its default. This shipped once: saved materials came back with a
  blank standard and 0 for every number. `CustomLibraryService.LibraryJson` exists
  for this; use it for every read and write of `data`.
- **Custom entries go after the built-ins, never interleaved.** Pages bind material
  dropdowns by *index*; a built-in changing position would silently repoint an
  in-progress calculation at a different material.
- **A custom entry may not take a built-in's name** (`IsBuiltInName`, enforced in
  `SaveMaterialAsync`). Name is the key everywhere — share links, saved calculations,
  `MaterialService.GetMaterial(name)` — and a shadowed built-in would resolve
  differently for different users.
- **Signed out clears the custom list.** `RefreshAsync` with no token empties it
  rather than leaving the previous user's entries in a shared browser's dropdowns.
- **The edit form binds to a copy**, never to the instance in the list — a half-typed
  or failed edit must not reach the calculators.
- **Validate at the source.** The engines divide by yield strength, E, ν and
  permissible surface pressure; a zero there surfaces much later as a nonsense safety
  factor rather than an error.
- **Share links carry the material by name only.** A recipient without that custom
  material falls back to their own selection (see the share-state rules above) — the
  Materials page says so under the form. Do not "fix" this by embedding material
  properties in the link.

**Gear materials are a fourth kind, not a flavour of `material`.** `Models/Material.cs` has
no field for an ISO 6336-5 classification, and a gear grade's σ_Flim / σ_Hlim are *derived*
from that classification rather than entered — so `GearMaterial` (in `GearPairEngine.cs`) is
its own model with its own merged provider. Three things are specific to it:

- **`supabase/library_items_gear_material.sql` must be run once**, after `library_items.sql`.
  The original `kind` CHECK only allowed three values; this widens it. Everything else
  (indexes, the four RLS policies) already covers the new kind.
- **The key is `GearMaterial.Label` — `"Name - HeatTreatment"` — not the bare name.** C45
  appears twice among the built-ins with different treatments, so the name alone is
  ambiguous. That label is what the `name` column stores, what the unique index enforces,
  what share links carry, and what the gear page's dropdown resolves by. `GearPair.razor`
  formats it in exactly one place (`Label`) for this reason; a second `$"{Name} - {Heat}"`
  anywhere is how the two would drift and start resolving links to the wrong grade.
  `ToGearMaterial` splits it back on the **last** `" - "`, so a name that itself contains a
  dash survives the round trip.
- **`SetCustomGearMaterials` calls `ApplyIso6336Strength()` on the way in.** The stored jsonb
  carries the classification, not the stress numbers, so skipping this leaves σ_Flim/σ_Hlim
  at 0 — and the engine divides by them, so every safety factor for that grade comes out 0.
  The built-ins get the same treatment in the static constructor.

**To add bolts:** the table already allows the kind. `BoltService` still exposes
`public static List<…>` properties read directly by the pages — those need to become
merged accessors over private built-in fields first, exactly as `BearingService` now
does. Then extend `RefreshAsync`, and reuse the `Bearings.razor` UI shape (it is the
richer of the two: type selector plus conditional fields).

**Dropdown label convention:** every module's material select renders
`materials[i].ToString()` — `"C45 (EN 10083)"`, or just the name when the grade has
no standard. Do not go back to `.Name`; the standard is what disambiguates two
grades sharing a name. Where a select binds by value rather than index
(`SingleBolt.razor`), the **value stays the bare `Name`** — share links, saved
calculations and `GetMaterial(name)` all resolve by it.

### **Cylindrical Gear Pair — the standards chain**

The gear module is the site's deepest calculation and is meant to stay that way. The
engine is deliberately thin: `GearPairEngine` sequences the steps and holds the
inputs/outputs, while every standard's equations live in their own service so each
file can be read against the clause it implements.

| service | covers |
|---|---|
| `Iso6336DynamicFactor.cs` | K_V — ISO 6336-1 Clause 6, Method B (stiffness, resonance, speed ranges) |
| `Iso6336FaceLoadFactor.cs` | K_Hβ, K_Fβ — ISO 6336-1 Clauses 7.5 (Method C) and 7.6, incl. the Eq. (56) floor on F_βx |
| `Iso6336ShaftDeflection.cs` | f_sh — ISO 6336-1 Eq. (57)/(58) with the Figure 13 constant K′ |
| `Iso6336TransverseFactor.cs` | K_Hα, K_Fα — ISO 6336-1 Clause 8, with the 8.3.3/8.3.4 limits |
| `Iso6336ToothForm.cs` | Y_F, Y_S — ISO 6336-3 Method B, 30° tangent construction |
| `Iso6336SurfaceFactors.cs` | Z_B/Z_D, Z_E, Z_L/Z_v/Z_R — ISO 6336-2 Clauses 6, 7, 12 |
| `Iso6336LifeFactors.cs` | Y_NT/Z_NT, Y_δrelT, Y_RrelT, Y_X, Z_X, Z_W |
| `Iso6336Material.cs` | σ_Flim, σ_Hlim from ISO 6336-5 Table 1 (A·hardness + B) |
| `Iso1328Tolerance.cs` | flank tolerances, ISO 1328-1:2013 |
| `Din3967.cs` | tooth thickness allowance / tolerance series, DIN 3967:1978 Tables 1 & 2 |
| `Iso13989FlashTemperature.cs` | scuffing, ISO/TR 13989-1 flash temperature method |
| `Iso13989IntegralTemperature.cs` | scuffing, ISO/TR 13989-2 integral temperature method |
| `Iso15144Micropitting.cs` | micropitting, ISO/TR 15144-1 Method B |
| `GearToothMeasurement.cs` | tooth thickness, span W_k, over-balls M_d, chordal, backlash |

**The tooth thickness can be specified seven ways** (`ToothThicknessAllowanceMode`), all
converted to A_sne/A_sni on the way in: the automatic ISO/TR 10064-2 minimum, a DIN 3967
zone, the allowances themselves, j_bn, j_wt, j_r, W_k limits or M_d limits. The split that
matters: **backlash is a property of the pair**, so it fixes only A_sn1 + A_sn2 and needs a
`BacklashSplit` rule; **W_k and M_d are measured on one gear** and invert per gear with no
split. New enum members are appended, never reordered — the mode rides in shared links by
name.

**Rules — these are the ones that actually bit:**

- **The tip alteration coefficient k is not the tip chamfer.** k (`TipAlterationSource`)
  moves the whole tip circle in by k·m_n to restore the clearance profile shift ate; a tip
  chamfer leaves d_a alone and only shortens the usable involute. The anchor for k:
  substituting k = y − Σx into c = a − d_a1/2 − d_f2/2 collapses to c = m_n(h*_fP − h*_aP)
  exactly, for any shifted pair. Default stays `None` so links written before k existed
  still reproduce their own numbers.
- **"D_M = 9,297 ≈ 9" means measure with the 9 mm ball.** Verifying against the DIN 3967
  Clause 5 example with the *best* size instead of the ball actually used put M_d out by
  0.9 mm and looked like an engine bug. With the right ball the module reproduces the
  standard's published dimensions to under 1 µm (pinion M_d 117.7096 vs 117.710, wheel
  508.057 vs 508.058, wheel W_k 177.6544 vs 177.654).
- Our best-size ball differs from that example's (9.557 vs 9.297) because we target
  mid-flank rather than the reference cylinder — see the rule further down. That is a
  deliberate difference, not drift.
- **Both permitted limits sit below nominal**, and each gear has its own mean allowance.
  Checking the wheel against the pinion's mean is how a passing implementation looks broken.

- **Never apply Y_ε together with ISO 6336-3 Method B.** Method B applies the load at the
  *outer point of single pair tooth contact*, so load sharing is already inside Y_F. Y_ε
  belongs to the DIN 3990 Method C scheme, where the load sits at the *tip* (Y_Fa, Y_Sa)
  and Y_ε corrects afterwards. Mixing them shipped once: σ_F0 came out 29 % low and every
  tooth root safety factor was ~40 % too high. The two schemes were cross-checked against
  each other and against the classical Lewis form factor — all three agree at ~122 MPa for
  the default gear; the mixed version gave 86 MPa.
- **Every curve is anchored, not eyeballed.** Y_NT/Z_NT, Y_RrelT, Y_X, Z_X, Y_B and Y_DT
  each reproduce their standard's own tabulated end points (e.g. Y_DT = 0.701 at
  ε_αn = 2.5). Y_RrelT is the one to be careful with: Rz = 10 µm is the reference
  roughness and the standard names 1.000 there, but Table 4's three expressions
  actually land on 1.00100, 1.00164 and 0.99436. This note used to record 1.000 as
  exact, and a test asserting it to three decimals fails against correct code. If you touch a
  constant, re-check the anchor — that is what makes these trustworthy rather than plausible.
- **`f_sh` is never silently zero.** Shaft deflection dominates K_Hβ — on the default gear,
  12 µm takes it from 1.76 to 3.02 — so `ShaftDeflectionSource` makes the choice explicit:
  *Calculated* (ISO 6336-1 Eq. 57 from span, offset, diameter and the Figure 13 arrangement),
  *Manual*, or *Neglected*. Calculated is the default, and when the shaft dimensions are left
  blank the engine stands in a representative shaft (l = 3b, d₁/d_sh = 1.15) **and says so in
  the results**. Never make Neglected the default again: this module does not model shafts,
  but pretending they are rigid is the one answer that is always wrong.
- **The Eq. (56) floor on F_βx was missing and is non-conservative when omitted.** Both
  Eq. (52) and Eq. (53) carry `F_βx ≥ F_βx,min = max(0.005·F_m/b, 0.5·f_Hβ)`. Only the
  additive Eq. (52) is implemented — Eq. (53) is the compensatory branch and is only allowed
  once a favourable contact pattern has been *verified*, which a web calculator cannot do.
- **The "best size" ball targets mid-flank, not the reference cylinder.** The textbook rule
  (contact at the reference cylinder) only works for an unshifted gear; on a positively
  shifted one it picks a ball that never reaches past the tip, so the measurement cannot
  physically be taken. Same target as the span measurement.
- **Allowances are negative, and the "nominal" W_k / M_d is the zero-allowance theoretical
  value.** Both permitted limits therefore sit *below* nominal. The results label them
  "Largest/Smallest Permitted (at A_sne/A_sni)" for exactly this reason — "Upper/Lower
  Limit" next to a larger nominal reads as a bug.
- **M_d limits are recomputed, not differentiated.** `MdForThicknessDeviation` re-runs the
  whole involute solve with the thinned tooth. Exact, and no linearisation to get wrong.
- **Status text uses literal characters, never HTML entities.** Razor escapes the strings it
  renders, so a `GetSafetyStatus` returning `"&#10003; OK"` puts that text on the page
  verbatim. This module shipped that way. Use `"✓ OK"` / `"⚠️ Marginal"` / `"❌ FAIL"`,
  matching KeyConnection.
- **ISO 1328-1 edition mismatch is real and is surfaced in the UI.** Tolerances use the 2013
  edition (classes 1–11); ISO 6336-1:2006 normatively references the 1995 edition (grades
  0–12). The numbering and formulae differ. Do not silently equate them.

**Six more, found in Aug 2026 by benchmarking against KISSsoft Tutorial 8** (z 16/43,
m_n 1.5, β 25°, a 48.9, x₁ 0.3215 — the engine driven with the tutorial's exact inputs and
~120 quantities diffed against its printed report). Geometry, kinematics, forces and the
control dimensions already matched to under 0.1 %; every one of these was in the strength
chain, and four of the six were **non-conservative**:

- **Z_NT has two rows in ISO 6336-2 Table 2, and only the optimistic one was implemented.**
  The steel/hardened group splits on whether pitting is acceptable on the finished flank:
  the knee sits at **5×10⁷** cycles normally and at 10⁹ only when it is. At 1.1×10⁸ cycles
  that is Z_NT = 1.00 versus 1.13 — σ_HP inflated 13 %, and the module reported S_H = 1.30
  for a pair the reference rates 1.15. The conservative row is the default;
  `LimitedPittingPermissible` selects the other. Anchor: exponent ln(1,6)/ln(500) = 0,075627
  reproduces Z_NT = 1,0135 at N = 4,186×10⁷.
  **An anchor test against the curve you chose to implement cannot tell you the curve is the
  wrong one.** That is how this survived an earlier review that called it verified.
- **Y_F and Y_S describe the tooth that is *cut*, not the nominal one.** Feed them
  x_E = x + A_sne/(2 m_n tan α_n), not x. The nominal tooth is thicker, so s_Fn came out
  high, the 30° tangent point too high, and σ_F0 ~4 % low — on the unsafe side. This is why
  `CalculateMeasurements()` runs **before** `CalculateToothRootStrength()` in `Calculate()`:
  the allowances must be resolved first. Do not reorder them back. `Iso6336ToothForm` was
  never wrong — fed x_E it reproduces the reference's s_Fn, q_s and Y_F·Y_S to 0.25 %.
- **C_B in ISO 6336-1 Eq. (86) references h_fP = 1,2 m_n, not 1,25.** It shipped with 1,25,
  which made the factor exactly 1,000 for the ISO 53 profile A rack nearly every gear here
  uses — i.e. the factor did nothing for the common case, which is the tell. Correct value
  0,975; c′ and c_γ were ~2,5 % high, carrying into K_V, K_Hβ and K_Hα.
- **K_Bγ belongs in w_Bt in *both* scuffing methods.** It was applied in the integral method
  and in micropitting but not in the flash method, so the same gear was loaded two different
  ways depending on which criterion was running — 20 % low on this pair. Both parts now call
  one shared `HelicalLoadFactor`. When you touch one scuffing method, check the other.
- **Tooth root stress uses each gear's own face width**, capped at the mating width plus one
  module of overhang per end (ISO 6336-3). `min(b1, b2)` for both was conservative but wrong,
  and showed up as exactly b₂/b₁ on the wider wheel.
- **K_V defaulted to a hard-coded 1.10 while K_Hβ beside it was calculated.** Every result on
  the page used that stand-in unless the user opened the dialog; in the resonance range it is
  an order of magnitude low. Both default to *calculated* now, and `KvShown`/`KhbShown` return
  1.000 rather than 0 before the first run — a 0 in the box invites the user to "correct" it,
  which silently flips the field back to manual. **Test the page, not just the engine**: a
  harness that sets `UseDirectDynamicFactor = false` to compare against a standard will never
  see this class of bug.
- **The K_V dialog then opened with nothing in it.** Moving the deviation fields off the card
  and into the dialog kept the `@if (engine.UseMeasuredDeviations)` around them but dropped the
  checkbox that was the only thing ever setting that flag, so it stayed false for good and the
  whole of Method B's input was unreachable — the dialog showed its intro and its footer and no
  fields at all. The switch is now a *string-backed* select (`DeviationSource`), for the same
  reason `ToleranceSource` is one: `@bind` on a `<select>` goes through the value attribute, and
  the renderer drops that attribute when it is handed a bool `false`, so a bool-bound select
  cannot tell its two options apart. The class-derived values stay on screen `readonly`, the way
  the centre distance deviations do, so the dialog is never blank and you can see what the
  standard picked. **When a conditional block loses its condition's only writer, the block is
  dead** — check for one whenever inputs move between a card and a dialog.

**Five more, found in Aug 2026 against a real ISO 6336:2006 Method B report** (KISSsoft
03/2017; z 21/42, m_n 2, β 20°, a 68, x₁ 0,5, VG 220 at 70 °C). Tutorial 8 could never settle
these because its printed report is a DIN 3990 run — this one is the ISO run, and it closed the
~17 % on S_H that had been open for three rounds:

- **Z_β = 1/√(cos β), not √(cos β)** — ISO 6336-2 as corrected by **Corrigendum 1:2008**.
  √(cos β) is the DIN 3990-2 form; it is below 1, so it *credits* a helical pair instead of
  penalising it. This was the largest non-conservative error the flank side has had: 6 % on
  σ_H0 at β = 20°, 10 % at β = 25°. Three confirmations — the report prints the formula; its
  σ_H0 = 685,35 only reconstructs with 1,032 (the other factors give 664,29); and the same
  software's DIN run prints Z_β = 0,952 = √(cos 25°), i.e. it applies the two forms to the two
  standards exactly as the corrigendum implies.
- **Z_NT keeps descending past the 5×10⁷ knee**, to 0,85 at 10¹⁰ (exponent
  ln(1/0,85)/ln(200) = 0,030674). It was left flat at 1,0 as far as 10⁹. Both branches now
  reproduce the report to four figures: 1,008 at 4,5×10⁷ and 0,982 at 9,0×10⁷.
- **f_ma is the root sum of squares of the two gears' f_Hβ, not the larger** (Eq. 64). The
  deviations are independent. Anchor: f_Hβ 14 and 15 µm give f_ma = 20,51, which is what the
  report carries; max() would give 15.
- **Tip relief C_a was not an input at all.** Both scuffing services accepted `Ca1`/`Ca2` and
  the engine never supplied them, so every gear was rated as an unmodified profile. Now
  `TipRelief1/2`. Note the module *deliberately* keeps ISO/TR 13989-2 Clause 6.1.12, which only
  credits tip relief at ISO 1328-1 grade 6 or better — the reference software credits it
  regardless (X_Ca = 1,251 on a grade 8 pair). Fed grade 6 our X_Ca comes out 1,2508 against
  their 1,251, so the *equation* agrees and only the restriction differs; the results say so
  rather than silently ignoring an entered C_a.
- **Ra is its own input, not Rz/6.** The report calls out Rz 4,8 *and* Ra 0,60 — a ratio of 8.
  µ_m scales as Ra^0,25, so Rz/6 fed the flash temperature a 33 % high roughness.
  `FlankRoughnessRa1/2`, with Rz/6 as the fallback.

**What that leaves.** Fed the report's *own* ISO 1328-1:1995 deviations, our chain reproduces
it to K_Hβ 0,2 %, σ_H 0,4 %, S_H 0,6 % and S_F 2 %. Run with our own ISO 1328-1:2013 class 8
tolerances it lands S_H ~4 % low and S_F ~8 % low, because the 2013 class 8 is coarser than the
1995 grade 8 (f_Hβ 16/16 against 14/15). **That single edition mismatch is now the dominant
remaining difference on this module**, and it is conservative. Implementing the 1995 grades
alongside the 2013 classes is the next real accuracy step.

**The scuffing coefficient of friction, resolved.** µ_m ran ~1/0,85 high in both methods on
two unrelated gears. Two causes, both now fixed, and the size of each was recovered by solving
the reference reports' own printed numbers backwards:

- **ISO/TR 13989-1 Eq. (28): X_R = √Ra, not Ra^0,25.** Part *2* has the different expression
  2,2·(Ra/ρ_redC)^0,25, and that quarter-power had been carried across into Part 1. Anchor: both
  reports use Ra = 0,60 µm and nothing else in common, and both require X_R = 0,7746 = √0,60.
- **K_Bγ multiplies w_Bt in the flash *temperature* but not in the *friction*.** Folding it into
  w_Bt wholesale — which is how it was first added — put it into both. The reports print the two
  separately for exactly this reason: "wBt 195,426" with "Kbg = 1,220, wBt*Kbg = 238,368"
  alongside. Part 2 *does* use K_Bγ in its friction; the two parts genuinely differ, so do not
  "harmonise" them.
- Related: Part 2 is now fed the entered Ra, not Ra/0,6. The 1/0,6 scale-up assumed Part 2 wants
  the as-manufactured roughness while Part 1 wants the run-in one; the references feed one Ra to
  both and set X_E = 1,000, which is what X_E is for.

µ_m now lands +1,4 % and +5,2 % on the two reports, and that residual is the load, not the
friction: our w_Bt is 9 % and 29 % high because of K_Hβ (the ISO 1328 edition on one, the
favourable-contact-pattern Eq. (53) on the other), and µ ∝ w^0,2 accounts for it exactly.

**θ_S was never wrong.** It was recorded as "14 % low" — that was measured against Tutorial 8,
whose printed report is a DIN 3990 run. Against both ISO 6336 reports it lands within 0,2 %.
Do not compare a scuffing temperature across the two standards.

**What Tutorial 8's Figure 19 says now.** The ISO run it prints five numbers for — the one that
was ~17 % away for three rounds — is reproduced to S_H −1,8 % and S_F −3,8 %, conservative. Its
*scuffing* section still differs by design: it is DIN 3990-4, which has its own friction formula
and folds K_Bγ into the printed w_Bt, so our µ_m of 0,074 against its 0,111 is a standards
difference, not an error. Fixing Part 1 moved us toward the two ISO references and away from the
DIN one, which is the right direction for an ISO implementation.

Still open on scuffing: the mean flash temperature runs ~21 % high on the first report while µ_m
and w_Bt together only account for ~8 %, so the load sharing factor X_Γ is the next place to
look (the report prints X_Gam = 0,907).

**The lubricant library** (`Services/LubricantLibrary.cs`). Ten named products, so the card asks
for a lubricant instead of six numbers. It is small on purpose:

- **Every field is copied from the manufacturer's published data sheet and cited per entry.**
  Viscosity, density and above all the FZG stages are properties of a *product*, not of a
  viscosity grade. Inventing a "typical" FZG stage would put a made-up number straight into a
  scuffing safety factor. Ten verified entries beat fifty plausible ones. **Where a value is not
  published it is `null`, the field stays editable, and the card says "not from this product".**
- **The FZG test variant is a trap.** ISO/TR 13989 Eq. (99) is calibrated to **A/8,3/90**
  (ISO 14635-1). Data sheets also quote **A/16,6/90**, and Klüber test their gear GREASES on the
  special low-speed rig **A/2,76/50**. All three are different tests and their stage numbers are
  not interchangeable. `FzgStageA8390` is only ever filled from a line that names A/8,3/90
  explicitly — Mobilgear's sheet prints two variants on separate lines, which is how the
  distinction became visible, and both Microlube greases publish only A/2,76/50 so they carry
  `null` while Klübersynth GE 46-1200, which does publish A/8,3/90, carries 12. Micropitting is
  FVA 54 / FZG GF-C, a fourth rig again.
- **The lubricant list is filtered by lubrication method** — greases when the mesh is
  grease-packed, oils otherwise — and the last pick on each side is remembered, so toggling
  oil → grease → oil does not silently change the user's viscosity grade. Names that shipped
  once must keep resolving: `Renamed` maps the old "… (grease)" spellings, because share links
  carry the name.
- **Resolve by name, never by index** — same rule as the material and bearing libraries, because
  share links carry the name.
- **A grease is a base oil plus a thickener.** `LubricationMethod.Grease` only sets X_S = 1,2;
  the base oil *type* and the base oil *viscosities* still apply and are still asked for. Grease
  + Mineral is a correct combination, not a contradiction — the KISSsoft tutorial's own gear runs
  on exactly that. NLGI consistency enters no equation here and is not stored.
- **Naming a default preset is not the same as loading it.** `ResetEngine` must call
  `OnLubricantChanged()`, or the card claims a product while the engine holds its own defaults.
  It shipped that way for one build: the summary read "Mobilgear 600 XP 220 … ν₁₀₀ = est." for an
  oil whose sheet publishes 19,0.
- In `ApplyShareState`, `OnLubricantChanged()` runs **before** the stored oil values are
  reapplied, so a link's own edits win over the preset.

**Scuffing — ISO/TR 13989-1, flash temperature method.** Θ_B(Γ) = Θ_M + Θ_fl(Γ), swept over
200 points on the path of contact. Three rules from getting it working:

- **X_M carries a factor of 1000 that is not in the printed formula.** Clause 3.2 says the
  units of B_M, c_γ and X_M "are adapted to the mixed application of metre and millimetre" —
  E_r is N/mm² while B_M is N/(mm^½·m^½·s^½·K). The anchor is Eq. (A.14): E = 206000, ν = 0.3,
  B_M = 435 must give X_M = 50.0. Without the 1000 the flash temperature comes out at 0.03 K
  instead of 27 K, which is how it was caught — a result three orders of magnitude out still
  produced a plausible-looking safety factor.
- **X_αβ comes from Part 2 Eq. (13), not from Part 1 Eq. (A.8).** Part 1 prints an abbreviated
  form that drops cos^0.25(α_n) and cos^0.5(α_t) and lands 1.6 % (spur) to 3.1 % (β = 20°)
  below its own Table A.1. Part 2's full expression reproduces both parts' tables to 5·10⁻⁴.
  This was found only by implementing the second method and comparing — which is the whole
  argument for carrying both.
- **Lubrication method reaches scuffing and nothing else.** ISO 6336-2's film factors Z_L, Z_v
  and Z_R see only viscosity, velocity and roughness; the method enters solely through X_S in
  Eq. (22), which sets the bulk temperature. The flash temperature itself is unchanged by it —
  there is a test for exactly that.
- **Judge scuffing on the margin in kelvin, not on S_B.** Clause 10.5 says a safety expressed
  as a quotient of temperatures "may cause confusion" and advises a demanded difference,
  "for instance ≥ 50 K". The results colour the row on the margin.
- The oil temperature is either entered or built as ambient + rise, with the rise supplied by
  the user. This module does not model a thermal network and must not pretend to.

**Micropitting — ISO/TR 15144-1, Method B.** A lubrication-regime failure, so the criterion
is a ratio of lengths: λ_GF,Y = h_Y/Ra, evaluated at the seven points A, AB, B, C, D, DE, E
and minimised. Three things worth knowing:

- **The flash temperature is exactly zero at the pitch point C**, because the contact rolls
  without sliding there. That falls out of the equations rather than being imposed, so it is
  the cheapest sanity check on the whole sweep — if C is not zero, the tangential velocities
  are wrong.
- **λ_GFP comes from a digitised figure, and that is the weakest number in the module.**
  Annex A Figure A.1 is informative, drawn for Ra = 0.50 µm, and has to be read off a chart.
  The engine therefore accepts λ_GFP directly and the results say where the value came from.
- **The levers are roughness and viscosity, not material.** Halving Rz roughly doubles λ;
  a stronger steel does nothing. Do not let anyone "fix" a micropitting result by changing
  the material.

**Agreed for the next change to this module** (Aug 2026, not yet done):

- **Helix hand is not an input.** It reaches no ISO 6336 equation, which is why it was never
  asked for — but the 3D viewer needs it and the reference reports print it (`Hand of gear:
  right / left`). Add a select under the helix angle for GEAR 1 only; on an external pair gear 2
  is necessarily the opposite hand, so it is derived. Hide it at β = 0. It has to reach both the
  share state and `Build3dParameters()`, or a shared link draws a mirrored model. Check what the
  viewer currently assumes before wiring it.
- **The oil temperature defaults to the estimated route.** `OilTemperatureFromAmbient` is `true`,
  so a new calculation runs on ambient + a 50 K rise rather than on a stated θ_oil. That is the
  same class of problem the K_V default was: a stand-in reaching the results without being asked
  for. ISO/TR 13989 wants θ_oil itself, and commercial software asks only for it (its root and
  flank temperature fields belong to dry-run and plastic gears, which this module does not
  cover). Flip the default to "entered directly" and keep ambient + rise as the opt-in estimate.

**Two deliberate conservatisms are named in the results card**, because a visitor comparing
against commercial software otherwise reads them as errors:

- **K_Hβ stays on the additive Eq. (52) with B₁ = B₂ = 1.** Commercial packages apply Eq. (53)
  and B = 0,7 once told the helix carries end relief and the contact pattern is favourable. On
  the 22/45 benchmark that is ~9 % on K_Hβ and ~6 % on S_H. Note the *floor* is what actually
  governs on their side: their F_βx of 4,50 µm is Eq. (55)'s max(0,005·F_m/b; 0,5·f_Hβ), not the
  4,39 µm the compensating form gives. Our engine computes the same 4,50 floor — it simply never
  binds, because the additive branch sits far above it.
- **σ_Flim comes from ISO 6336-5:2016**, i.e. 425 MPa for case-hardened MQ. The 2006 edition
  gives 430, worth ~1,2 % on S_F. Both are defensible; the newer table is the one implemented.

**Out of scope, stated in the results card:** tooth flank fracture, planetary and internal
arrangements. Within micropitting: the profile-modified load sharing branches and the
buttressing factor, and ε_α > 2 which the standard restricts to Method A. Within scuffing: bevel/hypoid geometry and the profile-modified load sharing
branches (Clauses 9.3, 9.5, 9.7) are not implemented — a stated tip relief still reaches the
approach factor.

**Verifying a change:** `Tests/Mekanika.Tests.csproj` (xUnit, `dotnet test`), run by the
deploy workflow before Publish. The gear anchors live there as tests rather than as prose:
inverse-involute round trip, W_k for m=1 z=20 20° = 7.6604 mm, Z_H = 2.494573 against its
closed form, Z_E = 189.8, ε_α = 1.6352 by hand, the life-factor curve ends, and that K_V and
K_Hβ are calculated rather than the stand-in 1.10 they once defaulted to. That anchor set is
how the Y_ε bug was found; a plausible-looking safety factor will not reveal it.

**An anchor proves the curve is evaluated right, not that it is the right curve.** Z_NT
passed its own anchor for as long as it was on the wrong row of ISO 6336-2 Table 2. Where a
test can check direction as well as a point — monotonic life factors, a rougher root never
scoring better, doubling the power never raising a safety factor — it does, because that is
the part a single point cannot see.

### **Related Calculators Feature**

```razor
<!-- Create: Shared/RelatedCalculators.razor -->
@inject NavigationManager NavigationManager

<div class="card" style="margin-top: 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);">
    <div class="card-header" style="color: white; border-color: rgba(255,255,255,0.2);">
        <span>🔗</span>
        <h2 style="color: white;">Related Calculators</h2>
    </div>
    <div class="modules-grid" style="grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));">
        @foreach (var module in RelatedModules)
        {
            <a href="@module.Url" class="module-card" style="background: white;">
                <div class="icon">@module.Icon</div>
                <h3>@module.Name</h3>
                <p style="font-size: 12px; color: #666;">@module.ShortDescription</p>
                <span class="badge">@module.Standard</span>
            </a>
        }
    </div>
</div>

@code {
    [Parameter] public string CurrentModule { get; set; } = "";

    private List<RelatedModule> RelatedModules => GetRelatedModules(CurrentModule);

    private List<RelatedModule> GetRelatedModules(string currentModule)
    {
        // Define relationships
        var relationships = new Dictionary<string, List<string>>
        {
            ["interference-fit"] = new() { "taper-fit", "key-connection", "clamp-connection" },
            ["taper-fit"] = new() { "interference-fit", "key-connection" },
            ["key-connection"] = new() { "interference-fit", "taper-fit", "single-bolt" },
            ["single-bolt"] = new() { "key-connection", "clamp-connection" },
            ["ball-bearing"] = new() { "roller-bearing", "tapered-roller-bearing" },
            ["roller-bearing"] = new() { "ball-bearing", "tapered-roller-bearing" },
            // ... add more relationships
        };

        if (!relationships.ContainsKey(currentModule))
            return new List<RelatedModule>();

        return relationships[currentModule]
            .Select(url => ModuleDatabase.GetModule(url))
            .Where(m => m != null)
            .Take(3)
            .ToList();
    }
}

public class RelatedModule
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Icon { get; set; }
    public string ShortDescription { get; set; }
    public string Standard { get; set; }
}
```

### **3-Month Action Plan**

**Month 1: Foundation & Quality**
```
Week 1:
✅ Setup email addresses (Zoho Mail)
✅ Migrate to GA4
✅ Add SEO meta tags (all pages)
✅ Verify top 3 modules
✅ Create YouTube channel

Week 2:
✅ Update 5 modules to KeyConnection format
✅ Add verification badges
✅ Implement enhanced event tracking
✅ Create sitemap.xml

Week 3:
✅ Produce channel trailer video
✅ Produce first 3 tutorial videos
✅ Add Related Calculators component
✅ Update remaining modules

Week 4:
✅ Launch YouTube channel
✅ Add structured data (Schema.org)
✅ Performance optimization (lazy loading)
✅ Carbon Ads implementation
```

**Month 2: Content & Growth**
```
Week 1-2:
✅ Publish 2 tutorial videos/week
✅ Start blog section (SEO content)
✅ Reddit/forum outreach campaign
✅ LinkedIn professional network

Week 3-4:
✅ Complete all tutorial videos
✅ Email newsletter setup
✅ Community features (ratings)
✅ User testimonials collection
```

**Month 3: Monetization & Scale**
```
Week 1-2:
✅ Freemium model implementation
✅ Payment integration (Stripe)
✅ User accounts system
✅ Calculation history feature

Week 3-4:
✅ B2B outreach campaign
✅ API access (beta)
✅ Advanced features (batch calc)
✅ Affiliate partnerships
```

**Target Metrics (End of Month 3):**
```
Conservative:
- 500 users/month
- 5,000 events/month
- 25+ YouTube subscribers
- 10+ backlinks
- $50/month revenue

Aggressive:
- 1,000 users/month
- 10,000 events/month
- 100+ YouTube subscribers
- 25+ backlinks
- $200/month revenue
```

---

**Last Updated:** 2026-02-19
**Template Version:** 3.1 (Module Status Updated)
**Standard Format:** `Pages/KeyConnection.razor` + `Services/KeyConnectionEngine.cs`
**Contact:** contact@mekanika.org
**YouTube:** youtube@mekanika.org

---

## Quick Checklist for New Modules

When creating or updating a module, verify:

- [ ] Two-phase interface (Input → Results with navigation buttons)
- [ ] Four input cards with proper icons (📏 Geometry, 📐 Dimensions, ⚡ Loading, 🧱 Materials)
- [ ] Results organized in 5 cards: Geometry, Forces, Stresses, Safety Factors, Design Recommendation
- [ ] All tables use `.results-table` class with 4-column structure
- [ ] Important values use `.highlight` class
- [ ] Safety factor rows use color coding (danger/warning/success)
- [ ] Helper methods `GetSafetyRowClass()` and `GetSafetyStatus()` exist
- [ ] Design recommendation card with margin calculation
- [ ] Context-aware alerts (3 levels: danger/warning/success)
- [ ] PDF export button calls `generatePdf("results-content", filename)`
- [ ] Complete CSS styles copied from KeyConnection
- [ ] Engine follows 6-step calculation pattern
- [ ] All properties have units in comments
- [ ] Standard data (if applicable) with lookup function

**If unsure, open KeyConnection.razor and KeyConnectionEngine.cs and copy directly!**
