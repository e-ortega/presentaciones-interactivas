using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Server.Domain;
using Server.Infrastructure.Data;

namespace Server.Features.Import;

/// <summary>In-process queue of presentations waiting to be imported.</summary>
public class ImportQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid presentationId) => _channel.Writer.TryWrite(presentationId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>
/// Imports one presentation at a time: LibreOffice is heavy, and a large deck must not block
/// the upload request. When this grows, the queue moves to Azure Storage Queues and this
/// worker to a Container Apps Job, without touching <see cref="ISlideRenderer"/>.
/// </summary>
public class ImportWorker(
    ImportQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<ImportOptions> options,
    ILogger<ImportWorker> logger)
    : BackgroundService
{
    /// <summary>How many times an import is attempted when it cannot even run.</summary>
    private const int MaxAttempts = 3;

    private readonly Dictionary<Guid, int> _failedAttempts = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnqueueUnfinishedAsync(stoppingToken);

        await foreach (var presentationId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var importer = scope.ServiceProvider.GetRequiredService<PresentationImporter>();
                await importer.ImportAsync(presentationId, stoppingToken);
                _failedAttempts.Remove(presentationId);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // The importer records its own failures. Getting here means it could not read
                // or save the presentation: typically the database is paused or unreachable.
                await RetryLaterAsync(presentationId, ex, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Waits and puts the import back in the queue, so a database that is waking up does not
    /// leave a presentation stuck in Pending or Processing until the next restart.
    /// </summary>
    private async Task RetryLaterAsync(Guid presentationId, Exception exception, CancellationToken stoppingToken)
    {
        var attempts = _failedAttempts.GetValueOrDefault(presentationId) + 1;
        if (attempts >= MaxAttempts)
        {
            _failedAttempts.Remove(presentationId);
            logger.LogError(
                exception,
                "Import of presentation {PresentationId} could not run after {Attempts} attempts; it will be retried at the next startup",
                presentationId, attempts);
            return;
        }

        _failedAttempts[presentationId] = attempts;
        logger.LogWarning(
            exception,
            "Import of presentation {PresentationId} could not run (attempt {Attempt} of {MaxAttempts}); retrying",
            presentationId, attempts, MaxAttempts);

        // Nothing else can be imported while the database is down, so waiting here costs nothing.
        await Task.Delay(options.Value.RetryDelay, stoppingToken);
        queue.Enqueue(presentationId);
    }

    /// <summary>Imports cut short by a restart are picked up again.</summary>
    private async Task EnqueueUnfinishedAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var unfinished = await db.Presentations
                    .Where(p => p.Status == ImportStatus.Pending || p.Status == ImportStatus.Processing)
                    .OrderBy(p => p.CreatedAt)
                    .Select(p => p.Id)
                    .ToListAsync(stoppingToken);

                foreach (var presentationId in unfinished)
                {
                    queue.Enqueue(presentationId);
                }

                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(
                    ex, "Could not look for unfinished imports at startup (attempt {Attempt} of {MaxAttempts})", attempt, MaxAttempts);
                if (attempt < MaxAttempts)
                {
                    await Task.Delay(options.Value.RetryDelay, stoppingToken);
                }
            }
        }
    }
}
