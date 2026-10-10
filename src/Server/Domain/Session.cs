namespace Server.Domain;

public enum SessionStatus
{
    Lobby,
    Live,
    Ended,
}

/// <summary>One live use of a presentation. Reusing a deck is another session (D-20).</summary>
public class Session
{
    private Session() { } // EF Core

    public Session(Guid presentationId, string code, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        PresentationId = presentationId;
        Code = code;
        Status = SessionStatus.Lobby;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid PresentationId { get; private set; }

    /// <summary>8 random digits, unique among sessions that have not ended.</summary>
    public string Code { get; private set; } = "";

    public SessionStatus Status { get; private set; }

    /// <summary>Last position shown, so a later session can offer to resume there (D-21).</summary>
    public int CurrentSlideIndex { get; private set; }
    public int CurrentStepIndex { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
}
