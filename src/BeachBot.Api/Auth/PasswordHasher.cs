using System.Security.Cryptography;
using System.Text;

namespace BeachBot.Api.Auth;

/// <summary>
/// Meteor hashes the password client-side before sending it. This produces the
/// lowercase hex SHA-256 digest the <c>login</c> method expects.
/// </summary>
public static class PasswordHasher
{
    public static string Sha256Hex(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexStringLower(hash);
    }
}
