using System.Net;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Server.Domain;
using Server.Features.Join;
using Server.Features.Sessions;
using Server.Realtime;
using Server.Tests.Data;
using Server.Tests.Import;

namespace Server.Tests.Sessions;

/// <summary>
/// A live session from end to end: real HTTP requests and real SignalR clients against the
/// app, with a real database. This is the test that keeps the hub contract honest.
/// </summary>
[Collection(ImportPipelineCollection.Name)]
public sealed class LiveSessionTests(SqlServerFixture sql) : IAsyncDisposable
{
    private SessionScenario? _scenario;

    public async ValueTask DisposeAsync()
    {
        if (_scenario is not null)
        {
            await _scenario.DisposeAsync();
        }
    }

    private async Task<SessionScenario> StartScenarioAsync() =>
        _scenario = await SessionScenario.CreateAsync(await sql.GetConnectionStringAsync());

    [Fact]
    public async Task The_presenter_connects_to_a_new_session_in_the_lobby()
    {
        var scenario = await StartScenarioAsync();

        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var snapshot = await presenter.NextAsync<SessionSnapshot>();

        Assert.Equal(SessionStatus.Lobby, snapshot.Status);
        Assert.Equal("Semana 01", snapshot.Title);
        Assert.Equal((0, 0), (snapshot.SlideIndex, snapshot.StepIndex));
        Assert.Equal([1, 1, 1], snapshot.StepCounts);
        Assert.Equal(scenario.Session.Code, snapshot.Code);
        Assert.Matches("^[0-9]{8}$", snapshot.Code!);
        Assert.Equal(new AttendanceUpdate(0, []), snapshot.Attendance, AttendanceComparer.Instance);
    }

    [Fact]
    public async Task A_student_who_joins_shows_up_for_the_presenter_by_name_and_for_the_display_as_a_count()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var display = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Display);

        var (_, student) = await scenario.JoinedStudentAsync("Ana Mora", "ana@example.com");

        var forPresenter = await presenter.NextAsync<AttendanceUpdate>(update => update.ConnectedCount == 1);
        var attendee = Assert.Single(forPresenter.Attendees!);
        Assert.Equal(("Ana Mora", true), (attendee.DisplayName, attendee.Connected));

        // The consent text promises only the teacher sees names: the shared window gets a number.
        var forDisplay = await display.NextAsync<AttendanceUpdate>(update => update.ConnectedCount == 1);
        Assert.Null(forDisplay.Attendees);

        var studentSnapshot = await student.NextAsync<SessionSnapshot>();
        Assert.Equal(SessionStatus.Lobby, studentSnapshot.Status);
        Assert.Null(studentSnapshot.Code);
        Assert.Null(studentSnapshot.Attendance);
    }

    [Fact]
    public async Task Everyone_follows_the_presenter_from_the_start_to_the_end_of_the_class()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var display = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Display);
        var (_, student) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");

        await presenter.Connection.InvokeAsync(nameof(SessionHub.StartSession), TestContext.Current.CancellationToken);
        Assert.Equal(new HubProbe.Started(0, 0), await student.NextAsync<HubProbe.Started>());
        Assert.Equal(new HubProbe.Started(0, 0), await display.NextAsync<HubProbe.Started>());

        await presenter.Connection.InvokeAsync(nameof(SessionHub.GoTo), 2, 0, TestContext.Current.CancellationToken);
        Assert.Equal(new HubProbe.SlideChanged(2, 0), await student.NextAsync<HubProbe.SlideChanged>());
        Assert.Equal(new HubProbe.SlideChanged(2, 0), await display.NextAsync<HubProbe.SlideChanged>());
        Assert.Equal(new HubProbe.SlideChanged(2, 0), await presenter.NextAsync<HubProbe.SlideChanged>());

        await presenter.Connection.InvokeAsync(nameof(SessionHub.EndSession), TestContext.Current.CancellationToken);
        await student.NextAsync<HubProbe.Ended>();
        await display.NextAsync<HubProbe.Ended>();
    }

    [Fact]
    public async Task A_student_who_reconnects_or_arrives_late_gets_the_slide_the_class_is_on()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var (browser, firstConnection) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");
        await presenter.Connection.InvokeAsync(nameof(SessionHub.StartSession), TestContext.Current.CancellationToken);
        await presenter.Connection.InvokeAsync(nameof(SessionHub.GoTo), 1, 0, TestContext.Current.CancellationToken);
        await firstConnection.NextAsync<HubProbe.SlideChanged>();

        // The phone lost its connection and came back: same browser, same cookie, new connection.
        await firstConnection.Connection.StopAsync(TestContext.Current.CancellationToken);
        var secondConnection = await browser.ConnectAsync(scenario.Session.Id, ClientRole.Participant);
        var (_, lateStudent) = await scenario.JoinedStudentAsync("Luis", "luis@example.com");

        foreach (var hub in new[] { secondConnection, lateStudent })
        {
            var snapshot = await hub.NextAsync<SessionSnapshot>();
            Assert.Equal(SessionStatus.Live, snapshot.Status);
            Assert.Equal((1, 0), (snapshot.SlideIndex, snapshot.StepIndex));
        }
    }

    [Fact]
    public async Task The_presenter_sees_who_is_connected_and_who_dropped()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var (_, ana) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");
        await scenario.JoinedStudentAsync("Luis", "luis@example.com");
        await presenter.NextAsync<AttendanceUpdate>(update => update.ConnectedCount == 2);

        await ana.Connection.StopAsync(TestContext.Current.CancellationToken);

        var update = await presenter.NextAsync<AttendanceUpdate>(u => u.ConnectedCount == 1);
        Assert.Equal(
            [("Ana", false), ("Luis", true)],
            update.Attendees!.Select(attendee => (attendee.DisplayName, attendee.Connected)));
    }

    [Fact]
    public async Task Slides_only_move_while_the_class_is_live_and_only_to_slides_that_exist()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);

        await AssertRefusedAsync(HubErrors.InvalidState, presenter, nameof(SessionHub.GoTo), 1, 0); // still in the lobby

        await presenter.Connection.InvokeAsync(nameof(SessionHub.StartSession), TestContext.Current.CancellationToken);
        await AssertRefusedAsync(HubErrors.InvalidPosition, presenter, nameof(SessionHub.GoTo), 3, 0); // the deck has 3 slides
        await AssertRefusedAsync(HubErrors.InvalidPosition, presenter, nameof(SessionHub.GoTo), 0, 1); // one step per slide
        await AssertRefusedAsync(HubErrors.InvalidPosition, presenter, nameof(SessionHub.GoTo), -1, 0);
        await AssertRefusedAsync(HubErrors.InvalidState, presenter, nameof(SessionHub.StartSession)); // already started

        await presenter.Connection.InvokeAsync(nameof(SessionHub.EndSession), TestContext.Current.CancellationToken);
        await AssertRefusedAsync(HubErrors.InvalidState, presenter, nameof(SessionHub.GoTo), 1, 0);
        await AssertRefusedAsync(HubErrors.InvalidState, presenter, nameof(SessionHub.EndSession));
    }

    [Fact]
    public async Task Only_the_presenter_controls_the_session()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var display = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Display);
        var (_, student) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");
        var attendee = (await presenter.NextAsync<AttendanceUpdate>(update => update.ConnectedCount == 1)).Attendees![0];

        foreach (var hub in new[] { student, display })
        {
            await AssertRefusedAsync(HubErrors.NotAllowed, hub, nameof(SessionHub.StartSession));
            await AssertRefusedAsync(HubErrors.NotAllowed, hub, nameof(SessionHub.GoTo), 1, 0);
            await AssertRefusedAsync(HubErrors.NotAllowed, hub, nameof(SessionHub.EndSession));
            await AssertRefusedAsync(HubErrors.NotAllowed, hub, nameof(SessionHub.RemoveParticipant), attendee.AttendanceId);
        }
    }

    [Fact]
    public async Task A_browser_cannot_take_a_role_its_cookies_do_not_give_it()
    {
        var scenario = await StartScenarioAsync();
        var (studentBrowser, _) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");

        // A student asking to be the presenter or the display: the server closes the connection
        // and says why, so the page does not keep retrying.
        await (await studentBrowser.ConnectAsync(scenario.Session.Id, ClientRole.Presenter)).AssertRefusedAsync(HubErrors.NotAllowed);
        await (await studentBrowser.ConnectAsync(scenario.Session.Id, ClientRole.Display)).AssertRefusedAsync(HubErrors.NotAllowed);

        // The teacher has not joined as a student, and a session that does not exist has no one.
        await (await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Participant)).AssertRefusedAsync(HubErrors.NotAllowed);
        await (await scenario.Teacher.ConnectAsync(Guid.NewGuid(), ClientRole.Presenter)).AssertRefusedAsync(HubErrors.NotAllowed);

        // No cookie at all does not even get to connect.
        await Assert.ThrowsAsync<HttpRequestException>(
            () => scenario.NewBrowser().ConnectAsync(scenario.Session.Id, ClientRole.Participant));
    }

    [Fact]
    public async Task A_removed_student_is_told_stops_following_and_cannot_come_back()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var (browser, student) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");
        var (_, other) = await scenario.JoinedStudentAsync("Luis", "luis@example.com");
        var attendees = (await presenter.NextAsync<AttendanceUpdate>(update => update.ConnectedCount == 2)).Attendees!;
        await presenter.Connection.InvokeAsync(nameof(SessionHub.StartSession), TestContext.Current.CancellationToken);
        await student.NextAsync<HubProbe.Started>();

        var ana = attendees.Single(attendee => attendee.DisplayName == "Ana");
        await presenter.Connection.InvokeAsync(nameof(SessionHub.RemoveParticipant), ana.AttendanceId, TestContext.Current.CancellationToken);

        await student.NextAsync<HubProbe.Removed>();
        var afterRemoval = await presenter.NextAsync<AttendanceUpdate>(update => update.ConnectedCount == 1);
        Assert.Equal(["Luis"], afterRemoval.Attendees!.Select(attendee => attendee.DisplayName));

        // Even if the removed browser keeps its connection open, it hears nothing more.
        await presenter.Connection.InvokeAsync(nameof(SessionHub.GoTo), 1, 0, TestContext.Current.CancellationToken);
        await other.NextAsync<HubProbe.SlideChanged>();
        await Assert.ThrowsAsync<TimeoutException>(() => student.NextAsync<HubProbe.SlideChanged>().WaitAsync(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken));

        // And it cannot get back in: not by reconnecting (it is told why), not by joining again,
        // not to the slides.
        await (await browser.ConnectAsync(scenario.Session.Id, ClientRole.Participant)).AssertRefusedAsync(HubErrors.Removed);
        var rejoin = await browser.JoinAsync(scenario.Session.Code, "Ana", "ana@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, rejoin.StatusCode);
        Assert.Equal(nameof(JoinError.Removed), await TestBrowser.ErrorCodeAsync(rejoin));
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync(SlideUrl(scenario, 1))).StatusCode);
    }

    [Fact]
    public async Task The_same_email_is_the_same_person_whatever_the_capitals()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        await scenario.JoinedStudentAsync("Ana", "ana@example.com");

        await scenario.JoinedStudentAsync("Ana", "  ANA@Example.com "); // her phone, after her laptop

        var update = await presenter.NextAsync<AttendanceUpdate>(u => u.Attendees is [{ Connected: true }] && u.ConnectedCount == 1);
        Assert.Single(update.Attendees!);
    }

    [Fact]
    public async Task Nobody_typing_a_classmates_email_can_change_the_name_the_teacher_sees()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        await scenario.JoinedStudentAsync("Ana Mora", "ana@example.com");
        await presenter.NextAsync<AttendanceUpdate>(u => u.ConnectedCount == 1);

        // Emails are not verified per session (D-18), so this browser gets in as Ana. Sending the
        // form again, now holding the cookie of Ana's attendance, must not rename her either.
        var impostor = scenario.NewBrowser();
        (await impostor.JoinAsync(scenario.Session.Code, "Something rude", "ana@example.com")).EnsureSuccessStatusCode();
        (await impostor.JoinAsync(scenario.Session.Code, "Something rude", "ana@example.com")).EnsureSuccessStatusCode();
        await impostor.ConnectAsync(scenario.Session.Id, ClientRole.Participant);

        var update = await presenter.NextAsync<AttendanceUpdate>();
        Assert.Equal(["Ana Mora"], update.Attendees!.Select(attendee => attendee.DisplayName));
    }

    [Theory]
    [InlineData("", "ana@example.com", true, JoinError.NameRequired)]
    [InlineData("   ", "ana@example.com", true, JoinError.NameRequired)]
    [InlineData("Ana", "", true, JoinError.EmailInvalid)]
    [InlineData("Ana", "not an email", true, JoinError.EmailInvalid)]
    [InlineData("Ana", "ana@localhost", true, JoinError.EmailInvalid)]
    [InlineData("Ana", "Ana <ana@example.com>", true, JoinError.EmailInvalid)]
    [InlineData("Ana", "ana@example.com", false, JoinError.ConsentRequired)]
    public async Task Joining_needs_a_name_an_email_and_consent(string name, string email, bool consent, JoinError expected)
    {
        var scenario = await StartScenarioAsync();
        var browser = scenario.NewBrowser();

        var response = await browser.JoinAsync(scenario.Session.Code, name, email, consent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expected.ToString(), await TestBrowser.ErrorCodeAsync(response));
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task A_name_longer_than_the_limit_is_refused()
    {
        var scenario = await StartScenarioAsync();

        var response = await scenario.NewBrowser().JoinAsync(scenario.Session.Code, new string('a', 101), "ana@example.com");

        Assert.Equal(nameof(JoinError.NameTooLong), await TestBrowser.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task The_code_works_as_typed_with_or_without_a_dash_and_stops_working_when_the_class_ends()
    {
        var scenario = await StartScenarioAsync();
        var browser = scenario.NewBrowser();
        var code = scenario.Session.Code;
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);

        foreach (var typed in new[] { code, $"{code[..4]}-{code[4..]}", $"{code[..4]} {code[4..]}" })
        {
            var info = await TestBrowser.ReadAsync<JoinInfo>(await browser.GetAsync($"/api/join/{Uri.EscapeDataString(typed)}"));
            Assert.Equal(new JoinInfo("Semana 01", "1.0"), info);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync("/api/join/00000000")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync("/api/join/abc")).StatusCode);

        await presenter.Connection.InvokeAsync(nameof(SessionHub.StartSession), TestContext.Current.CancellationToken);
        await presenter.Connection.InvokeAsync(nameof(SessionHub.EndSession), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync($"/api/join/{code}")).StatusCode);
        var join = await browser.JoinAsync(code, "Ana", "ana@example.com");
        Assert.Equal(nameof(JoinError.SessionNotFound), await TestBrowser.ErrorCodeAsync(join));
    }

    [Fact]
    public async Task Students_see_the_slides_only_while_the_class_is_live_and_outsiders_never()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var (student, _) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");
        var outsider = scenario.NewBrowser();

        Assert.Equal(HttpStatusCode.OK, (await scenario.Teacher.GetAsync(SlideUrl(scenario, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(SlideUrl(scenario, 1))).StatusCode); // lobby
        Assert.Equal(HttpStatusCode.Unauthorized, (await outsider.GetAsync(SlideUrl(scenario, 1))).StatusCode);

        await presenter.Connection.InvokeAsync(nameof(SessionHub.StartSession), TestContext.Current.CancellationToken);
        await presenter.Connection.InvokeAsync(nameof(SessionHub.GoTo), 1, 0, TestContext.Current.CancellationToken);

        var image = await student.GetAsync(SlideUrl(scenario, 1));
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal(FakeSlideRenderer.ImageBytes(1), await image.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));

        await presenter.Connection.InvokeAsync(nameof(SessionHub.EndSession), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(SlideUrl(scenario, 1))).StatusCode);
    }

    [Fact]
    public async Task A_student_can_only_fetch_the_slide_on_screen_never_the_ones_not_shown_yet()
    {
        var scenario = await StartScenarioAsync();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var (student, _) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");
        await presenter.Connection.InvokeAsync(nameof(SessionHub.StartSession), TestContext.Current.CancellationToken);

        // On slide 0: the next slides may hold the answers to an exercise.
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync(SlideUrl(scenario, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(SlideUrl(scenario, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(SlideUrl(scenario, 2))).StatusCode);

        await presenter.Connection.InvokeAsync(nameof(SessionHub.GoTo), 2, 0, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync(SlideUrl(scenario, 2))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(SlideUrl(scenario, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(SlideUrl(scenario, 9))).StatusCode);

        // The teacher, and the window the teacher shares, are not tied to the current slide.
        Assert.Equal(HttpStatusCode.OK, (await scenario.Teacher.GetAsync(SlideUrl(scenario, 0))).StatusCode);
    }

    [Fact]
    public async Task Once_the_class_ends_its_former_members_only_learn_that_it_ended()
    {
        var scenario = await StartScenarioAsync();
        var projector = scenario.NewBrowser();
        var token = (await TestBrowser.ReadAsync<DisplayToken>(
            await scenario.Teacher.PostAsync($"/api/admin/sessions/{scenario.Session.Id}/display-token"))).Token;
        (await projector.PostAsync("/api/display/access", new RedeemDisplayTokenRequest(token))).EnsureSuccessStatusCode();
        var presenter = await scenario.Teacher.ConnectAsync(scenario.Session.Id, ClientRole.Presenter);
        var (student, studentHub) = await scenario.JoinedStudentAsync("Ana", "ana@example.com");
        await presenter.Connection.InvokeAsync(nameof(SessionHub.StartSession), TestContext.Current.CancellationToken);
        await presenter.Connection.InvokeAsync(nameof(SessionHub.EndSession), TestContext.Current.CancellationToken);
        await studentHub.NextAsync<HubProbe.Ended>();
        await studentHub.Connection.StopAsync(TestContext.Current.CancellationToken);

        // Reloading the page afterwards: both are told the class ended, and nothing else.
        var studentAgain = await (await student.ConnectAsync(scenario.Session.Id, ClientRole.Participant)).NextAsync<SessionSnapshot>();
        var displayAgain = await (await projector.ConnectAsync(scenario.Session.Id, ClientRole.Display)).NextAsync<SessionSnapshot>();
        foreach (var snapshot in new[] { studentAgain, displayAgain })
        {
            Assert.Equal(SessionStatus.Ended, snapshot.Status);
            Assert.Null(snapshot.Code); // free for another session now
            Assert.Null(snapshot.Attendance);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await projector.GetAsync(SlideUrl(scenario, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await scenario.Teacher.GetAsync(SlideUrl(scenario, 0))).StatusCode);

        // And the server keeps nothing in memory for a class that is over.
        var store = Assert.IsType<InMemoryLiveSessionStore>(
            scenario.Factory.Services.GetService(typeof(ILiveSessionStore)));
        Assert.Equal(0, store.TrackedSessions);
    }

    [Fact]
    public async Task The_display_window_opens_with_a_one_time_link_from_the_presenter()
    {
        var scenario = await StartScenarioAsync();
        var projector = scenario.NewBrowser(); // another computer: no teacher cookie
        var tokenResponse = await scenario.Teacher.PostAsync($"/api/admin/sessions/{scenario.Session.Id}/display-token");
        var token = (await TestBrowser.ReadAsync<DisplayToken>(tokenResponse)).Token;

        var access = await TestBrowser.ReadAsync<DisplayAccess>(
            await projector.PostAsync("/api/display/access", new RedeemDisplayTokenRequest(token)));
        Assert.Equal(scenario.Session.Id, access.SessionId);

        var display = await projector.ConnectAsync(scenario.Session.Id, ClientRole.Display);
        var snapshot = await display.NextAsync<SessionSnapshot>();
        Assert.Equal(scenario.Session.Code, snapshot.Code); // the waiting room shows the code
        Assert.Null(snapshot.Attendance!.Attendees);
        Assert.Equal(HttpStatusCode.OK, (await projector.GetAsync(SlideUrl(scenario, 0))).StatusCode);

        // The link works once, and the display cookie is not a presenter's.
        var again = await scenario.NewBrowser().PostAsync("/api/display/access", new RedeemDisplayTokenRequest(token));
        Assert.Equal("InvalidDisplayToken", await TestBrowser.ErrorCodeAsync(again));
        await (await projector.ConnectAsync(scenario.Session.Id, ClientRole.Presenter)).ClosedAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await projector.GetAsync($"/api/admin/sessions/{scenario.Session.Id}")).StatusCode);
    }

    [Fact]
    public async Task The_presenter_view_gets_the_notes_over_http_never_through_the_hub()
    {
        var scenario = await StartScenarioAsync();

        var detail = await TestBrowser.ReadAsync<SessionDetail>(
            await scenario.Teacher.GetAsync($"/api/admin/sessions/{scenario.Session.Id}"));

        Assert.Equal(["First note", "Second note", "Third note"], detail.Slides.Select(slide => slide.SpeakerNotes));
        Assert.Equal(scenario.Session.Code, detail.Code);
        Assert.DoesNotContain(typeof(SessionSnapshot).GetProperties(), property => property.Name.Contains("Note", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_session_needs_a_presentation_that_exists()
    {
        var scenario = await StartScenarioAsync();

        var missing = await scenario.Teacher.PostAsync("/api/admin/sessions", new CreateSessionRequest(Guid.NewGuid()));
        var anonymous = await scenario.NewBrowser().PostAsync("/api/admin/sessions", new CreateSessionRequest(scenario.Session.PresentationId));

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    private static string SlideUrl(SessionScenario scenario, int slideIndex) =>
        $"/api/sessions/{scenario.Session.Id}/slides/{slideIndex}/steps/0";

    private static async Task AssertRefusedAsync(string expectedError, HubProbe hub, string method, params object[] arguments)
    {
        var exception = await Assert.ThrowsAsync<HubException>(
            () => hub.Connection.InvokeCoreAsync(method, arguments, TestContext.Current.CancellationToken));
        Assert.Contains(expectedError, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Records compare lists by reference; this compares what is in them.</summary>
    private sealed class AttendanceComparer : IEqualityComparer<AttendanceUpdate?>
    {
        public static readonly AttendanceComparer Instance = new();

        public bool Equals(AttendanceUpdate? x, AttendanceUpdate? y) =>
            x?.ConnectedCount == y?.ConnectedCount
            && (x?.Attendees ?? []).SequenceEqual(y?.Attendees ?? []);

        public int GetHashCode(AttendanceUpdate? obj) => obj?.ConnectedCount ?? 0;
    }
}
