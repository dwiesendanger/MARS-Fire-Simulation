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
    ///     Burning cells will spread fire to adjacent trees and then transition to ember state.
    /// </summary>
    Burning = 2,
    
    /// <summary>
    ///     Cell with hot embers, first cooling stage. Displayed as bright orange-red.
    ///     Embers gradually cool down through multiple stages before becoming fully burned.
    /// </summary>
    Ember3 = 3,
    
    /// <summary>
    ///     Cell with medium embers, second cooling stage. Displayed as orange.
    ///     Continues the cooling process from hot embers toward burned state.
    /// </summary>
    Ember2 = 4,
    
    /// <summary>
    ///     Cell with cool embers, third cooling stage. Displayed as dark orange-red.
    ///     Nearly extinguished, will become fully burned in the next stage.
    /// </summary>
    Ember1 = 5,
    
    /// <summary>
    ///     Cell that has finished burning completely. Displayed as dark red/black in the visualization.
    ///     Burned cells are in their final state and cannot burn again or spread fire.
    /// </summary>
    Burned = 6
}
