using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SharpQuill.Api.Tests.Helpers;

public static class AuthHelper
{
    private const string TestSecret = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    private const string TestIssuer = "https://localhost:5000";
    private const string TestAudience = "https://localhost:5000";

    public static string GenerateTestToken(int authorId = 1, string email = "admin@example.com", string name = "Admin", string role = "admin")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, authorId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim("role", role),
            new Claim("user_type", "admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateWebsiteUserTestToken(int userId = 1, string email = "approved@example.com", string name = "Approved User")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim("user_type", "website"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience + "/website",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Builds a signed token with exactly the given claims and audience, for negative tests.</summary>
    public static string GenerateCustomToken(IEnumerable<Claim> claims, string audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient CreateAuthenticatedClient(HttpClient client, int authorId = 1, string role = "admin")
    {
        var token = GenerateTestToken(authorId, role: role);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient CreateWebsiteUserAuthenticatedClient(HttpClient client, int userId = 1, string email = "approved@example.com", string name = "Approved User")
    {
        var token = GenerateWebsiteUserTestToken(userId, email, name);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static void SetCertificateHeader(HttpClient client, string thumbprint)
    {
        client.DefaultRequestHeaders.Remove("X-Client-Cert-Fingerprint");
        client.DefaultRequestHeaders.Add("X-Client-Cert-Fingerprint", thumbprint);
    }
}
