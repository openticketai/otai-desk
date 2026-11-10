using OtaiDesk.Domain;
using OtaiDesk.Modules.Tickets.Domain;
using OtaiDesk.Shared;

namespace OtaiDesk.Modules.Tickets.Application;

public sealed record NewTicket(string Title, string Status, TicketFields Fields, IReadOnlyDictionary<string, string> Attributes);

public sealed class TicketUseCases(ITicketStore store, ITicketAttributeDefinitions definitions, TicketProvider provider, TimeProvider clock)
{
    public async Task<IReadOnlyList<TicketRead>> Search(Actor actor, TicketFilter filter)
    {
        provider.RequireDesk();
        var known = await definitions.For(actor);
        var tickets = await store.Search(actor.OrganizationId, filter);
        return tickets.Select(ticket => Read(ticket, known)).ToList();
    }

    public async Task<TicketRead> Get(Actor actor, Guid id)
    {
        provider.RequireDesk();
        return Read(await Load(actor, id), await definitions.For(actor));
    }

    public async Task<IReadOnlyList<TicketAttributeDefinition>> Definitions(Actor actor) => await definitions.For(actor);

    public async Task<TicketRead> Create(Actor actor, NewTicket draft)
    {
        provider.RequireDesk();
        var known = await definitions.For(actor);
        var values = ValuesByDefinition(known, draft.Attributes, requireAll: true);
        var sequence = await store.Count(actor.OrganizationId) + 1;
        var ticket = Ticket.Open(actor.OrganizationId, sequence, draft.Title, draft.Status, draft.Fields, actor.Id, clock.GetUtcNow());
        ticket.SetAttributes(values);
        store.Add(ticket);
        await store.Save();
        return Read(ticket, known);
    }

    public async Task<TicketRead> Update(Actor actor, Guid id, TicketChange change, IReadOnlyDictionary<string, string>? attributes)
    {
        provider.RequireDesk();
        var ticket = await Load(actor, id);
        var known = await definitions.For(actor);
        if (attributes is not null)
        {
            ticket.SetAttributes(ValuesByDefinition(known, attributes, requireAll: false));
        }
        ticket.Change(change, clock.GetUtcNow());
        await store.Save();
        return Read(ticket, known);
    }

    public async Task<Article> Reply(Actor actor, Guid ticketId, ArticleDraft draft)
    {
        provider.RequireDesk();
        var ticket = await Load(actor, ticketId);
        var article = ticket.Reply(draft, actor.Id, actor.Email, clock.GetUtcNow());
        await store.Save();
        return article;
    }

    private async Task<Ticket> Load(Actor actor, Guid id)
    {
        return await store.Find(actor.OrganizationId, id) ?? throw new OtaiDeskRejectedException("Ticket not found");
    }

    private static Dictionary<Guid, string> ValuesByDefinition(IReadOnlyList<TicketAttributeDefinition> known, IReadOnlyDictionary<string, string> attributes, bool requireAll)
    {
        AttributeKinds.CheckWrite(known.Select(definition => definition.Snapshot()), attributes, requireAll);
        var byName = known.ToDictionary(definition => definition.Name);
        return attributes.ToDictionary(pair => byName[pair.Key].Id, pair => pair.Value);
    }

    private static TicketRead Read(Ticket ticket, IReadOnlyList<TicketAttributeDefinition> known)
    {
        var names = known.ToDictionary(definition => definition.Id, definition => definition.Name);
        var attributes = ticket.Values
            .Where(value => names.ContainsKey(value.DefinitionId))
            .OrderBy(value => names[value.DefinitionId], StringComparer.Ordinal)
            .ToDictionary(value => names[value.DefinitionId], value => value.Value);
        return new TicketRead(ticket, attributes);
    }
}
