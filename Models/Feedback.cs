namespace MechanicalCalculatorWeb.Models;

/// <summary>
/// Represents a single feedback submission from a user
/// </summary>
public class FeedbackSubmission
{
    /// <summary>
    /// Module identifier (e.g., "key-connection", "interference-fit")
    /// </summary>
    public string ModuleKey { get; set; } = string.Empty;

    /// <summary>
    /// Rating from 1 to 5 stars
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Optional comment from user (max 200 characters)
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when feedback was submitted
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Session ID for analytics tracking
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// Represents aggregated rating data for a module
/// </summary>
public class AggregateRating
{
    /// <summary>
    /// Average rating (1.0 to 5.0)
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Total number of ratings submitted
    /// </summary>
    public int RatingCount { get; set; }

    /// <summary>
    /// When the aggregate was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Distribution of ratings (1★ → count, 2★ → count, etc.)
    /// </summary>
    public Dictionary<int, int> Distribution { get; set; } = new();

    /// <summary>
    /// Returns true if this module has sufficient ratings to display
    /// </summary>
    public bool HasSufficientData => RatingCount >= 1;
}
