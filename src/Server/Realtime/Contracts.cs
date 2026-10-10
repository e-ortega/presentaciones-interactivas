using Server.Domain;

namespace Server.Realtime;

// The contract of the session hub. The same names and shapes are written by hand in
// src/web/src/realtime/contracts.ts; an integration test connects a real client to keep
// the server honest, and the two files must be changed together.

/// <summary>What a client is in a session (D-39: one mode, three windows).</summary>
public enum ClientRole
{
    /// <summary>The teacher's view, with notes and controls.</summary>
    Presenter,

    /// <summary>The window to share or project: no notes, no controls.</summary>
    Display,

    /// <summary>A student.</summary>
    Participant,
}

/// <summary>Messages the server sends to clients.</summary>
public interface ISessionClient
{
    /// <summary>The whole state, sent on connecting and on every reconnection, so nobody is left out of step.</summary>
    Task SessionSnapshot(SessionSnapshot snapshot);

    Task SessionStarted(int slideIndex, int stepIndex);

    Task SlideChanged(int slideIndex, int stepIndex);

    Task SessionEnded();

    /// <summary>Presenter and display only: students do not get the list.</summary>
    Task AttendanceChanged(AttendanceUpdate attendance);

    /// <summary>Sent only to a participant the teacher removed.</summary>
    Task RemovedFromSession();
}

/// <param name="StepCounts">Steps of each slide, in deck order: enough to navigate and to ask for images.</param>
/// <param name="Code">The join code. Null for participants, who are already in.</param>
/// <param name="Attendance">Null for participants.</param>
public record SessionSnapshot(
    Guid SessionId,
    SessionStatus Status,
    string Title,
    int SlideIndex,
    int StepIndex,
    IReadOnlyList<int> StepCounts,
    string? Code,
    AttendanceUpdate? Attendance);

/// <summary>
/// Who is in the session, for the presenter and for the window the class sees: names are
/// visible to the class, as in a video call (D-85). Emails never leave the server.
/// </summary>
/// <param name="ConnectedCount">Participants with at least one open connection.</param>
/// <param name="Attendees">Everyone who joined and was not removed, in the order they joined.</param>
public record AttendanceUpdate(int ConnectedCount, IReadOnlyList<Attendee> Attendees);

public record Attendee(Guid AttendanceId, string DisplayName, bool Connected);

/// <summary>
/// Reasons the hub refuses a connection or a call, sent as the message of a HubException.
/// A refused connection is closed with one of these, so the client knows not to retry.
/// </summary>
public static class HubErrors
{
    /// <summary>These cookies do not give that role in that session, or there is no such session.</summary>
    public const string NotAllowed = "NotAllowed";

    /// <summary>The teacher removed this participant from the session.</summary>
    public const string Removed = "Removed";
    public const string InvalidPosition = "InvalidPosition";
    public const string InvalidState = "InvalidState";
    public const string NotFound = "NotFound";
}
