using System.Security.Claims;
using OtaiDesk.Domain;

namespace OtaiDesk.Shared;

public sealed record Actor(Guid Id, Guid OrganizationId, string Email, string DisplayName, IReadOnlySet<string> Rights, string DesignId);

public static class ActorClaims
{
    public static ClaimsPrincipal Principal(Actor actor)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, actor.Id.ToString()),
            new(ClaimTypes.Email, actor.Email),
            new(ClaimTypes.Name, actor.DisplayName),
            new("org", actor.OrganizationId.ToString()),
            new("design", actor.DesignId),
        };
        claims.AddRange(actor.Rights.Select(right => new Claim("right", right)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "line"));
    }

    public static Actor? FromPrincipal(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var org = principal.FindFirstValue("org");
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var name = principal.FindFirstValue(ClaimTypes.Name);
        var design = principal.FindFirstValue("design");
        if (id is null || org is null || email is null || name is null || design is null)
        {
            return null;
        }
        var rights = principal.FindAll("right").Select(claim => claim.Value).ToHashSet();
        return new Actor(Guid.Parse(id), Guid.Parse(org), email, name, rights, design);
    }

    public static Actor Require(ClaimsPrincipal principal)
    {
        return FromPrincipal(principal) ?? throw new OtaiDeskRejectedException("Missing actor");
    }
}
