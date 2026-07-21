using MechanicalCalculatorWeb.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Service for managing user feedback and ratings
/// Uses localStorage for client-side persistence (GDPR compliant, no backend required)
/// </summary>
public class FeedbackService
{
    private readonly IJSRuntime _js;
    private readonly AnalyticsService _analytics;

    public FeedbackService(IJSRuntime js, AnalyticsService analytics)
    {
        _js = js;
        _analytics = analytics;
    }

    /// <summary>
    /// Check if user has already submitted feedback for a specific module
    /// </summary>
    /// <param name="moduleKey">Module identifier (e.g., "key-connection")</param>
    /// <returns>True if feedback already submitted</returns>
    public async Task<bool> HasSubmittedFeedback(string moduleKey)
    {
        try
        {
            return await _js.InvokeAsync<bool>("hasSubmittedFeedback", moduleKey);
        }
        catch
        {
            return false; // Fail gracefully if localStorage unavailable
        }
    }

    /// <summary>
    /// Submit user feedback and update aggregate ratings
    /// </summary>
    /// <param name="feedback">Feedback submission data</param>
    public async Task SubmitFeedback(FeedbackSubmission feedback)
    {
        try
        {
            // Save individual feedback to localStorage
            var feedbackJson = JsonSerializer.Serialize(feedback);
            await _js.InvokeVoidAsync("saveFeedback", feedbackJson);

            // Update aggregate ratings
            await UpdateAggregateRatings(feedback.ModuleKey);

            // Track in analytics
            await _analytics.TrackFeedbackSubmitted(
                feedback.ModuleKey,
                feedback.Rating,
                !string.IsNullOrEmpty(feedback.Comment)
            );
        }
        catch (Exception ex)
        {
            // Log error but don't throw - feedback is optional feature
            await _js.InvokeVoidAsync("console.error", $"Feedback submission error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get aggregate rating data for a module
    /// </summary>
    /// <param name="moduleKey">Module identifier</param>
    /// <returns>Aggregate rating or default values if no ratings exist</returns>
    public async Task<AggregateRating> GetAggregateRating(string moduleKey)
    {
        try
        {
            var aggregateJson = await _js.InvokeAsync<string>("getAggregateRating", moduleKey);

            if (string.IsNullOrEmpty(aggregateJson))
            {
                return new AggregateRating
                {
                    AverageRating = 0,
                    RatingCount = 0,
                    LastUpdated = DateTime.UtcNow,
                    Distribution = new Dictionary<int, int>()
                };
            }

            return JsonSerializer.Deserialize<AggregateRating>(aggregateJson)
                ?? new AggregateRating();
        }
        catch
        {
            return new AggregateRating();
        }
    }

    /// <summary>
    /// Get all feedback submissions for a module
    /// </summary>
    /// <param name="moduleKey">Module identifier</param>
    /// <returns>List of feedback submissions</returns>
    public async Task<List<FeedbackSubmission>> GetModuleFeedback(string moduleKey)
    {
        try
        {
            var feedbackJson = await _js.InvokeAsync<string>("getModuleFeedback", moduleKey);

            if (string.IsNullOrEmpty(feedbackJson))
            {
                return new List<FeedbackSubmission>();
            }

            return JsonSerializer.Deserialize<List<FeedbackSubmission>>(feedbackJson)
                ?? new List<FeedbackSubmission>();
        }
        catch
        {
            return new List<FeedbackSubmission>();
        }
    }

    /// <summary>
    /// Get rating distribution (how many 1★, 2★, etc.)
    /// </summary>
    /// <param name="moduleKey">Module identifier</param>
    /// <returns>Dictionary of star rating → count</returns>
    public async Task<Dictionary<int, int>> GetRatingDistribution(string moduleKey)
    {
        var aggregate = await GetAggregateRating(moduleKey);
        return aggregate.Distribution;
    }

    /// <summary>
    /// Update aggregate ratings for a module based on all feedback
    /// </summary>
    /// <param name="moduleKey">Module identifier</param>
    private async Task UpdateAggregateRatings(string moduleKey)
    {
        try
        {
            // Get all feedback for this module
            var allFeedback = await GetModuleFeedback(moduleKey);

            if (allFeedback.Count == 0)
            {
                return;
            }

            // Calculate aggregate
            var aggregate = new AggregateRating
            {
                RatingCount = allFeedback.Count,
                AverageRating = allFeedback.Average(f => f.Rating),
                LastUpdated = DateTime.UtcNow,
                Distribution = allFeedback
                    .GroupBy(f => f.Rating)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Save aggregate to localStorage
            var aggregateJson = JsonSerializer.Serialize(aggregate);
            await _js.InvokeVoidAsync("saveAggregateRating", moduleKey, aggregateJson);
        }
        catch (Exception ex)
        {
            await _js.InvokeVoidAsync("console.error", $"Aggregate update error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get overall feedback statistics across all modules
    /// </summary>
    /// <returns>Summary statistics</returns>
    public async Task<FeedbackStats> GetOverallStats()
    {
        try
        {
            var statsJson = await _js.InvokeAsync<string>("getFeedbackStats");
            return JsonSerializer.Deserialize<FeedbackStats>(statsJson)
                ?? new FeedbackStats();
        }
        catch
        {
            return new FeedbackStats();
        }
    }
}

/// <summary>
/// Overall feedback statistics
/// </summary>
public class FeedbackStats
{
    public int TotalFeedback { get; set; }
    public double OverallAverage { get; set; }
    public int ModulesWithFeedback { get; set; }
    public Dictionary<string, int> FeedbackByModule { get; set; } = new();
}
