using SquadCommerce.Mcp.Data;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for querying inventory levels across stores.
/// Exposed to agents via the Model Context Protocol.
/// </summary>
/// <remarks>
/// This tool:
/// - Returns structured inventory data (NOT raw text)
/// - Filters by SKU or StoreId if provided
/// - Returns all inventory if no filter specified
/// - Emits OpenTelemetry spans for observability
/// </remarks>
public sealed class GetInventoryLevelsTool
{
    private readonly IInventoryRepository _repository;

    public GetInventoryLevelsTool(IInventoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Tool name as registered with MCP server.
    /// </summary>
    public string Name => "GetInventoryLevels";

    /// <summary>
    /// Tool description for MCP discovery.
    /// </summary>
    public string Description => "Queries inventory levels for stores. Accepts optional 'sku' or 'storeId' parameters.";

    /// <summary>
    /// Executes the tool with the provided parameters.
    /// </summary>
    /// <param name="sku">Optional: Filter by SKU</param>
    /// <param name="storeId">Optional: Filter by store ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON-serializable inventory data</returns>
    public async Task<object> ExecuteAsync(string? sku = null, string? storeId = null, CancellationToken cancellationToken = default)
    {
        // TODO: Add OpenTelemetry span emission
        // using var activity = ActivitySource.StartActivity("GetInventoryLevels");
        // activity?.SetTag("mcp.tool", Name);
        // activity?.SetTag("sku", sku);
        // activity?.SetTag("storeId", storeId);

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var levels = await _repository.GetInventoryLevelsBySku(sku, cancellationToken);
            return new { Sku = sku, Stores = levels };
        }

        if (!string.IsNullOrWhiteSpace(storeId))
        {
            var levels = await _repository.GetInventoryLevelsByStore(storeId, cancellationToken);
            return new { StoreId = storeId, Inventory = levels };
        }

        var allLevels = await _repository.GetAllInventoryLevels(cancellationToken);
        return new { AllInventory = allLevels };
    }
}
