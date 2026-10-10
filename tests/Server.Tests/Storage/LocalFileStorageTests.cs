using System.Text;
using Server.Infrastructure.Storage;

namespace Server.Tests.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pi-storage-" + Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _storage = new LocalFileStorage(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task Saves_and_reads_back_a_file_in_a_nested_path()
    {
        await _storage.SaveAsync("presentations/1/notes.txt", Content("hello"), "text/plain", TestContext.Current.CancellationToken);

        await using var stream = await _storage.OpenReadAsync("presentations/1/notes.txt", TestContext.Current.CancellationToken);

        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        Assert.Equal("hello", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Returns_null_for_a_missing_file()
    {
        Assert.Null(await _storage.OpenReadAsync("missing.txt", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deletes_a_file()
    {
        await _storage.SaveAsync("a.txt", Content("x"), "text/plain", TestContext.Current.CancellationToken);

        await _storage.DeleteAsync("a.txt", TestContext.Current.CancellationToken);

        Assert.Null(await _storage.OpenReadAsync("a.txt", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("a/../../outside.txt")]
    [InlineData("")]
    public async Task Rejects_paths_outside_the_root(string path)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.SaveAsync(path, Content("x"), "text/plain", TestContext.Current.CancellationToken));
    }
}
