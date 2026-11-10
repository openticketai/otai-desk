using OtaiDesk;
using OtaiDesk.Domain;
using OtaiDesk.Modules.Tickets;
using OtaiDesk.Shared;

IDeskModule[] modules = [new TicketsModule()];

var settings = OtaiDeskApp.ReadSettings();
var builder = WebApplication.CreateBuilder(args);
builder.AddOtaiDesk(settings, modules);
var app = builder.Build();
await OtaiDeskApp.Seed(app, settings);
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ConnectorUnwiredException ex)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { detail = ex.Message });
    }
    catch (OtaiDeskRejectedException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { detail = ex.Message });
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapOtaiDeskApi(settings);
app.MapRazorPages();
app.Run();

public partial class Program;
