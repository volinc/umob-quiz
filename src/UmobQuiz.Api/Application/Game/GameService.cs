using System.Text;
using Microsoft.EntityFrameworkCore;
using UmobQuiz.Api.Application.Questions;
using UmobQuiz.Api.Domain.Entities;
using UmobQuiz.Api.Infrastructure.Persistence;
using UmobQuiz.Shared;

namespace UmobQuiz.Api.Application.Game;

public sealed class GameService(
    AppDbContext dbContext,
    QuestionPoolService questionPoolService,
    GameSessionStore sessionStore)
{
    public const int GameDurationSeconds = 60;
    public const int CorrectAnswerPoints = 50;
    public const int WrongAnswerPoints = -20;

    public async Task<StartGameResponse> StartGameAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("User account no longer exists.");
        }

        var activeSession = await dbContext.GameSessions
            .AnyAsync(s => s.UserId == userId && s.Status == GameSessionStatus.Active, cancellationToken);
        if (activeSession)
        {
            throw new InvalidOperationException("An active game session already exists.");
        }

        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartTime = DateTime.UtcNow,
            Score = 0,
            Status = GameSessionStatus.Active
        };

        dbContext.GameSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        sessionStore.Add(new ActiveGameSessionState
        {
            SessionId = session.Id,
            UserId = userId,
            StartTime = session.StartTime,
            Score = 0
        });

        return new StartGameResponse(session.Id, session.StartTime);
    }

    public Task<QuestionDto> GetQuestionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var state = GetActiveSession(userId, sessionId);
        EnsureWithinTimeLimit(state);

        if (!questionPoolService.TryGetRandomQuestion(out var question))
        {
            throw new InvalidOperationException("Question pool is not ready yet. Please try again in a moment.");
        }

        var attempts = 0;
        while (state.ServedQuestionIds.Contains(question.Id) && attempts < 20)
        {
            if (!questionPoolService.TryGetRandomQuestion(out question))
            {
                break;
            }

            attempts++;
        }

        state.PendingQuestion = question;
        state.ServedQuestionIds.Add(question.Id);

        return Task.FromResult(new QuestionDto(question.Id, question.Text, question.Options));
    }

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(
        Guid userId,
        Guid sessionId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var state = GetActiveSession(userId, sessionId);
        EnsureWithinTimeLimit(state);

        if (state.PendingQuestion is null || state.PendingQuestion.Id != request.QuestionId)
        {
            throw new InvalidOperationException("No matching pending question for this session.");
        }

        var isCorrect = request.SelectedOptionIndex == state.PendingQuestion.CorrectOptionIndex;
        var delta = isCorrect ? CorrectAnswerPoints : WrongAnswerPoints;
        state.Score += delta;
        state.PendingQuestion = null;

        var session = await dbContext.GameSessions.SingleAsync(s => s.Id == sessionId, cancellationToken);
        session.Score = state.Score;

        var gameEnded = false;
        var status = GameSessionStatus.Active;

        if (state.Score < 0)
        {
            status = GameSessionStatus.Lost;
            gameEnded = true;
        }
        else if (DateTime.UtcNow >= state.StartTime.AddSeconds(GameDurationSeconds))
        {
            status = GameSessionStatus.Won;
            gameEnded = true;
        }

        if (gameEnded)
        {
            session.Status = status;
            session.EndTime = DateTime.UtcNow;
            sessionStore.Remove(sessionId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitAnswerResponse(
            isCorrect,
            delta,
            state.Score,
            status.ToString(),
            gameEnded);
    }

    public async Task<IReadOnlyList<GameSessionSummaryDto>> GetHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.GameSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartTime)
            .Take(20)
            .Select(s => new GameSessionSummaryDto(
                s.Id,
                s.StartTime,
                s.EndTime,
                s.Score,
                s.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task StreamHistoryCsvAsync(
        Guid userId,
        HistoryExportOptions options,
        Stream output,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
        await GameHistoryCsvWriter.WriteHeaderAsync(writer, cancellationToken);

        var query = dbContext.GameSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        if (!options.IncludeActive)
        {
            query = query.Where(s => s.Status != GameSessionStatus.Active && s.EndTime != null);
        }

        if (options.FromUtc is not null)
        {
            query = query.Where(s => s.StartTime >= options.FromUtc);
        }

        if (options.ToUtc is not null)
        {
            query = query.Where(s => s.StartTime <= options.ToUtc);
        }

        await foreach (var row in query
            .OrderByDescending(s => s.StartTime)
            .Take(options.Limit)
            .Select(s => new GameHistoryCsvRow(
                s.Id,
                s.StartTime,
                s.EndTime,
                s.Score,
                s.Status.ToString()))
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            await GameHistoryCsvWriter.WriteRowAsync(writer, row, cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    public async Task<SubmitAnswerResponse?> FinishGameAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!sessionStore.TryGet(sessionId, out var state) || state.UserId != userId)
        {
            return null;
        }

        var session = await dbContext.GameSessions.SingleAsync(s => s.Id == sessionId, cancellationToken);
        session.Score = state.Score;
        session.EndTime = DateTime.UtcNow;
        session.Status = state.Score >= 0 ? GameSessionStatus.Won : GameSessionStatus.Lost;
        sessionStore.Remove(sessionId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitAnswerResponse(
            false,
            0,
            state.Score,
            session.Status.ToString(),
            true);
    }

    private ActiveGameSessionState GetActiveSession(Guid userId, Guid sessionId)
    {
        if (!sessionStore.TryGet(sessionId, out var state) || state.UserId != userId)
        {
            throw new InvalidOperationException("Active game session was not found.");
        }

        return state;
    }

    private static void EnsureWithinTimeLimit(ActiveGameSessionState state)
    {
        if (DateTime.UtcNow >= state.StartTime.AddSeconds(GameDurationSeconds))
        {
            throw new InvalidOperationException("The one-minute game window has expired.");
        }
    }
}
