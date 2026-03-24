using Microsoft.Playwright;

namespace SquadCommerce.Playwright.Tests.Pages;

/// <summary>
/// Page Object Model for the main application layout
/// </summary>
public class MainPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public MainPage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    // Locators
    private ILocator HeaderBrand => _page.Locator(".header-brand h1");
    private ILocator HeaderSubtitle => _page.Locator(".header-subtitle");
    private ILocator StatusBar => _page.Locator("aside.sidebar-left .sidebar-header");
    private ILocator ChatPanel => _page.Locator("aside.sidebar-left");
    private ILocator MainContent => _page.Locator("main.main-content");
    private ILocator ContentHeader => _page.Locator(".content-header h2");
    private ILocator ContentBody => _page.Locator("article.content-body");
    private ILocator AppLayout => _page.Locator(".app-layout");

    // Methods
    public async Task NavigateAsync()
    {
        await _page.GotoAsync(_baseUrl);
    }

    public async Task WaitForAppLoadedAsync()
    {
        await AppLayout.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await HeaderBrand.WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    public async Task<bool> IsLayoutVisibleAsync()
    {
        return await AppLayout.IsVisibleAsync();
    }

    public async Task<string> GetHeaderTitleAsync()
    {
        return await HeaderBrand.TextContentAsync() ?? string.Empty;
    }

    public async Task<string> GetHeaderSubtitleAsync()
    {
        return await HeaderSubtitle.TextContentAsync() ?? string.Empty;
    }

    public async Task<bool> IsChatPanelVisibleAsync()
    {
        return await ChatPanel.IsVisibleAsync();
    }

    public async Task<bool> IsMainContentVisibleAsync()
    {
        return await MainContent.IsVisibleAsync();
    }

    public async Task<string> GetContentHeaderAsync()
    {
        return await ContentHeader.TextContentAsync() ?? string.Empty;
    }
}
