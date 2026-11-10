using OtaiDesk.Modules.Tickets.Api;
using OtaiDesk.Modules.Tickets.Application;
using OtaiDesk.Modules.Tickets.Infrastructure;
using OtaiDesk.Shared;
using Microsoft.EntityFrameworkCore;

namespace OtaiDesk.Modules.Tickets;

public sealed class TicketsModule : IDeskModule
{
    public void AddServices(IServiceCollection services)
    {
        services.AddScoped<ITicketStore, TicketStore>();
        services.AddScoped<ITicketAttributeDefinitions, DeskServiceAttributeDefinitions>();
        services.AddScoped<TicketUseCases>();
        services.AddScoped<TicketDemoSeed>();
    }

    public void BuildModel(ModelBuilder model) => TicketModel.Build(model);

    public void MapEndpoints(IEndpointRouteBuilder api) => TicketEndpoints.Map(api);

    public async Task Seed(IServiceProvider services, bool seedDemo)
    {
        if (seedDemo)
        {
            await services.GetRequiredService<TicketDemoSeed>().Run();
        }
    }
}
