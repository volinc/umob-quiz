namespace UmobQuiz.Api.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<GameSession> GameSessions { get; set; } = [];
}
