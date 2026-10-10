using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Server.Realtime;

/// <summary>
/// What is only true while people are connected: who is in each session right now.
/// Durable facts (status, current slide, attendances) are in the database, read when needed.
///
/// In memory, which is correct with one replica (D-47). With several replicas this is the
/// piece that moves to Redis or to the database, behind this same interface.
/// </summary>
public interface ILiveSessionStore
{
    void Connect(Guid sessionId, Guid attendanceId, string connectionId);

    void Disconnect(Guid sessionId, Guid attendanceId, string connectionId);

    /// <summary>Participants with at least one open connection.</summary>
    IReadOnlySet<Guid> ConnectedAttendances(Guid sessionId);

    /// <summary>Forgets a participant and returns the connections they had open.</summary>
    IReadOnlyList<string> Remove(Guid sessionId, Guid attendanceId);

    /// <summary>Forgets a session that ended.</summary>
    void Forget(Guid sessionId);
}

public class InMemoryLiveSessionStore : ILiveSessionStore
{
    // One lock for everything: a few dozen operations per class do not need more, and it
    // lets a session's entry disappear with its last connection without racing a new one.
    private readonly Lock _gate = new();

    // Per session: attendance -> its open connections (a student may have two tabs).
    // Only sessions with someone connected have an entry, so nothing accumulates.
    private readonly Dictionary<Guid, Dictionary<Guid, HashSet<string>>> _sessions = [];

    public void Connect(Guid sessionId, Guid attendanceId, string connectionId)
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                _sessions[sessionId] = session = [];
            }

            if (!session.TryGetValue(attendanceId, out var connections))
            {
                session[attendanceId] = connections = [];
            }

            connections.Add(connectionId);
        }
    }

    public void Disconnect(Guid sessionId, Guid attendanceId, string connectionId)
    {
        lock (_gate)
        {
            if (_sessions.TryGetValue(sessionId, out var session)
                && session.TryGetValue(attendanceId, out var connections)
                && connections.Remove(connectionId)
                && connections.Count == 0)
            {
                session.Remove(attendanceId);
                ForgetIfEmpty(sessionId, session);
            }
        }
    }

    public IReadOnlySet<Guid> ConnectedAttendances(Guid sessionId)
    {
        lock (_gate)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session.Keys.ToHashSet() : [];
        }
    }

    public IReadOnlyList<string> Remove(Guid sessionId, Guid attendanceId)
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var session) || !session.Remove(attendanceId, out var connections))
            {
                return [];
            }

            ForgetIfEmpty(sessionId, session);
            return [.. connections];
        }
    }

    public void Forget(Guid sessionId)
    {
        lock (_gate)
        {
            _sessions.Remove(sessionId);
        }
    }

    /// <summary>How many sessions have an entry. For tests: it must go back to zero.</summary>
    public int TrackedSessions
    {
        get
        {
            lock (_gate)
            {
                return _sessions.Count;
            }
        }
    }

    private void ForgetIfEmpty(Guid sessionId, Dictionary<Guid, HashSet<string>> session)
    {
        if (session.Count == 0)
        {
            _sessions.Remove(sessionId);
        }
    }
}

/// <summary>
/// One-time tokens that let a browser open the display window of a session without the
/// access key: the presenter creates one and the display window trades it for its cookie.
/// So someone who only knows the join code cannot open the teacher's shared window.
/// </summary>
public class DisplayTokenStore(TimeProvider time)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    private readonly ConcurrentDictionary<string, (Guid SessionId, DateTimeOffset ExpiresAt)> _tokens = new();

    public string Create(Guid sessionId)
    {
        var now = time.GetUtcNow();
        foreach (var expired in _tokens.Where(token => token.Value.ExpiresAt <= now).ToList())
        {
            _tokens.TryRemove(expired.Key, out _);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _tokens[token] = (sessionId, now + Lifetime);
        return token;
    }

    /// <summary>The session the token opens, or null if it is unknown, expired or already used.</summary>
    public Guid? Redeem(string? token)
    {
        if (token is not null
            && _tokens.TryRemove(token, out var entry)
            && entry.ExpiresAt > time.GetUtcNow())
        {
            return entry.SessionId;
        }

        return null;
    }
}
