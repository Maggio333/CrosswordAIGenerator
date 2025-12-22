using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.WPF.Infrastructure;
using CrosswordAIGenerator.WPF.Presentation.Views;

namespace CrosswordAIGenerator.WPF.Presentation.ViewModels;

/// <summary>
/// ViewModel dla chatbota Bielika
/// </summary>
public partial class ChatbotViewModel : ObservableObject
{
    /// <summary>
    /// Stała ścieżka do katalogu z obrazami chatbota
    /// </summary>
    private static readonly string ChatbotImagesDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, 
        "images", 
        "chatbot"
    );
    
    private readonly IChatbotService _chatbotService;
    private readonly ICrossGridGenerator _crossGridGenerator;
    private readonly IXamlGenerator _xamlGenerator;
    private readonly IScreenshotService _screenshotService;
    private readonly ICursorLogger? _logger;
    
    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();
    
    [ObservableProperty]
    private string _currentPrompt = string.Empty;
    
    [ObservableProperty]
    private ChatbotMode _selectedMode = ChatbotMode.General;
    
    [ObservableProperty]
    private CrosswordModel _selectedCrosswordModel = CrosswordModel.Bielik;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _isServerRunning;
    
    [ObservableProperty]
    private string _serverStatusMessage = "Sprawdzanie statusu serwera...";
    
    private CancellationTokenSource? _currentCancellation;

    public ChatbotViewModel(
        IChatbotService chatbotService,
        ICrossGridGenerator crossGridGenerator,
        IXamlGenerator xamlGenerator,
        IScreenshotService screenshotService,
        ICursorLogger? logger = null)
    {
        _chatbotService = chatbotService ?? throw new ArgumentNullException(nameof(chatbotService));
        _crossGridGenerator = crossGridGenerator ?? throw new ArgumentNullException(nameof(crossGridGenerator));
        _xamlGenerator = xamlGenerator ?? throw new ArgumentNullException(nameof(xamlGenerator));
        _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        _logger = logger;
        
        // Sprawdź status serwera przy starcie
        _ = CheckServerStatusAsync();
    }

    /// <summary>
    /// Komenda do wysyłania wiadomości
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPrompt))
        {
            return;
        }

        var prompt = CurrentPrompt.Trim();
        CurrentPrompt = string.Empty; // Wyczyść input
        
        // Dodaj wiadomość użytkownika
        var userMessage = new ChatMessage(prompt, isUser: true, SelectedMode);
        Messages.Add(userMessage);
        
        IsLoading = true;
        ServerStatusMessage = "Generowanie odpowiedzi...";
        
        _currentCancellation = new CancellationTokenSource();
        
        try
        {
            var logMsg = $"📤 Rozpoczynam SendMessageAsync (tryb: {SelectedMode}, prompt: '{prompt.Substring(0, Math.Min(50, prompt.Length))}...')";
            _logger?.Info(logMsg);
            Debug.WriteLine($"[ChatbotViewModel] {logMsg}");
            
            string fullResponse;
            ChatMessage botMessage;
            
            // Użyj streamingu dla trybu Crossword, zwykłej metody dla General
            if (SelectedMode == ChatbotMode.Crossword)
            {
                // Streaming dla Crossword
                _logger?.Info("🔄 Używam streamingu dla trybu Crossword");
                botMessage = new ChatMessage(string.Empty, isUser: false, SelectedMode);
                Messages.Add(botMessage);
                
                ServerStatusMessage = "Generowanie odpowiedzi...";
                
                fullResponse = string.Empty;
                int chunkCount = 0;
                await foreach (var chunk in _chatbotService.StreamMessageAsync(
                    prompt,
                    SelectedMode,
                    SelectedMode == ChatbotMode.Crossword ? SelectedCrosswordModel : null,
                    _currentCancellation.Token))
                {
                    chunkCount++;
                    // Aktualizuj odpowiedź chunk po chunk
                    fullResponse += chunk;
                    botMessage.Content = fullResponse;
                    
                    if (chunkCount % 10 == 0) // Loguj co 10 chunków
                    {
                        _logger?.Debug($"📦 Otrzymano {chunkCount} chunków, długość odpowiedzi: {fullResponse.Length} znaków");
                    }
                    
                    // Wymuś odświeżenie UI (podobnie jak w ChatElioraSystem)
                    await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                }
                
                _logger?.Info($"✅ Streaming zakończony. Otrzymano {chunkCount} chunków, całkowita długość: {fullResponse.Length} znaków");
                _logger?.Info($"📄 Pełna odpowiedź (ostatnie 500 znaków): {fullResponse.Substring(Math.Max(0, fullResponse.Length - 500))}");
            }
            else
            {
                // Zwykła metoda dla General (nie obsługuje streamingu)
                _logger?.Info("🔄 Używam zwykłej metody dla trybu General");
                ServerStatusMessage = "Generowanie odpowiedzi...";
                
                fullResponse = await _chatbotService.SendMessageAsync(
                    prompt,
                    SelectedMode,
                    SelectedMode == ChatbotMode.Crossword ? SelectedCrosswordModel : null,
                    _currentCancellation.Token
                );
                
                _logger?.Info($"✅ Otrzymano odpowiedź (długość: {fullResponse.Length} znaków)");
                
                botMessage = new ChatMessage(fullResponse, isUser: false, SelectedMode);
                Messages.Add(botMessage);
            }
            
            // WAŻNE: Sprawdź Grid PO zakończeniu streamingu (na głównym wątku)
            var checkMsg = $"🔍 [PO STREAMINGU] Sprawdzam czy odpowiedź zawiera Grid (długość: {fullResponse?.Length ?? 0} znaków)...";
            _logger?.Info(checkMsg);
            Debug.WriteLine($"[ChatbotViewModel] {checkMsg}");
            
            if (fullResponse != null && fullResponse.Length > 0)
            {
                var first200 = fullResponse.Substring(0, Math.Min(200, fullResponse.Length));
                var last200 = fullResponse.Substring(Math.Max(0, fullResponse.Length - 200));
                _logger?.Info($"📄 [PO STREAMINGU] Pierwsze 200 znaków: {first200}");
                _logger?.Info($"📄 [PO STREAMINGU] Ostatnie 200 znaków: {last200}");
                Debug.WriteLine($"[ChatbotViewModel] Pierwsze 200: {first200}");
                Debug.WriteLine($"[ChatbotViewModel] Ostatnie 200: {last200}");
            }
            else
            {
                _logger?.Warning("❌ fullResponse jest null lub pusty!");
                Debug.WriteLine("[ChatbotViewModel] ❌ fullResponse jest null lub pusty!");
            }
            
            // Sprawdź Grid na osobnym wątku, żeby nie blokować UI
            var hasGrid = await Task.Run(() => ContainsGrid(fullResponse ?? string.Empty));
            var gridResultMsg = $"🔍 Wynik ContainsGrid: {hasGrid}";
            _logger?.Info(gridResultMsg);
            Debug.WriteLine($"[ChatbotViewModel] {gridResultMsg}");
            
            if (hasGrid)
            {
                _logger?.Info("✅ Wykryto Grid w odpowiedzi! Rozpoczynam przetwarzanie...");
                botMessage.HasGrid = true;
                ServerStatusMessage = "Generowanie podglądu krzyżówki...";
                
                try
                {
                    _logger?.Info("🔄 Wywołuję ProcessGridResponseAsync...");
                    // Przenieś przetwarzanie Grid na osobny wątek (fire-and-forget dla UI)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessGridResponseAsync(botMessage, fullResponse);
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                ServerStatusMessage = "Gotowe - podgląd wygenerowany";
                            });
                            _logger?.Info($"✅ Przetwarzanie Grid zakończone. ImagePath: {botMessage.ImagePath ?? "NULL"}");
                        }
                        catch (Exception ex)
                        {
                            _logger?.Error($"❌ Błąd podczas przetwarzania Grid: {ex.Message}", ex);
                            _logger?.Error($"Stack trace: {ex.StackTrace}");
                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                ServerStatusMessage = "Gotowe (błąd generowania podglądu)";
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger?.Error($"❌ Błąd podczas przetwarzania Grid: {ex.Message}", ex);
                    _logger?.Error($"Stack trace: {ex.StackTrace}");
                    ServerStatusMessage = "Gotowe (błąd generowania podglądu)";
                    // Kontynuuj - wyświetl wiadomość bez obrazu
                }
            }
            else
            {
                _logger?.Info("ℹ️  Odpowiedź nie zawiera Grid");
                ServerStatusMessage = "Gotowe";
            }
            
            // Wymuś odświeżenie UI dla ImagePath (jeśli został ustawiony)
            if (!string.IsNullOrEmpty(botMessage.ImagePath))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Znajdź wiadomość w kolekcji i wymuś odświeżenie
                    var index = Messages.IndexOf(botMessage);
                    if (index >= 0)
                    {
                        Messages[index] = botMessage; // Wymuś odświeżenie przez zastąpienie
                    }
                }, DispatcherPriority.Render);
            }
            
            _logger?.Debug($"Otrzymano odpowiedź z chatbota (tryb: {SelectedMode}, długość: {fullResponse.Length}, hasGrid: {botMessage.HasGrid})");
        }
        catch (OperationCanceledException)
        {
            ServerStatusMessage = "Anulowano";
            _logger?.Info("Anulowano generowanie odpowiedzi");
        }
        catch (TimeoutException ex)
        {
            ServerStatusMessage = "Timeout - sprawdź czy serwer działa";
            var errorMessage = new ChatMessage(
                $"❌ Błąd: {ex.Message}",
                isUser: false,
                SelectedMode
            );
            Messages.Add(errorMessage);
            _logger?.Error($"Timeout podczas generowania odpowiedzi: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            ServerStatusMessage = "Błąd - sprawdź logi";
            var errorMessage = new ChatMessage(
                $"❌ Błąd: {ex.Message}",
                isUser: false,
                SelectedMode
            );
            Messages.Add(errorMessage);
            _logger?.Error($"Błąd podczas generowania odpowiedzi: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
            _currentCancellation?.Dispose();
            _currentCancellation = null;
        }
    }

    private bool CanSendMessage()
    {
        return !IsLoading && 
               !string.IsNullOrWhiteSpace(CurrentPrompt) &&
               IsServerRunning;
    }

    /// <summary>
    /// Komenda do czyszczenia historii rozmowy
    /// </summary>
    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
        ServerStatusMessage = "Historia wyczyszczona";
        _logger?.Debug("Wyczyszczono historię rozmowy");
    }

    /// <summary>
    /// Komenda do sprawdzania statusu serwera
    /// </summary>
    [RelayCommand]
    private async Task CheckServerStatusAsync()
    {
        ServerStatusMessage = "Sprawdzanie statusu serwera...";
        
        try
        {
            IsServerRunning = await _chatbotService.IsServerRunningAsync();
            
            if (IsServerRunning)
            {
                ServerStatusMessage = "✅ Serwer działa";
            }
            else
            {
                ServerStatusMessage = "❌ Serwer nie odpowiada - uruchom chatbot_server.py";
            }
        }
        catch (Exception ex)
        {
            IsServerRunning = false;
            ServerStatusMessage = "❌ Błąd sprawdzania statusu";
            _logger?.Error($"Błąd podczas sprawdzania statusu serwera: {ex.Message}", ex);
        }
        
        // Odśwież możliwość wysłania wiadomości
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Komenda do ładowania adaptera Crossword
    /// </summary>
    [RelayCommand]
    private async Task LoadCrosswordAdapterAsync()
    {
        ServerStatusMessage = $"Ładowanie adaptera Crossword ({SelectedCrosswordModel})...";
        _logger?.Info($"🔄 Rozpoczynam ładowanie adaptera Crossword (model: {SelectedCrosswordModel})...");
        
        try
        {
            var message = await _chatbotService.LoadCrosswordAdapterAsync(SelectedCrosswordModel);
            ServerStatusMessage = $"✅ {message}";
            _logger?.Info($"✅ Adapter załadowany: {message}");
        }
        catch (Exception ex)
        {
            ServerStatusMessage = $"❌ Błąd ładowania adaptera: {ex.Message}";
            _logger?.Error($"❌ Błąd podczas ładowania adaptera: {ex.Message}", ex);
        }
    }
    
    partial void OnSelectedCrosswordModelChanged(CrosswordModel value)
    {
        _logger?.Debug($"Zmieniono model Crossword na: {value}");
        
        // Jeśli jesteśmy w trybie Crossword i serwer działa, przeładuj adapter
        if (SelectedMode == ChatbotMode.Crossword && IsServerRunning)
        {
            _logger?.Info($"🔄 Przełączono model na {value} - przeładowywanie adaptera...");
            _ = LoadCrosswordAdapterAsync();
        }
    }

    /// <summary>
    /// Komenda do anulowania generowania
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelGeneration()
    {
        _currentCancellation?.Cancel();
        IsLoading = false;
        ServerStatusMessage = "Anulowano";
    }

    private bool CanCancel()
    {
        return IsLoading;
    }

    /// <summary>
    /// Aktualizuje możliwość wysłania wiadomości gdy zmienia się prompt lub tryb
    /// </summary>
    partial void OnCurrentPromptChanged(string value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedModeChanged(ChatbotMode value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
        _logger?.Debug($"Zmieniono tryb chatbota na: {value}");
        
        // Automatycznie załaduj adapter gdy przełączamy na tryb Crossword
        if (value == ChatbotMode.Crossword && IsServerRunning)
        {
            _logger?.Info($"🔄 Automatyczne ładowanie adaptera Crossword (model: {SelectedCrosswordModel})...");
            _ = LoadCrosswordAdapterAsync(); // Uruchom asynchronicznie bez czekania
        }
    }

    partial void OnIsLoadingChanged(bool value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
        CancelGenerationCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsServerRunningChanged(bool value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Sprawdza czy odpowiedź zawiera format Grid
    /// </summary>
    private bool ContainsGrid(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            _logger?.Debug("ContainsGrid: response jest null lub pusty");
            return false;
        }

        // Szukaj wzorca #Grid lub # GRID (case-insensitive)
        // Może być też w różnych formatach: #Grid, # GRID, #Grid:, #GRID:, etc.
        var pattern = @"#\s*GRID\s*:?";
        var matches = Regex.IsMatch(response, pattern, RegexOptions.IgnoreCase);
        
        _logger?.Debug($"ContainsGrid: sprawdzam wzorzec '{pattern}' - wynik: {matches}");
        if (matches)
        {
            var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
            _logger?.Debug($"ContainsGrid: znaleziono match na pozycji {match.Index}: '{match.Value}'");
        }
        
        return matches;
    }

    /// <summary>
    /// Wyodrębnia sekcję Grid z odpowiedzi - bierze OSTATNIE wystąpienie # Grid
    /// </summary>
    private string ExtractGridSection(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            _logger?.Warning("ExtractGridSection: response jest null lub pusty");
            return string.Empty;
        }

        _logger?.Debug($"ExtractGridSection: szukam sekcji Grid w odpowiedzi (długość: {response.Length} znaków)");

        // Znajdź WSZYSTKIE wystąpienia #Grid lub # GRID
        var matches = Regex.Matches(response, @"#\s*GRID\s*:?", RegexOptions.IgnoreCase);
        _logger?.Info($"🔍 Znaleziono {matches.Count} wystąpień '# Grid' w odpowiedzi");
        
        if (matches.Count == 0)
        {
            _logger?.Warning("❌ Nie znaleziono żadnego wystąpienia '# Grid'");
            return string.Empty;
        }

        // Weź OSTATNIE wystąpienie (najważniejsze - końcowa odpowiedź LLM)
        var lastMatch = matches[matches.Count - 1];
        var startIndex = lastMatch.Index;
        _logger?.Info($"✅ Używam ostatniego wystąpienia '# Grid' na pozycji {startIndex}");
        
        // Wyodrębnij tekst od ostatniego # Grid do końca (lub do następnego #)
        var remainingText = response.Substring(startIndex);
        
        // Znajdź następne # (jeśli istnieje) - to będzie koniec sekcji Grid
        var nextHashIndex = remainingText.IndexOf("#", 1, StringComparison.OrdinalIgnoreCase);
        string gridText;
        
        if (nextHashIndex > 0)
        {
            // Jest kolejne # - weź tekst do tego miejsca
            gridText = remainingText.Substring(0, nextHashIndex).Trim();
        }
        else
        {
            // Nie ma kolejnego # - weź cały tekst do końca
            gridText = remainingText.Trim();
        }
        
        _logger?.Info($"✅ Wyodrębniono sekcję Grid (długość: {gridText.Length} znaków)");
        _logger?.Debug($"📄 Pierwsze 300 znaków Grid: {gridText.Substring(0, Math.Min(300, gridText.Length))}");
        _logger?.Debug($"📄 Ostatnie 200 znaków Grid: {gridText.Substring(Math.Max(0, gridText.Length - 200))}");
        
        return gridText;
    }

    /// <summary>
    /// Przetwarza odpowiedź zawierającą Grid: konwertuje do XAML i generuje screenshot
    /// </summary>
    private async Task ProcessGridResponseAsync(ChatMessage message, string response)
    {
        _logger?.Info("🔄 Rozpoczynam przetwarzanie Grid do obrazu...");
        try
        {
            // Wyodrębnij sekcję Grid
            var gridText = ExtractGridSection(response);
            if (string.IsNullOrWhiteSpace(gridText))
            {
                _logger?.Warning("❌ Nie udało się wyodrębnić sekcji Grid z odpowiedzi");
                return;
            }

            _logger?.Info($"✅ Wyodrębniono Grid (długość: {gridText.Length} znaków)");

            // Normalizuj tekst - zamień escape sequences na rzeczywiste znaki nowej linii
            var normalizedText = gridText
                .Replace("\\r\\n", "\r\n")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r");

            // Konwertuj Grid do XAML - przenieś na osobny wątek, żeby nie blokować UI
            string xaml;
            try
            {
                _logger?.Info("🔄 Konwertuję Grid do XAML...");
                xaml = await Task.Run(() => _crossGridGenerator.CrossGridToXaml(normalizedText, _xamlGenerator));
                _logger?.Info($"✅ Skonwertowano Grid do XAML (długość: {xaml.Length} znaków)");
            }
            catch (Exception ex)
            {
                _logger?.Error($"❌ Błąd podczas konwersji Grid do XAML: {ex.Message}", ex);
                _logger?.Error($"Stack trace: {ex.StackTrace}");
                return; // Nie przerywaj - wyświetl tylko tekst
            }

            // Utwórz katalog dla obrazów chatbota (używamy stałej ścieżki) - przenieś na osobny wątek
            _logger?.Info($"📁 Tworzę katalog obrazów: {ChatbotImagesDirectory}");
            try
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(ChatbotImagesDirectory))
                    {
                        Directory.CreateDirectory(ChatbotImagesDirectory);
                        _logger?.Info($"✅ Katalog utworzony: {ChatbotImagesDirectory}");
                    }
                    else
                    {
                        _logger?.Info($"ℹ️  Katalog już istnieje: {ChatbotImagesDirectory}");
                    }
                    
                    // Sprawdź czy katalog rzeczywiście istnieje
                    if (Directory.Exists(ChatbotImagesDirectory))
                    {
                        _logger?.Info($"✅ Katalog istnieje i jest dostępny: {ChatbotImagesDirectory}");
                    }
                    else
                    {
                        _logger?.Error($"❌ Katalog NIE istnieje po utworzeniu: {ChatbotImagesDirectory}");
                        throw new DirectoryNotFoundException($"Nie można utworzyć katalogu: {ChatbotImagesDirectory}");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.Error($"❌ Błąd podczas tworzenia katalogu: {ex.Message}", ex);
                throw;
            }

            // Utwórz tymczasowy CrosswordView w niewidocznym oknie (wymagane dla renderowania)
            Window? tempWindow = null;
            CrosswordView? crosswordView = null;

            try
            {
                _logger?.Info("🔄 Tworzę tymczasowe okno i CrosswordView...");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Utwórz niewidoczne okno
                    tempWindow = new Window
                    {
                        WindowState = WindowState.Minimized,
                        ShowInTaskbar = false,
                        Visibility = Visibility.Hidden,
                        Width = 800,
                        Height = 800
                    };

                    // Utwórz CrosswordView
                    crosswordView = new CrosswordView();
                    tempWindow.Content = crosswordView;
                    tempWindow.Show(); // Wymagane dla renderowania, ale okno jest niewidoczne
                    _logger?.Info("✅ Tymczasowe okno utworzone");
                }, DispatcherPriority.Loaded);

                // Załaduj XAML do CrosswordView
                _logger?.Info("🔄 Ładuję XAML do CrosswordView...");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (crosswordView != null)
                    {
                        crosswordView.LoadXaml(xaml);
                        crosswordView.UpdateLayout();
                        _logger?.Info("✅ XAML załadowany do CrosswordView");
                    }
                    else
                    {
                        _logger?.Error("❌ CrosswordView jest null!");
                    }
                }, DispatcherPriority.Render);

                // Poczekaj na renderowanie
                _logger?.Info("⏳ Czekam na renderowanie (500ms)...");
                await Task.Delay(500);

                // Wygeneruj screenshot - przenieś ciężką operację na osobny wątek
                var fileName = $"crossword_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine(ChatbotImagesDirectory, fileName);
                _logger?.Info($"📸 Generuję screenshot: {filePath}");

                // Screenshot musi być wykonany na wątku UI (WPF wymaga RenderTargetBitmap)
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (crosswordView == null)
                    {
                        _logger?.Error("❌ CrosswordView jest null podczas generowania screenshotu!");
                        throw new InvalidOperationException("CrosswordView nie został utworzony");
                    }

                    try
                    {
                        // Spróbuj renderować ScrollViewer (zawiera Grid z białym tłem)
                        var scrollViewer = crosswordView.GetScrollViewer();
                        if (scrollViewer != null)
                        {
                            _logger?.Info("📸 Renderuję ScrollViewer...");
                            scrollViewer.UpdateLayout();
                            scrollViewer.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                            scrollViewer.Arrange(new System.Windows.Rect(scrollViewer.DesiredSize));
                            _screenshotService.CaptureToJpg(scrollViewer, filePath);
                            _logger?.Info($"✅ Screenshot zapisany (ScrollViewer): {filePath}");
                        }
                        else
                        {
                            // Fallback - renderuj wewnętrzny Grid
                            var innerGrid = crosswordView.GetInnerGrid();
                            if (innerGrid != null)
                            {
                                _logger?.Info("📸 Renderuję wewnętrzny Grid...");
                                innerGrid.UpdateLayout();
                                innerGrid.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                                innerGrid.Arrange(new System.Windows.Rect(innerGrid.DesiredSize));
                                _screenshotService.CaptureToJpg(innerGrid, filePath);
                                _logger?.Info($"✅ Screenshot zapisany (InnerGrid): {filePath}");
                            }
                            else
                            {
                                // Ostatni fallback - renderuj cały UserControl
                                _logger?.Info("📸 Renderuję cały CrosswordView...");
                                _screenshotService.CaptureToJpg(crosswordView, filePath);
                                _logger?.Info($"✅ Screenshot zapisany (CrosswordView): {filePath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"❌ Błąd podczas generowania screenshotu: {ex.Message}", ex);
                        _logger?.Error($"Stack trace: {ex.StackTrace}");
                        throw;
                    }
                }, DispatcherPriority.Background); // Użyj Background zamiast Render, żeby nie blokować UI
                
                // Sprawdź czy plik został utworzony - przenieś na Task.Run (nie wymaga UI)
                await Task.Run(() =>
                {
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        _logger?.Info($"✅ Plik istnieje! Rozmiar: {fileInfo.Length} bajtów");
                    }
                    else
                    {
                        _logger?.Error($"❌ Plik NIE istnieje po zapisie: {filePath}");
                    }
                });

                // Ustaw ścieżkę do obrazu w wiadomości
                if (File.Exists(filePath))
                {
                    message.ImagePath = filePath;
                    _logger?.Info($"✅ ImagePath ustawiony: {filePath}");
                }
                else
                {
                    _logger?.Error($"❌ Nie można ustawić ImagePath - plik nie istnieje: {filePath}");
                }
            }
            finally
            {
                // Zamknij i usuń tymczasowe okno
                if (tempWindow != null)
                {
                    _logger?.Info("🔄 Zamykam tymczasowe okno...");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        tempWindow.Close();
                        _logger?.Info("✅ Tymczasowe okno zamknięte");
                    });
                }
            }
            
            _logger?.Info("🎉 Przetwarzanie Grid zakończone pomyślnie!");
        }
        catch (Exception ex)
        {
            _logger?.Error($"❌ Błąd podczas przetwarzania Grid: {ex.Message}", ex);
            _logger?.Error($"Stack trace: {ex.StackTrace}");
            throw; // Rzuć dalej, ale nie przerywaj wyświetlania wiadomości
        }
    }
}

