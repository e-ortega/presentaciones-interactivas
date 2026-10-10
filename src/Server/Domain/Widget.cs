namespace Server.Domain;

public enum WidgetType
{
    MultipleChoice,
}

/// <summary>
/// The editable design of an interaction on a slide. Responses never hang from a widget,
/// only from a <see cref="WidgetLaunch"/>, which keeps a snapshot of the question (D-22).
/// </summary>
public class Widget
{
    private Widget() { } // EF Core

    public Widget(Guid slideId, WidgetType type, string configJson)
    {
        Id = Guid.CreateVersion7();
        SlideId = slideId;
        Type = type;
        ConfigJson = configJson;
    }

    public Guid Id { get; private set; }
    public Guid SlideId { get; private set; }
    public WidgetType Type { get; private set; }
    public string ConfigJson { get; private set; } = "";
}

/// <summary>A widget used in a specific session, with an exact copy of what students saw.</summary>
public class WidgetLaunch
{
    private WidgetLaunch() { } // EF Core

    public WidgetLaunch(Guid sessionId, Guid widgetId, string snapshotJson, DateTimeOffset openedAt)
    {
        Id = Guid.CreateVersion7();
        SessionId = sessionId;
        WidgetId = widgetId;
        SnapshotJson = snapshotJson;
        OpenedAt = openedAt;
    }

    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid WidgetId { get; private set; }
    public string SnapshotJson { get; private set; } = "";
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public bool ResultsRevealed { get; private set; }
}

/// <summary>One answer per participant and launch (unique index on LaunchId + AttendanceId).</summary>
public class Response
{
    private Response() { } // EF Core

    public Response(Guid launchId, Guid attendanceId, string payloadJson, DateTimeOffset submittedAt)
    {
        Id = Guid.CreateVersion7();
        LaunchId = launchId;
        AttendanceId = attendanceId;
        PayloadJson = payloadJson;
        SubmittedAt = submittedAt;
    }

    public Guid Id { get; private set; }
    public Guid LaunchId { get; private set; }
    public Guid AttendanceId { get; private set; }
    public string PayloadJson { get; private set; } = "";
    public DateTimeOffset SubmittedAt { get; private set; }
}
