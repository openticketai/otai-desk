using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using OtaiDesk.Data;
using OtaiDesk.Domain;
using OtaiDesk.Services;
using OtaiDesk.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace OtaiDesk;

public sealed record OtaiDeskSettings(string DatabaseUrl, string SecretKey, string ApiToken, string TicketProvider, bool SeedDemo, string? DemoPassword);

public sealed class ApiTokenHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }
        var settings = Context.RequestServices.GetRequiredService<OtaiDeskSettings>();
        var token = header["Bearer ".Length..].Trim();
        if (!string.Equals(token, settings.ApiToken, StringComparison.Ordinal))
        {
            return AuthenticateResult.Fail("API token rejected");
        }
        var actor = await Context.RequestServices.GetRequiredService<DeskService>().ActorById(DeskService.ApiActorId);
        if (actor is null)
        {
            return AuthenticateResult.Fail("API actor is missing");
        }
        return AuthenticateResult.Success(new AuthenticationTicket(ActorClaims.Principal(actor), Scheme.Name));
    }
}

public static class OtaiDeskApp
{
    public static OtaiDeskSettings ReadSettings()
    {
        var database = Required("DATABASE_URL");
        var secret = Required("OTAI_DESK_SECRET_KEY");
        var token = Required("OTAI_DESK_API_TOKEN");
        if (token.Length < 16)
        {
            throw new InvalidOperationException("OTAI_DESK_API_TOKEN must be at least 16 characters");
        }
        var provider = Providers.Require(Environment.GetEnvironmentVariable("OTAI_DESK_TICKET_PROVIDER"));
        var seed = string.Equals(Environment.GetEnvironmentVariable("OTAI_DESK_SEED_DEMO"), "true", StringComparison.OrdinalIgnoreCase);
        var demoPassword = Environment.GetEnvironmentVariable("OTAI_DESK_DEMO_PASSWORD");
        if (seed && string.IsNullOrWhiteSpace(demoPassword))
        {
            throw new InvalidOperationException("OTAI_DESK_DEMO_PASSWORD is required when OTAI_DESK_SEED_DEMO is true");
        }
        return new OtaiDeskSettings(database, secret, token, provider, seed, demoPassword);
    }

    public static void AddOtaiDesk(this WebApplicationBuilder builder, OtaiDeskSettings settings, IReadOnlyList<IDeskModule> modules)
    {
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(new TicketProvider(settings.TicketProvider));
        builder.Services.AddSingleton<IPasswordHasher<UserRow>, PasswordHasher<UserRow>>();
        builder.Services.AddDbContext<OtaiDeskDb>(options => UseDatabase(options, settings.DatabaseUrl));
        builder.Services.AddScoped<DeskService>();
        foreach (var module in modules)
        {
            builder.Services.AddSingleton(module);
            module.AddServices(builder.Services);
        }
        var signingKey = new SymmetricSecurityKey(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(settings.SecretKey)));
        builder.Services.AddAuthentication(options => options.DefaultScheme = "smart")
            .AddCookie(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/login";
                options.Cookie.Name = "line";
                options.Cookie.HttpOnly = true;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = signingKey,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = ClaimTypes.NameIdentifier,
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiTokenHandler>("api-token", _ => { })
            .AddPolicyScheme("smart", "smart", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var header = context.Request.Headers.Authorization.ToString();
                    if (!header.StartsWith("Bearer ", StringComparison.Ordinal))
                    {
                        return Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                    }
                    var token = header["Bearer ".Length..].Trim();
                    return token == settings.ApiToken ? "api-token" : JwtBearerDefaults.AuthenticationScheme;
                };
            });
        builder.Services.AddAuthorization(options =>
        {
            foreach (var right in Rights.All)
            {
                options.AddPolicy(right, policy => policy.RequireClaim("right", right));
            }
        });
        builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
        builder.Services.AddRazorPages(options =>
        {
            options.RootDirectory = "/";
            options.Conventions.AuthorizeFolder("/");
            options.Conventions.AllowAnonymousToPage("/Pages/Login");
        });
    }

    public static async Task Seed(WebApplication app, OtaiDeskSettings settings)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OtaiDeskDb>();
        await db.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DeskService>().EnsureSeed(settings.SeedDemo, settings.DemoPassword ?? "");
        foreach (var module in scope.ServiceProvider.GetServices<IDeskModule>())
        {
            await module.Seed(scope.ServiceProvider, settings.SeedDemo);
        }
    }

    public static void MapOtaiDeskApi(this WebApplication app, OtaiDeskSettings settings)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        var api = app.MapGroup("/api/v1");
        api.MapPost("/token", async (HttpRequest request, DeskService desk) =>
        {
            var form = await request.ReadFormAsync();
            var actor = await desk.Authenticate(form["username"].ToString(), form["password"].ToString());
            if (actor is null)
            {
                return Results.Unauthorized();
            }
            var key = new SymmetricSecurityKey(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(settings.SecretKey)));
            var jwt = new JwtSecurityToken(claims: ActorClaims.Principal(actor).Claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return Results.Ok(new { access_token = new JwtSecurityTokenHandler().WriteToken(jwt), token_type = "bearer" });
        });
        api.MapGet("/me", (ClaimsPrincipal user) => ActorJson(user)).RequireAuthorization();
        foreach (var module in app.Services.GetServices<IDeskModule>())
        {
            module.MapEndpoints(api);
        }
    }

    private static void UseDatabase(DbContextOptionsBuilder options, string url)
    {
        if (url.StartsWith("sqlite:", StringComparison.Ordinal))
        {
            options.UseSqlite(url["sqlite:".Length..]);
            return;
        }
        options.UseNpgsql(ToNpgsql(url));
    }

    public static string ToNpgsql(string url)
    {
        if (!url.Contains("://", StringComparison.Ordinal))
        {
            return url;
        }
        var normalized = url.Replace("postgresql+asyncpg://", "postgresql://", StringComparison.Ordinal);
        var uri = new Uri(normalized);
        var parts = uri.UserInfo.Split(':', 2);
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={Uri.UnescapeDataString(parts[0])};Password={Uri.UnescapeDataString(parts[1])}";
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is required");
        }
        return value;
    }

    private static object ActorJson(ClaimsPrincipal user)
    {
        var actor = ActorClaims.Require(user);
        return new { id = actor.Id, email = actor.Email, display_name = actor.DisplayName, design_id = actor.DesignId, rights = actor.Rights.Order() };
    }
}
