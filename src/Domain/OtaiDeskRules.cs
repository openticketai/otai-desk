using System.Globalization;
using System.Text.RegularExpressions;

namespace OtaiDesk.Domain;

public static class Rights
{
    public const string TicketsRead = "tickets:read";
    public const string TicketsWrite = "tickets:write";
    public const string AttributesManage = "attributes:manage";
    public const string UsersManage = "users:manage";

    public static readonly string[] All = [TicketsRead, TicketsWrite, AttributesManage, UsersManage];

    public static readonly IReadOnlyDictionary<string, string[]> ByRole = new Dictionary<string, string[]>
    {
        ["admin"] = All,
        ["agent"] = [TicketsRead, TicketsWrite],
        ["employee"] = [TicketsRead, TicketsWrite],
    };
}

public static class Providers
{
    public static readonly string[] All = ["desk", "otobo", "znuny", "zammad", "kix"];

    public static string Require(string? raw)
    {
        var name = raw?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return "desk";
        }
        if (!All.Contains(name))
        {
            throw new OtaiDeskRejectedException($"Unknown ticket provider: {name}");
        }
        return name;
    }
}

public static class Designs
{
    public const string LightForrest = "light-forrest";
    public const string DarkForrest = "dark-forrest";

    public static readonly string[] BuiltIn = [LightForrest, DarkForrest];

    public static bool IsBuiltIn(string id) => BuiltIn.Contains(id);
}

public sealed record DesignColors(string Bg, string Surface, string Border, string Text, string TextDim, string Accent)
{
    private static readonly Regex Hex = new("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant);

    public static DesignColors Parse(string bg, string surface, string border, string text, string textDim, string accent)
    {
        return new DesignColors(HexOf(bg), HexOf(surface), HexOf(border), HexOf(text), HexOf(textDim), HexOf(accent));
    }

    public static DesignColors LightForrest { get; } = Parse("#f3f6f1", "#ffffff", "#c5d1c0", "#1c2b1f", "#5c6b5e", "#2a6249");

    public static DesignColors DarkForrest { get; } = Parse("#121a14", "#1c2820", "#2e4034", "#e7f0e6", "#a8bba8", "#7dba8a");

    private static string HexOf(string raw)
    {
        var value = raw.Trim();
        if (!Hex.IsMatch(value))
        {
            throw new OtaiDeskRejectedException($"Design color must be #rrggbb: {raw}");
        }
        return value.ToLowerInvariant();
    }
}

public static class AttributeKinds
{
    public static readonly string[] All = FieldCatalog.All.Select(type => type.ValueKind).ToArray();
    public static readonly string[] TicketColumns = ["status", "queue", "priority", "type", "service"];

    public static void CheckWrite(IEnumerable<AttributeDefinitionSnapshot> definitions, IReadOnlyDictionary<string, string> attributes, bool requireAll)
    {
        var byName = definitions.ToDictionary(definition => definition.Name);
        foreach (var (name, raw) in attributes)
        {
            if (TicketColumns.Contains(name) || !byName.TryGetValue(name, out var definition))
            {
                throw new OtaiDeskRejectedException($"Unknown attribute: {name}");
            }
            CheckValue(definition, raw);
        }
        if (!requireAll)
        {
            return;
        }
        var missing = definitions.Where(definition => definition.Required && !attributes.ContainsKey(definition.Name)).Select(definition => definition.Name).Order().ToArray();
        if (missing.Length > 0)
        {
            throw new OtaiDeskRejectedException($"Missing attributes: {string.Join(", ", missing)}");
        }
    }

    public static void CheckValue(AttributeDefinitionSnapshot definition, string raw)
    {
        switch (definition.ValueKind)
        {
            case "string":
                return;
            case "number":
                if (!int.TryParse(raw, out _))
                {
                    throw new OtaiDeskRejectedException($"Invalid value for {definition.Name}: {raw}");
                }
                return;
            case "boolean":
                if (raw is not ("true" or "false"))
                {
                    throw new OtaiDeskRejectedException($"Invalid value for {definition.Name}: {raw}");
                }
                return;
            case "date":
                if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    throw new OtaiDeskRejectedException($"Invalid value for {definition.Name}: {raw}");
                }
                return;
            case "datetime":
                if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    throw new OtaiDeskRejectedException($"Invalid value for {definition.Name}: {raw}");
                }
                return;
            case "user":
                if (!Guid.TryParse(raw, out _))
                {
                    throw new OtaiDeskRejectedException($"Invalid value for {definition.Name}: {raw}");
                }
                return;
            case "select":
                if (definition.Options is null || !definition.Options.Contains(raw))
                {
                    throw new OtaiDeskRejectedException($"Invalid value for {definition.Name}: {raw}");
                }
                return;
            default:
                throw new OtaiDeskRejectedException($"Unknown attribute kind: {definition.ValueKind}");
        }
    }
}

public sealed record AttributeDefinitionSnapshot(string Name, string ValueKind, bool Required, IReadOnlyList<string>? Options);

public sealed class OtaiDeskRejectedException(string message) : Exception(message);

public sealed class ConnectorUnwiredException(string provider)
    : Exception($"Ticket provider '{provider}' is not wired. otai-ts-connector is not connected.");
