using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MechanicalCalculatorWeb;
using MechanicalCalculatorWeb.Services;

// The site shows one number format to every visitor, whatever their machine's
// language is. This has to be the FIRST thing that runs: it must be in place
// before any component renders.
//
// Blazor WebAssembly otherwise takes CurrentCulture from the browser, so the
// ~830 ToString("F1")-style calls in the result tables rendered "40,5" on a
// Turkish machine and "40.5" on an American one. The UI is English throughout,
// so it reads one way for everyone.
//
// KNOWN AND ACCEPTED: this does not reach the input boxes, and cannot. The 204
// <input type="number"> fields hold an invariant value in the DOM - the HTML
// spec fixes that - but the browser DRAWS them in the operating system's locale,
// and nothing on this side changes that. A `lang="en"` attribute on the document
// or on the field itself was tried and makes no difference (Chromium still draws
// 40,5 under a Turkish system locale). So on a non-English machine the entry
// field reads "40,5" while the results read "40.5". That mismatch is the price
// of a stable results format and was chosen deliberately.
//
// Do NOT "fix" it by moving the fields to type="text". Blazor would then parse
// them with the invariant culture, and a visitor who types "40,5" - which the
// number field accepts today and converts correctly - would have the value
// silently read as nothing. A cosmetic mismatch is cheaper than losing an input.
//
// This pin is about presentation only. Nothing that has to round-trip depends on
// it: CalculationState writes and reads every number with an explicit
// InvariantCulture, and the lookup keys in BoltCalculationEngine and BoltService
// use ToLowerInvariant for the same reason.
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
