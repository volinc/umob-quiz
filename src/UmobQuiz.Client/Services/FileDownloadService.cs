using Microsoft.JSInterop;

namespace UmobQuiz.Client.Services;

public sealed class FileDownloadService(IJSRuntime jsRuntime, ApiClient apiClient)
{
    public Task DownloadHistoryCsvAsync(
        int? limit = null,
        bool includeActive = false,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default) =>
        DownloadAsync(
            () => apiClient.DownloadHistoryCsvAsync(limit, includeActive, fromUtc, toUtc, cancellationToken));

    private async Task DownloadAsync(Func<Task<DownloadedFile>> download)
    {
        var file = await download();
        var base64 = Convert.ToBase64String(file.Content);
        await jsRuntime.InvokeVoidAsync("umobQuizDownloadFile", file.FileName, file.ContentType, base64);
    }
}
