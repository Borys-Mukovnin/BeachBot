namespace BeachBot.Api.Ddp;

/// <summary>Connection settings for the DDP/WebSocket transport.</summary>
public sealed class DdpOptions
{
    /// <summary>
    /// The Meteor websocket endpoint. This is the real DDP backend (verified to
    /// return HTTP 101). Note: www.beachvolleyball.de is a different site that 301-
    /// redirects to a non-Meteor host, so it 404s the upgrade — use .nrw. Override if needed.
    /// </summary>
    public Uri Uri { get; set; } = new("wss://www.beachvolleyball.nrw/websocket");

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
