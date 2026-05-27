using Microsoft.JSInterop;

namespace UmobQuiz.Client.Services;

public sealed class LocalStorageAuthPersistence(IJSRuntime jsRuntime, AuthState authState)
{
    private const string TokenKey = "umobquiz_token";
    private const string UsernameKey = "umobquiz_username";

    public async Task LoadAsync()
    {
        var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        var username = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", UsernameKey);
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(username))
        {
            if (!string.IsNullOrWhiteSpace(token) || !string.IsNullOrWhiteSpace(username))
            {
                await ClearAsync();
            }

            return;
        }

        authState.SetSession(token, username);
    }

    public async Task SaveAsync(string token, string username)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", UsernameKey, username);
        authState.SetSession(token, username);
    }

    public async Task ClearAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", UsernameKey);
        authState.SignOut();
    }
}
