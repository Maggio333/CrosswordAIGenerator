using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Implementacja serwisu do komunikacji z chatbotem Bielika przez HTTP API
/// </summary>
public class ChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ICursorLogger? _logger;
    private const int TimeoutSeconds = 300; // 5 minut dla generowania (streaming może trwać długo)

    public ChatbotService(string baseUrl = "http://localhost:5000", ICursorLogger? logger = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };
    }

    public async Task<string> SendMessageAsync(string prompt, ChatbotMode mode, CrosswordModel? crosswordModel = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt nie może być pusty", nameof(prompt));
        }

        try
        {
            var requestBody = new
            {
                prompt = prompt,
                mode = mode == ChatbotMode.General ? "general" : "crossword",
                model = mode == ChatbotMode.Crossword && crosswordModel.HasValue 
                    ? (crosswordModel.Value == CrosswordModel.Bielik ? "bielik" : "qwen")
                    : "bielik",  // domyślnie Bielik dla kompatybilności wstecznej
                max_tokens = 512,
                temperature = mode == ChatbotMode.General ? 0.7 : 0.5
            };

            _logger?.Debug($"Wysyłanie wiadomości do chatbota (tryb: {mode})");
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/chat",
                requestBody,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.Error($"Błąd HTTP {response.StatusCode}: {errorContent}");
                
                var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                if (errorJson.TryGetProperty("error", out var errorElement))
                {
                    throw new HttpRequestException($"Błąd serwera: {errorElement.GetString()}");
                }
                
                throw new HttpRequestException($"Błąd HTTP {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken);
            
            if (result == null || string.IsNullOrEmpty(result.Response))
            {
                throw new InvalidOperationException("Serwer zwrócił pustą odpowiedź");
            }

            _logger?.Debug($"Otrzymano odpowiedź z chatbota (długość: {result.Response.Length} znaków)");
            return result.Response;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.Warning("Anulowano żądanie do chatbota");
            throw new OperationCanceledException("Anulowano żądanie", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger?.Error("Timeout podczas komunikacji z chatbotem");
            throw new TimeoutException($"Timeout po {TimeoutSeconds} sekundach");
        }
        catch (HttpRequestException ex)
        {
            _logger?.Error($"Błąd HTTP podczas komunikacji z chatbotem: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Nieoczekiwany błąd podczas komunikacji z chatbotem: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<bool> IsServerRunningAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/health",
                cancellationToken
            );

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.Debug($"Serwer chatbota nie jest dostępny: {ex.Message}");
            return false;
        }
    }

    public async Task<string> LoadCrosswordAdapterAsync(CrosswordModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.Info($"Ładowanie adaptera Crossword (model: {model})...");
            
            var requestBody = new
            {
                model = model == CrosswordModel.Bielik ? "bielik" : "qwen"
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/models/load-crossword",
                requestBody,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.Error($"Błąd HTTP {response.StatusCode}: {errorContent}");
                throw new HttpRequestException($"Błąd serwera: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<LoadAdapterResponse>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                throw new InvalidOperationException("Serwer zwrócił pustą odpowiedź");
            }

            var message = result.Message ?? result.Status ?? "Nieznany status";
            _logger?.Info($"Adapter załadowany: {message}");
            return message;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.Warning("Anulowano żądanie ładowania adaptera");
            throw new OperationCanceledException("Anulowano żądanie", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger?.Error("Timeout podczas ładowania adaptera");
            throw new TimeoutException($"Timeout po {TimeoutSeconds} sekundach");
        }
        catch (HttpRequestException ex)
        {
            _logger?.Error($"Błąd HTTP podczas ładowania adaptera: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Nieoczekiwany błąd podczas ładowania adaptera: {ex.Message}", ex);
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(string prompt, ChatbotMode mode, CrosswordModel? crosswordModel = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt nie może być pusty", nameof(prompt));
        }

        var requestBody = new
        {
            prompt = prompt,
            mode = mode == ChatbotMode.General ? "general" : "crossword",
            model = mode == ChatbotMode.Crossword && crosswordModel.HasValue 
                ? (crosswordModel.Value == CrosswordModel.Bielik ? "bielik" : "qwen")
                : "bielik",  // domyślnie Bielik dla kompatybilności wstecznej
            max_tokens = 512,
            temperature = mode == ChatbotMode.General ? 0.7 : 0.5,
            stream = true
        };

        _logger?.Debug($"Wysyłanie wiadomości do chatbota z streamem (tryb: {mode})");
        
        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        // Przygotuj request poza try-catch
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        // Ważne dla SSE
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("text/event-stream");

        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.Error($"Błąd HTTP {response.StatusCode}: {errorContent}");
                throw new HttpRequestException($"Błąd serwera: {errorContent}");
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            reader = new StreamReader(stream, Encoding.UTF8);
            _logger?.Info($"✅ Połączenie streamingu nawiązane. Content-Type: {response.Content.Headers.ContentType?.MediaType}");
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.Warning("Anulowano żądanie streamingu do chatbota");
            yield break;
        }
        catch (TaskCanceledException)
        {
            _logger?.Error("Timeout podczas streamingu z chatbotem");
            throw new TimeoutException($"Timeout po {TimeoutSeconds} sekundach");
        }
        catch (HttpRequestException ex)
        {
            _logger?.Error($"Błąd HTTP podczas streamingu z chatbotem: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Nieoczekiwany błąd podczas streamingu z chatbotem: {ex.Message}", ex);
            throw;
        }

        // Stream chunks poza try-catch (żeby uniknąć problemu z yield)
        if (reader != null)
        {
            _logger?.Info("🔄 Rozpoczynam czytanie chunks ze streamu...");
            int chunkCount = 0;
            await foreach (var chunk in ReadStreamChunks(reader, cancellationToken))
            {
                chunkCount++;
                if (chunkCount <= 5 || chunkCount % 20 == 0) // Loguj pierwsze 5 i potem co 20
                {
                    _logger?.Debug($"📦 Chunk #{chunkCount}: '{chunk}' (długość: {chunk.Length})");
                }
                yield return chunk;
            }
            _logger?.Info($"✅ Zakończono czytanie streamu. Otrzymano {chunkCount} chunków");
        }
        else
        {
            _logger?.Error("❌ Reader jest null - nie można czytać streamu!");
        }

        // Cleanup
        reader?.Dispose();
        stream?.Dispose();
        response?.Dispose();
    }

    private async IAsyncEnumerable<string> ReadStreamChunks(StreamReader reader, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int lineCount = 0;
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            lineCount++;
            
            if (line == null)
            {
                _logger?.Debug($"📄 Linia {lineCount}: null (koniec streamu)");
                break;
            }
            
            if (lineCount <= 3 || lineCount % 50 == 0) // Loguj pierwsze 3 linie i potem co 50
            {
                _logger?.Debug($"📄 Linia {lineCount}: '{line.Substring(0, Math.Min(100, line.Length))}'");
            }
            
            if (line.Length == 0) continue; // keep-alive/puste
            if (line.StartsWith(":")) continue; // komentarze SSE
            if (!line.StartsWith("data:"))
            {
                _logger?.Warning($"⚠️  Linia nie zaczyna się od 'data:': '{line.Substring(0, Math.Min(50, line.Length))}'");
                continue;
            }

            var payload = line["data:".Length..].Trim();

            if (payload == "[DONE]" || payload.Contains("\"done\":true"))
            {
                _logger?.Info("🏁 Otrzymano sygnał [DONE] - koniec streamingu");
                yield break;
            }

            // Parsuj JSON poza try-catch (żeby yield mógł działać)
            string? chunk = null;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                // Sprawdź format odpowiedzi - może być {"chunk": "..."} lub {"done": true}
                if (root.TryGetProperty("chunk", out var chunkElement) && chunkElement.ValueKind == JsonValueKind.String)
                {
                    chunk = chunkElement.GetString();
                }
                else
                {
                    _logger?.Warning($"⚠️  JSON nie zawiera 'chunk': {payload.Substring(0, Math.Min(100, payload.Length))}");
                }
            }
            catch (JsonException ex)
            {
                _logger?.Warning($"❌ Błąd parsowania JSON chunk: {ex.Message}. Payload: {payload.Substring(0, Math.Min(100, payload.Length))}");
                continue;
            }

            // Yield poza try-catch
            if (!string.IsNullOrEmpty(chunk))
            {
                yield return chunk;
            }
            else if (chunk == null)
            {
                _logger?.Warning($"⚠️  Chunk jest null dla payload: {payload.Substring(0, Math.Min(50, payload.Length))}");
            }
        }
        
        _logger?.Info($"📊 Przeczytano {lineCount} linii ze streamu");
    }
    
    /// <summary>
    /// Klasa pomocnicza do deserializacji odpowiedzi z API
    /// </summary>
    private class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Klasa pomocnicza do deserializacji odpowiedzi z endpointu ładowania adaptera
    /// </summary>
    public async Task<string> UnloadCrosswordAdapterAsync(CrosswordModel? model, CancellationToken cancellationToken = default)
    {
        try
        {
            var modelName = model == null ? null : (model == CrosswordModel.Bielik ? "bielik" : "qwen");
            _logger?.Info($"Zwalnianie adaptera Crossword (model: {modelName ?? "wszystkie"})...");
            
            var requestBody = new
            {
                model = modelName
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/models/unload-crossword",
                requestBody,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.Error($"Błąd HTTP {response.StatusCode}: {errorContent}");
                throw new HttpRequestException($"Błąd serwera: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<LoadAdapterResponse>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                throw new InvalidOperationException("Serwer zwrócił pustą odpowiedź");
            }

            var message = result.Message ?? result.Status ?? "Nieznany status";
            _logger?.Info($"Adapter zwolniony: {message}");
            return message;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.Warning("Anulowano żądanie zwolnienia adaptera");
            throw new OperationCanceledException("Anulowano żądanie", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger?.Error("Timeout podczas zwalniania adaptera");
            throw new TimeoutException($"Timeout po {TimeoutSeconds} sekundach");
        }
        catch (HttpRequestException ex)
        {
            _logger?.Error($"Błąd HTTP podczas zwalniania adaptera: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Nieoczekiwany błąd podczas zwalniania adaptera: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<ModelsStatus> GetModelsStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.Debug("Pobieranie statusu modeli...");
            
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/models/status",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.Error($"Błąd HTTP {response.StatusCode}: {errorContent}");
                throw new HttpRequestException($"Błąd serwera: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<ModelsStatusResponse>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                throw new InvalidOperationException("Serwer zwrócił pustą odpowiedź");
            }

            return new ModelsStatus
            {
                GgufLoaded = result.GgufLoaded ?? false,
                BielikLoaded = result.CrosswordAdapters?.Bielik?.Loaded ?? false,
                QwenLoaded = result.CrosswordAdapters?.Qwen?.Loaded ?? false,
                VramAllocatedGb = result.Vram?.AllocatedGb ?? 0,
                VramTotalGb = result.Vram?.TotalGb ?? 0,
                VramFreeGb = result.Vram?.FreeGb ?? 0,
                VramUsagePercent = result.Vram?.UsagePercent ?? 0
            };
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.Warning("Anulowano żądanie statusu modeli");
            throw new OperationCanceledException("Anulowano żądanie", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger?.Error("Timeout podczas pobierania statusu modeli");
            throw new TimeoutException($"Timeout po {TimeoutSeconds} sekundach");
        }
        catch (HttpRequestException ex)
        {
            _logger?.Error($"Błąd HTTP podczas pobierania statusu modeli: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Nieoczekiwany błąd podczas pobierania statusu modeli: {ex.Message}", ex);
            throw;
        }
    }

    private class ModelsStatusResponse
    {
        public bool? GgufLoaded { get; set; }
        public CrosswordAdaptersResponse? CrosswordAdapters { get; set; }
        public VramInfoResponse? Vram { get; set; }
    }
    
    private class CrosswordAdaptersResponse
    {
        public AdapterInfoResponse? Bielik { get; set; }
        public AdapterInfoResponse? Qwen { get; set; }
    }
    
    private class AdapterInfoResponse
    {
        public bool? Loaded { get; set; }
        public string? Model { get; set; }
        public string? AdapterPath { get; set; }
    }
    
    private class VramInfoResponse
    {
        public double? AllocatedGb { get; set; }
        public double? TotalGb { get; set; }
        public double? FreeGb { get; set; }
        public double? UsagePercent { get; set; }
    }

    private class LoadAdapterResponse
    {
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}

