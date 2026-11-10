using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Modules.Tickets.Domain;
using OtaiDesk.Services;

namespace OtaiDesk.Modules.Tickets.Infrastructure;

public sealed class TicketDemoSeed(DeskService desk, ITicketStore store, TicketUseCases tickets)
{
    public async Task Run()
    {
        var agent = await desk.ActorByEmail(DeskService.DemoAgentEmail)
            ?? throw new InvalidOperationException($"Demo agent {DeskService.DemoAgentEmail} is missing");
        if (await store.Count(agent.OrganizationId) > 0)
        {
            return;
        }
        await tickets.Create(agent, new NewTicket("VPN drops every hour", "open", new TicketFields("Network", "high", "incident", "vpn"), new Dictionary<string, string>()));
    }
}
