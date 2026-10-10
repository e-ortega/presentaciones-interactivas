using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Server.Domain;
using Server.Features.Join;
using Server.Features.Presentations;
using Server.Features.Sessions;
using Server.Realtime;
using Server.Tests.Import;

namespace Server.Tests.Sessions;

/// <summary>
/// One browser: it keeps the cookies the app gives it and sends them back, on HTTP requests
/// and on the SignalR connection, like the teacher's or a student's browser would.
/// </summary>
public sealed class TestBrowser(ImportAppFactory factory) : IAsyncDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _http = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    private readonly Dictionary<string, string> _cookies = [];
    private readonly List<HubConnection> _connections = [];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        if (_cookies.Count > 0)
        {
            request.Headers.Add("Cookie", CookieHeader);
        }

        var response = await _http.SendAsync(request, Ct);
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies)
            {
                var pair = setCookie.Split(';')[0].Split('=', 2);
                _cookies[pair[0]] = pair[1];
            }
        }

        return response;
    }

    public Task<HttpResponseMessage> GetAsync(string url) => SendAsync(HttpMethod.Get, url);

    public Task<HttpResponseMessage> PostAsync(string url, object? body = null) =>
        SendAsync(HttpMethod.Post, url, body is null ? null : JsonContent.Create(body));

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;
    }

    /// <summary>The "code" of a problem response, e.g. "EmailInvalid".</summary>
    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("code").GetString();

    public async Task SignInAsTeacherAsync() =>
        (await PostAsync("/api/access", new { key = AppFactory.AccessKey })).EnsureSuccessStatusCode();

    /// <summary>Joins as a student, like filling in the form with the consent box ticked.</summary>
    public async Task<HttpResponseMessage> JoinAsync(string code, string name, string email, bool consent = true) =>
        await PostAsync($"/api/join/{code}", new JoinRequest(name, email, consent));

    /// <summary>Opens the SignalR connection of this browser to a session, with the role it asks for.</summary>
    public async Task<HubProbe> ConnectAsync(Guid sessionId, ClientRole role)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"https://localhost{SessionHub.Path}?sessionId={sessionId}&role={role}", options =>
            {
                // The in-memory test server has no sockets: long polling goes through its handler.
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (_cookies.Count > 0)
                {
                    options.Headers["Cookie"] = CookieHeader;
                }
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        _connections.Add(connection);
        var probe = new HubProbe(connection);
        await connection.StartAsync(Ct);
        await probe.ReadyOrClosedAsync();
        return probe;
    }

    private string CookieHeader => string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}"));

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            await connection.DisposeAsync();
        }

        _http.Dispose();
    }
}

/// <summary>Records what the hub sends to one connection, so a test can wait for a message.</summary>
public sealed class HubProbe
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly Channel<object> _messages = Channel.CreateUnbounded<object>();
    private readonly TaskCompletionSource<string?> _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public HubProbe(HubConnection connection)
    {
        Connection = connection;
        connection.On<SessionSnapshot>(nameof(ISessionClient.SessionSnapshot), snapshot =>
        {
            Record(snapshot);
            _ready.TrySetResult();
        });
        connection.On<int, int>(nameof(ISessionClient.SessionStarted), (slide, step) => Record(new Started(slide, step)));
        connection.On<int, int>(nameof(ISessionClient.SlideChanged), (slide, step) => Record(new SlideChanged(slide, step)));
        connection.On(nameof(ISessionClient.SessionEnded), () => Record(new Ended()));
        connection.On<AttendanceUpdate>(nameof(ISessionClient.AttendanceChanged), Record);
        connection.On(nameof(ISessionClient.RemovedFromSession), () => Record(new Removed()));
        connection.Closed += error =>
        {
            _closed.TrySetResult(error?.Message);
            return Task.CompletedTask;
        };
    }

    public record Started(int SlideIndex, int StepIndex);

    public record SlideChanged(int SlideIndex, int StepIndex);

    public record Ended;

    public record Removed;

    public HubConnection Connection { get; }

    /// <summary>The next message of that type that matches, skipping others. Fails after 10 seconds.</summary>
    public async Task<T> NextAsync<T>(Func<T, bool>? matches = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            while (true)
            {
                if (await _messages.Reader.ReadAsync(timeout.Token) is T message && (matches is null || matches(message)))
                {
                    return message;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"No {typeof(T).Name} message arrived in {Timeout.TotalSeconds:0} seconds.");
        }
    }

    /// <summary>
    /// Completes when the server has finished setting this connection up (its snapshot arrived)
    /// or has refused it. Starting a connection only completes the handshake: the server may
    /// still be adding it to its groups, and a message sent to a group in that instant would
    /// be missed. The snapshot is sent last, so after it nothing is missed.
    /// </summary>
    public Task ReadyOrClosedAsync() =>
        Task.WhenAny(_ready.Task, _closed.Task).WaitAsync(Timeout, TestContext.Current.CancellationToken);

    /// <summary>
    /// Completes when the server closes the connection, as it does to a client it refuses, with
    /// the reason it gave (one of <see cref="HubErrors"/>, inside the close message).
    /// </summary>
    public Task<string?> ClosedAsync() => _closed.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

    /// <summary>Asserts the server refused this connection for that reason.</summary>
    public async Task AssertRefusedAsync(string reason)
    {
        var message = await ClosedAsync();
        Assert.Contains(reason, message ?? "(closed without a reason)", StringComparison.Ordinal);
    }

    private void Record(object message) => _messages.Writer.TryWrite(message);
}

/// <summary>A running app with a three-slide deck imported and a session created for it.</summary>
public sealed class SessionScenario : IAsyncDisposable
{
    private readonly List<TestBrowser> _browsers = [];

    private SessionScenario(ImportAppFactory factory, TestBrowser teacher, SessionInfo session)
    {
        Factory = factory;
        Teacher = teacher;
        Session = session;
    }

    public ImportAppFactory Factory { get; }
    public TestBrowser Teacher { get; }
    public SessionInfo Session { get; }

    public static async Task<SessionScenario> CreateAsync(string connectionString)
    {
        var factory = new ImportAppFactory(connectionString);
        var teacher = new TestBrowser(factory);
        await teacher.SignInAsTeacherAsync();

        var deck = SyntheticDeck.Create(new("One", "First note"), new("Two", "Second note"), new("Three", "Third note"));
        var file = new ByteArrayContent(deck);
        file.Headers.ContentType = new MediaTypeHeaderValue(PptxUpload.ContentType);
        var upload = await teacher.SendAsync(
            HttpMethod.Post, "/api/admin/presentations", new MultipartFormDataContent { { file, "file", "Semana 01.pptx" } });
        var presentation = await TestBrowser.ReadAsync<PresentationSummary>(upload);
        await WaitUntilReadyAsync(teacher, presentation.Id);

        var created = await teacher.PostAsync("/api/admin/sessions", new CreateSessionRequest(presentation.Id));
        var scenario = new SessionScenario(factory, teacher, await TestBrowser.ReadAsync<SessionInfo>(created));
        scenario._browsers.Add(teacher);
        return scenario;
    }

    public TestBrowser NewBrowser()
    {
        var browser = new TestBrowser(Factory);
        _browsers.Add(browser);
        return browser;
    }

    /// <summary>A student who joined and opened the page: joined over HTTP and connected to the hub.</summary>
    public async Task<(TestBrowser Browser, HubProbe Hub)> JoinedStudentAsync(string name, string email)
    {
        var browser = NewBrowser();
        (await browser.JoinAsync(Session.Code, name, email)).EnsureSuccessStatusCode();
        return (browser, await browser.ConnectAsync(Session.Id, ClientRole.Participant));
    }

    private static async Task WaitUntilReadyAsync(TestBrowser teacher, Guid presentationId)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var detail = await TestBrowser.ReadAsync<PresentationDetail>(await teacher.GetAsync($"/api/admin/presentations/{presentationId}"));
            if (detail.Status == ImportStatus.Ready)
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException("The deck was not imported in 10 seconds.");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var browser in _browsers)
        {
            await browser.DisposeAsync();
        }

        await Factory.DisposeAsync();
    }
}
