namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Tryb chatbota Bielika
/// </summary>
public enum ChatbotMode
{
    /// <summary>
    /// Tryb ogólny - używa modelu GGUF
    /// </summary>
    General,
    
    /// <summary>
    /// Tryb krzyżówek - używa modelu Transformers + QLoRA adapter
    /// </summary>
    Crossword
}

