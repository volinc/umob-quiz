using UmobQuiz.Api.Application.Questions;
using UmobQuiz.Api.Infrastructure.Gbfs;

namespace UmobQuiz.Api.Workers;

/// <summary>
/// Periodically ingests GBFS feeds and refreshes the in-memory question pool.
/// </summary>
public sealed class GbfsIngestionWorker(
    IServiceProvider serviceProvider,
    ILogger<GbfsIngestionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<GbfsIngestionService>();
                var questionPoolService = scope.ServiceProvider.GetRequiredService<QuestionPoolService>();

                await ingestionService.IngestAllProvidersAsync(stoppingToken);
                await questionPoolService.RefreshPoolAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "GBFS ingestion cycle failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
