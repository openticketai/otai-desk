using OtaiDesk.Domain;
using OtaiDesk.Modules.Tickets.Domain;
using OtaiDesk.Shared;

namespace OtaiDesk.Modules.Tickets.Application;

public sealed record TicketFilter(string? Status, string? Queue, string? Priority);

public sealed record TicketAttributeDefinition(Guid Id, string Name, string Label, string ValueKind, string InputKind, string ViewKind, string GroupName, bool Required, IReadOnlyList<string>? Options)
{
    public AttributeDefinitionSnapshot Snapshot() => new(Name, ValueKind, Required, Options);
}

public sealed record TicketRead(Ticket Ticket, IReadOnlyDictionary<string, string> Attributes);

public interface ITicketStore
{
    Task<IReadOnlyList<Ticket>> Search(Guid organizationId, TicketFilter filter);

    Task<Ticket?> Find(Guid organizationId, Guid id);

    Task<int> Count(Guid organizationId);

    void Add(Ticket ticket);

    Task Save();
}

public interface ITicketAttributeDefinitions
{
    Task<IReadOnlyList<TicketAttributeDefinition>> For(Actor actor);
}
