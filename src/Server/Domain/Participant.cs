using System.Net.Mail;

namespace Server.Domain;

/// <summary>A student. The email is their identity across sessions (D-16).</summary>
public class Participant
{
    public const int MaxEmailLength = 254;
    public const int MaxNameLength = 100;

    private Participant() { } // EF Core

    /// <param name="email">Already normalized with <see cref="NormalizeEmail"/>.</param>
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

    /// <summary>
    /// The form in which an email identifies a participant: trimmed and in lower case.
    /// Returns null when the text is not an email address.
    /// </summary>
    public static string? NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim() ?? "";
        if (trimmed.Length is 0 or > MaxEmailLength
            || !MailAddress.TryCreate(trimmed, out var address)
            || address.Address != trimmed // rejects "Ana <ana@example.com>"
            || !address.Host.Contains('.', StringComparison.Ordinal))
        {
            return null;
        }

        return trimmed.ToLowerInvariant();
    }
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
        Admitted = true;
    }

    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid ParticipantId { get; private set; }
    /// <summary>The name given on first joining this session. It does not change afterwards (D-84).</summary>
    public string DisplayName { get; private set; } = "";
    public DateTimeOffset ConsentedAt { get; private set; }

    /// <summary>Version of the privacy notice the participant accepted.</summary>
    public string NoticeVersion { get; private set; } = "";

    /// <summary>
    /// Everyone who joins is in, until the teacher removes them. Someone removed stays out:
    /// joining again with the same email does not let them back in.
    /// </summary>
    public bool Admitted { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>The teacher takes this participant out of the session.</summary>
    public void Remove() => Admitted = false;
}
