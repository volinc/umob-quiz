using System.Security.Claims;
using UmobQuiz.Api.Application.Game;
using UmobQuiz.Shared;

namespace UmobQuiz.Api.Endpoints;

public static class GameEndpoints
{
    public static RouteGroupBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/game").WithTags("Game").RequireAuthorization();

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
