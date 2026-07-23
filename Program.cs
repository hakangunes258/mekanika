using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MechanicalCalculatorWeb;
using MechanicalCalculatorWeb.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

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

await builder.Build().RunAsync();
