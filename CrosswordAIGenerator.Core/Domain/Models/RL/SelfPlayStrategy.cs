namespace CrosswordAIGenerator.Core.Domain.Models.RL;

/// <summary>
/// Strategia self-play dla generowania gier RL
/// </summary>
public enum SelfPlayStrategy
{
    /// <summary>
    /// Losowe umieszczanie słów
    /// </summary>
    Random,
    
    /// <summary>
    /// Wybór akcji z najwyższą natychmiastową nagrodą (greedy)
    /// </summary>
    Greedy,
    
    /// <summary>
    /// Użycie modelu do wyboru akcji (dla przyszłości)
    /// </summary>
    PolicyBased
}
