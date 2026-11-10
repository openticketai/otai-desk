using Microsoft.EntityFrameworkCore;

namespace OtaiDesk.Shared;

public interface IDeskModule
{
    void AddServices(IServiceCollection services);

    void BuildModel(ModelBuilder model);

    void MapEndpoints(IEndpointRouteBuilder api);

    Task Seed(IServiceProvider services, bool seedDemo);
}
