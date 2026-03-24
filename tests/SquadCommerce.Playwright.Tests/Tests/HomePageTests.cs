using NUnit.Framework;
using SquadCommerce.Playwright.Tests.Fixtures;
using SquadCommerce.Playwright.Tests.Pages;

namespace SquadCommerce.Playwright.Tests.Tests;

/// <summary>
/// Tests for the home page and main layout
/// </summary>
[TestFixture]
[Category("UI")]
[Category("Smoke")]
public class HomePageTests : PlaywrightTestBase
{
    private MainPage? _mainPage;
    private AgentChatPage? _chatPage;

    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();
        _mainPage = new MainPage(Page!, BaseUrl);
        _chatPage = new AgentChatPage(Page!);
    }

    [Test]
    public async Task Should_LoadMainLayout_When_AppStarts()
    {
        // Arrange & Act
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Assert
        var isLayoutVisible = await _mainPage.IsLayoutVisibleAsync();
        Assert.That(isLayoutVisible, Is.True, "Main layout should be visible");
        
        var headerTitle = await _mainPage.GetHeaderTitleAsync();
        Assert.That(headerTitle, Does.Contain("Squad Commerce"), "Header should contain 'Squad Commerce'");
    }

    [Test]
    public async Task Should_ShowStatusBar_When_PageLoads()
    {
        // Arrange & Act
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Assert
        var headerSubtitle = await _mainPage.GetHeaderSubtitleAsync();
        Assert.That(headerSubtitle, Is.Not.Empty, "Header subtitle should be visible");
    }

    [Test]
    public async Task Should_ShowChatPanel_When_PageLoads()
    {
        // Arrange & Act
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Assert
        var isChatVisible = await _mainPage.IsChatPanelVisibleAsync();
        Assert.That(isChatVisible, Is.True, "Chat panel should be visible");
        
        var isChatComponentVisible = await _chatPage!.IsChatVisibleAsync();
        Assert.That(isChatComponentVisible, Is.True, "Chat component should be visible");
    }

    [Test]
    public async Task Should_ShowEmptyDashboard_When_NoAnalysisRunning()
    {
        // Arrange & Act
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Assert
        var isMainContentVisible = await _mainPage.IsMainContentVisibleAsync();
        Assert.That(isMainContentVisible, Is.True, "Main content area should be visible");
        
        var contentHeader = await _mainPage.GetContentHeaderAsync();
        Assert.That(contentHeader, Does.Contain("Analysis Dashboard").Or.Contain("Dashboard"), 
            "Content header should indicate dashboard");
    }

    [Test]
    public async Task Should_HaveResponsiveLayout_When_ViewportChanges()
    {
        // Arrange
        await _mainPage!.NavigateAsync();
        await _mainPage.WaitForAppLoadedAsync();

        // Act - Test mobile viewport
        await Page!.SetViewportSizeAsync(375, 667);
        await Task.Delay(500); // Wait for responsive layout adjustment

        // Assert
        var isChatVisible = await _mainPage.IsChatPanelVisibleAsync();
        Assert.That(isChatVisible, Is.True, "Chat panel should still be accessible on mobile");

        // Act - Test desktop viewport
        await Page.SetViewportSizeAsync(1920, 1080);
        await Task.Delay(500);

        // Assert
        var isLayoutVisible = await _mainPage.IsLayoutVisibleAsync();
        Assert.That(isLayoutVisible, Is.True, "Layout should be visible on desktop");
    }
}
