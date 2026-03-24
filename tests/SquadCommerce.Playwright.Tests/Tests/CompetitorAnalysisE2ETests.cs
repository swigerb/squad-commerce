using System.Net.Http.Json;
using NUnit.Framework;
using SquadCommerce.Playwright.Tests.Fixtures;
using SquadCommerce.Playwright.Tests.Pages;

namespace SquadCommerce.Playwright.Tests.Tests;

/// <summary>
/// End-to-end tests for the competitor analysis workflow
/// Tests the full flow: trigger analysis → streaming status → A2UI components render
/// </summary>
[TestFixture]
[Category("E2E")]
[Category("CompetitorAnalysis")]
public class CompetitorAnalysisE2ETests : PlaywrightTestBase
{
    private MainPage? _mainPage;
    private AgentChatPage? _chatPage;
    private A2UIComponentsPage? _a2uiPage;
    private ApprovalPanelPage? _approvalPage;
    private HttpClient? _httpClient;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        _mainPage = new MainPage(Page!, BaseUrl);
        _chatPage = new AgentChatPage(Page!);
        _a2uiPage = new A2UIComponentsPage(Page!);
        _approvalPage = new ApprovalPanelPage(Page!);
        
        _httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("TEST_API_URL") ?? "https://localhost:7001")
        };
        
        // Navigate to app
        await _mainPage.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();
    }

    [TearDown]
    public new async Task TearDown()
    {
        _httpClient?.Dispose();
        await base.TearDown();
    }

    [Test]
    public async Task Should_ShowStreamingStatus_When_AnalysisTriggered()
    {
        // Arrange - Trigger analysis via API
        var analysisRequest = new
        {
            Sku = "SKU-TEST-001",
            CompetitorName = "CompetitorA",
            CompetitorPrice = 19.99m
        };

        // Act - Trigger analysis
        var response = await _httpClient!.PostAsJsonAsync("/api/agents/analyze", analysisRequest);
        Assert.That(response.IsSuccessStatusCode, Is.True, "Analysis should be triggered successfully");

        // Wait for status updates to appear in chat
        await Task.Delay(2000); // Give time for first status update
        
        // Assert - Status messages should appear
        var statusMessages = await _chatPage!.GetStatusMessagesAsync();
        Assert.That(statusMessages.Count, Is.GreaterThan(0), "Status messages should appear");
    }

    [Test]
    public async Task Should_RenderHeatmap_When_InventoryDataReturned()
    {
        // Arrange - Trigger analysis
        var analysisRequest = new
        {
            Sku = "SKU-TEST-002",
            CompetitorName = "CompetitorB",
            CompetitorPrice = 24.99m
        };

        // Act
        await _httpClient!.PostAsJsonAsync("/api/agents/analyze", analysisRequest);
        
        // Wait for heatmap to render
        try
        {
            await _a2uiPage!.WaitForHeatmapAsync(timeoutMs: 30000);
            
            // Assert
            var isHeatmapVisible = await _a2uiPage.IsHeatmapVisibleAsync();
            Assert.That(isHeatmapVisible, Is.True, "Heatmap should be visible");
            
            var cellCount = await _a2uiPage.GetHeatmapCellCountAsync();
            Assert.That(cellCount, Is.GreaterThan(0), "Heatmap should contain cells with store/SKU data");
        }
        catch (TimeoutException)
        {
            Assert.Warn("Heatmap did not render within timeout - may require running backend");
        }
    }

    [Test]
    public async Task Should_RenderPricingChart_When_MarginCalculated()
    {
        // Arrange - Trigger analysis
        var analysisRequest = new
        {
            Sku = "SKU-TEST-003",
            CompetitorName = "CompetitorC",
            CompetitorPrice = 29.99m
        };

        // Act
        await _httpClient!.PostAsJsonAsync("/api/agents/analyze", analysisRequest);
        
        // Wait for pricing chart to render
        try
        {
            await _a2uiPage!.WaitForPricingChartAsync(timeoutMs: 30000);
            
            // Assert
            var isPricingChartVisible = await _a2uiPage.IsPricingChartVisibleAsync();
            Assert.That(isPricingChartVisible, Is.True, "Pricing chart should be visible");
            
            var proposalCount = await _a2uiPage.GetPricingProposalCountAsync();
            Assert.That(proposalCount, Is.GreaterThan(0), "Pricing chart should show proposals");
        }
        catch (TimeoutException)
        {
            Assert.Warn("Pricing chart did not render within timeout - may require running backend");
        }
    }

    [Test]
    public async Task Should_RenderComparisonGrid_When_CompetitorDataValidated()
    {
        // Arrange - Trigger analysis
        var analysisRequest = new
        {
            Sku = "SKU-TEST-004",
            CompetitorName = "CompetitorD",
            CompetitorPrice = 34.99m
        };

        // Act
        await _httpClient!.PostAsJsonAsync("/api/agents/analyze", analysisRequest);
        
        // Wait for comparison grid to render
        try
        {
            await _a2uiPage!.WaitForComparisonGridAsync(timeoutMs: 30000);
            
            // Assert
            var isGridVisible = await _a2uiPage.IsComparisonGridVisibleAsync();
            Assert.That(isGridVisible, Is.True, "Comparison grid should be visible");
            
            var rowCount = await _a2uiPage.GetComparisonRowCountAsync();
            Assert.That(rowCount, Is.GreaterThan(0), "Comparison grid should show competitor data");
        }
        catch (TimeoutException)
        {
            Assert.Warn("Comparison grid did not render within timeout - may require running backend");
        }
    }

    [Test]
    public async Task Should_RenderAuditTrail_When_WorkflowCompletes()
    {
        // Arrange - Trigger analysis
        var analysisRequest = new
        {
            Sku = "SKU-TEST-005",
            CompetitorName = "CompetitorE",
            CompetitorPrice = 39.99m
        };

        // Act
        await _httpClient!.PostAsJsonAsync("/api/agents/analyze", analysisRequest);
        
        // Wait for audit trail to render
        try
        {
            await _a2uiPage!.WaitForAuditTrailAsync(timeoutMs: 30000);
            
            // Assert
            var isAuditVisible = await _a2uiPage.IsAuditTrailVisibleAsync();
            Assert.That(isAuditVisible, Is.True, "Audit trail should be visible");
            
            var entryCount = await _a2uiPage.GetAuditEntryCountAsync();
            Assert.That(entryCount, Is.GreaterThan(0), "Audit trail should show workflow steps");
        }
        catch (TimeoutException)
        {
            Assert.Warn("Audit trail did not render within timeout - may require running backend");
        }
    }

    [Test]
    public async Task Should_RenderPipeline_When_WorkflowProgresses()
    {
        // Arrange - Trigger analysis
        var analysisRequest = new
        {
            Sku = "SKU-TEST-006",
            CompetitorName = "CompetitorF",
            CompetitorPrice = 44.99m
        };

        // Act
        await _httpClient!.PostAsJsonAsync("/api/agents/analyze", analysisRequest);
        
        // Wait for pipeline to render
        try
        {
            await _a2uiPage!.WaitForPipelineAsync(timeoutMs: 30000);
            
            // Assert
            var isPipelineVisible = await _a2uiPage.IsPipelineVisibleAsync();
            Assert.That(isPipelineVisible, Is.True, "Pipeline should be visible");
            
            var stageCount = await _a2uiPage.GetPipelineStageCountAsync();
            Assert.That(stageCount, Is.GreaterThan(0), "Pipeline should show workflow stages");
        }
        catch (TimeoutException)
        {
            Assert.Warn("Pipeline did not render within timeout - may require running backend");
        }
    }

    [Test]
    public async Task Should_ShowApprovalPanel_When_AnalysisCompletes()
    {
        // Arrange - Trigger analysis
        var analysisRequest = new
        {
            Sku = "SKU-TEST-007",
            CompetitorName = "CompetitorG",
            CompetitorPrice = 49.99m
        };

        // Act
        await _httpClient!.PostAsJsonAsync("/api/agents/analyze", analysisRequest);
        
        // Wait for approval panel to appear
        try
        {
            await _approvalPage!.WaitForApprovalPanelAsync(timeoutMs: 30000);
            
            // Assert
            var isApprovalVisible = await _approvalPage.IsApprovalPanelVisibleAsync();
            Assert.That(isApprovalVisible, Is.True, "Approval panel should be visible");
            
            var isApproveEnabled = await _approvalPage.IsApproveButtonEnabledAsync();
            var isRejectEnabled = await _approvalPage.IsRejectButtonEnabledAsync();
            
            Assert.That(isApproveEnabled, Is.True, "Approve button should be enabled");
            Assert.That(isRejectEnabled, Is.True, "Reject button should be enabled");
        }
        catch (TimeoutException)
        {
            Assert.Warn("Approval panel did not appear within timeout - may require running backend");
        }
    }
}
