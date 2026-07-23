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

var host = builder.Build();

// Restore any persisted session before the first render, so the navbar shows the
// signed-in state immediately rather than flashing "Sign In".
await host.Services.GetRequiredService<SupabaseAuthService>().InitializeAsync();

await host.RunAsync();
