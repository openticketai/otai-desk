using System.Security.Claims;
using System.Text.Json;
using OtaiDesk.Domain;
using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Modules.Tickets.Domain;
using OtaiDesk.Shared;

namespace OtaiDesk.Modules.Tickets.Api;

public static class TicketEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("/tickets", async (ClaimsPrincipal user, TicketUseCases tickets, string? status, string? queue, string? priority) =>
        {
            var found = await tickets.Search(ActorClaims.Require(user), new TicketFilter(status, queue, priority));
            return Results.Ok(found.Select(TicketJson));
        }).RequireAuthorization(Rights.TicketsRead);
        api.MapPost("/tickets", async (JsonElement body, ClaimsPrincipal user, TicketUseCases tickets) =>
        {
            var fields = new TicketFields(Optional(body, "queue"), Optional(body, "priority"), Optional(body, "type"), Optional(body, "service"));
            var created = await tickets.Create(ActorClaims.Require(user), new NewTicket(Text(body, "title"), Text(body, "status"), fields, Attributes(body)));
            return Results.Created($"/api/v1/tickets/{created.Ticket.Id}", TicketJson(created));
        }).RequireAuthorization(Rights.TicketsWrite);
        api.MapGet("/tickets/{id:guid}", async (Guid id, ClaimsPrincipal user, TicketUseCases tickets) =>
            Results.Ok(TicketJson(await tickets.Get(ActorClaims.Require(user), id)))).RequireAuthorization(Rights.TicketsRead);
        api.MapGet("/tickets/{id:guid}/articles", async (Guid id, ClaimsPrincipal user, TicketUseCases tickets) =>
        {
            var read = await tickets.Get(ActorClaims.Require(user), id);
            return Results.Ok(read.Ticket.Articles.Select(ArticleJson));
        }).RequireAuthorization(Rights.TicketsRead);
        api.MapPost("/tickets/{id:guid}/articles", async (Guid id, JsonElement body, ClaimsPrincipal user, TicketUseCases tickets) =>
        {
            var meta = body.GetProperty("meta");
            var draft = new ArticleDraft(Text(meta, "body"), Text(meta, "subject"), Text(meta, "to"), Text(meta, "content_type"), Text(meta, "sender_type"), meta.GetProperty("is_internal").GetBoolean());
            var article = await tickets.Reply(ActorClaims.Require(user), id, draft);
            return Results.Created($"/api/v1/tickets/{id}/articles/{article.Id}", ArticleJson(article));
        }).RequireAuthorization(Rights.TicketsWrite);
    }

    private static object TicketJson(TicketRead read) => new
    {
        id = read.Ticket.Id,
        ticket_number = read.Ticket.TicketNumber,
        title = read.Ticket.Title,
        status = read.Ticket.Status,
        queue = read.Ticket.Queue,
        priority = read.Ticket.Priority,
        type = read.Ticket.TicketType,
        service = read.Ticket.Service,
        attributes = read.Attributes,
    };

    private static object ArticleJson(Article article) => new
    {
        id = article.Id,
        ticket_id = article.TicketId,
        meta = new Dictionary<string, object?>
        {
            ["body"] = article.Body,
            ["subject"] = article.Subject,
            ["from"] = article.FromAddress,
            ["to"] = article.ToAddress,
            ["content_type"] = article.ContentType,
            ["sender_type"] = article.SenderType,
            ["created_at"] = article.CreatedAt,
            ["author_user_id"] = article.AuthorUserId,
            ["is_internal"] = article.IsInternal,
        },
    };

    private static string Text(JsonElement body, string name) => body.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

    private static string? Optional(JsonElement body, string name) => body.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;

    private static Dictionary<string, string> Attributes(JsonElement body)
    {
        if (!body.TryGetProperty("attributes", out var attributes) || attributes.ValueKind != JsonValueKind.Object)
        {
            return [];
        }
        return attributes.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString() ?? "");
    }
}
