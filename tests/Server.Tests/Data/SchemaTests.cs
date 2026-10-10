using Microsoft.EntityFrameworkCore;
using Server.Domain;
using Server.Infrastructure.Data;

namespace Server.Tests.Data;

public class MigrationsTests
{
    [Fact]
    public void Migrations_are_up_to_date_with_the_model()
    {
        // Does not connect: it only compares the model with the last migration snapshot.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options;
        using var context = new AppDbContext(options);

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "The model changed. Add a migration with 'dotnet ef migrations add <Name> --project src/Server'.");
    }
}

public class SchemaTests(SqlServerFixture sql)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Already imported. A pending one would be picked up by the import worker of any test
    /// that starts the app against this same database.
    /// </summary>
    private static Presentation ReadyPresentation()
    {
        var presentation = new Presentation("Deck", Now);
        presentation.StartProcessing();
        presentation.AddSlide("notes").AddStep("slide.png");
        presentation.CompleteImport();
        return presentation;
    }

    [Fact]
    public async Task Participant_email_is_unique()
    {
        await using var context = await sql.CreateContextAsync();
        context.Participants.Add(new Participant("ana@example.com", "Ana"));
        context.Participants.Add(new Participant("ana@example.com", "Another Ana"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Session_code_is_unique_among_sessions_that_have_not_ended()
    {
        await using var context = await sql.CreateContextAsync();
        var presentation = ReadyPresentation();
        context.Presentations.Add(presentation);
        context.Sessions.Add(new Session(presentation.Id, "12345678", Now));
        await context.SaveChangesAsync(Ct);

        context.Sessions.Add(new Session(presentation.Id, "12345678", Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task A_participant_answers_a_launch_only_once()
    {
        await using var context = await sql.CreateContextAsync();
        var presentation = ReadyPresentation();
        var slide = presentation.Slides[0];
        var widget = new Widget(slide.Id, WidgetType.MultipleChoice, "{}");
        var session = new Session(presentation.Id, "87654321", Now);
        var participant = new Participant("luis@example.com", "Luis");
        var attendance = new Attendance(session.Id, participant.Id, "Luis", Now, "2026-10");
        var launch = new WidgetLaunch(session.Id, widget.Id, "{}", Now);
        context.AddRange(presentation, widget, session, participant, attendance, launch);
        context.Responses.Add(new Response(launch.Id, attendance.Id, "{}", Now));
        await context.SaveChangesAsync(Ct);

        context.Responses.Add(new Response(launch.Id, attendance.Id, "{}", Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }
}
