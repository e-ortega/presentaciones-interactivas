using Server.Features.Sessions;
using Server.Realtime;

namespace Server.Tests.Sessions;

public class SessionCodeTests
{
    [Fact]
    public void A_generated_code_is_eight_digits_and_they_are_not_all_the_same()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => SessionCode.Generate()).ToList();

        Assert.All(codes, code => Assert.Matches("^[0-9]{8}$", code));
        Assert.True(codes.Distinct().Count() > 190, "200 random codes should almost never repeat.");
    }

    [Theory]
    [InlineData("88886666", "88886666")]
    [InlineData("8888-6666", "88886666")]
    [InlineData("8888 6666", "88886666")]
    [InlineData(" 8888-6666 ", "88886666")]
    [InlineData("00001234", "00001234")] // leading zeros are part of the code
    public void A_code_is_read_as_typed_with_or_without_separators(string typed, string expected)
    {
        Assert.Equal(expected, SessionCode.Normalize(typed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("8888666")] // 7 digits
    [InlineData("888866667")] // 9 digits
    [InlineData("8888-666a")]
    [InlineData("８８８８６６６６")] // full-width digits are not ASCII digits
    public void Anything_that_is_not_eight_digits_is_no_code(string? typed)
    {
        Assert.Null(SessionCode.Normalize(typed));
    }
}

public class LiveSessionStoreTests
{
    private static readonly Guid Session = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Luis = Guid.NewGuid();

    [Fact]
    public void A_participant_is_connected_while_they_have_at_least_one_connection()
    {
        var store = new InMemoryLiveSessionStore();
        store.Connect(Session, Ana, "phone");
        store.Connect(Session, Ana, "laptop"); // two tabs or two devices
        store.Connect(Session, Luis, "luis-phone");

        store.Disconnect(Session, Ana, "phone");
        Assert.Equal(new[] { Ana, Luis }.Order(), store.ConnectedAttendances(Session).Order());

        store.Disconnect(Session, Ana, "laptop");
        Assert.Equal([Luis], store.ConnectedAttendances(Session));
    }

    [Fact]
    public void Sessions_do_not_see_each_others_participants()
    {
        var store = new InMemoryLiveSessionStore();
        store.Connect(Session, Ana, "a");

        Assert.Empty(store.ConnectedAttendances(Guid.NewGuid()));
    }

    [Fact]
    public void Removing_a_participant_returns_their_open_connections_and_forgets_them()
    {
        var store = new InMemoryLiveSessionStore();
        store.Connect(Session, Ana, "phone");
        store.Connect(Session, Ana, "laptop");

        Assert.Equal(["laptop", "phone"], store.Remove(Session, Ana).Order());
        Assert.Empty(store.ConnectedAttendances(Session));
        Assert.Empty(store.Remove(Session, Ana));
    }

    [Fact]
    public void Disconnecting_someone_unknown_or_forgetting_a_session_twice_is_harmless()
    {
        var store = new InMemoryLiveSessionStore();

        store.Disconnect(Session, Ana, "never-connected");
        store.Forget(Session);
        store.Connect(Session, Ana, "a");
        store.Forget(Session);
        store.Forget(Session);

        Assert.Empty(store.ConnectedAttendances(Session));
    }
}

public class DisplayTokenStoreTests
{
    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void A_token_opens_its_session_exactly_once()
    {
        var store = new DisplayTokenStore(new ManualTime());
        var session = Guid.NewGuid();
        var token = store.Create(session);

        Assert.Equal(session, store.Redeem(token));
        Assert.Null(store.Redeem(token));
    }

    [Fact]
    public void A_token_expires_after_two_minutes()
    {
        var time = new ManualTime();
        var store = new DisplayTokenStore(time);
        var fresh = store.Create(Guid.NewGuid());
        var stale = store.Create(Guid.NewGuid());

        time.Now += TimeSpan.FromSeconds(119);
        Assert.NotNull(store.Redeem(fresh));

        time.Now += TimeSpan.FromSeconds(2);
        Assert.Null(store.Redeem(stale));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-token")]
    public void An_unknown_token_opens_nothing(string? token)
    {
        var store = new DisplayTokenStore(new ManualTime());
        store.Create(Guid.NewGuid());

        Assert.Null(store.Redeem(token));
    }

    [Fact]
    public void Tokens_are_long_and_never_repeat()
    {
        var store = new DisplayTokenStore(new ManualTime());
        var session = Guid.NewGuid();

        var tokens = Enumerable.Range(0, 50).Select(_ => store.Create(session)).ToList();

        Assert.All(tokens, token => Assert.Equal(64, token.Length)); // 32 random bytes in hex
        Assert.Equal(50, tokens.Distinct().Count());
    }
}
