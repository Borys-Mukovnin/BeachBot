namespace BeachBot.Api.Ddp;

/// <summary>
/// Typed wrapper around a DDP <c>error</c> reply. The server returns German
/// <see cref="Reason"/> text (e.g. "registration not open yet", "already registered").
/// </summary>
public sealed class DdpException : Exception
{
    public int? ErrorCode { get; }
    public string? Reason { get; }
    public string? ErrorType { get; }

    public DdpException(string message, int? errorCode = null, string? reason = null, string? errorType = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Reason = reason;
        ErrorType = errorType;
    }
}
