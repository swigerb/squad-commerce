namespace SquadCommerce.Contracts.A2UI;

public record A2UIPayload
{
    public required string Type { get; init; }
    public required string RenderAs { get; init; }
    public required object Data { get; init; }
}
