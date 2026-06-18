using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BeachBot.Api.Dtos;

namespace BeachBot.Api.Ddp;

/// <summary>
/// DDP-over-WebSocket transport for the Meteor backend. Sends the one-time
/// <c>connect</c> handshake, then turns each <c>method</c> call into a
/// request/reply correlated by id. A background loop reads incoming frames,
/// answers heartbeats, and completes pending calls.
/// </summary>
public sealed class DdpConnection : IDdpConnection
{
    private readonly DdpOptions _options;
    private readonly JsonSerializerOptions _json = BeachBotJson.Options;

    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pending = new();

    private ClientWebSocket? _socket;
    private CancellationTokenSource? _loopCts;
    private Task? _receiveLoop;
    private int _idCounter;
    private bool _disposed;

    public DdpConnection(DdpOptions options) => _options = options;

    public bool IsConnected => _socket?.State == WebSocketState.Open;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected)
                return;

            Teardown(new IOException("Reconnecting."));

            var socket = new ClientWebSocket();
            await socket.ConnectAsync(_options.Uri, cancellationToken).ConfigureAwait(false);
            _socket = socket;

            _loopCts = new CancellationTokenSource();
            var handshake = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _receiveLoop = Task.Run(() => ReceiveLoopAsync(socket, _loopCts.Token, handshake));

            // DDP handshake: announce protocol version and wait for "connected".
            await SendAsync(socket, new { msg = "connect", version = "1", support = new[] { "1", "pre2", "pre1" } }, cancellationToken)
                .ConfigureAwait(false);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.ConnectTimeout);
            await using (timeoutCts.Token.Register(() => handshake.TrySetCanceled()))
            {
                await handshake.Task.ConfigureAwait(false);
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task<JsonElement> CallAsync(string method, object?[] @params, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var socket = _socket;
        if (socket is null || socket.State != WebSocketState.Open)
            throw new InvalidOperationException("DDP connection is not open. Call ConnectAsync first.");

        var id = Interlocked.Increment(ref _idCounter).ToString();
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;

        try
        {
            await SendAsync(socket, new Dictionary<string, object?>
            {
                ["msg"] = "method",
                ["method"] = method,
                ["params"] = @params,
                ["id"] = id,
            }, cancellationToken).ConfigureAwait(false);

            await using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    public async Task<T?> CallAsync<T>(string method, object?[] @params, CancellationToken cancellationToken = default)
    {
        var result = await CallAsync(method, @params, cancellationToken).ConfigureAwait(false);
        if (result.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return default;
        return result.Deserialize<T>(_json);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken ct, TaskCompletionSource<bool> handshake)
    {
        var buffer = new byte[8192];
        var message = new MemoryStream();
        try
        {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                message.SetLength(0);
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                        throw new IOException("Server closed the connection.");
                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                Dispatch(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length), socket, ct, handshake);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            handshake.TrySetException(ex);
            FailAllPending(ex);
        }
    }

    private void Dispatch(string text, ClientWebSocket socket, CancellationToken ct, TaskCompletionSource<bool> handshake)
    {
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        if (!root.TryGetProperty("msg", out var msgProp))
            return;

        switch (msgProp.GetString())
        {
            case "connected":
                handshake.TrySetResult(true);
                break;

            case "failed":
                handshake.TrySetException(new IOException("DDP handshake failed (unsupported protocol version)."));
                break;

            case "ping":
                // Reply with pong, echoing the id when present.
                var pong = root.TryGetProperty("id", out var pingId)
                    ? new Dictionary<string, object?> { ["msg"] = "pong", ["id"] = pingId.GetString() }
                    : new Dictionary<string, object?> { ["msg"] = "pong" };
                _ = SendAsync(socket, pong, ct);
                break;

            case "result":
                if (!root.TryGetProperty("id", out var idProp) || idProp.GetString() is not { } id)
                    break;
                if (!_pending.TryGetValue(id, out var tcs))
                    break;

                if (root.TryGetProperty("error", out var error))
                    tcs.TrySetException(ToDdpException(error));
                else if (root.TryGetProperty("result", out var resultEl))
                    tcs.TrySetResult(resultEl.Clone());
                else
                    tcs.TrySetResult(default); // success with no payload
                break;

            // "updated", "ready", "nosub", "added", "changed", "removed", "pong" — not used here.
        }
    }

    private static DdpException ToDdpException(JsonElement error)
    {
        int? code = error.TryGetProperty("error", out var c)
            ? (c.ValueKind == JsonValueKind.Number ? c.GetInt32() : null)
            : null;
        string? reason = error.TryGetProperty("reason", out var r) ? r.GetString() : null;
        string? type = error.TryGetProperty("errorType", out var t) ? t.GetString() : null;
        string? msg = error.TryGetProperty("message", out var m) ? m.GetString() : null;
        return new DdpException(reason ?? msg ?? "DDP method failed.", code, reason, type);
    }

    private async Task SendAsync(ClientWebSocket socket, object payload, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, _json);
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void FailAllPending(Exception ex)
    {
        foreach (var key in _pending.Keys)
        {
            if (_pending.TryRemove(key, out var tcs))
                tcs.TrySetException(ex);
        }
    }

    private void Teardown(Exception reason)
    {
        try { _loopCts?.Cancel(); } catch { /* ignore */ }
        FailAllPending(reason);
        try { _socket?.Dispose(); } catch { /* ignore */ }
        _socket = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        var socket = _socket;
        _loopCts?.Cancel();

        if (socket is { State: WebSocketState.Open })
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        FailAllPending(new ObjectDisposedException(nameof(DdpConnection)));
        socket?.Dispose();
        _connectLock.Dispose();
        _sendLock.Dispose();
    }
}
