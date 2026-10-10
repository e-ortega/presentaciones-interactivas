namespace Server.Domain;

/// <summary>A student. The email is their identity across sessions (D-16).</summary>
public class Participant
{
    private Participant() { } // EF Core

    public Participant(string email, string name)
    {
        Id = Guid.CreateVersion7();
        Email = email;
        Name = name;
    }

    public Guid Id { get; private set; }

    /// <summary>Stored normalized (trimmed, lower case); unique.</summary>
    public string Email { get; private set; } = "";

    public string Name { get; private set; } = "";
}

/// <summary>A participant's presence in one session, with their Ley 8968 consent (D-29).</summary>
public class Attendance
{
    private Attendance() { } // EF Core

    public Attendance(
        Guid sessionId,
        Guid participantId,
        string displayName,
        DateTimeOffset consentedAt,
        string noticeVersion)
    {
        Id = Guid.CreateVersion7();
        SessionId = sessionId;
        ParticipantId = participantId;
        DisplayName = displayName;
        ConsentedAt = consentedAt;
        NoticeVersion = noticeVersion;
        JoinedAt = consentedAt;
    }

    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public string DisplayName { get; private set; } = "";
    public DateTimeOffset ConsentedAt { get; private set; }

    /// <summary>Version of the privacy notice the participant accepted.</summary>
    public string NoticeVersion { get; private set; } = "";

    public bool Admitted { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
}
