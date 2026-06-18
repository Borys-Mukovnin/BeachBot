namespace BeachBot.Api.Auth;

/// <summary>Runs the Meteor <c>login</c> method, by password or by resume token.</summary>
public interface IAuthenticator
{
    Task<AuthToken> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthToken> ResumeAsync(string resumeToken, CancellationToken cancellationToken = default);
}
