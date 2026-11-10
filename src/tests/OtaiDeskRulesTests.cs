using OtaiDesk.Domain;
using OtaiDesk.Ui;
using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Modules.Tickets.Domain;
using Xunit;

namespace OtaiDesk.Tests;

public sealed class OtaiDeskRulesTests
{
    [Fact]
    public void Select_rejects_a_value_outside_its_options()
    {
        var definition = new AttributeDefinitionSnapshot("severity", "select", false, ["sev1", "sev2"]);
        var error = Assert.Throws<OtaiDeskRejectedException>(() => AttributeKinds.CheckValue(definition, "sev9"));
        Assert.Contains("severity", error.Message);
    }

    [Fact]
    public void A_data_type_rejects_an_input_it_does_not_allow()
    {
        var error = Assert.Throws<OtaiDeskRejectedException>(() => FieldCatalog.RequirePair("string", "checkbox", "text"));
        Assert.Contains("checkbox", error.Message);
        FieldCatalog.RequirePair("string", "textarea", "prose");
        FieldCatalog.RequirePair("datetime", "timestamp", "text");
        FieldCatalog.RequirePair("boolean", "checkbox", "text");
        Assert.Contains("type=\"date\"", FieldMarkup.Inner("Due on", "due_on", "date", null, false, null));
        Assert.Contains("type=\"datetime-local\"", FieldMarkup.Inner("Opened", "opened_at", "timestamp", null, false, null));
        Assert.Contains("type=\"checkbox\"", FieldMarkup.Inner("Visible", "visible", "checkbox", "true", false, null));
    }

    [Fact]
    public void A_date_value_is_a_calendar_day_and_a_checkbox_posts_false_when_off()
    {
        var definition = new AttributeDefinitionSnapshot("due_on", "date", false, null);
        AttributeKinds.CheckValue(definition, "2026-11-09");
        Assert.Throws<OtaiDeskRejectedException>(() => AttributeKinds.CheckValue(definition, "2026-11-09T13:00"));
        Assert.Equal("false", FieldCatalog.ReadPosted("checkbox", []));
        Assert.Equal("true", FieldCatalog.ReadPosted("checkbox", ["true"]));
        Assert.Null(FieldCatalog.ReadPosted("text", [""]));
    }

    [Fact]
    public async Task A_mismatched_field_pair_is_rejected()
    {
        await using var app = await TestDesk.Start("desk");
        var actor = await app.ApiActor();
        await Assert.ThrowsAsync<OtaiDeskRejectedException>(() => app.Desk.CreateDefinition(actor, "note", "Note", "A note", "string", "checkbox", "text", "Details", false, null));
    }

    [Fact]
    public void Built_in_design_ids_are_the_forest_skins()
    {
        Assert.Equal(["dark-forrest", "light-forrest"], Designs.BuiltIn.Order().ToArray());
        Assert.Equal("#2a6249", DesignColors.LightForrest.Accent);
        Assert.Equal("#121a14", DesignColors.DarkForrest.Bg);
    }

    [Fact]
    public void A_design_color_must_be_hex()
    {
        Assert.Throws<OtaiDeskRejectedException>(() => DesignColors.Parse("green", "#ffffff", "#ffffff", "#ffffff", "#ffffff", "#ffffff"));
    }

    [Fact]
    public async Task Desk_core_creates_a_ticket_and_keeps_designs_personal()
    {
        await using var app = await TestDesk.Start("desk");
        var desk = app.Desk;
        Assert.Null(await desk.Authenticate("api@helpdesk.local", "nope"));
        var created = await desk.CreateUser(await app.ApiActor(), "ada@example.com", "Ada", "secret-secret", "agent", Designs.LightForrest);
        var actor = await desk.Authenticate("ada@example.com", "secret-secret");
        Assert.NotNull(actor);
        Assert.Equal(created.Id, actor!.Id);
        var fields = new TicketFields("Hardware", "low", "incident", null);
        var ticket = await app.Tickets.Create(actor, new NewTicket("Printer jam", "open", fields, new Dictionary<string, string>()));
        Assert.StartsWith("IT-", ticket.Ticket.TicketNumber);
        var own = await desk.CreateDesign(actor, "Moss", DesignColors.Parse("#102010", "#203020", "#304030", "#e0f0e0", "#90a090", "#3a7a4a"));
        await desk.SelectDesign(actor, own.Id);
        await Assert.ThrowsAsync<OtaiDeskRejectedException>(() => desk.SelectDesign(actor, "missing-skin"));
        var other = await desk.CreateUser(await app.ApiActor(), "bea@example.com", "Bea", "secret-secret", "employee", Designs.DarkForrest);
        var bea = (await desk.Authenticate("bea@example.com", "secret-secret"))!;
        await Assert.ThrowsAsync<OtaiDeskRejectedException>(() => desk.SelectDesign(bea, own.Id));
        Assert.Equal(Designs.DarkForrest, (await desk.ActorById(other.Id))!.DesignId);
    }

    [Fact]
    public async Task External_provider_fails_loud()
    {
        await using var app = await TestDesk.Start("otobo");
        var actor = await app.ApiActor();
        var filter = new TicketFilter(null, null, null);
        var error = await Assert.ThrowsAsync<ConnectorUnwiredException>(() => app.Tickets.Search(actor, filter));
        Assert.Contains("otobo", error.Message);
    }
}
