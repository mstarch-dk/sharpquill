using System.Text;
using SharpQuill.Application.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SharpQuill.Api.Infrastructure.Auth;

/// <summary>
/// Wires JWT authentication and the authorization policies. Admin and website tokens are validated by separate
/// schemes, each accepting only its own audience and its own <c>user_type</c>, so the two cannot be swapped.
/// </summary>
public static class AuthenticationSetup
{
    private const string LoggerCategory = "SharpQuill.Api.Auth";

    public static IServiceCollection AddCmsAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
    {
        services
            .AddAuthentication(AuthSchemes.Default)
            .AddPolicyScheme(AuthSchemes.Default, AuthSchemes.Default, options =>
            {
                options.ForwardDefaultSelector = context => SelectScheme(context, jwtSettings);
            })
            .AddJwtBearer(AuthSchemes.Admin, options =>
                Configure(options, jwtSettings, jwtSettings.AdminAudience, SharpQuillClaims.UserTypeAdmin))
            .AddJwtBearer(AuthSchemes.Website, options =>
                Configure(options, jwtSettings, jwtSettings.WebsiteAudience, SharpQuillClaims.UserTypeWebsite));

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.AdminUser, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Admin)
                .RequireAuthenticatedUser()
                .RequireClaim(SharpQuillClaims.UserType, SharpQuillClaims.UserTypeAdmin))
            .AddPolicy(AuthPolicies.AdminRole, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Admin)
                .RequireAuthenticatedUser()
                .RequireClaim(SharpQuillClaims.UserType, SharpQuillClaims.UserTypeAdmin)
                .RequireRole(SharpQuillClaims.RoleAdmin))
            .AddPolicy(AuthPolicies.WebsiteUser, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Website)
                .RequireAuthenticatedUser()
                .RequireClaim(SharpQuillClaims.UserType, SharpQuillClaims.UserTypeWebsite));

        services.AddSingleton<IAuthorizationHandler, ArticleOwnershipHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();

        return services;
    }

    /// <summary>
    /// Picks the scheme for endpoints without a policy. Reads the audience without trusting it: whichever scheme is
    /// chosen validates signature, issuer, audience and lifetime itself.
    /// </summary>
    private static string SelectScheme(HttpContext context, JwtSettings jwtSettings)
    {
        var header = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var token = header[prefix.Length..].Trim();
            var handler = new JsonWebTokenHandler();
            if (handler.CanReadToken(token))
            {
                try
                {
                    if (handler.ReadJsonWebToken(token).Audiences.Contains(jwtSettings.WebsiteAudience))
                    {
                        return AuthSchemes.Website;
                    }
                }
                catch (ArgumentException)
                {
                    // Malformed token: the admin scheme rejects (and logs) it
                }
            }
        }

        return AuthSchemes.Admin;
    }

    private static void Configure(JwtBearerOptions options, JwtSettings jwtSettings, string audience, string userType)
    {
        // Keep the claim names as issued (sub, role, user_type, ...) instead of mapping them to WS-* URIs
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = SharpQuillClaims.Name,
            RoleClaimType = SharpQuillClaims.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal;
                var actualUserType = principal?.FindFirst(SharpQuillClaims.UserType)?.Value;
                if (actualUserType != userType)
                {
                    Log(context.HttpContext, context.Scheme.Name,
                        $"user_type '{actualUserType ?? "(missing)"}' does not match the token audience");
                    context.Fail("user_type does not match the token audience");
                }
                else if (!int.TryParse(principal?.FindFirst(SharpQuillClaims.Subject)?.Value, out _))
                {
                    Log(context.HttpContext, context.Scheme.Name, "sub is missing or not a numeric id");
                    context.Fail("sub is missing or not a numeric id");
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // The exception type names the reason (expired, bad signature, wrong audience, ...). Its message is
                // left out: it can carry key metadata, and nothing about the token itself belongs in the logs.
                Log(context.HttpContext, context.Scheme.Name, context.Exception.GetType().Name);
                return Task.CompletedTask;
            }
        };
    }

    private static void Log(HttpContext context, string scheme, string reason)
    {
        context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(LoggerCategory)
            .LogWarning("Rejected JWT ({Scheme}) for {Method} {Path}: {Reason}",
                scheme, context.Request.Method, context.Request.Path, reason);
    }
}
