namespace CrosswordAIGenerator.Core.Domain.Models.RL;

/// <summary>
/// Nagroda w środowisku RL z komponentami
/// </summary>
public class CrosswordRLReward
{
    /// <summary>
    /// Całkowita wartość nagrody
    /// </summary>
    public double TotalReward { get; set; }
    
    /// <summary>
    /// Nagroda za ukończenie krzyżówki
    /// </summary>
    public double CompletionReward { get; set; }
    
    /// <summary>
    /// Nagroda za umieszczenie słowa
    /// </summary>
    public double PlacementReward { get; set; }
    
    /// <summary>
    /// Nagroda za przecięcia z istniejącymi słowami
    /// </summary>
    public double IntersectionReward { get; set; }
    
    /// <summary>
    /// Kara za niepoprawne akcje
    /// </summary>
    public double Penalty { get; set; }
    
    public CrosswordRLReward()
    {
    }
    
    public CrosswordRLReward(double totalReward, double completionReward = 0, double placementReward = 0, double intersectionReward = 0, double penalty = 0)
    {
        TotalReward = totalReward;
        CompletionReward = completionReward;
        PlacementReward = placementReward;
        IntersectionReward = intersectionReward;
        Penalty = penalty;
    }
    
    public override string ToString()
    {
        return $"Total: {TotalReward:F2} (Completion: {CompletionReward:F2}, Placement: {PlacementReward:F2}, Intersection: {IntersectionReward:F2}, Penalty: {Penalty:F2})";
    }
}
