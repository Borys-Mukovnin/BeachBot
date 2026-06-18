using BeachBot.Api.Auth;
using Xunit;

namespace BeachBot.Tests;

public class AuthTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsValid_True_WhenExpiryInFuture()
        => Assert.True(new AuthToken("t", Now.AddHours(1)).IsValid(Now));

    [Fact]
    public void IsValid_False_WhenExpired()
        => Assert.False(new AuthToken("t", Now.AddHours(-1)).IsValid(Now));
}
