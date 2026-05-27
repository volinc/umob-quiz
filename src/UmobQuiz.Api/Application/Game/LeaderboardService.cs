using Microsoft.EntityFrameworkCore;
using UmobQuiz.Api.Domain.Entities;
using UmobQuiz.Api.Infrastructure.Persistence;
using UmobQuiz.Shared;

namespace UmobQuiz.Api.Application.Game;

public sealed class LeaderboardService(AppDbContext dbContext)
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit, 1, MaxLimit);

        var completedSessions = dbContext.GameSessions
            .AsNoTracking()
            .Where(s => s.Status != GameSessionStatus.Active && s.EndTime != null);

        var bestScores = completedSessions
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                BestScore = g.Max(x => x.Score)
            });

        var bestSessionTimes = completedSessions
            .Join(
                bestScores,
                s => new { s.UserId, Score = s.Score },
                b => new { b.UserId, Score = b.BestScore },
                (s, b) => new { b.UserId, b.BestScore, AchievedAt = s.EndTime!.Value })
            .GroupBy(x => new { x.UserId, x.BestScore })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.BestScore,
                AchievedAt = g.Max(x => x.AchievedAt)
            });

        return await bestSessionTimes
            .Join(
                dbContext.Users.AsNoTracking(),
                x => x.UserId,
                u => u.Id,
                (x, u) => new
                {
                    u.Username,
                    x.BestScore,
                    x.AchievedAt
                })
            .OrderByDescending(x => x.BestScore)
            .ThenByDescending(x => x.AchievedAt)
            .ThenBy(x => x.Username)
            .Take(take)
            .Select(x => new LeaderboardEntryDto(x.Username, x.BestScore, x.AchievedAt))
            .ToListAsync(cancellationToken);
    }
}
