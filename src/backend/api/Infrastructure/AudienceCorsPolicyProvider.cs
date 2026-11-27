using Microsoft.AspNetCore.Cors.Infrastructure;

namespace SharpQuill.Api.Infrastructure;

/// <summary>
/// Picks the CORS policy per audience instead of one policy for the whole API:
/// <list type="bullet">
/// <item><c>/api/admin/*</c> accepts credentialed calls from the admin origin(s) only.</item>
/// <item>Everything else accepts credentialed calls from the public website origin(s) only.</item>
/// <item>The admin app may additionally read public endpoints (e.g. site settings) with GET, without credentials.</item>
/// </list>
/// A page on one origin therefore cannot make credentialed calls to the other audience's endpoints.
/// </summary>
public class AudienceCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly string[] _adminOrigins;
    private readonly string[] _publicOrigins;
    private readonly CorsPolicy _admin;
    private readonly CorsPolicy _public;
    private readonly CorsPolicy _adminReadsPublic;

    public AudienceCorsPolicyProvider(string[] adminOrigins, string[] publicOrigins)
    {
        _adminOrigins = adminOrigins;
        _publicOrigins = publicOrigins;

        _admin = new CorsPolicyBuilder().WithOrigins(adminOrigins)
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials().Build();
        _public = new CorsPolicyBuilder().WithOrigins(publicOrigins)
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials().Build();
        _adminReadsPublic = new CorsPolicyBuilder().WithOrigins(adminOrigins)
            .AllowAnyHeader().WithMethods("GET", "HEAD").Build();
    }

    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (context.Request.Path.StartsWithSegments("/api/admin"))
        {
            return Task.FromResult<CorsPolicy?>(_admin);
        }

        var origin = context.Request.Headers.Origin.ToString();
        var isAdminOnlyOrigin = _adminOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
            && !_publicOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);

        return Task.FromResult<CorsPolicy?>(isAdminOnlyOrigin ? _adminReadsPublic : _public);
    }
}
