using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Api.Services;
using SquadCommerce.Contracts.Interfaces;
using Xunit;

namespace SquadCommerce.Integration.Tests.Smoke;

/// <summary>
/// Smoke tests verify the entire system can start up and basic infrastructure works.
/// These are the first tests to run - if smoke tests fail, nothing else will work.
/// </summary>
public class SystemSmokeTests
{
    [Fact]
    public void Should_BuildAllProjects_Successfully()
    {
        // Arrange - This test verifies the solution compiles

        // Act - If this test is running, compilation succeeded

        // Assert
        true.Should().BeTrue("solution compiled and tests are running");
    }

    [Fact]
    public void Should_RegisterAllAgents_When_DIContainerBuilt()
    {
        // Arrange - Build service collection with agent registrations
        var services = new ServiceCollection();
        
        // Register logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Register repositories
        services.AddSingleton<IInventoryRepository, SquadCommerce.Mcp.Data.InventoryRepository>();
        services.AddSingleton<IPricingRepository, SquadCommerce.Mcp.Data.PricingRepository>();
        
        // Register A2A components
        services.AddHttpClient<IA2AClient, SquadCommerce.A2A.A2AClient>();
        services.AddSingleton<SquadCommerce.A2A.Validation.ExternalDataValidator>();
        
        // Register domain agents
        services.AddScoped<InventoryAgent>();
        services.AddScoped<PricingAgent>();
        services.AddScoped<MarketIntelAgent>();
        
        // Register orchestrator
        services.AddScoped<ChiefSoftwareArchitectAgent>();

        var serviceProvider = services.BuildServiceProvider();

        // Act - Resolve all agent services
        using var scope = serviceProvider.CreateScope();
        var inventoryAgent = scope.ServiceProvider.GetService<InventoryAgent>();
        var pricingAgent = scope.ServiceProvider.GetService<PricingAgent>();
        var marketIntelAgent = scope.ServiceProvider.GetService<MarketIntelAgent>();
        var orchestrator = scope.ServiceProvider.GetService<ChiefSoftwareArchitectAgent>();

        // Assert - All agents resolved successfully
        inventoryAgent.Should().NotBeNull("InventoryAgent should be registered");
        pricingAgent.Should().NotBeNull("PricingAgent should be registered");
        marketIntelAgent.Should().NotBeNull("MarketIntelAgent should be registered");
        orchestrator.Should().NotBeNull("ChiefSoftwareArchitectAgent should be registered");

        inventoryAgent!.AgentName.Should().Be("InventoryAgent");
        pricingAgent!.AgentName.Should().Be("PricingAgent");
        marketIntelAgent!.AgentName.Should().Be("MarketIntelAgent");
    }

    [Fact]
    public void Should_RegisterAllRepositories_When_DIContainerBuilt()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IInventoryRepository, SquadCommerce.Mcp.Data.InventoryRepository>();
        services.AddSingleton<IPricingRepository, SquadCommerce.Mcp.Data.PricingRepository>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var inventoryRepo = serviceProvider.GetService<IInventoryRepository>();
        var pricingRepo = serviceProvider.GetService<IPricingRepository>();

        // Assert
        inventoryRepo.Should().NotBeNull("IInventoryRepository should be registered");
        pricingRepo.Should().NotBeNull("IPricingRepository should be registered");
        inventoryRepo.Should().BeOfType<SquadCommerce.Mcp.Data.InventoryRepository>();
        pricingRepo.Should().BeOfType<SquadCommerce.Mcp.Data.PricingRepository>();
    }

    [Fact]
    public void Should_HaveDemoData_When_RepositoriesCreated()
    {
        // Arrange
        var inventoryRepo = new SquadCommerce.Mcp.Data.InventoryRepository();
        var pricingRepo = new SquadCommerce.Mcp.Data.PricingRepository();

        // Act - Query demo data
        var inventoryTask = inventoryRepo.GetInventoryLevelsAsync("SKU-1001", CancellationToken.None);
        var pricingTask = pricingRepo.GetCurrentPriceAsync("SEA-001", "SKU-1001", CancellationToken.None);

        inventoryTask.Wait();
        pricingTask.Wait();

        var inventory = inventoryTask.Result;
        var price = pricingTask.Result;

        // Assert - Demo data loaded
        inventory.Should().NotBeEmpty("inventory should have demo data for 5 stores");
        inventory.Should().HaveCount(5, "SKU-1001 should be stocked in all 5 stores");
        price.Should().NotBeNull("pricing should have demo data");
        price!.Value.Should().BeGreaterThan(0, "price should be positive");
    }

    [Fact]
    public void Should_RegisterAgUiStreamWriter_When_DIContainerBuilt()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<SquadCommerce.Observability.SquadCommerceMetrics>();
        services.AddSingleton<IAgUiStreamWriter, AgUiStreamWriter>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var streamWriter = serviceProvider.GetService<IAgUiStreamWriter>();

        // Assert
        streamWriter.Should().NotBeNull("IAgUiStreamWriter should be registered");
        streamWriter.Should().BeOfType<AgUiStreamWriter>();
    }

    [Fact]
    public async Task Should_WriteEvent_When_AgUiStreamWriterPublishes()
    {
        // Arrange
        var metrics = new SquadCommerce.Observability.SquadCommerceMetrics();
        var streamWriter = new AgUiStreamWriter(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AgUiStreamWriter>.Instance, 
            metrics);
        var sessionId = Guid.NewGuid().ToString();

        // Act - Write an event to session
        await streamWriter.WriteStatusUpdateAsync(sessionId, "Test event", CancellationToken.None);

        // Assert - Event written successfully (no exception thrown)
        true.Should().BeTrue("event written successfully");
    }

    [Fact]
    public void Should_RegisterAllContractTypes_Successfully()
    {
        // Arrange - Verify all contract types can be instantiated

        // Act
        var inventorySnapshot = new SquadCommerce.Contracts.Models.InventorySnapshot
        {
            StoreId = "TEST-001",
            Sku = "SKU-TEST",
            UnitsOnHand = 10,
            ReorderPoint = 5,
            UnitsOnOrder = 0,
            LastUpdated = DateTimeOffset.UtcNow
        };

        var priceChange = new SquadCommerce.Contracts.Models.PriceChange
        {
            Sku = "SKU-TEST",
            StoreId = "TEST-001",
            OldPrice = 29.99m,
            NewPrice = 24.99m,
            Reason = "Test",
            RequestedBy = "test@example.com",
            Timestamp = DateTimeOffset.UtcNow
        };

        var competitorPricing = new SquadCommerce.Contracts.Models.CompetitorPricing
        {
            Sku = "SKU-TEST",
            CompetitorName = "TestMart",
            Price = 24.99m,
            Source = "A2A:Test",
            Verified = true,
            LastUpdated = DateTimeOffset.UtcNow,
            ValidationNotes = "Test"
        };

        // Assert
        inventorySnapshot.Should().NotBeNull();
        priceChange.Should().NotBeNull();
        competitorPricing.Should().NotBeNull();
    }

    [Fact]
    public void Should_CreateAllA2UIPayloadTypes_Successfully()
    {
        // Arrange & Act
        var heatmap = new SquadCommerce.Contracts.A2UI.RetailStockHeatmapData
        {
            Sku = "SKU-TEST",
            Stores = new[]
            {
                new SquadCommerce.Contracts.A2UI.StoreStockLevel
                {
                    StoreId = "TEST-001",
                    StoreName = "Test Store",
                    UnitsOnHand = 10,
                    ReorderPoint = 5,
                    StockStatus = "Normal"
                }
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        var chart = new SquadCommerce.Contracts.A2UI.PricingImpactChartData
        {
            Sku = "SKU-TEST",
            CurrentPrice = 29.99m,
            ProposedPrice = 24.99m,
            Scenarios = new[]
            {
                new SquadCommerce.Contracts.A2UI.PriceScenario
                {
                    ScenarioName = "Test",
                    Price = 24.99m,
                    EstimatedMargin = 35.0m,
                    EstimatedRevenue = 5000.00m,
                    ProjectedUnitsSold = 200
                }
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        var grid = new SquadCommerce.Contracts.A2UI.MarketComparisonGridData
        {
            Sku = "SKU-TEST",
            ProductName = "Test Product",
            Competitors = new[]
            {
                new SquadCommerce.Contracts.A2UI.CompetitorPrice
                {
                    CompetitorName = "TestMart",
                    Price = 24.99m,
                    Source = "A2A:Test",
                    Verified = true,
                    LastUpdated = DateTimeOffset.UtcNow
                }
            },
            OurPrice = 29.99m,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        heatmap.Should().NotBeNull();
        chart.Should().NotBeNull();
        grid.Should().NotBeNull();

        heatmap.Stores.Should().HaveCount(1);
        chart.Scenarios.Should().HaveCount(1);
        grid.Competitors.Should().HaveCount(1);
    }
}
