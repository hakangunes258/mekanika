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

Verification status is a single flag on `ModuleInfo` in
`Services/ModuleMetadataService.cs`:

```csharp
IsVerified = true,                                   // drives the module badge
VerificationStandards = new[] { "DIN 6885", "ISO 773" },
```

Set `IsVerified = false` for any module whose engine uses a simplified or
uncalibrated model, and record why in a comment next to the flag. Currently
`false` for: **gear-pair** (ISO 6336 factors heavily simplified),
**clamp-connection** (split-hub model; single-slit lever effect not modelled),
and **bolt** (general-purpose, no standard reference).

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
**key-connection**, **compression-spring**.

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

**Measurement tool:** the "Measure" button lets the user click two points on the
model and reads out the straight distance plus its **axial (ΔX)** and **radial**
components. Model units are millimetres — builders are fed engine values directly,
so no scaling is applied and hit points are real dimensions. Measurements are
cleared whenever the geometry moves or changes (explode toggle, spring state).

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
  a terminal: check layer bounding boxes against the inputs, and confirm every
  layer of a cutaway leaves the *same* quadrant empty.

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
