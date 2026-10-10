namespace Server.Domain;

public enum SessionStatus
{
    /// <summary>Waiting room: the code is shown and students come in.</summary>
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

    /// <summary>Students can come in during the lobby and after the class started, never once it ended.</summary>
    public bool CanBeJoined => Status != SessionStatus.Ended;

    public void Start(DateTimeOffset now)
    {
        if (Status != SessionStatus.Lobby)
        {
            throw new InvalidOperationException($"Only a session in the lobby can start; this one is {Status}.");
        }

        Status = SessionStatus.Live;
        StartedAt = now;
    }

    /// <summary>
    /// Moves to a slide and step (D-26). The caller checks that the position exists in the deck;
    /// the session only knows that it must be live.
    /// </summary>
    public void GoTo(int slideIndex, int stepIndex)
    {
        if (Status != SessionStatus.Live)
        {
            throw new InvalidOperationException($"Only a live session can change slide; this one is {Status}.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(slideIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(stepIndex);
        CurrentSlideIndex = slideIndex;
        CurrentStepIndex = stepIndex;
    }

    public void End(DateTimeOffset now)
    {
        if (Status == SessionStatus.Ended)
        {
            throw new InvalidOperationException("The session has already ended.");
        }

        Status = SessionStatus.Ended;
        EndedAt = now;
    }
}
