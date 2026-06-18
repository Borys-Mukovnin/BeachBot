namespace BeachBot.Core.Models;

/// <summary>A player who can be chosen as a registration partner.</summary>
public sealed class Player
{
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Club { get; init; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
