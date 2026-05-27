using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UmobQuiz.Api.Application.Auth;
using UmobQuiz.Api.Infrastructure.Persistence;
using UmobQuiz.Shared;

namespace UmobQuiz.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, AuthService authService, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await authService.RegisterAsync(request, ct));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/login", async (LoginRequest request, AuthService authService, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await authService.LoginAsync(request, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapGet("/me", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
            {
                return Results.Unauthorized();
            }

            var username = user.Identity?.Name;
            return username is null ? Results.Unauthorized() : Results.Ok(new { username });
        }).RequireAuthorization();

        return group;
    }
}
