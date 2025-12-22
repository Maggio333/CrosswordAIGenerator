using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs serwisu do komunikacji z chatbotem Bielika
/// </summary>
public interface IChatbotService
{
    /// <summary>
    /// Wysyła wiadomość do chatbota i zwraca odpowiedź
    /// </summary>
    /// <param name="prompt">Prompt użytkownika</param>
    /// <param name="mode">Tryb chatbota (General lub Crossword)</param>
    /// <param name="crosswordModel">Model do użycia w trybie Crossword (opcjonalne)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Odpowiedź modelu</returns>
    Task<string> SendMessageAsync(string prompt, ChatbotMode mode, CrosswordModel? crosswordModel = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sprawdza czy serwer chatbota jest uruchomiony
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>True jeśli serwer jest dostępny</returns>
    Task<bool> IsServerRunningAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ładuje adapter Crossword (QLoRA) na serwerze
    /// </summary>
    /// <param name="model">Model do załadowania (Bielik lub Qwen)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Komunikat o statusie ładowania</returns>
    Task<string> LoadCrosswordAdapterAsync(CrosswordModel model, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Zwalnia adapter Crossword (QLoRA) z pamięci
    /// </summary>
    /// <param name="model">Model do zwolnienia (Bielik, Qwen lub null dla wszystkich)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Komunikat o statusie zwolnienia</returns>
    Task<string> UnloadCrosswordAdapterAsync(CrosswordModel? model, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pobiera szczegółowy status załadowanych modeli
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Status modeli i VRAM</returns>
    Task<ModelsStatus> GetModelsStatusAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Wysyła wiadomość do chatbota i zwraca strumień odpowiedzi (streaming)
    /// </summary>
    /// <param name="prompt">Prompt użytkownika</param>
    /// <param name="mode">Tryb chatbota (General lub Crossword)</param>
    /// <param name="crosswordModel">Model do użycia w trybie Crossword (opcjonalne)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Strumień chunków odpowiedzi</returns>
    IAsyncEnumerable<string> StreamMessageAsync(string prompt, ChatbotMode mode, CrosswordModel? crosswordModel = null, CancellationToken cancellationToken = default);
}

