using System.Collections.Concurrent;

namespace UmobQuiz.Api.Application.Game;

public sealed class GameSessionStore
{
    private readonly ConcurrentDictionary<Guid, ActiveGameSessionState> _sessions = new();

    public void Add(ActiveGameSessionState session) => _sessions[session.SessionId] = session;

    public bool TryGet(Guid sessionId, out ActiveGameSessionState session) =>
        _sessions.TryGetValue(sessionId, out session!);

    public void Remove(Guid sessionId) => _sessions.TryRemove(sessionId, out _);
}
