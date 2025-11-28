using System.Collections.Generic;

namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Wynik walidacji formatu CrossGrid
/// </summary>
public class CrossGridValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Details { get; set; } = new();
}

