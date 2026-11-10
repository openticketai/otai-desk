using OtaiDesk.Domain;
using OtaiDesk.Services;
using OtaiDesk.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OtaiDesk.Pages;

[AllowAnonymous]
public sealed class LoginModel(DeskService desk) : PageModel
{
    public string? Failure { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost(string email, string password)
    {
        try
        {
            var actor = await desk.Authenticate(email, password);
            if (actor is null)
            {
                Failure = "Sign-in rejected.";
                return Page();
            }
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, ActorClaims.Principal(actor));
            return Redirect("/");
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Failure = ex.Message;
            return Page();
        }
    }
}

[AllowAnonymous]
public sealed class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPost()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }
}

[Authorize]
public sealed class DesignsModel(DeskService desk) : OtaiDeskPage(desk)
{
    public IReadOnlyList<Data.DesignRow> Choices { get; private set; } = [];

    public async Task OnGet() => Choices = await Desk.DesignsFor(Current);

    public async Task<IActionResult> OnPostSelect(string designId)
    {
        try
        {
            await Desk.SelectDesign(Current, designId);
            var refreshed = await Desk.ActorById(Current.Id) ?? Current;
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, ActorClaims.Principal(refreshed));
            return Redirect("/designs");
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
            Choices = await Desk.DesignsFor(Current);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCreate(string name, string bg, string surface, string border, string text, string textDim, string accent)
    {
        try
        {
            var created = await Desk.CreateDesign(Current, name, DesignColors.Parse(bg, surface, border, text, textDim, accent));
            await Desk.SelectDesign(Current, created.Id);
            var refreshed = await Desk.ActorById(Current.Id) ?? Current;
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, ActorClaims.Principal(refreshed));
            return Redirect("/designs");
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
            Choices = await Desk.DesignsFor(Current);
            return Page();
        }
    }
}

[Authorize(Policy = Rights.UsersManage)]
public sealed class UsersModel(DeskService desk) : OtaiDeskPage(desk)
{
    public IReadOnlyList<Data.UserRow> Users { get; private set; } = [];

    public async Task OnGet() => Users = await Desk.ListUsers(Current);

    public async Task<IActionResult> OnPost(string email, string displayName, string password, string roleName, string designId)
    {
        try
        {
            await Desk.CreateUser(Current, email, displayName, password, roleName, designId);
            return Redirect("/users");
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
            Users = await Desk.ListUsers(Current);
            return Page();
        }
    }
}

[Authorize(Policy = Rights.AttributesManage)]
public sealed class AttributesModel(DeskService desk) : OtaiDeskPage(desk)
{
    public IReadOnlyList<Data.AttributeDefinitionRow> Definitions { get; private set; } = [];
    public IReadOnlyList<string> Inputs { get; private set; } = [];
    public IReadOnlyList<string> Views { get; private set; } = [];

    public async Task OnGet() => Definitions = await Desk.ListDefinitions(Current);

    public IActionResult OnGetWidgets(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return Content("");
        }
        var type = FieldCatalog.RequireType(kind);
        Inputs = type.Inputs;
        Views = type.Views;
        return Partial("_AttributeWidgets", this);
    }

    public async Task<IActionResult> OnPost(string name, string label, string description, string kind, string inputKind, string viewKind, string groupName, bool required, string? options)
    {
        try
        {
            await Desk.CreateDefinition(Current, name, label, description, kind, inputKind, viewKind, groupName, required, options);
            return Redirect("/attributes");
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
            Definitions = await Desk.ListDefinitions(Current);
            return Page();
        }
    }
}

[Authorize(Policy = Rights.UsersManage)]
public sealed class AccountsModel(DeskService desk) : OtaiDeskPage(desk)
{
    public IReadOnlyList<Data.CustomerAccountRow> Accounts { get; private set; } = [];

    public async Task OnGet() => Accounts = await Desk.ListAccounts(Current);

    public async Task<IActionResult> OnPost(string name, string? domain)
    {
        try
        {
            await Desk.CreateAccount(Current, name, domain);
            return Redirect("/accounts");
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
            Accounts = await Desk.ListAccounts(Current);
            return Page();
        }
    }
}

[Authorize(Policy = Rights.TicketsRead)]
public sealed class KnowledgeModel(DeskService desk) : OtaiDeskPage(desk)
{
    public IReadOnlyList<Data.FaqRow> Faqs { get; private set; } = [];

    public async Task OnGet() => Faqs = await Desk.ListFaqs(Current);

    public async Task<IActionResult> OnPost(string question, string answer)
    {
        if (!Current.Rights.Contains(Rights.TicketsWrite))
        {
            return Forbid();
        }
        try
        {
            await Desk.CreateFaq(Current, question, answer);
            return Redirect("/knowledge");
        }
        catch (Exception ex) when (ex is OtaiDeskRejectedException or ConnectorUnwiredException)
        {
            Fail(ex);
            Faqs = await Desk.ListFaqs(Current);
            return Page();
        }
    }
}
