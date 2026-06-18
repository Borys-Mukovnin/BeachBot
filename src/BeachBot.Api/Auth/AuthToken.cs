namespace BeachBot.Api.Auth;

/// <summary>
/// A Meteor resume token plus its expiry. Stored for "remember me" so later
/// sessions can re-authenticate without the password.
/// </summary>
public sealed record AuthToken(string Token, DateTimeOffset Expires)
{
    public bool IsValid(DateTimeOffset now) => now < Expires;
}
