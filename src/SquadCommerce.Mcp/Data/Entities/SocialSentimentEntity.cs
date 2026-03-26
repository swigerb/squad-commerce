namespace SquadCommerce.Mcp.Data.Entities;

/// <summary>
/// EF Core entity representing social media sentiment data.
/// Maps to SocialSentiment table with auto-incremented primary key (Id).
/// </summary>
public sealed class SocialSentimentEntity
{
    public int Id { get; set; }
    public required string Sku { get; set; }
    public required string ProductName { get; set; }
    public required string Platform { get; set; } // TikTok, Instagram, Twitter
    public required double SentimentScore { get; set; } // 0.0-1.0
    public required double Velocity { get; set; } // rate of change
    public required string Region { get; set; }
    public required DateTimeOffset DetectedAt { get; set; }
}
