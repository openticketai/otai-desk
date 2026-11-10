using System.Text.Json;
using OtaiDesk.Data;
using OtaiDesk.Domain;
using OtaiDesk.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace OtaiDesk.Services;

public sealed class DeskService(OtaiDeskDb db, IPasswordHasher<UserRow> hasher, TimeProvider clock, TicketProvider provider)
{
    public const string ApiActorEmail = "api@helpdesk.local";
    public const string DemoAgentEmail = "agent@demo.openticketai.com";
    public static readonly Guid DefaultOrgId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid ApiActorId = Guid.Parse("00000000-0000-0000-0000-000000000110");

    public async Task EnsureSeed(bool seedDemo, string demoPassword)
    {
        await EnsureFieldColumns();
        await EnsureDesign(Designs.LightForrest, "Light forrest", DesignColors.LightForrest, null);
        await EnsureDesign(Designs.DarkForrest, "Dark forrest", DesignColors.DarkForrest, null);
        foreach (var rightName in Rights.All)
        {
            if (!await db.Rights.AnyAsync(row => row.Name == rightName))
            {
                db.Rights.Add(new RightRow { Id = Guid.NewGuid(), Name = rightName });
            }
        }
        await db.SaveChangesAsync();
        foreach (var (roleName, rightNames) in Rights.ByRole)
        {
            var role = await db.Roles.Include(row => row.Rights).FirstOrDefaultAsync(row => row.Name == roleName);
            if (role is null)
            {
                role = new RoleRow { Id = Guid.NewGuid(), Name = roleName };
                db.Roles.Add(role);
            }
            var rights = await db.Rights.Where(row => rightNames.Contains(row.Name)).ToListAsync();
            foreach (var right in rights)
            {
                if (role.Rights.All(existing => existing.Id != right.Id))
                {
                    role.Rights.Add(right);
                }
            }
        }
        if (!await db.Organizations.AnyAsync(row => row.Id == DefaultOrgId))
        {
            db.Organizations.Add(new OrganizationRow { Id = DefaultOrgId, Name = "Default Org", Slug = "default" });
        }
        await db.SaveChangesAsync();
        await EnsureDefinitions(DefaultOrgId);
        if (!await db.Users.AnyAsync(row => row.Id == ApiActorId))
        {
            var admin = await Role("admin");
            db.Users.Add(new UserRow
            {
                Id = ApiActorId,
                OrganizationId = DefaultOrgId,
                Email = ApiActorEmail,
                DisplayName = "API token",
                PasswordHash = hasher.HashPassword(new UserRow { Id = ApiActorId, OrganizationId = DefaultOrgId, Email = ApiActorEmail, DisplayName = "API token", PasswordHash = "x", DesignId = Designs.LightForrest, IsActive = true }, Guid.NewGuid().ToString("n")),
                IsActive = true,
                DesignId = Designs.LightForrest,
                Roles = [admin],
            });
        }
        if (seedDemo)
        {
            await SeedDemo(demoPassword);
        }
        await db.SaveChangesAsync();
    }

    public async Task<Actor?> Authenticate(string email, string password)
    {
        provider.RequireDesk();
        var user = await db.Users.Include(row => row.Roles).ThenInclude(row => row.Rights).FirstOrDefaultAsync(row => row.Email == email && row.IsActive);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            return null;
        }
        return ToActor(user);
    }

    public async Task<Actor?> ActorById(Guid id)
    {
        var user = await db.Users.Include(row => row.Roles).ThenInclude(row => row.Rights).FirstOrDefaultAsync(row => row.Id == id && row.IsActive);
        return user is null ? null : ToActor(user);
    }

    public async Task<Actor?> ActorByEmail(string email)
    {
        var user = await db.Users.Include(row => row.Roles).ThenInclude(row => row.Rights).FirstOrDefaultAsync(row => row.Email == email && row.IsActive);
        return user is null ? null : ToActor(user);
    }

    public async Task<DesignRow> DesignFor(Actor actor)
    {
        var design = await db.Designs.FirstOrDefaultAsync(row => row.Id == actor.DesignId);
        if (design is null)
        {
            throw new OtaiDeskRejectedException($"Unknown design: {actor.DesignId}");
        }
        return design;
    }

    public async Task<IReadOnlyList<DesignRow>> DesignsFor(Actor actor)
    {
        return await db.Designs.Where(row => row.IsBuiltIn || row.OwnerUserId == actor.Id).OrderBy(row => row.Name).ToListAsync();
    }

    public async Task<DesignRow> CreateDesign(Actor actor, string name, DesignColors colors)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0 || Designs.IsBuiltIn(trimmed))
        {
            throw new OtaiDeskRejectedException("Design name is not available.");
        }
        var row = new DesignRow
        {
            Id = "user-" + Guid.NewGuid().ToString("n"),
            Name = trimmed,
            IsBuiltIn = false,
            OwnerUserId = actor.Id,
            Bg = colors.Bg,
            Surface = colors.Surface,
            Border = colors.Border,
            Text = colors.Text,
            TextDim = colors.TextDim,
            Accent = colors.Accent,
        };
        db.Designs.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    public async Task SelectDesign(Actor actor, string designId)
    {
        var design = await db.Designs.FirstOrDefaultAsync(row => row.Id == designId);
        if (design is null || (!design.IsBuiltIn && design.OwnerUserId != actor.Id))
        {
            throw new OtaiDeskRejectedException($"Unknown design: {designId}");
        }
        var user = await db.Users.FirstAsync(row => row.Id == actor.Id);
        user.DesignId = design.Id;
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AttributeDefinitionRow>> ListDefinitions(Actor actor) => await Definitions(actor.OrganizationId);

    public async Task<AttributeDefinitionRow> CreateDefinition(Actor actor, string name, string label, string description, string kind, string inputKind, string viewKind, string groupName, bool required, string? options)
    {
        if (!AttributeKinds.All.Contains(kind) || AttributeKinds.TicketColumns.Contains(name.Trim()))
        {
            throw new OtaiDeskRejectedException($"Unknown attribute kind or reserved name: {name}");
        }
        FieldCatalog.RequirePair(kind, inputKind, viewKind);
        string[]? parsed = kind == "select" ? SplitOptions(options) : null;
        if (kind == "select" && (parsed is null || parsed.Length == 0))
        {
            throw new OtaiDeskRejectedException("select needs options");
        }
        if (kind != "select" && !string.IsNullOrWhiteSpace(options))
        {
            throw new OtaiDeskRejectedException("options are only for select");
        }
        var row = new AttributeDefinitionRow
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Name = Required(name, "name"),
            Label = Required(label, "label"),
            Description = Required(description, "description"),
            ValueKind = kind,
            InputKind = inputKind,
            ViewKind = viewKind,
            GroupName = Required(groupName, "group"),
            Required = required,
            IsSystem = false,
            OptionsJson = parsed is null ? null : JsonSerializer.Serialize(parsed),
        };
        db.AttributeDefinitions.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    public async Task<IReadOnlyList<UserRow>> ListUsers(Actor actor)
    {
        return await db.Users.Include(row => row.Roles).Include(row => row.Design).Where(row => row.OrganizationId == actor.OrganizationId).OrderBy(row => row.Email).ToListAsync();
    }

    public async Task<UserRow> CreateUser(Actor actor, string email, string displayName, string password, string roleName, string designId)
    {
        var design = await db.Designs.FirstOrDefaultAsync(row => row.Id == designId && row.IsBuiltIn);
        if (design is null)
        {
            throw new OtaiDeskRejectedException($"Unknown design: {designId}");
        }
        if (!Rights.ByRole.ContainsKey(roleName))
        {
            throw new OtaiDeskRejectedException($"Unknown role: {roleName}");
        }
        var role = await Role(roleName);
        var user = new UserRow
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Email = Required(email, "email"),
            DisplayName = Required(displayName, "display name"),
            PasswordHash = "pending",
            IsActive = true,
            DesignId = design.Id,
            Roles = [role],
        };
        user.PasswordHash = hasher.HashPassword(user, Required(password, "password"));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<IReadOnlyList<CustomerAccountRow>> ListAccounts(Actor actor)
    {
        return await db.CustomerAccounts.Where(row => row.OrganizationId == actor.OrganizationId).OrderBy(row => row.Name).ToListAsync();
    }

    public async Task<CustomerAccountRow> CreateAccount(Actor actor, string name, string? domain)
    {
        var row = new CustomerAccountRow
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Name = Required(name, "name"),
            Domain = BlankToNull(domain),
            CreatedAt = clock.GetUtcNow(),
        };
        db.CustomerAccounts.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    public async Task<IReadOnlyList<FaqRow>> ListFaqs(Actor actor)
    {
        return await db.Faqs.Where(row => row.OrganizationId == actor.OrganizationId).OrderBy(row => row.Question).ToListAsync();
    }

    public async Task<FaqRow> CreateFaq(Actor actor, string question, string answer)
    {
        var now = clock.GetUtcNow();
        var row = new FaqRow
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Question = Required(question, "question"),
            Answer = Required(answer, "answer"),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Faqs.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    private async Task SeedDemo(string password)
    {
        if (await db.Users.AnyAsync(row => row.Email == "admin@demo.openticketai.com"))
        {
            return;
        }
        var org = await db.Organizations.FirstAsync(row => row.Id == DefaultOrgId);
        org.Name = "Acme Demo GmbH";
        org.Slug = "demo";
        await AddDemoUser("admin@demo.openticketai.com", "Anna Admin", password, "admin", Designs.LightForrest);
        await AddDemoUser(DemoAgentEmail, "Max Agent", password, "agent", Designs.LightForrest);
        await AddDemoUser("employee@demo.openticketai.com", "Erika Employee", password, "employee", Designs.DarkForrest);
        await db.SaveChangesAsync();
    }

    private async Task AddDemoUser(string email, string name, string password, string roleName, string designId)
    {
        var role = await Role(roleName);
        var user = new UserRow
        {
            Id = Guid.NewGuid(),
            OrganizationId = DefaultOrgId,
            Email = email,
            DisplayName = name,
            PasswordHash = "pending",
            IsActive = true,
            DesignId = designId,
            Roles = [role],
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
    }

    private async Task EnsureDesign(string id, string name, DesignColors colors, Guid? ownerId)
    {
        if (await db.Designs.AnyAsync(row => row.Id == id))
        {
            return;
        }
        db.Designs.Add(new DesignRow
        {
            Id = id,
            Name = name,
            IsBuiltIn = ownerId is null,
            OwnerUserId = ownerId,
            Bg = colors.Bg,
            Surface = colors.Surface,
            Border = colors.Border,
            Text = colors.Text,
            TextDim = colors.TextDim,
            Accent = colors.Accent,
        });
    }

    private async Task EnsureDefinitions(Guid orgId)
    {
        var seeds = new (string Name, string Label, string Kind, string Input, string View, string Group, string[]? Options)[]
        {
            ("description", "Description", "string", "textarea", "prose", "Details", null),
            ("source", "Source", "select", "select", "badge", "Classification", ["desk_chat", "email", "api", "manual"]),
            ("severity", "Severity", "select", "select", "badge", "Classification", ["sev4", "sev3", "sev2", "sev1"]),
            ("category", "Category", "string", "text", "text", "Details", null),
            ("impact", "Impact", "number", "number", "number", "Details", null),
            ("customer_visible", "Customer visible", "boolean", "checkbox", "checkbox", "Details", null),
            ("due_on", "Due on", "date", "date", "date", "Times", null),
            ("assignee", "Assignee", "user", "user", "user", "People", null),
            ("requester", "Requester", "user", "user", "user", "People", null),
            ("resolver", "Resolver", "user", "user", "user", "People", null),
            ("opened_at", "Opened at", "datetime", "timestamp", "timestamp", "Times", null),
            ("resolved_at", "Resolved at", "datetime", "timestamp", "timestamp", "Times", null),
            ("closed_at", "Closed at", "datetime", "timestamp", "timestamp", "Times", null),
        };
        var existing = await db.AttributeDefinitions.Where(row => row.OrganizationId == orgId).Select(row => row.Name).ToListAsync();
        foreach (var seed in seeds)
        {
            if (existing.Contains(seed.Name))
            {
                continue;
            }
            db.AttributeDefinitions.Add(new AttributeDefinitionRow
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                Name = seed.Name,
                Label = seed.Label,
                Description = seed.Label,
                ValueKind = seed.Kind,
                InputKind = seed.Input,
                ViewKind = seed.View,
                GroupName = seed.Group,
                Required = false,
                IsSystem = true,
                OptionsJson = seed.Options is null ? null : JsonSerializer.Serialize(seed.Options),
            });
        }
        var stored = await db.AttributeDefinitions.Where(row => row.OrganizationId == orgId).ToListAsync();
        foreach (var row in stored)
        {
            if (row.InputKind.Length > 0 && row.ViewKind.Length > 0 && row.GroupName.Length > 0)
            {
                continue;
            }
            var type = FieldCatalog.RequireType(row.ValueKind);
            if (row.InputKind.Length == 0)
            {
                row.InputKind = type.PrimaryInput;
            }
            if (row.ViewKind.Length == 0)
            {
                row.ViewKind = type.PrimaryView;
            }
            if (row.GroupName.Length == 0)
            {
                row.GroupName = "Details";
            }
        }
        await db.SaveChangesAsync();
    }

    private async Task EnsureFieldColumns()
    {
        var names = new List<string>();
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = db.Database.ProviderName?.Contains("Sqlite", StringComparison.Ordinal) == true
                ? "SELECT name FROM pragma_table_info('AttributeDefinitions')"
                : "SELECT column_name FROM information_schema.columns WHERE table_name = 'AttributeDefinitions'";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }
        }
        if (!names.Contains("InputKind"))
        {
            await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "AttributeDefinitions" ADD COLUMN "InputKind" TEXT NOT NULL DEFAULT ''""");
        }
        if (!names.Contains("ViewKind"))
        {
            await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "AttributeDefinitions" ADD COLUMN "ViewKind" TEXT NOT NULL DEFAULT ''""");
        }
        if (!names.Contains("GroupName"))
        {
            await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "AttributeDefinitions" ADD COLUMN "GroupName" TEXT NOT NULL DEFAULT ''""");
        }
    }

    private async Task<List<AttributeDefinitionRow>> Definitions(Guid orgId)
    {
        await EnsureDefinitions(orgId);
        return await db.AttributeDefinitions.Where(row => row.OrganizationId == orgId).OrderBy(row => row.Name).ToListAsync();
    }

    private async Task<RoleRow> Role(string name)
    {
        return await db.Roles.Include(row => row.Rights).FirstAsync(row => row.Name == name);
    }

    private static Actor ToActor(UserRow user)
    {
        var rights = user.Roles.SelectMany(role => role.Rights).Select(right => right.Name).ToHashSet();
        return new Actor(user.Id, user.OrganizationId, user.Email, user.DisplayName, rights, user.DesignId);
    }

    private static string Required(string raw, string field)
    {
        var value = raw.Trim();
        if (value.Length == 0)
        {
            throw new OtaiDeskRejectedException($"{field} is required");
        }
        return value;
    }

    private static string? BlankToNull(string? raw)
    {
        var value = raw?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string[] SplitOptions(string? raw)
    {
        return (raw ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
