using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MechanicalCalculatorWeb;
using MechanicalCalculatorWeb.Services;

// Every number on this site is formatted and parsed the same way, whatever the
// visitor's browser language is set to. This has to be the FIRST thing that runs:
// it must be in place before any component renders.
//
// Blazor WebAssembly otherwise takes CurrentCulture from the browser, and that
// split the app against itself. The 827 result-table values are written with
// ToString("F1") and friends, which follow CurrentCulture - so a Turkish browser
// rendered "40,5". The 208 <input type="number"> boxes do NOT: Blazor always binds
// those through InvariantCulture, because the HTML spec fixes the format of a
// number field's value. Same page, same quantity, two different decimal separators.
//
// It also fixes a quieter one. BoltCalculationEngine falls back to
// `materialName.ToLower() switch { "cast iron" => 300, ... }` for names the material
// database does not carry, and Turkish lowercases 'I' to dotless 'ı' - so "Cast Iron"
// became "cast ıron", matched nothing, and silently returned the 235 MPa mild-steel
// default. Invariant casing makes those comparisons mean what they read as.
//
// Invariant rather than a named locale: it is what the input boxes, the share links,
// the saved calculations and the PDF already use, and a decimal point is what the
// DIN/ISO documents these engines implement are written with.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Supabase configuration (bound from wwwroot/appsettings.json — publishable key only)
var supabaseConfig = builder.Configuration.GetSection("Supabase").Get<SupabaseConfig>() ?? new SupabaseConfig();
builder.Services.AddSingleton(supabaseConfig);

// Core Services (Static Data → Singleton)
builder.Services.AddSingleton<MaterialService>();

// Analytics & SEO Services
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<SeoService>();
builder.Services.AddSingleton<ModuleMetadataService>();

// Feedback Service
builder.Services.AddScoped<FeedbackService>();

// Shareable calculation links (state layer reused by cloud save later)
builder.Services.AddScoped<CalculationShareService>();

// Authentication (Supabase GoTrue over HttpClient)
builder.Services.AddScoped<SupabaseAuthService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();

// Cloud-saved calculations (Supabase PostgREST, RLS-scoped to the user)
builder.Services.AddScoped<CalculationStorageService>();

// The user's own library entries (custom materials), merged into MaterialService
builder.Services.AddScoped<CustomLibraryService>();

var host = builder.Build();

// Restore any persisted session before the first render, so the navbar shows the
// signed-in state immediately rather than flashing "Sign In".
var auth = host.Services.GetRequiredService<SupabaseAuthService>();
await auth.InitializeAsync();

// Load the user's custom library entries before the first render too, so every
// calculator's material dropdown is already complete and every consumer downstream
// can stay synchronous. Signing in or out re-runs this; signed out it clears, which
// is what stops one user's materials outliving their session in a shared browser.
var library = host.Services.GetRequiredService<CustomLibraryService>();
await library.RefreshAsync();
auth.AuthStateChanged += () => _ = library.RefreshAsync();

await host.RunAsync();
