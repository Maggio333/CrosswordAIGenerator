using System.Text.Json.Serialization;

namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Specyfikacja krzyżówki używana do generowania promptów dla LLM
/// </summary>
public class CrosswordSpecification
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    
    [JsonPropertyName("size")]
    public string Size => $"{Rows}x{Columns}";
    
    public List<CrosswordWordSpec> Words { get; set; } = new();
    
    public class CrosswordWordSpec
    {
        public int Id { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public int Length { get; set; }
        public string Direction { get; set; } // "across" or "down"
        public string Clue { get; set; }
        public string? Word { get; set; } // Opcjonalnie - jeśli słowo jest już znane
    }
    
    public CrosswordSpecification(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
    }
    
    /// <summary>
    /// Konwertuje specyfikację na format JSON dla LLM
    /// </summary>
    public string ToJson()
    {
        var across = Words.Where(w => w.Direction == "across").ToList();
        var down = Words.Where(w => w.Direction == "down").ToList();
        
        var spec = new
        {
            size = Size,
            words = new
            {
                across = across.Select(w => new
                {
                    id = w.Id,
                    row = w.Row,
                    col = w.Col,
                    length = w.Length,
                    clue = w.Clue,
                    word = w.Word
                }),
                down = down.Select(w => new
                {
                    id = w.Id,
                    row = w.Row,
                    col = w.Col,
                    length = w.Length,
                    clue = w.Clue,
                    word = w.Word
                })
            }
        };
        
        return System.Text.Json.JsonSerializer.Serialize(spec, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

