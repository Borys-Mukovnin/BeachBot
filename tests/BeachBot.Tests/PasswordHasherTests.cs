using BeachBot.Api.Auth;
using Xunit;

namespace BeachBot.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Sha256Hex_MatchesKnownVector()
    {
        // SHA-256("password") — the canonical test vector.
        Assert.Equal(
            "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
            PasswordHasher.Sha256Hex("password"));
    }

    [Fact]
    public void Sha256Hex_IsLowercaseHex_64Chars()
    {
        var digest = PasswordHasher.Sha256Hex("Sömething-with-Ümlauts!");
        Assert.Equal(64, digest.Length);
        Assert.DoesNotContain(digest, char.IsUpper);
    }
}
