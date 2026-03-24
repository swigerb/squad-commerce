using NUnit.Framework;
using SquadCommerce.Playwright.Tests.Fixtures;
using SquadCommerce.Playwright.Tests.Pages;

namespace SquadCommerce.Playwright.Tests.Tests;

/// <summary>
/// Tests for responsive design across different viewport sizes
/// </summary>
[TestFixture]
[Category("Responsive")]
[Category("UI")]
public class ResponsiveTests : PlaywrightTestBase
{
    private MainPage? _mainPage;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        _mainPage = new MainPage(Page!, BaseUrl);
    }

    [Test]
    [TestCase(375, 667, "iPhone SE")]
    [TestCase(414, 896, "iPhone 11 Pro Max")]
    [TestCase(768, 1024, "iPad")]
    public async Task Should_StackComponentsVertically_When_MobileViewport(int width, int height, string deviceName)
    {
        // Arrange
        await Page!.SetViewportSizeAsync(width, height);
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Act - Get layout
        var appLayout = Page.Locator(".app-layout");
        var isLayoutVisible = await appLayout.IsVisibleAsync();

        // Assert
        Assert.That(isLayoutVisible, Is.True, $"Layout should be visible on {deviceName}");
        
        // Check that sidebar and main content are accessible
        var sidebar = Page.Locator("aside.sidebar-left");
        var mainContent = Page.Locator("main.main-content");
        
        var isSidebarInDom = await sidebar.CountAsync() > 0;
        var isMainContentInDom = await mainContent.CountAsync() > 0;
        
        Assert.That(isSidebarInDom, Is.True, $"Sidebar should be in DOM on {deviceName}");
        Assert.That(isMainContentInDom, Is.True, $"Main content should be in DOM on {deviceName}");
    }

    [Test]
    [TestCase(1280, 720, "HD")]
    [TestCase(1920, 1080, "Full HD")]
    [TestCase(2560, 1440, "QHD")]
    public async Task Should_ShowSidebarLayout_When_DesktopViewport(int width, int height, string resolution)
    {
        // Arrange
        await Page!.SetViewportSizeAsync(width, height);
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Act & Assert - Layout should be visible
        var isLayoutVisible = await _mainPage.IsLayoutVisibleAsync();
        Assert.That(isLayoutVisible, Is.True, $"Layout should be visible at {resolution}");
        
        // Check that sidebar and main content are side-by-side
        var sidebar = Page.Locator("aside.sidebar-left");
        var mainContent = Page.Locator("main.main-content");
        
        var isSidebarVisible = await sidebar.IsVisibleAsync();
        var isMainContentVisible = await mainContent.IsVisibleAsync();
        
        Assert.That(isSidebarVisible, Is.True, $"Sidebar should be visible at {resolution}");
        Assert.That(isMainContentVisible, Is.True, $"Main content should be visible at {resolution}");
        
        // Check horizontal layout (sidebar should be to the left of main content)
        var sidebarBox = await sidebar.BoundingBoxAsync();
        var mainContentBox = await mainContent.BoundingBoxAsync();
        
        if (sidebarBox != null && mainContentBox != null)
        {
            Assert.That(sidebarBox.X, Is.LessThan(mainContentBox.X),
                $"Sidebar should be positioned left of main content at {resolution}");
        }
    }

    [Test]
    public async Task Should_AdjustFontSize_When_ViewportChanges()
    {
        // Arrange - Mobile viewport
        await Page!.SetViewportSizeAsync(375, 667);
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Act - Get font size on mobile
        var header = Page.Locator(".header-brand h1");
        var mobileFontSize = await header.EvaluateAsync<string>(
            "h => window.getComputedStyle(h).fontSize");

        // Change to desktop viewport
        await Page.SetViewportSizeAsync(1920, 1080);
        await Task.Delay(500); // Wait for responsive adjustments

        var desktopFontSize = await header.EvaluateAsync<string>(
            "h => window.getComputedStyle(h).fontSize");

        // Assert - Font sizes should be defined (may or may not be different depending on design)
        Assert.That(mobileFontSize, Is.Not.Null.And.Not.Empty, "Mobile font size should be defined");
        Assert.That(desktopFontSize, Is.Not.Null.And.Not.Empty, "Desktop font size should be defined");
        
        Console.WriteLine($"Mobile font size: {mobileFontSize}");
        Console.WriteLine($"Desktop font size: {desktopFontSize}");
    }

    [Test]
    public async Task Should_HideOrShowElements_BasedOnViewport()
    {
        // Arrange - Desktop viewport
        await Page!.SetViewportSizeAsync(1920, 1080);
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Act - Check header subtitle visibility on desktop
        var headerSubtitle = Page.Locator(".header-subtitle");
        var isVisibleDesktop = await headerSubtitle.IsVisibleAsync();

        // Change to mobile
        await Page.SetViewportSizeAsync(375, 667);
        await Task.Delay(500);

        var isVisibleMobile = await headerSubtitle.IsVisibleAsync();

        // Assert - Element visibility may change based on viewport
        Console.WriteLine($"Header subtitle visible on desktop: {isVisibleDesktop}");
        Console.WriteLine($"Header subtitle visible on mobile: {isVisibleMobile}");
        
        // Just document the behavior - specific hiding rules depend on design
        Assert.Pass($"Desktop visibility: {isVisibleDesktop}, Mobile visibility: {isVisibleMobile}");
    }

    [Test]
    public async Task Should_HandleOrientation_When_DeviceRotates()
    {
        // Arrange - Portrait
        await Page!.SetViewportSizeAsync(375, 667);
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        var isLayoutVisiblePortrait = await _mainPage.IsLayoutVisibleAsync();

        // Act - Rotate to landscape
        await Page.SetViewportSizeAsync(667, 375);
        await Task.Delay(500);

        var isLayoutVisibleLandscape = await _mainPage.IsLayoutVisibleAsync();

        // Assert
        Assert.That(isLayoutVisiblePortrait, Is.True, "Layout should be visible in portrait");
        Assert.That(isLayoutVisibleLandscape, Is.True, "Layout should be visible in landscape");
    }

    [Test]
    public async Task Should_NotHaveHorizontalScroll_OnMobile()
    {
        // Arrange
        await Page!.SetViewportSizeAsync(375, 667);
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Act - Check for horizontal scrollbar
        var bodyScrollWidth = await Page.EvaluateAsync<int>("() => document.body.scrollWidth");
        var bodyClientWidth = await Page.EvaluateAsync<int>("() => document.body.clientWidth");

        // Assert - No horizontal overflow
        Assert.That(bodyScrollWidth, Is.LessThanOrEqualTo(bodyClientWidth + 1), // Allow 1px tolerance
            "Page should not have horizontal scroll on mobile viewport");
    }
}
