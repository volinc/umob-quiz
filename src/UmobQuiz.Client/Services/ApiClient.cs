using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UmobQuiz.Shared;

namespace UmobQuiz.Client.Services;

public sealed class ApiClient(HttpClient httpClient, AuthState authState, AuthSessionService authSession)
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

    public async Task<DownloadedFile> DownloadHistoryCsvAsync(
        int? limit = null,
        bool includeActive = false,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildHistoryExportQuery(limit, includeActive, fromUtc, toUtc);
        var response = await SendAuthorizedAsync(HttpMethod.Get, $"api/game/history/export{query}");

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Export rate limit exceeded. Try again later.");
        }

        await EnsureSuccessAsync(response);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/csv";
        var fileName = GetDownloadFileName(response) ?? "umob-quiz-history.csv";

        return new DownloadedFile(content, fileName, contentType);
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(int? limit = null)
    {
        var url = limit is null ? "api/leaderboard" : $"api/leaderboard?limit={limit.Value}";
        var response = await SendAuthorizedAsync(HttpMethod.Get, url);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<List<LeaderboardEntryDto>>())!;
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

    private static string BuildHistoryExportQuery(
        int? limit,
        bool includeActive,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var parts = new List<string>();
        if (limit is not null)
        {
            parts.Add($"limit={limit.Value}");
        }

        if (includeActive)
        {
            parts.Add("includeActive=true");
        }

        if (fromUtc is not null)
        {
            parts.Add($"from={Uri.EscapeDataString(fromUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (toUtc is not null)
        {
            parts.Add($"to={Uri.EscapeDataString(toUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    private static string? GetDownloadFileName(HttpResponseMessage response)
    {
        if (ContentDispositionHeaderValue.TryParse(
                response.Content.Headers.ContentDisposition?.ToString(),
                out var disposition))
        {
            var name = disposition.FileNameStar ?? disposition.FileName;
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim('"');
            }
        }

        return null;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await authSession.InvalidateAsync();
            throw new UnauthorizedAccessException("Your session has expired. Please sign in again.");
        }

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"API call failed ({(int)response.StatusCode}): {body}");
    }
}
