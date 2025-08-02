using System;
using System.Collections.Generic;
using System.Linq;
using Mars.Components.Layers;
using Mars.Core.Data;
using Mars.Interfaces.Data;
using Mars.Interfaces.Layers;
using GridBlueprint.Model.Agents;
using Mars.Interfaces.Annotations;

namespace GridBlueprint.Model.Layers;

/// <summary>
///     The FireLayer represents a NetLogo-style Fire simulation model on a 2D raster grid.
///     This implementation simulates fire spreading from left to right through a forest with configurable tree density.
///     The fire starts at the entire left column (x=0) and spreads to adjacent trees using 4-connected neighborhood.
///     Performance is optimized by tracking only burning cells instead of scanning the entire grid each tick.
/// </summary>
public class FireLayer : RasterLayer
{
    #region Fields and Properties

    /// <summary>
    ///     NOTE: This is a fallback value. The actual density is loaded from config.json and overrides this value.
    ///     To change tree density, modify the "density" parameter in config.json, not this property.
    /// </summary>
    [PropertyDescription(Name = "density")]
    public double Density { get; set; } = 0.65;

    /// <summary>
    ///     Queue storing coordinates of cells that are currently burning.
    ///     Uses FIFO processing to ensure fire spreads in the correct temporal order.
    /// </summary>
    private readonly Queue<(int x, int y)> _burningCells = new();

    /// <summary>
    ///     HashSet for O(1) lookup to check if a cell is already burning.
    ///     Prevents duplicate entries in the burning cells queue and improves performance.
    /// </summary>
    private readonly HashSet<(int x, int y)> _burningSet = new();

    /// <summary>
    ///     Flag indicating whether the fire simulation has completed (no more burning cells).
    /// </summary>
    private bool _simulationComplete = false;

    /// <summary>
    ///     Cached array of 4-connected neighbor directions (North, South, East, West).
    ///     Used for efficient neighbor iteration during fire spreading calculations.
    /// </summary>
    private readonly (int dx, int dy)[] _neighbors = { (-1, 0), (1, 0), (0, -1), (0, 1) };

    /// <summary>
    ///     Gets whether the fire simulation has completed (no more cells are burning).
    /// </summary>
    public bool IsSimulationComplete => _simulationComplete;

    #endregion

    #region Initialization

    /// <summary>
    ///     Initializes the FireLayer by loading the base raster data and setting up the NetLogo Fire simulation.
    ///     Creates a forest with random tree distribution based on the density parameter,
    ///     then ignites the entire left column to start the fire simulation.
    /// </summary>
    /// <param name="layerInitData">Initialization data containing configuration and dependencies</param>
    /// <param name="registerAgentHandle">Handle for registering agents with the simulation</param>
    /// <param name="unregisterAgentHandle">Handle for unregistering agents from the simulation</param>
    /// <returns>True if initialization was successful, false otherwise</returns>
    public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle,
        UnregisterAgent unregisterAgentHandle)
    {
        // Initialize the base RasterLayer with CSV data
        var initLayer = base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);
        
        // Use fallback density if configuration value is invalid
        var density = Density > 0 ? Density : 0.65;
        var rnd = new Random(42); // Fixed seed for reproducible results
        
        // Override CSV data with density-based forest generation
        GenerateForest(density, rnd);
        
        // Ignite the entire left column to start the NetLogo Fire simulation
        IgniteLeftColumn();
        
        // Spawn and register the HelperAgent that will drive the simulation
        var agentManager = layerInitData.Container.Resolve<IAgentManager>();
        var helperAgents = agentManager.Spawn<HelperAgent, FireLayer>().ToList();

        return initLayer;
    }

    /// <summary>
    ///     Generates a forest by randomly placing trees across the grid based on the specified density.
    ///     Overwrites any existing raster data with the new forest configuration.
    ///     Uses batch processing for improved cache performance on large grids.
    /// </summary>
    /// <param name="density">Probability (0.0-1.0) that any given cell will contain a tree</param>
    /// <param name="rnd">Random number generator for consistent tree placement</param>
    private void GenerateForest(double density, Random rnd)
    {
        // Process grid in batches for better cache performance
        var batchSize = Math.Min(1000, Width * Height / 10);
        for (int batch = 0; batch < Width * Height; batch += batchSize)
        {
            var endBatch = Math.Min(batch + batchSize, Width * Height);
            for (int i = batch; i < endBatch; i++)
            {
                int x = i % Width;
                int y = i / Width;
                // Cell states: 0=Empty, 1=Tree, 2=Burning, 3=Burned
                this[x, y] = (rnd.NextDouble() < density) ? 1.0 : 0.0;
            }
        }
    }

    /// <summary>
    ///     Ignites all cells in the leftmost column (x=0) to start the fire simulation.
    ///     This follows the NetLogo Fire model where fire starts from one edge and spreads across the forest.
    /// </summary>
    private void IgniteLeftColumn()
    {
        for (int y = 0; y < Height; y++)
        {
            this[0, y] = 2.0; // Set to burning state
            var cell = (0, y);
            _burningCells.Enqueue(cell);
            _burningSet.Add(cell);
        }
    }

    #endregion

    #region Simulation Logic

    /// <summary>
    ///     Executes one time step of the fire simulation.
    ///     Processes all currently burning cells, spreads fire to adjacent trees,
    ///     and transitions burning cells to the burned state.
    ///     Performance is optimized by only processing active burning cells.
    /// </summary>
    public void Tick()
    {
        // Early exit if simulation is complete or no cells are burning
        if (_simulationComplete || _burningCells.Count == 0)
        {
            if (!_simulationComplete)
            {
                _simulationComplete = true;
                Console.WriteLine($"Fire simulation completed at tick {GetCurrentTick()}. No more burning cells.");
            }
            return;
        }

        // Process all currently burning cells
        var currentBurningCount = _burningCells.Count;
        var newBurningCells = new List<(int x, int y)>(currentBurningCount * 4); // Preallocate for performance

        // Process each burning cell in FIFO order
        for (int i = 0; i < currentBurningCount; i++)
        {
            var (x, y) = _burningCells.Dequeue();
            _burningSet.Remove((x, y));

            // Spread fire to all 4-connected neighbors
            SpreadFireToNeighbors(x, y, newBurningCells);
            
            // Transition current burning cell to burned state
            this[x, y] = 3.0; // Burned
        }

        // Add all newly ignited cells to the burning queue
        foreach (var cell in newBurningCells)
        {
            _burningCells.Enqueue(cell);
            _burningSet.Add(cell);
        }
    }

    /// <summary>
    ///     Attempts to spread fire from a burning cell to all its 4-connected neighbors.
    ///     Only cells containing trees (state 1.0) that are not already burning can be ignited.
    /// </summary>
    /// <param name="x">X-coordinate of the burning cell</param>
    /// <param name="y">Y-coordinate of the burning cell</param>
    /// <param name="newBurningCells">Collection to store newly ignited cells</param>
    private void SpreadFireToNeighbors(int x, int y, List<(int x, int y)> newBurningCells)
    {
        // Check all 4-connected neighbors (North, South, East, West)
        CheckAndIgniteNeighbor(x - 1, y, newBurningCells); // West
        CheckAndIgniteNeighbor(x + 1, y, newBurningCells); // East
        CheckAndIgniteNeighbor(x, y - 1, newBurningCells); // South
        CheckAndIgniteNeighbor(x, y + 1, newBurningCells); // North
    }

    /// <summary>
    ///     Checks if a neighbor cell can be ignited and adds it to the burning queue if possible.
    ///     A cell can be ignited if it's within bounds, contains a tree, and is not already burning.
    /// </summary>
    /// <param name="x">X-coordinate of the neighbor cell to check</param>
    /// <param name="y">Y-coordinate of the neighbor cell to check</param>
    /// <param name="newBurningCells">Collection to add the newly ignited cell to</param>
    private void CheckAndIgniteNeighbor(int x, int y, List<(int x, int y)> newBurningCells)
    {
        // Check if coordinates are within grid bounds
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        
        // Check if cell contains a tree and is not already burning
        if (this[x, y] == 1.0 && !_burningSet.Contains((x, y)))
        {
            this[x, y] = 2.0; // Set to burning state
            newBurningCells.Add((x, y));
        }
    }

    #endregion
}
