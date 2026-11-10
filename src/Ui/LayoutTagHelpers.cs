using Microsoft.AspNetCore.Razor.TagHelpers;

namespace OtaiDesk.Ui;

[HtmlTargetElement("ui-stack")]
public sealed class StackTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var child = await output.GetChildContentAsync();
        output.TagName = "div";
        output.Attributes.SetAttribute("class", Join("ui-stack", output.Attributes["class"]?.Value?.ToString()));
        output.Content.SetHtmlContent(child);
    }

    internal static string Join(string component, string? extra)
    {
        return string.IsNullOrWhiteSpace(extra) ? component : component + " " + extra.Trim();
    }
}

[HtmlTargetElement("ui-section")]
public sealed class SectionTagHelper : TagHelper
{
    public string Title { get; set; } = "";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Title.Trim().Length == 0)
        {
            throw new InvalidOperationException("ui-section needs a title");
        }
        var child = await output.GetChildContentAsync();
        output.TagName = "section";
        output.Attributes.Clear();
        output.Attributes.SetAttribute("class", "ui-section");
        output.Content.SetHtmlContent($"""<h2 class="text-sm font-semibold">{System.Net.WebUtility.HtmlEncode(Title)}</h2>""");
        output.Content.AppendHtml(child);
    }
}

[HtmlTargetElement("ui-grid")]
public sealed class GridTagHelper : TagHelper
{
    public int Columns { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Columns < 1)
        {
            throw new InvalidOperationException("ui-grid needs columns");
        }
        var child = await output.GetChildContentAsync();
        output.TagName = "div";
        output.Attributes.Clear();
        output.Attributes.SetAttribute("class", "ui-grid");
        output.Attributes.SetAttribute("style", $"--cols: {Columns}");
        output.Content.SetHtmlContent(child);
    }
}

[HtmlTargetElement("ui-columns")]
public sealed class ColumnsTagHelper : TagHelper
{
    public string Tracks { get; set; } = "";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Tracks.Trim().Length == 0)
        {
            throw new InvalidOperationException("ui-columns needs tracks");
        }
        var child = await output.GetChildContentAsync();
        output.TagName = "div";
        output.Attributes.Clear();
        output.Attributes.SetAttribute("class", "ui-columns");
        output.Attributes.SetAttribute("style", "--tracks: " + Tracks.Trim());
        output.Content.SetHtmlContent(child);
    }
}

[HtmlTargetElement("ui-table")]
public sealed class TableTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var child = await output.GetChildContentAsync();
        output.TagName = "table";
        output.Attributes.SetAttribute("class", "data");
        output.PreElement.SetHtmlContent("<div class=\"card overflow-hidden\">");
        output.PostElement.SetHtmlContent("</div>");
        output.Content.SetHtmlContent(child);
    }
}
