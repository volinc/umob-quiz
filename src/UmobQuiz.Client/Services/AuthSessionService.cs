using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace UmobQuiz.Client.Services;

public sealed class AuthSessionService(
    AuthState authState,
    LocalStorageAuthPersistence persistence,
    HttpClient httpClient,
    NavigationManager navigation)
{
    public async Task RestoreAsync()
    {
        await persistence.LoadAsync();

        if (!authState.IsAuthenticated)
        {
            return;
        }

        if (JwtTokenHelper.IsExpired(authState.Token!))
        {
            await InvalidateAsync(redirect: false);
            return;
        }

        if (!await ValidateWithServerAsync())
        {
            await InvalidateAsync(redirect: false);
        }
    }

    public async Task InvalidateAsync(bool redirect = true)
    {
        await persistence.ClearAsync();

        if (redirect && !IsPublicRoute())
        {
            navigation.NavigateTo("/login");
        }
    }

    private async Task<bool> ValidateWithServerAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Token);
        var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private bool IsPublicRoute()
    {
        var path = new Uri(navigation.Uri).AbsolutePath.TrimEnd('/');
        return path is "/login" or "/register" or "";
    }
}
