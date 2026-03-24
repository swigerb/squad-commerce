using Microsoft.Playwright;

namespace SquadCommerce.Playwright.Tests.Pages;

/// <summary>
/// Page Object Model for A2UI visualization components
/// </summary>
public class A2UIComponentsPage
{
    private readonly IPage _page;

    public A2UIComponentsPage(IPage page)
    {
        _page = page;
    }

    // Locators for A2UI components
    private ILocator HeatmapContainer => _page.Locator(".retail-stock-heatmap, [data-testid='heatmap']");
    private ILocator HeatmapCells => HeatmapContainer.Locator(".heatmap-cell, .stock-cell");
    
    private ILocator PricingChartContainer => _page.Locator(".pricing-impact-chart, [data-testid='pricing-chart']");
    private ILocator PricingProposals => PricingChartContainer.Locator(".pricing-proposal, .proposal-item");
    
    private ILocator ComparisonGridContainer => _page.Locator(".market-comparison-grid, [data-testid='comparison-grid']");
    private ILocator ComparisonRows => ComparisonGridContainer.Locator("tr, .comparison-row");
    
    private ILocator AuditTrailContainer => _page.Locator(".decision-audit-trail, [data-testid='audit-trail']");
    private ILocator AuditEntries => AuditTrailContainer.Locator(".audit-entry, .timeline-item");
    
    private ILocator PipelineContainer => _page.Locator(".agent-pipeline-visualizer, [data-testid='pipeline']");
    private ILocator PipelineStages => PipelineContainer.Locator(".pipeline-stage, .stage-item");

    // Heatmap Methods
    public async Task WaitForHeatmapAsync(int timeoutMs = 10000)
    {
        await HeatmapContainer.WaitForAsync(new() 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = timeoutMs 
        });
    }

    public async Task<int> GetHeatmapCellCountAsync()
    {
        return await HeatmapCells.CountAsync();
    }

    public async Task<IReadOnlyList<string>> GetHeatmapCellsAsync()
    {
        var cells = new List<string>();
        var cellElements = await HeatmapCells.AllAsync();
        
        foreach (var cell in cellElements)
        {
            var text = await cell.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                cells.Add(text);
            }
        }
        
        return cells;
    }

    public async Task<bool> IsHeatmapVisibleAsync()
    {
        return await HeatmapContainer.IsVisibleAsync();
    }

    // Pricing Chart Methods
    public async Task WaitForPricingChartAsync(int timeoutMs = 10000)
    {
        await PricingChartContainer.WaitForAsync(new() 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = timeoutMs 
        });
    }

    public async Task<int> GetPricingProposalCountAsync()
    {
        return await PricingProposals.CountAsync();
    }

    public async Task<IReadOnlyList<string>> GetPricingProposalsAsync()
    {
        var proposals = new List<string>();
        var proposalElements = await PricingProposals.AllAsync();
        
        foreach (var proposal in proposalElements)
        {
            var text = await proposal.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                proposals.Add(text);
            }
        }
        
        return proposals;
    }

    public async Task<bool> IsPricingChartVisibleAsync()
    {
        return await PricingChartContainer.IsVisibleAsync();
    }

    // Comparison Grid Methods
    public async Task WaitForComparisonGridAsync(int timeoutMs = 10000)
    {
        await ComparisonGridContainer.WaitForAsync(new() 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = timeoutMs 
        });
    }

    public async Task<int> GetComparisonRowCountAsync()
    {
        return await ComparisonRows.CountAsync();
    }

    public async Task<IReadOnlyList<string>> GetComparisonRowsAsync()
    {
        var rows = new List<string>();
        var rowElements = await ComparisonRows.AllAsync();
        
        foreach (var row in rowElements)
        {
            var text = await row.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                rows.Add(text);
            }
        }
        
        return rows;
    }

    public async Task<bool> IsComparisonGridVisibleAsync()
    {
        return await ComparisonGridContainer.IsVisibleAsync();
    }

    // Audit Trail Methods
    public async Task WaitForAuditTrailAsync(int timeoutMs = 10000)
    {
        await AuditTrailContainer.WaitForAsync(new() 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = timeoutMs 
        });
    }

    public async Task<int> GetAuditEntryCountAsync()
    {
        return await AuditEntries.CountAsync();
    }

    public async Task<IReadOnlyList<string>> GetAuditEntriesAsync()
    {
        var entries = new List<string>();
        var entryElements = await AuditEntries.AllAsync();
        
        foreach (var entry in entryElements)
        {
            var text = await entry.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                entries.Add(text);
            }
        }
        
        return entries;
    }

    public async Task<bool> IsAuditTrailVisibleAsync()
    {
        return await AuditTrailContainer.IsVisibleAsync();
    }

    // Pipeline Methods
    public async Task WaitForPipelineAsync(int timeoutMs = 10000)
    {
        await PipelineContainer.WaitForAsync(new() 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = timeoutMs 
        });
    }

    public async Task<int> GetPipelineStageCountAsync()
    {
        return await PipelineStages.CountAsync();
    }

    public async Task<IReadOnlyList<string>> GetPipelineStagesAsync()
    {
        var stages = new List<string>();
        var stageElements = await PipelineStages.AllAsync();
        
        foreach (var stage in stageElements)
        {
            var text = await stage.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                stages.Add(text);
            }
        }
        
        return stages;
    }

    public async Task<bool> IsPipelineVisibleAsync()
    {
        return await PipelineContainer.IsVisibleAsync();
    }
}
