using Server.Domain;

namespace Server.Tests.Domain;

public class SessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

    private static Session NewSession() => new(Guid.NewGuid(), "12345678", Now);

    private static Session LiveSession()
    {
        var session = NewSession();
        session.Start(Now.AddMinutes(5));
        return session;
    }

    [Fact]
    public void A_new_session_waits_in_the_lobby_on_the_first_slide()
    {
        var session = NewSession();

        Assert.Equal(SessionStatus.Lobby, session.Status);
        Assert.Equal((0, 0), (session.CurrentSlideIndex, session.CurrentStepIndex));
        Assert.True(session.CanBeJoined);
    }

    [Fact]
    public void Starting_makes_it_live_and_records_when()
    {
        var session = LiveSession();

        Assert.Equal(SessionStatus.Live, session.Status);
        Assert.Equal(Now.AddMinutes(5), session.StartedAt);
        Assert.True(session.CanBeJoined); // late arrivals are welcome
    }

    [Fact]
    public void It_starts_only_once()
    {
        var session = LiveSession();

        Assert.Throws<InvalidOperationException>(() => session.Start(Now));
    }

    [Fact]
    public void A_live_session_moves_by_slide_and_step_and_remembers_where_it_is()
    {
        var session = LiveSession();

        session.GoTo(3, 2);

        Assert.Equal((3, 2), (session.CurrentSlideIndex, session.CurrentStepIndex));
    }

    [Fact]
    public void It_does_not_move_in_the_lobby_or_after_it_ended()
    {
        var lobby = NewSession();
        var ended = LiveSession();
        ended.End(Now.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => lobby.GoTo(1, 0));
        Assert.Throws<InvalidOperationException>(() => ended.GoTo(1, 0));
    }

    [Fact]
    public void A_position_is_never_negative()
    {
        var session = LiveSession();

        Assert.Throws<ArgumentOutOfRangeException>(() => session.GoTo(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.GoTo(0, -1));
    }

    [Fact]
    public void Ending_closes_the_door_keeps_the_last_position_and_happens_once()
    {
        var session = LiveSession();
        session.GoTo(4, 0);

        session.End(Now.AddHours(1));

        Assert.Equal(SessionStatus.Ended, session.Status);
        Assert.Equal(Now.AddHours(1), session.EndedAt);
        Assert.False(session.CanBeJoined);
        Assert.Equal(4, session.CurrentSlideIndex); // where the next session can offer to resume (D-21)
        Assert.Throws<InvalidOperationException>(() => session.End(Now));
    }

    [Fact]
    public void A_session_can_be_ended_from_the_lobby_without_ever_starting()
    {
        var session = NewSession();

        session.End(Now);

        Assert.Equal(SessionStatus.Ended, session.Status);
        Assert.Null(session.StartedAt);
    }
}

public class ParticipantTests
{
    [Theory]
    [InlineData("ana@example.com", "ana@example.com")]
    [InlineData("  Ana.Mora@Example.COM ", "ana.mora@example.com")]
    [InlineData("ana+clase@ulatina.ac.cr", "ana+clase@ulatina.ac.cr")]
    public void An_email_identifies_a_participant_trimmed_and_in_lower_case(string typed, string expected)
    {
        Assert.Equal(expected, Participant.NormalizeEmail(typed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    [InlineData("ana@localhost")] // no dot in the domain
    [InlineData("ana mora@example.com")]
    [InlineData("Ana <ana@example.com>")] // a display name is not an address
    [InlineData("ana@example.com, luis@example.com")]
    public void Text_that_is_not_one_email_address_is_rejected(string? typed)
    {
        Assert.Null(Participant.NormalizeEmail(typed));
    }

    [Fact]
    public void An_email_longer_than_the_limit_is_rejected()
    {
        var tooLong = new string('a', Participant.MaxEmailLength) + "@example.com";

        Assert.Null(Participant.NormalizeEmail(tooLong));
    }

    [Fact]
    public void Whoever_joins_is_in_until_the_teacher_removes_them()
    {
        var consentedAt = new DateTimeOffset(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);
        var attendance = new Attendance(Guid.NewGuid(), Guid.NewGuid(), "Ana", consentedAt, "1.0");

        Assert.True(attendance.Admitted);
        Assert.Equal((consentedAt, "1.0"), (attendance.ConsentedAt, attendance.NoticeVersion));

        attendance.Remove();

        Assert.False(attendance.Admitted);
    }
}
