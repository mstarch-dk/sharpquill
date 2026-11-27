using System.Security.Claims;

namespace SharpQuill.Api.Infrastructure.Auth;

/// <summary>Typed access to the caller's identity, as established by the authentication schemes.</summary>
public static class ClaimsPrincipalExtensions
{
    public static bool IsAuthor(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && user.HasClaim(SharpQuillClaims.UserType, SharpQuillClaims.UserTypeAdmin);

    public static bool IsWebsiteUser(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && user.HasClaim(SharpQuillClaims.UserType, SharpQuillClaims.UserTypeWebsite);

    /// <summary>True for an author with the admin role. A missing role claim is never treated as admin.</summary>
    public static bool IsAdminRole(this ClaimsPrincipal user) =>
        user.IsAuthor() && user.HasClaim(SharpQuillClaims.Role, SharpQuillClaims.RoleAdmin);

    /// <summary>The signed-in author's id. Only call behind the AdminUser or AdminRole policy.</summary>
    public static int GetAuthorId(this ClaimsPrincipal user) =>
        user.IsAuthor()
            ? GetSubject(user)
            : throw new InvalidOperationException("The caller is not an authenticated author");

    /// <summary>The signed-in website reader's id. Only call behind the WebsiteUser policy.</summary>
    public static int GetWebsiteUserId(this ClaimsPrincipal user) =>
        user.IsWebsiteUser()
            ? GetSubject(user)
            : throw new InvalidOperationException("The caller is not an authenticated website user");

    // The schemes reject tokens whose sub is not an integer, so this parse cannot fail for an authenticated caller
    private static int GetSubject(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(SharpQuillClaims.Subject)!);
}
