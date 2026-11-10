using OtaiDesk.Data;
using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Modules.Tickets.Domain;
using Microsoft.EntityFrameworkCore;

namespace OtaiDesk.Modules.Tickets.Infrastructure;

public sealed class TicketStore(OtaiDeskDb db) : ITicketStore
{
    public async Task<IReadOnlyList<Ticket>> Search(Guid organizationId, TicketFilter filter)
    {
        var query = db.Set<Ticket>().Include(row => row.Values).Where(row => row.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(row => row.Status == filter.Status);
        }
        if (!string.IsNullOrWhiteSpace(filter.Queue))
        {
            query = query.Where(row => row.Queue == filter.Queue);
        }
        if (!string.IsNullOrWhiteSpace(filter.Priority))
        {
            query = query.Where(row => row.Priority == filter.Priority);
        }
        var rows = await query.ToListAsync();
        return rows.OrderByDescending(row => row.UpdatedAt).ToList();
    }

    public async Task<Ticket?> Find(Guid organizationId, Guid id)
    {
        return await db.Set<Ticket>()
            .Include(row => row.Values)
            .Include(row => row.Articles)
            .FirstOrDefaultAsync(row => row.Id == id && row.OrganizationId == organizationId);
    }

    public async Task<int> Count(Guid organizationId) => await db.Set<Ticket>().CountAsync(row => row.OrganizationId == organizationId);

    public void Add(Ticket ticket) => db.Set<Ticket>().Add(ticket);

    public async Task Save() => await db.SaveChangesAsync();
}
