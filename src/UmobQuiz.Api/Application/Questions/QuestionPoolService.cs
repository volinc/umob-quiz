using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UmobQuiz.Api.Configuration;
using UmobQuiz.Api.Infrastructure.Persistence;

namespace UmobQuiz.Api.Application.Questions;

public sealed class QuestionPoolService(
    AppDbContext dbContext,
    IMemoryCache memoryCache,
    IConfiguration configuration,
    IEnumerable<IQuestionGenerator> generators,
    ILogger<QuestionPoolService> logger)
{
    public const string CacheKey = "question-pool";
    private const int TargetPoolSize = 100;

    public async Task RefreshPoolAsync(CancellationToken cancellationToken)
    {
        var providers = configuration.GetSection(GbfsProviderOptions.SectionName).Get<GbfsProviderOptions[]>() ?? [];
        var stats = new List<ProviderStats>();

        foreach (var provider in providers)
        {
            var floatingBikeCount = await dbContext.Bikes.CountAsync(
                b => b.Provider == provider.Key && b.IsActive,
                cancellationToken);
            var dockedBikeCount = await dbContext.Stations
                .Where(s => s.Provider == provider.Key && s.IsActive)
                .SumAsync(s => s.NumBikesAvailable ?? 0, cancellationToken);
            var stationCount = await dbContext.Stations.CountAsync(
                s => s.Provider == provider.Key && s.IsActive,
                cancellationToken);

            // Many GBFS systems only expose docked bikes via station_status.
            stats.Add(new ProviderStats(
                provider.Key,
                provider.DisplayName,
                floatingBikeCount + dockedBikeCount,
                stationCount));
        }

        if (stats.All(s => s.ActiveBikeCount == 0 && s.ActiveStationCount == 0))
        {
            logger.LogWarning("Skipping question pool refresh because no active GBFS data is available yet");
            return;
        }

        var random = new Random();
        var questions = generators
            .SelectMany(g => g.Generate(stats, random))
            .ToList();

        if (questions.Count == 0)
        {
            logger.LogWarning("No questions were generated from current provider stats");
            return;
        }

        while (questions.Count < TargetPoolSize)
        {
            questions.Add(questions[random.Next(questions.Count)]);
        }

        var pool = questions
            .OrderBy(_ => random.Next())
            .Take(TargetPoolSize)
            .ToList();

        memoryCache.Set(CacheKey, new QuestionPoolSnapshot(pool));
        logger.LogInformation("Question pool refreshed with {Count} questions", pool.Count);
    }

    public bool TryGetRandomQuestion(out GeneratedQuestion question)
    {
        question = null!;
        if (!memoryCache.TryGetValue<QuestionPoolSnapshot>(CacheKey, out var pool) || pool is null || pool.Questions.Count == 0)
        {
            return false;
        }

        question = pool.Questions[Random.Shared.Next(pool.Questions.Count)];
        return true;
    }

    public bool TryGetQuestion(string questionId, out GeneratedQuestion question)
    {
        question = null!;
        if (!memoryCache.TryGetValue<QuestionPoolSnapshot>(CacheKey, out var pool) || pool is null)
        {
            return false;
        }

        question = pool.Questions.FirstOrDefault(q => q.Id == questionId)!;
        return question is not null;
    }
}
