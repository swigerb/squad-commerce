using Microsoft.Playwright;

namespace SquadCommerce.Playwright.Tests.Pages;

/// <summary>
/// Page Object Model for the approval panel workflow
/// </summary>
public class ApprovalPanelPage
{
    private readonly IPage _page;

    public ApprovalPanelPage(IPage page)
    {
        _page = page;
    }

    // Locators
    private ILocator ApprovalPanelContainer => _page.Locator(".approval-panel, [data-testid='approval-panel']");
    private ILocator ApproveButton => _page.Locator("button.approve-btn, button[data-action='approve']");
    private ILocator RejectButton => _page.Locator("button.reject-btn, button[data-action='reject']");
    private ILocator ModifyButton => _page.Locator("button.modify-btn, button[data-action='modify']");
    private ILocator ConfirmationDialog => _page.Locator(".confirmation-dialog, [role='dialog']");
    private ILocator ConfirmButton => ConfirmationDialog.Locator("button.confirm, button[data-action='confirm']");
    private ILocator CancelButton => ConfirmationDialog.Locator("button.cancel, button[data-action='cancel']");
    private ILocator StatusMessage => _page.Locator(".approval-status, .status-message");

    // Methods
    public async Task<bool> IsApprovalPanelVisibleAsync()
    {
        return await ApprovalPanelContainer.IsVisibleAsync();
    }

    public async Task WaitForApprovalPanelAsync(int timeoutMs = 10000)
    {
        await ApprovalPanelContainer.WaitForAsync(new() 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = timeoutMs 
        });
    }

    public async Task<bool> IsApproveButtonEnabledAsync()
    {
        return await ApproveButton.IsEnabledAsync();
    }

    public async Task<bool> IsRejectButtonEnabledAsync()
    {
        return await RejectButton.IsEnabledAsync();
    }

    public async Task<bool> IsModifyButtonEnabledAsync()
    {
        return await ModifyButton.IsEnabledAsync();
    }

    public async Task ApproveAllAsync()
    {
        await ApproveButton.ClickAsync();
        
        // Wait for confirmation dialog if it appears
        try
        {
            await ConfirmationDialog.WaitForAsync(new() 
            { 
                State = WaitForSelectorState.Visible, 
                Timeout = 2000 
            });
            await ConfirmButton.ClickAsync();
        }
        catch
        {
            // No confirmation dialog, proceed
        }
        
        // Wait for status message
        await StatusMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    public async Task RejectAllAsync()
    {
        await RejectButton.ClickAsync();
        
        // Wait for confirmation dialog if it appears
        try
        {
            await ConfirmationDialog.WaitForAsync(new() 
            { 
                State = WaitForSelectorState.Visible, 
                Timeout = 2000 
            });
            await ConfirmButton.ClickAsync();
        }
        catch
        {
            // No confirmation dialog, proceed
        }
        
        // Wait for status message
        await StatusMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    public async Task ModifyPriceAsync(string sku, decimal newPrice)
    {
        await ModifyButton.ClickAsync();
        
        // Wait for modification UI
        await _page.WaitForTimeoutAsync(500);
        
        // Locate the price input for the SKU
        var priceInput = _page.Locator($"input[data-sku='{sku}'], input[name='price-{sku}']").First;
        await priceInput.FillAsync(newPrice.ToString("F2"));
        
        // Submit modification
        var submitButton = _page.Locator("button.submit-modification, button[type='submit']");
        await submitButton.ClickAsync();
        
        // Wait for confirmation
        await StatusMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    public async Task<string> GetStatusMessageAsync()
    {
        return await StatusMessage.TextContentAsync() ?? string.Empty;
    }

    public async Task<bool> IsConfirmationDialogVisibleAsync()
    {
        try
        {
            return await ConfirmationDialog.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }
}
