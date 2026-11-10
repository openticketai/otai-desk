using OtaiDesk.Data;
using OtaiDesk.Modules.Tickets.Domain;
using Microsoft.EntityFrameworkCore;

namespace OtaiDesk.Modules.Tickets.Infrastructure;

public static class TicketModel
{
    public static void Build(ModelBuilder model)
    {
        model.Entity<Ticket>(ticket =>
        {
            ticket.ToTable("Tickets");
            ticket.Property(row => row.Id).ValueGeneratedNever();
            ticket.HasIndex(row => new { row.OrganizationId, row.TicketNumber }).IsUnique();
            ticket.HasMany(row => row.Values).WithOne().HasForeignKey(row => row.TicketId);
            ticket.HasMany(row => row.Articles).WithOne().HasForeignKey(row => row.TicketId);
            ticket.Navigation(row => row.Values).UsePropertyAccessMode(PropertyAccessMode.Field);
            ticket.Navigation(row => row.Articles).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        model.Entity<TicketAttributeValue>(value =>
        {
            value.ToTable("TicketAttributeValues");
            value.Property(row => row.Id).ValueGeneratedNever();
            value.HasOne<AttributeDefinitionRow>().WithMany().HasForeignKey(row => row.DefinitionId);
        });
        model.Entity<Article>(article =>
        {
            article.ToTable("Articles");
            article.Property(row => row.Id).ValueGeneratedNever();
        });
    }
}
