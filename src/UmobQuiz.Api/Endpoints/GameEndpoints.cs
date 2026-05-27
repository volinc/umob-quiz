using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using UmobQuiz.Api.Application.Game;
using UmobQuiz.Shared;

namespace UmobQuiz.Api.Endpoints;

public static class GameEndpoints
{
    public const string HistoryExportRateLimitPolicy = "history-export";

    public static RouteGroupBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/game").WithTags("Game").RequireAuthorization();

        group.MapGet("/history/export", async (
            int? limit,
            bool? includeActive,
            DateTime? from,
            DateTime? to,
            ClaimsPrincipal user,
            GameService gameService,
            CancellationToken ct) =>
        {
            if (from is not null && to is not null && from > to)
            {
                return Results.BadRequest(new { message = "The 'from' date must be before or equal to 'to'." });
            }

            var options = new HistoryExportOptions(
                HistoryExportOptions.ClampLimit(limit),
                includeActive ?? false,
                from,
                to);

            var userId = GetUserId(user);
            var fileName = $"umob-quiz-history-{DateTime.UtcNow:yyyyMMdd}.csv";

            return Results.Stream(
                async stream => await gameService.StreamHistoryCsvAsync(userId, options, stream, ct),
                contentType: "text/csv; charset=utf-8",
                fileDownloadName: fileName);
        })
        .RequireRateLimiting(HistoryExportRateLimitPolicy);

        group.MapPost("/start", async (ClaimsPrincipal user, GameService gameService, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            try
            {
                return Results.Ok(await gameService.StartGameAsync(userId, ct));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        });

        group.MapGet("/{sessionId:guid}/question", async (
            Guid sessionId,
            ClaimsPrincipal user,
            GameService gameService,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await gameService.GetQuestionAsync(GetUserId(user), sessionId, ct));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/{sessionId:guid}/answer", async (
            Guid sessionId,
            SubmitAnswerRequest request,
            ClaimsPrincipal user,
            GameService gameService,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await gameService.SubmitAnswerAsync(GetUserId(user), sessionId, request, ct));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/{sessionId:guid}/finish", async (
            Guid sessionId,
            ClaimsPrincipal user,
            GameService gameService,
            CancellationToken ct) =>
        {
            var result = await gameService.FinishGameAsync(GetUserId(user), sessionId, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/history", async (ClaimsPrincipal user, GameService gameService, CancellationToken ct) =>
            Results.Ok(await gameService.GetHistoryAsync(GetUserId(user), ct)));

        return group;
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
