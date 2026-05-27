namespace UmobQuiz.Shared;

public sealed record StartGameResponse(Guid SessionId, DateTime StartTime);

public sealed record QuestionDto(
    string QuestionId,
    string Text,
    IReadOnlyList<string> Options);

public sealed record SubmitAnswerRequest(string QuestionId, int SelectedOptionIndex);

public sealed record SubmitAnswerResponse(
    bool IsCorrect,
    int ScoreDelta,
    int TotalScore,
    string Status,
    bool GameEnded);

public sealed record GameSessionSummaryDto(
    Guid SessionId,
    DateTime StartTime,
    DateTime? EndTime,
    int Score,
    string Status);
