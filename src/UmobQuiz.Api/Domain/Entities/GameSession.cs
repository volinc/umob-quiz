namespace UmobQuiz.Api.Domain.Entities;

public sealed class GameSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Score { get; set; }
    public GameSessionStatus Status { get; set; } = GameSessionStatus.Active;
}

public enum GameSessionStatus
{
    Active,
    Won,
    Lost
}
