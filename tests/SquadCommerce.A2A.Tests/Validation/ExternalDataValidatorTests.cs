using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.A2A.Validation;

namespace SquadCommerce.A2A.Tests.Validation;

public class ExternalDataValidatorTests
{
    private readonly ExternalDataValidator _validator;
    private readonly Mock<IPricingRepository> _mockPricingRepo;
    private readonly Mock<IInventoryRepository> _mockInventoryRepo;

    public ExternalDataValidatorTests()
    {
        _mockPricingRepo = new Mock<IPricingRepository>();
        _mockInventoryRepo = new Mock<IInventoryRepository>();
        _validator = new ExternalDataValidator(
            _mockPricingRepo.Object, 
            _mockInventoryRepo.Object, 
            NullLogger<ExternalDataValidator>.Instance);
    }

    [Fact]
    public async Task Should_ReturnHighConfidence_When_PriceIsReasonable()
    {
        // Arrange
        var competitorName = "Competitor A";
        var sku = "SKU-1001";
        var externalPrice = 24.99m;

        _mockPricingRepo.Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), sku, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(29.99m);

        // Act
        var result = await _validator.ValidatePricingAsync(competitorName, sku, externalPrice, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ConfidenceLevel.Should().BeOneOf("High", "Medium");
    }

    [Fact]
    public async Task Should_ReturnUnverified_When_PriceIsNegative()
    {
        // Arrange
        var competitorName = "Competitor B";
        var sku = "SKU-1002";
        var externalPrice = -10.00m;

        // Act
        var result = await _validator.ValidatePricingAsync(competitorName, sku, externalPrice, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ConfidenceLevel.Should().Be("Unverified");
    }

    [Fact]
    public async Task Should_ReturnUnverified_When_PriceIsExcessivelyHigh()
    {
        // Arrange
        var competitorName = "Competitor C";
        var sku = "SKU-1005";
        var externalPrice = 150000.00m;

        // Act
        var result = await _validator.ValidatePricingAsync(competitorName, sku, externalPrice, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ConfidenceLevel.Should().Be("Unverified");
    }

    [Theory]
    [InlineData(10.00)]
    [InlineData(50.00)]
    [InlineData(99.99)]
    [InlineData(500.00)]
    public async Task Should_ReturnValid_When_PriceWithinReasonableRange(decimal price)
    {
        // Arrange
        var competitorName = "Competitor D";
        var sku = "SKU-1001";

        _mockPricingRepo.Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), sku, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(price * 1.1m); // Simulating similar price

        // Act
        var result = await _validator.ValidatePricingAsync(competitorName, sku, price, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_IncludeTimestamp_When_ValidationCompletes()
    {
        // Arrange
        var competitorName = "Competitor E";
        var sku = "SKU-1003";
        var externalPrice = 599.99m;
        var beforeValidation = DateTimeOffset.UtcNow;

        _mockPricingRepo.Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), sku, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(550.00m);

        // Act
        var result = await _validator.ValidatePricingAsync(competitorName, sku, externalPrice, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Timestamp.Should().BeOnOrAfter(beforeValidation);
        result.Timestamp.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Should_ReturnMediumConfidence_When_InventoryValidated()
    {
        // Arrange
        var competitorName = "Competitor F";
        var sku = "SKU-1008";
        var availability = "In Stock";

        _mockInventoryRepo.Setup(r => r.GetInventoryLevelsAsync(sku, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new List<Contracts.Models.InventorySnapshot>
                          {
                              new Contracts.Models.InventorySnapshot 
                              { 
                                  StoreId = "SEA-001", 
                                  Sku = sku, 
                                  UnitsOnHand = 10, 
                                  ReorderPoint = 5, 
                                  UnitsOnOrder = 0, 
                                  LastUpdated = DateTimeOffset.UtcNow 
                              }
                          });

        // Act
        var result = await _validator.ValidateInventoryAsync(competitorName, sku, availability, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ConfidenceLevel.Should().Be("Medium");
    }

    [Fact]
    public async Task Should_IncludeReason_When_ValidationCompletes()
    {
        // Arrange
        var competitorName = "Competitor G";
        var sku = "SKU-1004";
        var externalPrice = 75.00m;

        _mockPricingRepo.Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), sku, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(79.99m);

        // Act
        var result = await _validator.ValidatePricingAsync(competitorName, sku, externalPrice, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_HandleNullOrEmptyCompetitorName_When_Validating()
    {
        // Arrange
        var sku = "SKU-1001";
        var externalPrice = 24.99m;

        _mockPricingRepo.Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), sku, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(29.99m);

        // Act
        var result = await _validator.ValidatePricingAsync(string.Empty, sku, externalPrice, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ConfidenceLevel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_ProvideValidationResult_When_DataCrossReferenced()
    {
        // Arrange - External data is cross-referenced against internal sources
        var competitorName = "Known Competitor";
        var sku = "SKU-1001";
        var externalPrice = 29.99m;

        _mockPricingRepo.Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), sku, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(29.99m);

        // Act
        var result = await _validator.ValidatePricingAsync(competitorName, sku, externalPrice, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ConfirmingSources.Should().NotBeNull();
        result.ConfirmingSources.Should().NotBeEmpty();
    }
}

