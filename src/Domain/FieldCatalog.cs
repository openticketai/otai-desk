namespace OtaiDesk.Domain;

public sealed record FieldType(string ValueKind, string[] Inputs, string[] Views, string PrimaryInput, string PrimaryView);

public static class FieldCatalog
{
    public static readonly FieldType String = new("string", ["text", "textarea"], ["text", "prose"], "text", "text");
    public static readonly FieldType Number = new("number", ["number", "text"], ["number", "text"], "number", "number");
    public static readonly FieldType Boolean = new("boolean", ["checkbox", "select"], ["checkbox", "text"], "checkbox", "checkbox");
    public static readonly FieldType Date = new("date", ["date"], ["date", "text"], "date", "date");
    public static readonly FieldType Timestamp = new("datetime", ["timestamp"], ["timestamp", "text"], "timestamp", "timestamp");
    public static readonly FieldType User = new("user", ["user", "text"], ["user", "text"], "user", "user");
    public static readonly FieldType Select = new("select", ["select"], ["badge", "text"], "select", "badge");

    public static readonly FieldType[] All = [String, Number, Boolean, Date, Timestamp, User, Select];

    public static FieldType RequireType(string valueKind)
    {
        foreach (var type in All)
        {
            if (type.ValueKind == valueKind)
            {
                return type;
            }
        }
        throw new OtaiDeskRejectedException($"Unknown attribute kind: {valueKind}");
    }

    public static void RequirePair(string valueKind, string inputKind, string viewKind)
    {
        var type = RequireType(valueKind);
        if (!type.Inputs.Contains(inputKind))
        {
            throw new OtaiDeskRejectedException($"Input {inputKind} is not allowed for {valueKind}");
        }
        if (!type.Views.Contains(viewKind))
        {
            throw new OtaiDeskRejectedException($"View {viewKind} is not allowed for {valueKind}");
        }
    }

    public static IReadOnlyList<string> Choices(string inputKind, IReadOnlyList<string>? options)
    {
        if (inputKind == "select" && (options is null || options.Count == 0))
        {
            return ["false", "true"];
        }
        return options ?? [];
    }

    public static string? ReadPosted(string inputKind, IReadOnlyList<string?> posted)
    {
        if (inputKind == "checkbox")
        {
            return posted.Contains("true") ? "true" : "false";
        }
        var raw = posted.LastOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
        return raw.Length == 0 ? null : raw;
    }
}
