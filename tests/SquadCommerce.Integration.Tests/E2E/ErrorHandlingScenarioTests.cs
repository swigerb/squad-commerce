using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Mcp.Data;
using Xunit;

namespace SquadCommerce.Integration.Tests.E2E;

/// <summary>
/// End-to-end error handling tests verifying graceful degradation and error recovery.
/// </summary>
public class ErrorHandlingScenarioTests
{
    [Fact]
    public async Task Should_ReturnGracefulError_When_McpToolFails()
    {
        // Arrange - Mock repository that throws
        var mockPricingRepo = new Mock<IPricingRepository>();
        mockPricingRepo
            .Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var inventoryRepo = new InventoryRepository();

        var pricingAgent = new PricingAgent(
            mockPricingRepo.Object,
            inventoryRepo,
            NullLogger<PricingAgent>.Instance);

        // Act - Agent should catch exception and return error result
        var result = await pricingAgent.ExecuteAsync("SKU-1001", 25.99m, CancellationToken.None);

        // Assert - Structured error returned (no exception thrown)
        result.Should().NotBeNull();
        result.Success.Should().BeFalse("agent should handle MCP tool failure gracefully");
        result.ErrorMessage.Should().Contain("Database connection failed");
        result.A2UIPayload.Should().BeNull("no payload on error");
        result.TextSummary.Should().Contain("Error");
    }

    [Fact]
    public async Task Should_FallbackToInternalData_When_A2AHandshakeFails()
    {
        // Arrange - Mock A2A client that fails
        var mockA2AClient = new Mock<IA2AClient>();
        mockA2AClient
            .Setup(c => c.GetCompetitorPricingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompetitorPricing>()); // Empty result = A2A failure

        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var validator = new ExternalDataValidator(
            pricingRepo,
            inventoryRepo,
            NullLogger<ExternalDataValidator>.Instance);

        var marketIntelAgent = new MarketIntelAgent(
            mockA2AClient.Object,
            validator,
            NullLogger<MarketIntelAgent>.Instance);

        // Act - MarketIntelAgent should handle A2A failure
        var result = await marketIntelAgent.ExecuteAsync("SKU-1002", 12.99m, CancellationToken.None);

        // Assert - Agent returns error result (doesn't throw)
        result.Should().NotBeNull();
        result.Success.Should().BeFalse("agent should report A2A failure");
        result.ErrorMessage.Should().Contain("A2A query returned no results");
        result.A2UIPayload.Should().BeNull();
    }

    [Fact]
    public async Task Should_ContinueWorkflow_When_NonCriticalAgentFails()
    {
        // Arrange - InventoryAgent that fails (non-critical)
        var mockInventoryRepo = new Mock<IInventoryRepository>();
        mockInventoryRepo
            .Setup(r => r.GetInventoryLevelsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Inventory service timeout"));

        var failingInventoryAgent = new InventoryAgent(
            mockInventoryRepo.Object,
            NullLogger<InventoryAgent>.Instance);

        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();

        var pricingAgent = new PricingAgent(
            pricingRepo,
            inventoryRepo,
            NullLogger<PricingAgent>.Instance);

        var mockA2AClient = new Mock<IA2AClient>();
        mockA2AClient
            .Setup(c => c.GetCompetitorPricingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompetitorPricing>
            {
                new CompetitorPricing
                {
                    Sku = "SKU-1003",
                    CompetitorName = "TechMart",
                    Price = 45.99m,
                    Source = "A2A:TechMart",
                    Verified = true,
                    LastUpdated = DateTimeOffset.UtcNow,
                    ValidationNotes = "Test"
                }
            });

        var validator = new ExternalDataValidator(
            pricingRepo,
            inventoryRepo,
            NullLogger<ExternalDataValidator>.Instance);

        var marketIntelAgent = new MarketIntelAgent(
            mockA2AClient.Object,
            validator,
            NullLogger<MarketIntelAgent>.Instance);

        var orchestrator = new ChiefSoftwareArchitectAgent(
            failingInventoryAgent,
            pricingAgent,
            marketIntelAgent,
            NullLogger<ChiefSoftwareArchitectAgent>.Instance);

        // Act - Orchestrator should continue even when InventoryAgent fails
        var result = await orchestrator.ProcessCompetitorPriceDropAsync("SKU-1003", 45.99m, CancellationToken.None);

        // Assert - Workflow continues (graceful degradation)
        result.Should().NotBeNull();
        result.AgentResults.Should().HaveCount(3, "should execute all agents");
        result.AgentResults[1].Success.Should().BeFalse("InventoryAgent should fail");
        result.AgentResults[0].Success.Should().BeTrue("MarketIntelAgent should succeed");
        result.AgentResults[2].Success.Should().BeTrue("PricingAgent should succeed despite inventory failure");
    }

    [Fact]
    public async Task Should_RejectExternalData_When_PriceDeltaExceeds50Percent()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var validator = new ExternalDataValidator(
            pricingRepo,
            inventoryRepo,
            NullLogger<ExternalDataValidator>.Instance);

        var sku = "SKU-1006"; // Noise-Cancelling Headphones
        var ourPrice = 199.99m;
        var suspiciousPrice = 50.00m; // 75% below our price - SUSPICIOUS!

        // Act - Validator should flag suspicious data
        var result = await validator.ValidatePricingAsync(
            "SuspiciousVendor",
            sku,
            suspiciousPrice,
            CancellationToken.None);

        // Assert - Low confidence or rejected
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse("price deviation >50% should be rejected");
        result.ConfidenceLevel.Should().Be("Low", "suspicious prices get low confidence");
        result.Reason.Should().Contain("50%");
    }

    [Fact]
    public async Task Should_ValidateReasonablePrice_When_WithinThreshold()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var validator = new ExternalDataValidator(
            pricingRepo,
            inventoryRepo,
            NullLogger<ExternalDataValidator>.Instance);

        var sku = "SKU-1007"; // External SSD
        var reasonableCompetitorPrice = 82.99m; // Within ~15% of typical prices

        // Act
        var result = await validator.ValidatePricingAsync(
            "TrustedRetailer",
            sku,
            reasonableCompetitorPrice,
            CancellationToken.None);

        // Assert - High confidence
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ConfidenceLevel.Should().BeOneOf("High", "Medium", "price within acceptable range should pass validation");
        result.ConfirmingSources.Should().Contain("Internal price history");
    }

    [Fact]
    public async Task Should_HandleInvalidSku_When_SkuNotFound()
    {
        // Arrange
        var inventoryRepo = new InventoryRepository();
        var inventoryAgent = new InventoryAgent(
            inventoryRepo,
            NullLogger<InventoryAgent>.Instance);

        // Act - Query non-existent SKU
        var result = await inventoryAgent.ExecuteAsync("SKU-INVALID", CancellationToken.None);

        // Assert - Graceful error
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("SKU-INVALID");
        result.ErrorMessage.Should().Contain("not found");
        result.A2UIPayload.Should().BeNull();
    }

    [Fact]
    public async Task Should_RejectPriceBelowCost_When_UpdatingPricing()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var sku = "SKU-1001"; // Wireless Mouse, Cost = $15.00
        var storeId = "SEA-001";
        var belowCostPrice = 10.00m; // Below cost!

        var currentPrice = await pricingRepo.GetCurrentPriceAsync(storeId, sku, CancellationToken.None);
        currentPrice.Should().NotBeNull();

        // Act - Try to update to below-cost price
        var priceChange = new PriceChange
        {
            Sku = sku,
            StoreId = storeId,
            OldPrice = currentPrice!.Value,
            NewPrice = belowCostPrice,
            Reason = "Test below cost",
            RequestedBy = "test@squadcommerce.com",
            Timestamp = DateTimeOffset.UtcNow
        };

        var result = await pricingRepo.UpdatePricingAsync(priceChange, CancellationToken.None);

        // Assert - Update rejected
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("below cost");
        result.StoresUpdated.Should().BeEmpty();

        // Verify price unchanged
        var stillOriginalPrice = await pricingRepo.GetCurrentPriceAsync(storeId, sku, CancellationToken.None);
        stillOriginalPrice.Should().Be(currentPrice.Value);
    }
}
