using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UmobQuiz.Api.Configuration;
using UmobQuiz.Api.Domain.Entities;
using UmobQuiz.Api.Infrastructure.Persistence;
using UmobQuiz.Shared;

namespace UmobQuiz.Api.Application.Auth;

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IOptions<JwtOptions> jwtOptions)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Username and password are required.");
        }

        if (await dbContext.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateToken(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Username == request.Username.Trim(), cancellationToken);

        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        return CreateToken(user);
    }

    private AuthResponse CreateToken(User user)
    {
        var options = jwtOptions.Value;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(options.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username);
    }
}
