namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Reprezentuje słowo z definicją wprowadzone przez użytkownika
/// </summary>
public class CustomWordEntry
{
    public string Word { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public int Index { get; set; } // Numer porządkowy (1, 2, 3...)
}

