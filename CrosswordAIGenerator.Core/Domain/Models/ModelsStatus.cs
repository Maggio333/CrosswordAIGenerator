namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Status załadowanych modeli i użycia VRAM
/// </summary>
public class ModelsStatus
{
    /// <summary>
    /// Czy model GGUF (General) jest załadowany
    /// </summary>
    public bool GgufLoaded { get; set; }
    
    /// <summary>
    /// Czy adapter Bielik jest załadowany
    /// </summary>
    public bool BielikLoaded { get; set; }
    
    /// <summary>
    /// Czy adapter Qwen jest załadowany
    /// </summary>
    public bool QwenLoaded { get; set; }
    
    /// <summary>
    /// Zaalokowana VRAM w GB
    /// </summary>
    public double VramAllocatedGb { get; set; }
    
    /// <summary>
    /// Całkowita VRAM w GB
    /// </summary>
    public double VramTotalGb { get; set; }
    
    /// <summary>
    /// Wolna VRAM w GB
    /// </summary>
    public double VramFreeGb { get; set; }
    
    /// <summary>
    /// Procent użycia VRAM
    /// </summary>
    public double VramUsagePercent { get; set; }
}

