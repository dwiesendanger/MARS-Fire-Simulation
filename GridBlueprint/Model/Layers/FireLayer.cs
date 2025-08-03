using System;
using System.Collections.Generic;
using System.Linq;
using Mars.Components.Layers;
using Mars.Core.Data;
using Mars.Interfaces.Data;
using Mars.Interfaces.Layers;
using GridBlueprint.Model.Agents;
using GridBlueprint.Model.Utils;

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
    ///     Tree density for forest generation. This value is loaded from config.json.
    ///     To change tree density, modify the "density" parameter in config.json.
    /// </summary>
    public double Density { get; set; } = 0.65; // Fallback value if config loading fails

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
    ///     Gets whether the fire simulation has completed (no more cells are burning).
    /// </summary>
    public bool IsSimulationComplete => _simulationComplete;

    /// <summary>
    ///     Global instance of the FireLayer, accessible from anywhere.
    ///     Set during initialization to provide a global access point to the FireLayer instance.
    /// </summary>
    public static FireLayer Instance { get; private set; }

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
        Instance = this;

        // Initialize the base RasterLayer with CSV data
        var initLayer = base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);

        // Load density from config.json
        double configDensity = 0.65; // fallback value
        
        try
        {
            // Read and parse config.json directly
            var configPath = "config.json";
            if (System.IO.File.Exists(configPath))
            {
                var configJson = System.IO.File.ReadAllText(configPath);
                var config = Newtonsoft.Json.Linq.JObject.Parse(configJson);
                
                // Find the FireLayer configuration
                var layers = config["layers"] as Newtonsoft.Json.Linq.JArray;
                if (layers != null)
                {
                    foreach (var layer in layers)
                    {
                        if (layer["name"]?.ToString() == "FireLayer" && layer["density"] != null)
                        {
                            if (double.TryParse(layer["density"].ToString(), out double parsedDensity))
                            {
                                configDensity = parsedDensity;
                                Console.WriteLine($"[FireLayer] Loaded density from config.json: {configDensity}");
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"[FireLayer] config.json not found, using fallback density: {configDensity}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FireLayer] Error loading config.json: {ex.Message}, using fallback density: {configDensity}");
        }
        
        var rnd = new Random();
        
        // Generate forest with density-based tree distribution
        GenerateForest(configDensity, rnd);
        
        // Ignite the entire left column to start the fire simulation
        IgniteLeftColumn();
        
        // Spawn the HelperAgent to drive the simulation
        var agentManager = layerInitData.Container.Resolve<IAgentManager>();
        agentManager.Spawn<HelperAgent, FireLayer>().ToList();

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
                this[x, y] = (rnd.NextDouble() < density) ? (double)CellState.Tree : (double)CellState.Empty;
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
            this[0, y] = (double)CellState.Burning;
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
                OutputBurnedPercentage();
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

            SpreadFire(x, y, newBurningCells);
            this[x, y] = (double)CellState.Burned;
        }

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
    private void SpreadFire(int x, int y, List<(int x, int y)> newBurningCells)
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
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        if ((CellState)this[x, y] == CellState.Tree && !_burningSet.Contains((x, y)))
        {
            this[x, y] = (double)CellState.Burning;
            newBurningCells.Add((x, y));
        }
    }

    /// <summary>
    ///     Calculates and outputs the percentage of burned area when the simulation is complete.
    ///     This method is called automatically at the end of the simulation.
    /// </summary>
    private void OutputBurnedPercentage()
    {
        int burned = 0;
        int total = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var state = (CellState)this[x, y];
                if (state == CellState.Tree || state == CellState.Burning || state == CellState.Burned)
                    total++;
                if (state == CellState.Burned)
                    burned++;
            }
        }
        double percent = total > 0 ? (100.0 * burned / total) : 0.0;
        Console.WriteLine($"Burned area: {burned} of {total} ({percent:F2}%)");
    }

    #endregion
}
