using OtaiDesk.Services;
using OtaiDesk.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OtaiDesk.Pages;

public abstract class OtaiDeskPage(DeskService desk) : PageModel
{
    protected DeskService Desk { get; } = desk;
    public Actor Current { get; private set; } = null!;
    public Data.DesignRow Skin { get; private set; } = null!;
    public string? Failure { get; private set; }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var actor = ActorClaims.FromPrincipal(User);
        if (actor is null)
        {
            context.Result = Challenge();
            return;
        }
        Current = actor;
        Skin = await Desk.DesignFor(actor);
        await next();
    }

    protected void Fail(Exception exception) => Failure = exception.Message;

    protected IActionResult FragmentOrPage(string partial)
    {
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial(partial, this);
        }
        return Page();
    }
}
