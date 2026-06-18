using System.Text.Json;

namespace BeachBot.Api.Ddp;

/// <summary>
/// The seam over the Meteor DDP protocol: one method call = one request/reply,
/// correlated by id. A <c>connect</c> handshake runs once when the socket opens.
/// </summary>
public interface IDdpConnection : IAsyncDisposable
{
    bool IsConnected { get; }

    /// <summary>Opens (or re-opens) the socket and performs the DDP handshake.</summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Calls a DDP method and returns the raw <c>result</c> element (undefined if none). Throws <see cref="DdpException"/> on an error reply.</summary>
    Task<JsonElement> CallAsync(string method, object?[] @params, CancellationToken cancellationToken = default);

    /// <summary>Calls a DDP method and deserializes the <c>result</c> into <typeparamref name="T"/>.</summary>
    Task<T?> CallAsync<T>(string method, object?[] @params, CancellationToken cancellationToken = default);
}
