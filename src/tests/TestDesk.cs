using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OtaiDesk.Data;
using OtaiDesk.Modules.Tickets;
using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Modules.Tickets.Infrastructure;
using OtaiDesk.Services;
using OtaiDesk.Shared;

namespace OtaiDesk.Tests;

public sealed class TestDesk : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private TestDesk(SqliteConnection connection, OtaiDeskDb db, DeskService desk, TicketUseCases tickets)
    {
        this.connection = connection;
        Db = db;
        Desk = desk;
        Tickets = tickets;
    }

    public OtaiDeskDb Db { get; }
    public DeskService Desk { get; }
    public TicketUseCases Tickets { get; }

    public static async Task<TestDesk> Start(string providerName)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OtaiDeskDb>().UseSqlite(connection).Options;
        var db = new OtaiDeskDb(options, [new TicketsModule()]);
        await db.Database.EnsureCreatedAsync();
        var provider = new TicketProvider(providerName);
        var desk = new DeskService(db, new PasswordHasher<UserRow>(), TimeProvider.System, provider);
        await desk.EnsureSeed(seedDemo: false, demoPassword: "");
        var tickets = new TicketUseCases(new TicketStore(db), new DeskServiceAttributeDefinitions(desk), provider, TimeProvider.System);
        return new TestDesk(connection, db, desk, tickets);
    }

    public async Task<Actor> ApiActor()
    {
        var user = await Db.Users.Include(row => row.Roles).ThenInclude(row => row.Rights).FirstAsync(row => row.Id == DeskService.ApiActorId);
        var rights = user.Roles.SelectMany(role => role.Rights).Select(right => right.Name).ToHashSet();
        return new Actor(user.Id, user.OrganizationId, user.Email, user.DisplayName, rights, user.DesignId);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await connection.DisposeAsync();
    }
}
