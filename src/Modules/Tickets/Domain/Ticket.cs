using OtaiDesk.Domain;

namespace OtaiDesk.Modules.Tickets.Domain;

public sealed record TicketFields(string? Queue, string? Priority, string? Type, string? Service);

public sealed record TicketChange(string? Title, string? Status, TicketFields Fields);

public sealed record ArticleDraft(string Body, string Subject, string To, string ContentType, string SenderType, bool IsInternal);

public sealed class Ticket
{
    private readonly List<TicketAttributeValue> values = [];
    private readonly List<Article> articles = [];

    private Ticket(Guid id, Guid organizationId, string ticketNumber, string title, string status, Guid? requesterId, Guid? assigneeId, Guid? customerAccountId, string? queue, string? priority, string? ticketType, string? service, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        OrganizationId = organizationId;
        TicketNumber = ticketNumber;
        Title = title;
        Status = status;
        RequesterId = requesterId;
        AssigneeId = assigneeId;
        CustomerAccountId = customerAccountId;
        Queue = queue;
        Priority = priority;
        TicketType = ticketType;
        Service = service;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string TicketNumber { get; private set; }
    public string Title { get; private set; }
    public string Status { get; private set; }
    public Guid? RequesterId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? CustomerAccountId { get; private set; }
    public string? Queue { get; private set; }
    public string? Priority { get; private set; }
    public string? TicketType { get; private set; }
    public string? Service { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<TicketAttributeValue> Values => values;
    public IReadOnlyList<Article> Articles => articles;

    public static Ticket Open(Guid organizationId, int sequence, string title, string status, TicketFields fields, Guid requesterId, DateTimeOffset now)
    {
        return new Ticket(
            Guid.NewGuid(),
            organizationId,
            $"IT-{now.Year}-{sequence:000000}",
            Required(title, "title"),
            Required(status, "status"),
            requesterId,
            null,
            null,
            BlankToNull(fields.Queue),
            BlankToNull(fields.Priority),
            BlankToNull(fields.Type),
            BlankToNull(fields.Service),
            now,
            now);
    }

    public void Change(TicketChange change, DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(change.Title))
        {
            Title = change.Title.Trim();
        }
        if (!string.IsNullOrWhiteSpace(change.Status))
        {
            Status = change.Status.Trim();
        }
        if (change.Fields.Queue is not null)
        {
            Queue = BlankToNull(change.Fields.Queue);
        }
        if (change.Fields.Priority is not null)
        {
            Priority = BlankToNull(change.Fields.Priority);
        }
        if (change.Fields.Type is not null)
        {
            TicketType = BlankToNull(change.Fields.Type);
        }
        if (change.Fields.Service is not null)
        {
            Service = BlankToNull(change.Fields.Service);
        }
        UpdatedAt = now;
    }

    public void SetAttributes(IReadOnlyDictionary<Guid, string> valuesByDefinition)
    {
        foreach (var (definitionId, value) in valuesByDefinition)
        {
            var existing = values.FirstOrDefault(row => row.DefinitionId == definitionId);
            if (existing is null)
            {
                values.Add(new TicketAttributeValue(Guid.NewGuid(), Id, definitionId, value));
            }
            else
            {
                existing.Change(value);
            }
        }
    }

    public Article Reply(ArticleDraft draft, Guid authorId, string fromAddress, DateTimeOffset now)
    {
        var article = new Article(
            Guid.NewGuid(),
            Id,
            Required(draft.Body, "body"),
            Required(draft.Subject, "subject"),
            fromAddress,
            Required(draft.To, "to"),
            Required(draft.ContentType, "content_type"),
            Required(draft.SenderType, "sender_type"),
            now,
            authorId,
            draft.IsInternal);
        articles.Add(article);
        UpdatedAt = now;
        return article;
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
}

public sealed class TicketAttributeValue
{
    internal TicketAttributeValue(Guid id, Guid ticketId, Guid definitionId, string value)
    {
        Id = id;
        TicketId = ticketId;
        DefinitionId = definitionId;
        Value = value;
    }

    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid DefinitionId { get; private set; }
    public string Value { get; private set; }

    internal void Change(string value) => Value = value;
}

public sealed class Article
{
    internal Article(Guid id, Guid ticketId, string body, string subject, string fromAddress, string toAddress, string contentType, string senderType, DateTimeOffset createdAt, Guid? authorUserId, bool isInternal)
    {
        Id = id;
        TicketId = ticketId;
        Body = body;
        Subject = subject;
        FromAddress = fromAddress;
        ToAddress = toAddress;
        ContentType = contentType;
        SenderType = senderType;
        CreatedAt = createdAt;
        AuthorUserId = authorUserId;
        IsInternal = isInternal;
    }

    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public string Body { get; private set; }
    public string Subject { get; private set; }
    public string FromAddress { get; private set; }
    public string ToAddress { get; private set; }
    public string ContentType { get; private set; }
    public string SenderType { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? AuthorUserId { get; private set; }
    public bool IsInternal { get; private set; }
}
