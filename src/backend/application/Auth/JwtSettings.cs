namespace SharpQuill.Application.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Audience of admin (author) access tokens.</summary>
    public string AdminAudience => Audience;

    /// <summary>Audience of website-user access tokens; distinct so the two token types cannot be swapped.</summary>
    public string WebsiteAudience => $"{Audience}/website";

    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public const int MinimumSecretLength = 32;

    // Values shipped in appsettings.json and the .env examples; anyone can read them in the repository
    private static readonly string[] PlaceholderSecrets =
    [
        "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
        "CHANGE_ME"
    ];

    /// <summary>
    /// Returns a description of why the signing secret is unsafe, or null if it is acceptable.
    /// </summary>
    public string? ValidateSecret()
    {
        if (string.IsNullOrWhiteSpace(Secret))
            return "the secret is not set";
        if (Secret.Length < MinimumSecretLength)
            return $"the secret is shorter than {MinimumSecretLength} characters";
        if (PlaceholderSecrets.Any(p => Secret.Contains(p, StringComparison.OrdinalIgnoreCase)))
            return "the secret is a placeholder value from the repository";
        return null;
    }
}
