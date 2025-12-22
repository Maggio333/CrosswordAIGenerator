using CrosswordAIGenerator.Core.Domain.Models.RL;

namespace CrosswordAIGenerator.Core.Domain.Services.RL;

/// <summary>
/// Adapter do konwersji między stanami/akcjami a formatem modelu
/// </summary>
public interface ICrosswordRLAdapter
{
    /// <summary>
    /// Konwertuje stan na prompt dla modelu
    /// </summary>
    string StateToPrompt(CrosswordRLState state);
    
    /// <summary>
    /// Parsuje odpowiedź modelu na akcję
    /// </summary>
    CrosswordRLAction? ParseAction(string modelResponse);
    
    /// <summary>
    /// Waliduje akcję względem stanu
    /// </summary>
    bool ValidateAction(CrosswordRLAction action, CrosswordRLState state);
}
