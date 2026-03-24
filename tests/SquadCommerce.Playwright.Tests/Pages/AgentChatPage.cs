using Microsoft.Playwright;

namespace SquadCommerce.Playwright.Tests.Pages;

/// <summary>
/// Page Object Model for the agent chat panel
/// </summary>
public class AgentChatPage
{
    private readonly IPage _page;

    public AgentChatPage(IPage page)
    {
        _page = page;
    }

    // Locators
    private ILocator ChatContainer => _page.Locator(".agent-chat-container, .chat-container");
    private ILocator MessageList => _page.Locator(".chat-messages, .message-list");
    private ILocator MessageItems => MessageList.Locator(".chat-message, .message-item");
    private ILocator InputField => _page.Locator("input[type='text'].chat-input, textarea.chat-input");
    private ILocator SendButton => _page.Locator("button.send-message, button.chat-send");
    private ILocator StatusMessages => _page.Locator(".status-message, .agent-status");
    private ILocator TypingIndicator => _page.Locator(".typing-indicator, .agent-typing");

    // Methods
    public async Task SendMessageAsync(string message)
    {
        await InputField.FillAsync(message);
        await SendButton.ClickAsync();
    }

    public async Task WaitForAgentResponseAsync(int timeoutMs = 30000)
    {
        await _page.WaitForTimeoutAsync(500); // Brief pause for response to start
        
        // Wait for either a new message or status update
        await _page.WaitForSelectorAsync(".chat-message:last-child, .status-message:last-child", 
            new() { Timeout = timeoutMs });
    }

    public async Task<IReadOnlyList<string>> GetStatusMessagesAsync()
    {
        var messages = new List<string>();
        var statusElements = await StatusMessages.AllAsync();
        
        foreach (var element in statusElements)
        {
            var text = await element.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                messages.Add(text);
            }
        }
        
        return messages;
    }

    public async Task<IReadOnlyList<string>> GetChatMessagesAsync()
    {
        var messages = new List<string>();
        var messageElements = await MessageItems.AllAsync();
        
        foreach (var element in messageElements)
        {
            var text = await element.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                messages.Add(text);
            }
        }
        
        return messages;
    }

    public async Task<bool> IsTypingIndicatorVisibleAsync()
    {
        try
        {
            return await TypingIndicator.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsChatVisibleAsync()
    {
        return await ChatContainer.IsVisibleAsync();
    }
}
