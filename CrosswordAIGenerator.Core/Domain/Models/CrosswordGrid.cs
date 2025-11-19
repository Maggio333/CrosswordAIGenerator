namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Reprezentuje siatkę krzyżówki z kratkami
/// </summary>
public class CrosswordGrid
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public Dictionary<(int row, int col), CrosswordCell> Cells { get; set; }
    
    public CrosswordGrid(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
        Cells = new Dictionary<(int row, int col), CrosswordCell>();
        
        // Inicjalizuj wszystkie kratki jako puste
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Cells[(r, c)] = new CrosswordCell(r, c, CrosswordCellType.Empty);
            }
        }
    }
    
    public CrosswordCell GetCell(int row, int col)
    {
        if (Cells.TryGetValue((row, col), out var cell))
        {
            return cell;
        }
        return new CrosswordCell(row, col, CrosswordCellType.Wall);
    }
    
    public void SetCell(int row, int col, CrosswordCellType type, char? letter = null)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Columns)
        {
            throw new ArgumentOutOfRangeException($"Cell position ({row}, {col}) is out of bounds");
        }
        
        Cells[(row, col)] = new CrosswordCell(row, col, type, letter);
    }
    
    public void SetLetter(int row, int col, char letter)
    {
        SetCell(row, col, CrosswordCellType.Letter, letter);
    }
    
    public void SetWall(int row, int col)
    {
        SetCell(row, col, CrosswordCellType.Wall);
    }
    
    public bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < Rows && col >= 0 && col < Columns;
    }
}

