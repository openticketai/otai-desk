using OtaiDesk.Domain;
using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Modules.Tickets.Domain;
using Xunit;

namespace OtaiDesk.Tests;

public sealed class TicketUseCasesTests
{
    private static readonly TicketFields VpnFields = new("Network", "high", "incident", "vpn");

    [Fact]
    public async Task A_ticket_keeps_replies_field_changes_and_attributes()
    {
        await using var app = await TestDesk.Start("desk");
        var actor = await app.ApiActor();
        var created = await app.Tickets.Create(actor, new NewTicket("VPN drops", "open", VpnFields, new Dictionary<string, string> { ["severity"] = "sev2" }));
        var id = created.Ticket.Id;

        await app.Tickets.Reply(actor, id, new ArticleDraft("Restarted the tunnel", "Re: VPN drops", "ada@example.com", "text/plain", "agent", false));
        var cleared = new TicketChange(null, "pending", new TicketFields("", null, null, null));
        await app.Tickets.Update(actor, id, cleared, new Dictionary<string, string> { ["severity"] = "sev1" });
        app.Db.ChangeTracker.Clear();

        var read = await app.Tickets.Get(actor, id);
        Assert.Equal("pending", read.Ticket.Status);
        Assert.Null(read.Ticket.Queue);
        Assert.Equal("high", read.Ticket.Priority);
        Assert.Equal("sev1", read.Attributes["severity"]);
        Assert.Equal("Restarted the tunnel", Assert.Single(read.Ticket.Articles).Body);
    }

    [Fact]
    public async Task An_unknown_attribute_is_rejected()
    {
        await using var app = await TestDesk.Start("desk");
        var actor = await app.ApiActor();
        var draft = new NewTicket("VPN drops", "open", VpnFields, new Dictionary<string, string> { ["colour"] = "red" });
        await Assert.ThrowsAsync<OtaiDeskRejectedException>(() => app.Tickets.Create(actor, draft));
    }

    [Fact]
    public void A_ticket_needs_a_title()
    {
        var error = Assert.Throws<OtaiDeskRejectedException>(() => Ticket.Open(Guid.NewGuid(), 1, "  ", "open", VpnFields, Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Contains("title", error.Message);
    }
}
