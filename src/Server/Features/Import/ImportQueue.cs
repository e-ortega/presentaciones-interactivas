using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
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
public class ImportWorker(ImportQueue queue, IServiceScopeFactory scopeFactory, ILogger<ImportWorker> logger)
    : BackgroundService
{
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
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The importer records its own failures; this is the database being unreachable.
                logger.LogError(ex, "Import of presentation {PresentationId} could not run", presentationId);
            }
        }
    }

    /// <summary>Imports cut short by a restart are picked up again.</summary>
    private async Task EnqueueUnfinishedAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unfinished = await db.Presentations
                .Where(p => p.Status == ImportStatus.Pending || p.Status == ImportStatus.Processing)
                .OrderBy(p => p.CreatedAt)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            foreach (var presentationId in unfinished)
            {
                queue.Enqueue(presentationId);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not look for unfinished imports at startup");
        }
    }
}
