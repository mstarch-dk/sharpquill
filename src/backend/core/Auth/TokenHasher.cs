using System.Security.Cryptography;
using System.Text;

namespace SharpQuill.Core.Auth;

/// <summary>
/// Refresh and login tokens are stored only as SHA-256 hashes, so a database leak does not yield usable tokens.
/// The tokens are high-entropy random values, so a fast unsalted hash is sufficient.
/// </summary>
public static class TokenHasher
{
    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
