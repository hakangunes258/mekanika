using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MechanicalCalculatorWeb;
using MechanicalCalculatorWeb.Services;

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
