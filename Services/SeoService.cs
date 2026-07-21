using Microsoft.JSInterop;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// SEO metadata management service for Blazor WebAssembly
/// Dynamically updates meta tags and structured data via JavaScript interop
/// </summary>
public class SeoService
{
    private readonly IJSRuntime _jsRuntime;

    public SeoService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Set meta tags for current page
    /// Updates title, description, Open Graph, Twitter Card, and canonical URL
    /// </summary>
    /// <param name="metadata">SEO metadata object</param>
    public async Task SetMetaTags(SeoMetadata metadata)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("setSeoMetaTags", metadata);
        }
        catch (Exception ex)
        {
            // Fail silently - SEO failures should not break user experience
            Console.WriteLine($"SEO metadata update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Set structured data (Schema.org JSON-LD) for current page
    /// Improves search engine understanding and enables rich snippets
    /// </summary>
    /// <param name="structuredData">Structured data object (will be serialized to JSON-LD)</param>
    public async Task SetStructuredData(object structuredData)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("setStructuredData", structuredData);
        }
        catch (Exception ex)
        {
            // Fail silently - structured data failures should not break user experience
            Console.WriteLine($"Structured data update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Set structured data with aggregate rating (for SEO rich snippets)
    /// Adds aggregateRating field to structured data if rating data is available
    /// </summary>
    /// <param name="structuredData">Base structured data object</param>
    /// <param name="averageRating">Average rating (1.0-5.0)</param>
    /// <param name="ratingCount">Total number of ratings</param>
    public async Task SetStructuredDataWithRating(object structuredData, double? averageRating = null, int? ratingCount = null)
    {
        try
        {
            // If no rating data, use standard method
            if (!averageRating.HasValue || !ratingCount.HasValue || ratingCount.Value == 0)
            {
                await SetStructuredData(structuredData);
                return;
            }

            // Create structured data with aggregateRating
            // Note: We need to use dynamic object manipulation or reflection
            // For simplicity, we'll use JavaScript to merge the rating data
            await _jsRuntime.InvokeVoidAsync("setStructuredDataWithRating",
                structuredData,
                averageRating.Value,
                ratingCount.Value);
        }
        catch (Exception ex)
        {
            // Fail silently and fallback to standard structured data
            Console.WriteLine($"Structured data with rating update failed: {ex.Message}");
            await SetStructuredData(structuredData);
        }
    }
}

/// <summary>
/// SEO metadata model containing all meta tag information
/// </summary>
public class SeoMetadata
{
    // Primary meta tags
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Keywords { get; set; } = "";
    public string CanonicalUrl { get; set; } = "";

    // Open Graph (Facebook, LinkedIn)
    public string OgTitle { get; set; } = "";
    public string OgDescription { get; set; } = "";
    public string OgImage { get; set; } = "";
    public string OgUrl { get; set; } = "";
    public string OgType { get; set; } = "website";

    // Twitter Card
    public string TwitterCard { get; set; } = "summary";
    public string TwitterTitle { get; set; } = "";
    public string TwitterDescription { get; set; } = "";
    public string TwitterImage { get; set; } = "";
}
