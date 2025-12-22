using CrosswordAIGenerator.Core.Domain.Models.RL;

namespace CrosswordAIGenerator.Core.Domain.Services.RL;

/// <summary>
/// Generator gier self-play
/// </summary>
public interface ICrosswordSelfPlayGenerator
{
    /// <summary>
    /// Generuje wiele gier self-play
    /// </summary>
    List<CrosswordRLTrajectory> GenerateSelfPlayGames(
        int gameCount,
        int rows,
        int columns,
        List<string> targetWords,
        SelfPlayStrategy strategy);
    
    /// <summary>
    /// Generuje pojedynczą grę self-play
    /// </summary>
    CrosswordRLTrajectory GenerateSingleGame(
        int rows,
        int columns,
        List<string> targetWords,
        SelfPlayStrategy strategy);
}
