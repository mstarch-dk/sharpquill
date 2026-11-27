using System.Text.RegularExpressions;

namespace SharpQuill.Core.Authors;

/// <summary>
/// Input rules for author (admin account) credentials, shared by the admin API and the db-init first-run bootstrap.
/// Emails are stored lower-cased and compared case-insensitively, so one mailbox can hold only one account.
/// </summary>
public static partial class AuthorRules
{
    public const int MinPasswordLength = 12;

    public static string PasswordPolicyMessage =>
        $"Password must be at least {MinPasswordLength} characters and not only spaces";

    /// <summary>Trims and lower-cases an email address; the form authors are stored and looked up in.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>A plain local@domain.tld shape check, the same one the admin form applies.</summary>
    public static bool IsValidEmail(string email) => EmailShape().IsMatch(email.Trim());

    public static bool IsValidPassword(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= MinPasswordLength;

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailShape();
}
