using OtaiDesk.Domain;
using OtaiDesk.Shared;
using Microsoft.EntityFrameworkCore;

namespace OtaiDesk.Data;

public sealed class OrganizationRow
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public List<UserRow> Users { get; set; } = [];
}

public sealed class RightRow
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public List<RoleRow> Roles { get; set; } = [];
}

public sealed class RoleRow
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public List<RightRow> Rights { get; set; } = [];
    public List<UserRow> Users { get; set; } = [];
}

public sealed class DesignRow
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public bool IsBuiltIn { get; set; }
    public Guid? OwnerUserId { get; set; }
    public required string Bg { get; set; }
    public required string Surface { get; set; }
    public required string Border { get; set; }
    public required string Text { get; set; }
    public required string TextDim { get; set; }
    public required string Accent { get; set; }
    public UserRow? Owner { get; set; }
}

public sealed class UserRow
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? CustomerAccountId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public required string DesignId { get; set; }
    public OrganizationRow? Organization { get; set; }
    public DesignRow? Design { get; set; }
    public List<RoleRow> Roles { get; set; } = [];
}

public sealed class AttributeDefinitionRow
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public required string Label { get; set; }
    public required string Description { get; set; }
    public required string ValueKind { get; set; }
    public required string InputKind { get; set; }
    public required string ViewKind { get; set; }
    public required string GroupName { get; set; }
    public bool Required { get; set; }
    public bool IsSystem { get; set; }
    public string? OptionsJson { get; set; }
}

public sealed class CustomerAccountRow
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public string? Domain { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class FaqRow
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class OtaiDeskDb(DbContextOptions<OtaiDeskDb> options, IEnumerable<IDeskModule> modules) : DbContext(options)
{
    public DbSet<OrganizationRow> Organizations => Set<OrganizationRow>();
    public DbSet<RightRow> Rights => Set<RightRow>();
    public DbSet<RoleRow> Roles => Set<RoleRow>();
    public DbSet<DesignRow> Designs => Set<DesignRow>();
    public DbSet<UserRow> Users => Set<UserRow>();
    public DbSet<AttributeDefinitionRow> AttributeDefinitions => Set<AttributeDefinitionRow>();
    public DbSet<CustomerAccountRow> CustomerAccounts => Set<CustomerAccountRow>();
    public DbSet<FaqRow> Faqs => Set<FaqRow>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<OrganizationRow>().HasIndex(row => row.Slug).IsUnique();
        model.Entity<RightRow>().HasIndex(row => row.Name).IsUnique();
        model.Entity<RoleRow>().HasIndex(row => row.Name).IsUnique();
        model.Entity<RoleRow>().HasMany(row => row.Rights).WithMany(row => row.Roles);
        model.Entity<UserRow>().HasIndex(row => row.Email).IsUnique();
        model.Entity<UserRow>().HasOne(row => row.Design).WithMany().HasForeignKey(row => row.DesignId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<UserRow>().HasMany(row => row.Roles).WithMany(row => row.Users);
        model.Entity<DesignRow>().HasOne(row => row.Owner).WithMany().HasForeignKey(row => row.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AttributeDefinitionRow>().HasIndex(row => new { row.OrganizationId, row.Name }).IsUnique();
        foreach (var module in modules)
        {
            module.BuildModel(model);
        }
    }
}
