using System.Net.Http.Headers;
using System.Net.Http.Json;
using UmobQuiz.Shared;

namespace UmobQuiz.Client.Services;

public sealed class ApiClient(HttpClient httpClient, AuthState authState)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/register", request);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", request);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public async Task<StartGameResponse> StartGameAsync()
    {
        var response = await SendAuthorizedAsync(HttpMethod.Post, "api/game/start");
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<StartGameResponse>())!;
    }

    public async Task<QuestionDto> GetQuestionAsync(Guid sessionId)
    {
        var response = await SendAuthorizedAsync(HttpMethod.Get, $"api/game/{sessionId}/question");
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<QuestionDto>())!;
    }

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(Guid sessionId, SubmitAnswerRequest request)
    {
        var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/game/{sessionId}/answer",
            JsonContent.Create(request));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!;
    }

    public async Task<SubmitAnswerResponse?> FinishGameAsync(Guid sessionId)
    {
        var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/game/{sessionId}/finish");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<SubmitAnswerResponse>();
    }

    public async Task<IReadOnlyList<GameSessionSummaryDto>> GetHistoryAsync()
    {
        var response = await SendAuthorizedAsync(HttpMethod.Get, "api/game/history");
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<List<GameSessionSummaryDto>>())!;
    }

    private Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url) =>
        SendAuthorizedAsync(method, url, null);

    private Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (!string.IsNullOrWhiteSpace(authState.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Token);
        }

        return httpClient.SendAsync(request);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"API call failed ({(int)response.StatusCode}): {body}");
    }
}
