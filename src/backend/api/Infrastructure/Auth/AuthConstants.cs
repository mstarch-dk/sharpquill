namespace SharpQuill.Api.Infrastructure.Auth;

/// <summary>Authentication scheme names.</summary>
public static class AuthSchemes
{
    /// <summary>JWT bearer scheme for admin (author) access tokens; validates the admin audience only.</summary>
    public const string Admin = "AdminJwt";

    /// <summary>JWT bearer scheme for website-user access tokens; validates the website audience only.</summary>
    public const string Website = "WebsiteJwt";

    /// <summary>
    /// Default scheme. Forwards to <see cref="Admin"/> or <see cref="Website"/> based on the token's audience, so
    /// endpoints without a policy (public endpoints that only behave differently for signed-in readers) still see
    /// the caller. It never decides access: the forwarded scheme validates the token in full.
    /// </summary>
    public const string Default = "Bearer";
}

/// <summary>Authorization policy names, used with <c>[Authorize(Policy = ...)]</c>.</summary>
public static class AuthPolicies
{
    /// <summary>Any signed-in author (admin token, any role).</summary>
    public const string AdminUser = "AdminUser";

    /// <summary>A signed-in author with the <c>admin</c> role.</summary>
    public const string AdminRole = "AdminRole";

    /// <summary>A signed-in website reader (website token).</summary>
    public const string WebsiteUser = "WebsiteUser";
}

/// <summary>Claim types and values the API issues and relies on.</summary>
public static class SharpQuillClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string UserType = "user_type";

    public const string UserTypeAdmin = "admin";
    public const string UserTypeWebsite = "website";

    public const string RoleAdmin = "admin";
}
