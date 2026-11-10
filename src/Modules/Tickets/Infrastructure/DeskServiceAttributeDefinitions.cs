using System.Text.Json;
using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Services;
using OtaiDesk.Shared;

namespace OtaiDesk.Modules.Tickets.Infrastructure;

public sealed class DeskServiceAttributeDefinitions(DeskService desk) : ITicketAttributeDefinitions
{
    public async Task<IReadOnlyList<TicketAttributeDefinition>> For(Actor actor)
    {
        var rows = await desk.ListDefinitions(actor);
        return rows
            .Select(row => new TicketAttributeDefinition(
                row.Id,
                row.Name,
                row.Label,
                row.ValueKind,
                row.InputKind,
                row.ViewKind,
                row.GroupName,
                row.Required,
                row.OptionsJson is null ? null : JsonSerializer.Deserialize<string[]>(row.OptionsJson)))
            .ToList();
    }
}
