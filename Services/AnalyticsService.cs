using Microsoft.JSInterop;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Google Analytics 4 event tracking service for Mekanika
/// Provides typed methods for tracking user interactions across calculator modules
/// </summary>
public class AnalyticsService
{
    private readonly IJSRuntime _jsRuntime;

    public AnalyticsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Track successful calculation completion
    /// </summary>
    /// <param name="moduleName">Calculator module name (e.g., "key-connection")</param>
    /// <param name="parameters">Optional calculation parameters to track</param>
    public async Task TrackCalculationCompleted(string moduleName, Dictionary<string, object>? parameters = null)
    {
        var eventParams = new Dictionary<string, object>
        {
            { "module_name", moduleName },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                eventParams[param.Key] = param.Value;
            }
        }

        await TrackEvent("calculation_completed", eventParams);
    }

    /// <summary>
    /// Track PDF export/download
    /// </summary>
    /// <param name="moduleName">Calculator module name</param>
    /// <param name="fileName">Generated PDF filename</param>
    public async Task TrackPdfDownload(string moduleName, string fileName)
    {
        await TrackEvent("pdf_download", new Dictionary<string, object>
        {
            { "module_name", moduleName },
            { "file_name", fileName },
            { "file_type", "pdf" }
        });
    }

    /// <summary>
    /// Track opening the 3D geometry viewer from a results page
    /// </summary>
    /// <param name="moduleName">Calculator module name</param>
    public async Task TrackGeometryView(string moduleName)
    {
        await TrackEvent("view_3d_geometry", new Dictionary<string, object>
        {
            { "module_name", moduleName }
        });
    }

    /// <summary>
    /// Track calculation errors and validation failures
    /// </summary>
    /// <param name="moduleName">Calculator module name</param>
    /// <param name="errorType">Type of error (e.g., "validation_failed", "calculation_failed")</param>
    /// <param name="errorMessage">Optional error message or details</param>
    public async Task TrackCalculationError(string moduleName, string errorType, string? errorMessage = null)
    {
        await TrackEvent("calculation_error", new Dictionary<string, object>
        {
            { "module_name", moduleName },
            { "error_type", errorType },
            { "error_message", errorMessage ?? "unknown" }
        });
    }

    /// <summary>
    /// Track video tutorial clicks (for future YouTube integration)
    /// </summary>
    /// <param name="moduleName">Calculator module name</param>
    /// <param name="videoId">YouTube video ID</param>
    /// <param name="videoTitle">Video title</param>
    public async Task TrackVideoClick(string moduleName, string videoId, string videoTitle)
    {
        await TrackEvent("video_click", new Dictionary<string, object>
        {
            { "module_name", moduleName },
            { "video_id", videoId },
            { "video_title", videoTitle },
            { "source", "module_page" }
        });
    }

    /// <summary>
    /// Track page/module view
    /// </summary>
    /// <param name="pagePath">Page URL path (e.g., "/key-connection")</param>
    /// <param name="pageTitle">Page title</param>
    public async Task TrackPageView(string pagePath, string pageTitle)
    {
        await TrackEvent("page_view", new Dictionary<string, object>
        {
            { "page_path", pagePath },
            { "page_title", pageTitle }
        });
    }

    /// <summary>
    /// Track related calculator navigation
    /// </summary>
    /// <param name="fromModule">Source module</param>
    /// <param name="toModule">Destination module</param>
    public async Task TrackRelatedCalculatorClick(string fromModule, string toModule)
    {
        await TrackEvent("related_calculator_click", new Dictionary<string, object>
        {
            { "from_module", fromModule },
            { "to_module", toModule },
            { "engagement_type", "related_navigation" }
        });
    }

    /// <summary>
    /// Track user feedback submission
    /// </summary>
    /// <param name="moduleName">Calculator module name</param>
    /// <param name="rating">Star rating (1-5)</param>
    /// <param name="hasComment">Whether user provided a comment</param>
    public async Task TrackFeedbackSubmitted(string moduleName, int rating, bool hasComment)
    {
        await TrackEvent("feedback_submitted", new Dictionary<string, object>
        {
            { "module_name", moduleName },
            { "rating", rating },
            { "has_comment", hasComment },
            { "feedback_type", "star_rating" }
        });
    }

    /// <summary>
    /// Track when user skips feedback widget
    /// </summary>
    /// <param name="moduleName">Calculator module name</param>
    public async Task TrackFeedbackSkipped(string moduleName)
    {
        await TrackEvent("feedback_skipped", new Dictionary<string, object>
        {
            { "module_name", moduleName },
            { "action", "dismissed" }
        });
    }

    /// <summary>
    /// Get current session ID for tracking
    /// </summary>
    /// <returns>Session ID string or empty if not available</returns>
    public async Task<string> GetSessionId()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("getSessionId");
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Check if gtag is fully loaded and ready to use
    /// </summary>
    /// <returns>True if gtag function exists and is callable</returns>
    private async Task<bool> IsGtagReady()
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("eval", "typeof gtag");
            return result == "function";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generic event tracking method (private)
    /// Calls gtag JavaScript function via interop
    /// Waits for gtag to be ready before sending events (handles Blazor WASM timing issues)
    /// Fails silently to prevent disruption of user experience
    /// </summary>
    /// <param name="eventName">GA4 event name</param>
    /// <param name="parameters">Event parameters</param>
    private async Task TrackEvent(string eventName, Dictionary<string, object> parameters)
    {
        try
        {
            // Wait for gtag to be ready (max 5 seconds, 10 retries × 500ms)
            bool isReady = false;
            for (int i = 0; i < 10; i++)
            {
                if (await IsGtagReady())
                {
                    isReady = true;
                    break;
                }

                Console.WriteLine($"[Analytics] Waiting for gtag... attempt {i + 1}/10");
                await Task.Delay(500); // Wait 500ms between checks
            }

            if (!isReady)
            {
                Console.WriteLine($"[Analytics] ERROR: gtag not ready after 5 seconds for event '{eventName}'");
                return;
            }

            // Use JavaScript wrapper for better error handling
            Console.WriteLine($"[Analytics] Sending event via wrapper: {eventName}");
            var success = await _jsRuntime.InvokeAsync<bool>("sendGAEvent", eventName, parameters);

            if (success)
            {
                Console.WriteLine($"[Analytics] ✅ Event sent successfully: {eventName}");
            }
            else
            {
                Console.WriteLine($"[Analytics] ❌ Event failed: {eventName}");
                Console.WriteLine($"[Analytics] Check browser console for [GA4 Wrapper] logs");
            }
        }
        catch (Exception ex)
        {
            // Fail silently - analytics failures should not break user experience
            // Log to console for debugging
            Console.WriteLine($"[Analytics] ERROR: Event '{eventName}' failed: {ex.Message}");
        }
    }
}
