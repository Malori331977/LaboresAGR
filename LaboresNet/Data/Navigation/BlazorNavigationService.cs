using Microsoft.JSInterop;

namespace LaboresNet.Data.Navigation;

public class BlazorNavigationService
{
    private readonly IJSRuntime _jsRuntime;

    public BlazorNavigationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> GoBackAsync()
    {
        try
        {
            var canGoBack = await _jsRuntime.InvokeAsync<bool>("navigationHelper.goBack");
            return canGoBack;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CanGoBackAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("navigationHelper.canGoBack");
        }
        catch
        {
            return false;
        }
    }
}
