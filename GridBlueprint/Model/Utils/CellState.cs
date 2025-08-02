namespace GridBlueprint.Model.Utils;

/// <summary>
///     Enumeration defining the possible states of cells in the NetLogo Fire simulation.
///     These values correspond to the numeric cell states used in the raster layer
///     and are mapped to specific colors in the visualization.
/// </summary>
public enum CellState
{
    /// <summary>
    ///     Empty cell with no vegetation. Displayed as black in the visualization.
    ///     Fire cannot spread to or through empty cells.
    /// </summary>
    Empty = 0,
    
    /// <summary>
    ///     Cell containing a tree or vegetation. Displayed as green in the visualization.
    ///     Trees can be ignited by adjacent burning cells.
    /// </summary>
    Tree = 1,
    
    /// <summary>
    ///     Cell that is currently on fire. Displayed as bright red in the visualization.
    ///     Burning cells will spread fire to adjacent trees and then transition to burned state.
    /// </summary>
    Burning = 2,
    
    /// <summary>
    ///     Cell that has finished burning. Displayed as dark red in the visualization.
    ///     Burned cells are in their final state and cannot burn again or spread fire.
    /// </summary>
    Burned = 3
}
