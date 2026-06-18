using BeachBot.Api.Ddp;
using BeachBot.Api.Dtos;

namespace BeachBot.Api.Auth;

/// <summary>Implements <see cref="IAuthenticator"/> over a DDP connection.</summary>
public sealed class Authenticator : IAuthenticator
{
    private readonly IDdpConnection _connection;

    public Authenticator(IDdpConnection connection) => _connection = connection;

    public Task<AuthToken> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var digest = PasswordHasher.Sha256Hex(password);
        var @params = new object?[]
        {
            new { user = new { email }, password = new { digest, algorithm = "sha-256" } },
        };
        return RunLoginAsync(@params, cancellationToken);
    }

    public Task<AuthToken> ResumeAsync(string resumeToken, CancellationToken cancellationToken = default)
    {
        var @params = new object?[] { new { resume = resumeToken } };
        return RunLoginAsync(@params, cancellationToken);
    }

    private async Task<AuthToken> RunLoginAsync(object?[] @params, CancellationToken cancellationToken)
    {
        var result = await _connection.CallAsync<LoginResultDto>("login", @params, cancellationToken).ConfigureAwait(false);
        if (result?.Token is null)
            throw new DdpException("Login succeeded but no resume token was returned.");

        // The API always returns an expiry; fall back defensively just in case.
        return new AuthToken(result.Token, result.TokenExpires ?? DateTimeOffset.UtcNow.AddDays(90));
    }
}
