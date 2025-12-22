using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;

namespace CrosswordAIGenerator.WPF.Presentation.Views;

/// <summary>
/// Interaction logic for ChatbotWindow.xaml
/// </summary>
public partial class ChatbotWindow : Window
{
    private readonly ChatbotViewModel _viewModel;
    private ChatMessage? _lastMessage;
    private bool _autoScroll = true;

    public ChatbotWindow(ChatbotViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        
        // Subskrybuj zmiany w kolekcji Messages, żeby automatycznie przewijać do końca
        _viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
        
        // Subskrybuj również Loaded event, żeby przewinąć po załadowaniu
        Loaded += ChatbotWindow_Loaded;
    }

    private void ChatbotWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Przewiń do końca po załadowaniu okna
        ScrollToEnd();
    }

    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Odsubskrybuj poprzednią wiadomość (jeśli była)
        if (_lastMessage != null)
        {
            _lastMessage.PropertyChanged -= LastMessage_PropertyChanged;
        }

        // Przewiń do końca gdy dodawane są nowe wiadomości
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            // Subskrybuj PropertyChanged na ostatniej wiadomości (dla streamingu)
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                var newMessage = e.NewItems[e.NewItems.Count - 1] as ChatMessage;
                if (newMessage != null)
                {
                    _lastMessage = newMessage;
                    newMessage.PropertyChanged += LastMessage_PropertyChanged;
                }
            }
            
            // Przewiń do końca
            ScrollToEnd();
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            ScrollToEnd();
        }
        else if (e.Action == NotifyCollectionChangedAction.Replace)
        {
            // Gdy wiadomość jest zastępowana (np. podczas streamingu)
            ScrollToEnd();
        }
    }

    private void LastMessage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Przewiń do końca gdy zmienia się Content ostatniej wiadomości (podczas streamingu)
        if (e.PropertyName == nameof(ChatMessage.Content) && _autoScroll)
        {
            ScrollToEnd();
        }
    }

    private void MessagesScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Automatycznie przewijaj do końca, gdy zawartość się rozszerza (podobnie jak AutoScrollBehavior)
        if (e.ExtentHeightChange > 0 && _autoScroll)
        {
            MessagesScrollViewer.ScrollToEnd();
        }
        
        // Sprawdź, czy użytkownik przewinął w górę - wtedy wyłącz auto-scroll
        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 1)
        {
            // Użytkownik jest na dole - włącz auto-scroll
            _autoScroll = true;
        }
        else if (e.VerticalChange < 0)
        {
            // Użytkownik przewinął w górę - wyłącz auto-scroll (żeby nie przeszkadzać)
            _autoScroll = false;
        }
    }

    private void ScrollToEnd()
    {
        if (!_autoScroll) return; // Nie przewijaj, jeśli użytkownik przewinął w górę
        
        // Użyj Dispatcher, żeby przewinąć po renderowaniu
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                MessagesScrollViewer?.ScrollToEnd();
            }
            catch (Exception)
            {
                // Ignoruj błędy podczas przewijania
            }
        }, DispatcherPriority.Loaded);
    }

    private void PromptTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Wysyłaj wiadomość gdy użytkownik naciśnie Ctrl+Enter
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (_viewModel.SendMessageCommand.CanExecute(null))
            {
                _viewModel.SendMessageCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Odsubskrybuj eventy, żeby uniknąć wycieków pamięci
        if (_viewModel != null)
        {
            _viewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
        }
        
        if (_lastMessage != null)
        {
            _lastMessage.PropertyChanged -= LastMessage_PropertyChanged;
        }
        
        Loaded -= ChatbotWindow_Loaded;
        
        base.OnClosed(e);
    }
}

