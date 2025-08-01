namespace GridBlueprint.Model.Utils;

public enum CellState
{
    Empty,    // No Tree
    Tree,     // Burnable
    Burning,  // Currently burning
    Burned    // Ash, not burnable
}