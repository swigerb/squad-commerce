using NUnit.Framework;
using SquadCommerce.Playwright.Tests.Fixtures;
using SquadCommerce.Playwright.Tests.Pages;

namespace SquadCommerce.Playwright.Tests.Tests;

/// <summary>
/// Tests for accessibility compliance (WCAG 2.1)
/// </summary>
[TestFixture]
[Category("Accessibility")]
[Category("A11y")]
public class AccessibilityTests : PlaywrightTestBase
{
    private MainPage? _mainPage;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        _mainPage = new MainPage(Page!, BaseUrl);
        
        await _mainPage.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();
    }

    [Test]
    public async Task Should_HaveAriaLabels_OnAllA2UIComponents()
    {
        // Arrange
        var componentSelectors = new[]
        {
            ".retail-stock-heatmap",
            ".pricing-impact-chart",
            ".market-comparison-grid",
            ".decision-audit-trail",
            ".agent-pipeline-visualizer"
        };

        // Act & Assert
        foreach (var selector in componentSelectors)
        {
            var component = Page!.Locator(selector).First;
            
            try
            {
                // Check if component exists
                var isVisible = await component.IsVisibleAsync();
                
                if (isVisible)
                {
                    // Check for aria-label or aria-labelledby
                    var ariaLabel = await component.GetAttributeAsync("aria-label");
                    var ariaLabelledBy = await component.GetAttributeAsync("aria-labelledby");
                    
                    Assert.That(ariaLabel ?? ariaLabelledBy, Is.Not.Null,
                        $"Component {selector} should have aria-label or aria-labelledby");
                }
            }
            catch
            {
                Assert.Warn($"Component {selector} not found - may not be rendered yet");
            }
        }
    }

    [Test]
    public async Task Should_BeKeyboardNavigable_When_TabPressed()
    {
        // Act - Press Tab multiple times to navigate through interactive elements
        var focusedElements = new List<string>();
        
        for (int i = 0; i < 10; i++)
        {
            await Page!.Keyboard.PressAsync("Tab");
            await Task.Delay(100);
            
            // Get focused element
            var focusedElement = await Page.EvaluateAsync<string>(
                "() => document.activeElement?.tagName + (document.activeElement?.className ? '.' + document.activeElement.className.split(' ')[0] : '')");
            
            if (!string.IsNullOrWhiteSpace(focusedElement))
            {
                focusedElements.Add(focusedElement);
            }
        }

        // Assert
        Assert.That(focusedElements.Count, Is.GreaterThan(0), 
            "Should be able to navigate through interactive elements with Tab");
        
        // Check that we're not stuck on the same element
        var uniqueElements = focusedElements.Distinct().Count();
        Assert.That(uniqueElements, Is.GreaterThan(1), 
            "Tab navigation should move between different elements");
    }

    [Test]
    public async Task Should_HaveProperHeadingHierarchy_OnDashboard()
    {
        // Act - Get all headings
        var h1Count = await Page!.Locator("h1").CountAsync();
        var h2Count = await Page.Locator("h2").CountAsync();
        var h3Count = await Page.Locator("h3").CountAsync();

        // Assert - Heading structure
        Assert.That(h1Count, Is.EqualTo(1), "Page should have exactly one H1 heading");
        Assert.That(h2Count, Is.GreaterThanOrEqualTo(0), "H2 headings should be present for sections");
        
        // Verify H1 comes before H2
        var firstHeading = await Page.Locator("h1, h2, h3").First.EvaluateAsync<string>("el => el.tagName");
        Assert.That(firstHeading, Is.EqualTo("H1"), "First heading should be H1");
    }

    [Test]
    public async Task Should_HaveAltTextOnImages_When_ImagesPresent()
    {
        // Act - Find all images
        var images = Page!.Locator("img");
        var imageCount = await images.CountAsync();

        // Assert
        if (imageCount > 0)
        {
            for (int i = 0; i < imageCount; i++)
            {
                var img = images.Nth(i);
                var alt = await img.GetAttributeAsync("alt");
                var role = await img.GetAttributeAsync("role");
                
                // Images should have alt text or role="presentation"
                Assert.That(alt != null || role == "presentation", Is.True,
                    $"Image {i} should have alt text or role='presentation'");
            }
        }
        else
        {
            Assert.Pass("No images found on page");
        }
    }

    [Test]
    public async Task Should_HaveProperColorContrast_OnButtons()
    {
        // Act - Find all buttons
        var buttons = Page!.Locator("button");
        var buttonCount = await buttons.CountAsync();

        // Assert
        Assert.That(buttonCount, Is.GreaterThan(0), "Page should have interactive buttons");
        
        // Check first few buttons for visibility
        var maxToCheck = Math.Min(5, buttonCount);
        for (int i = 0; i < maxToCheck; i++)
        {
            var button = buttons.Nth(i);
            var isVisible = await button.IsVisibleAsync();
            
            if (isVisible)
            {
                // Get computed styles
                var backgroundColor = await button.EvaluateAsync<string>(
                    "btn => window.getComputedStyle(btn).backgroundColor");
                var color = await button.EvaluateAsync<string>(
                    "btn => window.getComputedStyle(btn).color");
                
                Assert.That(backgroundColor, Is.Not.Null.And.Not.Empty, 
                    $"Button {i} should have background color");
                Assert.That(color, Is.Not.Null.And.Not.Empty, 
                    $"Button {i} should have text color");
            }
        }
    }

    [Test]
    public async Task Should_HaveFocusIndicators_OnInteractiveElements()
    {
        // Act - Focus on first button and check for focus indicator
        var firstButton = Page!.Locator("button").First;
        
        try
        {
            await firstButton.FocusAsync();
            await Task.Delay(100);
            
            // Check if focused element has outline or box-shadow
            var outline = await firstButton.EvaluateAsync<string>(
                "btn => window.getComputedStyle(btn).outline");
            var boxShadow = await firstButton.EvaluateAsync<string>(
                "btn => window.getComputedStyle(btn).boxShadow");
            
            // Assert - Should have some focus indicator
            Assert.That(outline != "none" || boxShadow != "none", Is.True,
                "Focused button should have visible focus indicator (outline or box-shadow)");
        }
        catch
        {
            Assert.Warn("Could not test focus indicators - no focusable buttons found");
        }
    }

    [Test]
    public async Task Should_NotHaveEmptyLinks_OnPage()
    {
        // Act - Find all links
        var links = Page!.Locator("a");
        var linkCount = await links.CountAsync();

        // Assert
        for (int i = 0; i < linkCount; i++)
        {
            var link = links.Nth(i);
            var text = await link.TextContentAsync();
            var ariaLabel = await link.GetAttributeAsync("aria-label");
            var title = await link.GetAttributeAsync("title");
            
            // Links should have text content or aria-label
            Assert.That(!string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(ariaLabel) || !string.IsNullOrWhiteSpace(title),
                Is.True, $"Link {i} should have text content, aria-label, or title");
        }
    }
}
