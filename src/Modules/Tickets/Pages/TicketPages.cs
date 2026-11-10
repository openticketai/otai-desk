using OtaiDesk.Domain;
using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Modules.Tickets.Domain;
using OtaiDesk.Pages;
using OtaiDesk.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OtaiDesk.Modules.Tickets.Pages;

[Authorize(Policy = Rights.TicketsRead)]
public sealed class IndexModel(DeskService desk, TicketUseCases tickets) : OtaiDeskPage(desk)
{
    public IReadOnlyList<TicketRead> Tickets { get; private set; } = [];
    public string? Status { get; private set; }
    public string? Queue { get; private set; }
    public string? Priority { get; private set; }

    public async Task<IActionResult> OnGet(string? status, string? queue, string? priority)
    {
        Status = status;
        Queue = queue;
        Priority = priority;
        try
        {
            Tickets = await tickets.Search(Current, new TicketFilter(status, queue, priority));
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
        }
        return FragmentOrPage("_TicketRows");
    }
}

[Authorize(Policy = Rights.TicketsRead)]
public sealed class ZoomModel(DeskService desk, TicketUseCases tickets) : OtaiDeskPage(desk)
{
    public TicketRead? Ticket { get; private set; }
    public IReadOnlyList<TicketAttributeDefinition> Definitions { get; private set; } = [];

    public async Task<IActionResult> OnGet(Guid id) => await Load(id);

    public async Task<IActionResult> OnPostReply(Guid id, string body, string subject, string to, bool isInternal)
    {
        try
        {
            await tickets.Reply(Current, id, new ArticleDraft(body, subject, to, "text/plain", Sender(), isInternal));
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
        }
        return await Load(id);
    }

    public async Task<IActionResult> OnPostFilterFields(Guid id, string? status, string? queue, string? priority, string? type, string? service)
    {
        try
        {
            await tickets.Update(Current, id, new TicketChange(null, status, new TicketFields(queue, priority, type, service)), null);
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
        }
        return await Load(id);
    }

    private async Task<IActionResult> Load(Guid id)
    {
        try
        {
            Definitions = await tickets.Definitions(Current);
            Ticket = await tickets.Get(Current, id);
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
        }
        return FragmentOrPage("_Zoom");
    }

    private string Sender() => Current.Rights.Contains(Rights.UsersManage) ? "admin" : "agent";
}

[Authorize(Policy = Rights.TicketsWrite)]
public sealed class NewModel(DeskService desk, TicketUseCases tickets) : OtaiDeskPage(desk)
{
    public IReadOnlyList<TicketAttributeDefinition> Definitions { get; private set; } = [];

    public IEnumerable<TicketAttributeDefinition> InGroup(string group) =>
        Definitions.Where(definition => definition.GroupName == group).OrderBy(definition => definition.Label);

    public IEnumerable<IGrouping<string, TicketAttributeDefinition>> GroupsExcept(string group) =>
        Definitions.Where(definition => definition.GroupName != group).OrderBy(definition => definition.Label).GroupBy(definition => definition.GroupName).OrderBy(item => item.Key);

    public async Task OnGet()
    {
        Definitions = await tickets.Definitions(Current);
    }

    public async Task<IActionResult> OnPost(string title, string status, string? queue, string? priority, string? type, string? service, IFormCollection form)
    {
        Definitions = await tickets.Definitions(Current);
        var attributes = new Dictionary<string, string>();
        foreach (var definition in Definitions)
        {
            var raw = FieldCatalog.ReadPosted(definition.InputKind, form["attr-" + definition.Name].ToArray());
            if (raw is not null)
            {
                attributes[definition.Name] = raw;
            }
        }
        try
        {
            var created = await tickets.Create(Current, new NewTicket(title, status, new TicketFields(queue, priority, type, service), attributes));
            return Redirect($"/tickets/{created.Ticket.Id}");
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
            return Page();
        }
    }
}
