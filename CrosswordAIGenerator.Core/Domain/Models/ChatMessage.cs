using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Reprezentuje wiadomość w czacie z chatbotem
/// </summary>
public class ChatMessage : INotifyPropertyChanged
{
    private string _content = string.Empty;
    
    /// <summary>
    /// Treść wiadomości
    /// </summary>
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }
    
    /// <summary>
    /// Czy to wiadomość użytkownika (true) czy modelu (false)
    /// </summary>
    public bool IsUser { get; set; }
    
    /// <summary>
    /// Czas wysłania wiadomości
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Tryb w którym została wysłana wiadomość
    /// </summary>
    public ChatbotMode Mode { get; set; }
    
    private string? _imagePath;
    
    /// <summary>
    /// Ścieżka do wygenerowanego obrazu JPG (jeśli wiadomość zawiera Grid)
    /// </summary>
    public string? ImagePath
    {
        get => _imagePath;
        set
        {
            if (_imagePath != value)
            {
                _imagePath = value;
                OnPropertyChanged();
            }
        }
    }
    
    /// <summary>
    /// Czy wiadomość zawiera format Grid
    /// </summary>
    public bool HasGrid { get; set; }
    
    public ChatMessage()
    {
        Timestamp = DateTime.Now;
    }
    
    public ChatMessage(string content, bool isUser, ChatbotMode mode) : this()
    {
        Content = content;
        IsUser = isUser;
        Mode = mode;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

