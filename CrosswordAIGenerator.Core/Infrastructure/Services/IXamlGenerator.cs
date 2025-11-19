using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Interfejs dla generatora XAML
/// </summary>
public interface IXamlGenerator
{
    /// <summary>
    /// Generuje XAML dla siatki krzyżówki
    /// </summary>
    string GenerateXaml(CrosswordGrid grid, int width = 500, int height = 500, Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null);
}

