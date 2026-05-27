using UmobQuiz.Api.Application.Questions;

namespace UmobQuiz.Api.Application.Game;

public sealed class ActiveGameSessionState
{
    public required Guid SessionId { get; init; }
    public required Guid UserId { get; init; }
    public required DateTime StartTime { get; init; }
    public int Score { get; set; }
    public GeneratedQuestion? PendingQuestion { get; set; }
    public HashSet<string> ServedQuestionIds { get; } = [];
}
