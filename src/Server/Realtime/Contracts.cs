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

    /// <summary>Presenter and display only. The display gets the count without the names.</summary>
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

/// <param name="ConnectedCount">Participants with at least one open connection.</param>
/// <param name="Attendees">
/// Everyone who joined and was not removed. Presenter only: the consent text says only the
/// teacher sees names, so the shared window gets null here and shows the count.
/// </param>
public record AttendanceUpdate(int ConnectedCount, IReadOnlyList<Attendee>? Attendees);

public record Attendee(Guid AttendanceId, string DisplayName, bool Connected);

/// <summary>Reasons the hub refuses a call, sent as the message of a HubException.</summary>
public static class HubErrors
{
    public const string NotAllowed = "NotAllowed";
    public const string InvalidPosition = "InvalidPosition";
    public const string InvalidState = "InvalidState";
    public const string NotFound = "NotFound";
}
