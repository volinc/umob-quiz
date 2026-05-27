using UmobQuiz.Api.Application.Game;

namespace UmobQuiz.Api.Endpoints;

public static class LeaderboardEndpoints
{
    public static RouteGroupBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leaderboard").WithTags("Leaderboard").RequireAuthorization();

        group.MapGet("/", async (
            int? limit,
            LeaderboardService leaderboardService,
            CancellationToken ct) =>
        {
            var entries = await leaderboardService.GetLeaderboardAsync(
                limit ?? LeaderboardService.DefaultLimit,
                ct);
            return Results.Ok(entries);
        });

        return group;
    }
}
