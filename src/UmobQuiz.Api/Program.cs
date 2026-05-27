using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Scalar.AspNetCore;
using UmobQuiz.Api.Application.Auth;
using UmobQuiz.Api.Application.Game;
using UmobQuiz.Api.Application.Questions;
using UmobQuiz.Api.Configuration;
using UmobQuiz.Api.Domain.Entities;
using UmobQuiz.Api.Endpoints;
using UmobQuiz.Api.Infrastructure.Gbfs;
using UmobQuiz.Api.Infrastructure.Persistence;
using UmobQuiz.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<GbfsProviderOptions[]>(builder.Configuration.GetSection(GbfsProviderOptions.SectionName));

builder.Services.AddMemoryCache();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsql => npgsql.UseNetTopologySuite());
});

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<GbfsIngestionService>();
builder.Services.AddScoped<QuestionPoolService>();
builder.Services.AddSingleton<GameSessionStore>();

builder.Services.AddSingleton<IQuestionGenerator, TotalBikesQuestionGenerator>();
builder.Services.AddSingleton<IQuestionGenerator, ProviderBikeComparisonQuestionGenerator>();
builder.Services.AddSingleton<IQuestionGenerator, TotalStationsQuestionGenerator>();
builder.Services.AddSingleton<IQuestionGenerator, ProviderStationComparisonQuestionGenerator>();

builder.Services.AddHttpClient<GbfsClient>()
    .AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHostedService<GbfsIngestionWorker>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin());
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));
app.MapAuthEndpoints();
app.MapGameEndpoints();

await WaitForDatabaseAsync(app.Services);
app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

static async Task WaitForDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    for (var attempt = 1; attempt <= 30; attempt++)
    {
        try
        {
            if (await db.Database.CanConnectAsync())
            {
                logger.LogInformation("Database connection established");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database not ready (attempt {Attempt}/30)", attempt);
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    throw new InvalidOperationException("Database connection could not be established.");
}
