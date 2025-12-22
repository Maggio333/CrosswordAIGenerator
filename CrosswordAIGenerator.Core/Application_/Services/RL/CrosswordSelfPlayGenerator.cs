using CrosswordAIGenerator.Core.Domain.Models.RL;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Services.RL;

namespace CrosswordAIGenerator.Core.Application.Services.RL;

/// <summary>
/// Generator gier self-play
/// </summary>
public class CrosswordSelfPlayGenerator : ICrosswordSelfPlayGenerator
{
    private readonly ICrosswordRLEnvironment _environment;
    private readonly IWordDictionary _wordDictionary;
    private readonly Random _random;
    private readonly ICursorLogger? _logger;
    
    private const int MaxGameSteps = 100; // Maksymalna liczba kroków w grze
    private const int MinWordLength = 6;
    private const int MaxWordLength = 12;
    
    public CrosswordSelfPlayGenerator(
        ICrosswordRLEnvironment environment,
        IWordDictionary wordDictionary,
        int? seed = null,
        ICursorLogger? logger = null)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _wordDictionary = wordDictionary ?? throw new ArgumentNullException(nameof(wordDictionary));
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _logger = logger;
    }
    
    public List<CrosswordRLTrajectory> GenerateSelfPlayGames(
        int gameCount,
        int rows,
        int columns,
        List<string> targetWords,
        SelfPlayStrategy strategy)
    {
        var trajectories = new List<CrosswordRLTrajectory>();
        
        _logger?.Info($"CrosswordSelfPlayGenerator: Generating {gameCount} games with strategy {strategy}");
        
        for (int i = 0; i < gameCount; i++)
        {
            try
            {
                var trajectory = GenerateSingleGame(rows, columns, targetWords, strategy);
                trajectories.Add(trajectory);
                
                _logger?.Debug($"CrosswordSelfPlayGenerator: Game {i + 1}/{gameCount} completed. Steps: {trajectory.Steps.Count}, Success: {trajectory.IsSuccessful}, Total reward: {trajectory.TotalReward:F2}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"CrosswordSelfPlayGenerator: Error generating game {i + 1}: {ex.Message}", ex);
            }
        }
        
        _logger?.Info($"CrosswordSelfPlayGenerator: Generated {trajectories.Count} games. Successful: {trajectories.Count(t => t.IsSuccessful)}");
        
        return trajectories;
    }
    
    public CrosswordRLTrajectory GenerateSingleGame(
        int rows,
        int columns,
        List<string> targetWords,
        SelfPlayStrategy strategy)
    {
        // Jeśli nie podano słów, wygeneruj losowe
        var words = targetWords ?? GenerateRandomWords(5);
        
        // Utwórz początkowy stan
        var initialState = _environment.GetInitialState(rows, columns, words);
        var trajectory = new CrosswordRLTrajectory(initialState);
        
        var currentState = initialState;
        int stepCount = 0;
        
        // Graj aż do zakończenia lub maksymalnej liczby kroków
        while (!_environment.IsTerminal(currentState) && stepCount < MaxGameSteps)
        {
            // Pobierz dostępne akcje
            var validActions = _environment.GetValidActions(currentState);
            
            if (validActions.Count == 0)
            {
                // Brak dostępnych akcji - gra zakończona
                _logger?.Debug($"CrosswordSelfPlayGenerator: No valid actions available at step {stepCount}");
                break;
            }
            
            // Wybierz akcję zgodnie ze strategią
            var action = SelectAction(currentState, validActions, strategy);
            
            // Wykonaj krok
            var (nextState, reward, isTerminal) = _environment.Step(currentState, action);
            
            // Oblicz szczegółową nagrodę
            var rewardDetails = CalculateRewardDetails(currentState, nextState, action, isTerminal);
            
            // Dodaj krok do trajektorii
            var step = new CrosswordRLStep(currentState, action, rewardDetails, nextState, isTerminal);
            trajectory.Steps.Add(step);
            
            trajectory.TotalReward += reward;
            currentState = nextState;
            stepCount++;
            
            if (isTerminal)
            {
                trajectory.IsSuccessful = true;
                trajectory.FinalState = nextState;
                break;
            }
        }
        
        trajectory.FinalState = currentState;
        trajectory.IsSuccessful = currentState.IsTerminal();
        // Epizod zakończył się naturalnie, jeśli wszystkie słowa zostały umieszczone
        // (nie przez osiągnięcie max_steps lub brak dostępnych akcji)
        trajectory.IsNaturalTermination = trajectory.IsSuccessful && stepCount < MaxGameSteps;
        
        _logger?.Debug($"CrosswordSelfPlayGenerator: Game completed. Steps: {stepCount}, Success: {trajectory.IsSuccessful}, Natural termination: {trajectory.IsNaturalTermination}, Total reward: {trajectory.TotalReward:F2}");
        
        return trajectory;
    }
    
    /// <summary>
    /// Wybiera akcję zgodnie ze strategią
    /// </summary>
    private CrosswordRLAction SelectAction(
        CrosswordRLState state,
        List<CrosswordRLAction> validActions,
        SelfPlayStrategy strategy)
    {
        return strategy switch
        {
            SelfPlayStrategy.Random => SelectRandomAction(validActions),
            SelfPlayStrategy.Greedy => SelectGreedyAction(state, validActions),
            SelfPlayStrategy.PolicyBased => SelectRandomAction(validActions), // Placeholder - użyj random na razie
            _ => SelectRandomAction(validActions)
        };
    }
    
    /// <summary>
    /// Wybiera losową akcję
    /// </summary>
    private CrosswordRLAction SelectRandomAction(List<CrosswordRLAction> validActions)
    {
        if (validActions.Count == 0)
        {
            throw new InvalidOperationException("No valid actions available");
        }
        
        return validActions[_random.Next(validActions.Count)];
    }
    
    /// <summary>
    /// Wybiera akcję z najwyższą natychmiastową nagrodą (greedy)
    /// </summary>
    private CrosswordRLAction SelectGreedyAction(
        CrosswordRLState state,
        List<CrosswordRLAction> validActions)
    {
        if (validActions.Count == 0)
        {
            throw new InvalidOperationException("No valid actions available");
        }
        
        // Dla każdej akcji, symuluj krok i oblicz nagrodę
        var actionRewards = new List<(CrosswordRLAction action, double reward)>();
        
        foreach (var action in validActions)
        {
            try
            {
                var (nextState, reward, _) = _environment.Step(state.Clone(), action);
                actionRewards.Add((action, reward));
            }
            catch
            {
                // Jeśli symulacja się nie powiodła, użyj domyślnej nagrody
                actionRewards.Add((action, 0));
            }
        }
        
        // Wybierz akcję z najwyższą nagrodą
        var bestAction = actionRewards.OrderByDescending(ar => ar.reward).First().action;
        
        return bestAction;
    }
    
    /// <summary>
    /// Generuje losowe słowa ze słownika
    /// </summary>
    private List<string> GenerateRandomWords(int count)
    {
        var words = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            var word = _wordDictionary.GetRandomWord(MinWordLength, MaxWordLength);
            if (word != null && !words.Contains(word, StringComparer.OrdinalIgnoreCase))
            {
                words.Add(word.ToUpperInvariant());
            }
        }
        
        return words;
    }
    
    /// <summary>
    /// Oblicza szczegółową nagrodę
    /// </summary>
    private CrosswordRLReward CalculateRewardDetails(
        CrosswordRLState previousState,
        CrosswordRLState currentState,
        CrosswordRLAction action,
        bool isTerminal)
    {
        var reward = new CrosswordRLReward();
        
        // Nagroda za ukończenie
        if (isTerminal && currentState.IsTerminal())
        {
            reward.CompletionReward = 100.0;
        }
        
        // Nagroda za umieszczenie (jeśli akcja była poprawna)
        if (currentState.PlacedWords.Any(pw => pw.Word.Equals(action.Word, StringComparison.OrdinalIgnoreCase)))
        {
            reward.PlacementReward = 10.0;
            
            // Nagroda za przecięcia
            var placedWord = currentState.PlacedWords.FirstOrDefault(
                pw => pw.Word.Equals(action.Word, StringComparison.OrdinalIgnoreCase));
            if (placedWord != null)
            {
                reward.IntersectionReward = placedWord.IntersectionCount * 5.0;
            }
        }
        else
        {
            // Kara za niepoprawną akcję
            reward.Penalty = -5.0;
        }
        
        reward.TotalReward = reward.CompletionReward + reward.PlacementReward + 
                            reward.IntersectionReward + reward.Penalty;
        
        return reward;
    }
}
