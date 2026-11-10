using System.Globalization;
using System.Net;
using System.Text;
using OtaiDesk.Domain;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace OtaiDesk.Ui;

public static class FieldMarkup
{
    public static string Inner(string label, string name, string input, string? value, bool required, IReadOnlyList<string>? options)
    {
        var caption = $"<span>{Encode(label)}</span>";
        var control = Control(name, input, value, required, options);
        return input == "checkbox" ? control + caption : caption + control;
    }

    public static string View(string view, string? value)
    {
        var raw = value ?? "";
        return view switch
        {
            "text" or "number" or "user" or "date" => $"<span>{Encode(raw)}</span>",
            "prose" => $"<span class=\"whitespace-pre-wrap\">{Encode(raw)}</span>",
            "timestamp" => $"<span>{Encode(Timestamp(raw))}</span>",
            "badge" => $"<span class=\"badge\">{Encode(raw)}</span>",
            "checkbox" => $"<input class=\"size-4\" type=\"checkbox\" disabled{(raw == "true" ? " checked" : "")} />",
            _ => throw new OtaiDeskRejectedException($"Unknown view: {view}"),
        };
    }

    private static string Control(string name, string input, string? value, bool required, IReadOnlyList<string>? options)
    {
        var requiredAttribute = required && input != "checkbox" ? " required" : "";
        return input switch
        {
            "text" or "user" => Box(name, "text", value, requiredAttribute),
            "email" => Box(name, "email", value, requiredAttribute),
            "password" => Box(name, "password", value, requiredAttribute),
            "number" => Box(name, "number", value, requiredAttribute),
            "date" => Box(name, "date", value, requiredAttribute),
            "timestamp" => Box(name, "datetime-local", TimestampInput(value), requiredAttribute),
            "textarea" => $"<textarea class=\"control min-h-28\" name=\"{Encode(name)}\"{requiredAttribute}>{Encode(value)}</textarea>",
            "checkbox" => $"<input class=\"size-4\" type=\"checkbox\" name=\"{Encode(name)}\" value=\"true\"{(value == "true" ? " checked" : "")} />",
            "select" => Select(name, value, requiredAttribute, FieldCatalog.Choices(input, options)),
            _ => throw new OtaiDeskRejectedException($"Unknown input: {input}"),
        };
    }

    private static string Box(string name, string type, string? value, string requiredAttribute)
    {
        return $"<input class=\"control\" type=\"{type}\" name=\"{Encode(name)}\" value=\"{Encode(value)}\"{requiredAttribute} />";
    }

    private static string Select(string name, string? value, string requiredAttribute, IReadOnlyList<string> options)
    {
        var markup = new StringBuilder();
        markup.Append($"<select class=\"control\" name=\"{Encode(name)}\"{requiredAttribute}>");
        markup.Append("<option value=\"\"></option>");
        foreach (var option in options)
        {
            var selected = option == value ? " selected" : "";
            markup.Append($"<option value=\"{Encode(option)}\"{selected}>{Encode(option)}</option>");
        }
        markup.Append("</select>");
        return markup.ToString();
    }

    private static string? TimestampInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
            : value;
    }

    private static string Timestamp(string value)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : value;
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "");
}

[HtmlTargetElement("ui-field")]
public sealed class FieldTagHelper : TagHelper
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Input { get; set; } = "";
    public string? Value { get; set; }
    public bool Required { get; set; }
    public IReadOnlyList<string>? Options { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "label";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Clear();
        output.Attributes.SetAttribute("class", Input == "checkbox" ? "field field-check" : "field");
        if (Input == "textarea")
        {
            output.Attributes.SetAttribute("style", "grid-column: 1 / -1");
        }
        output.Content.SetHtmlContent(FieldMarkup.Inner(Label, Name, Input, Value, Required, Options));
    }
}

[HtmlTargetElement("ui-value")]
public sealed class ValueTagHelper : TagHelper
{
    public string Label { get; set; } = "";
    public string View { get; set; } = "";
    public string? Value { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Clear();
        output.Attributes.SetAttribute("class", "ui-value");
        output.Content.SetHtmlContent($"""<span class="text-dim">{WebUtility.HtmlEncode(Label)}</span>{FieldMarkup.View(View, Value)}""");
    }
}
