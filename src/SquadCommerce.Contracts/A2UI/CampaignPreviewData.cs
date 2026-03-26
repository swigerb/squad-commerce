namespace SquadCommerce.Contracts.A2UI;

public sealed record CampaignPreviewData
{
    public required string Sku { get; init; }
    public required string ProductName { get; init; }
    public required string EmailSubjectLine { get; init; }
    public required string EmailBody { get; init; }
    public required string HeroBannerHeadline { get; init; }
    public required string HeroBannerSubtext { get; init; }
    public required string CallToAction { get; init; }
    public required decimal OriginalPrice { get; init; }
    public required decimal FlashSalePrice { get; init; }
    public required string TargetRegion { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
