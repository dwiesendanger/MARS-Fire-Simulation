using System;
using System.IO;
using GridBlueprint.Model.Agents;
using GridBlueprint.Model.Layers;
using Mars.Components.Starter;
using Mars.Interfaces.Model;

namespace GridBlueprint;

/// <summary>
///     Main entry point for the NetLogo Fire simulation using the MARS framework.
///     This program configures and starts a fire spread simulation that models forest fires
///     spreading from left to right through a 2D grid with configurable tree density.
/// </summary>
internal static class Program
{
    /// <summary>
    ///     Application entry point that initializes and runs the NetLogo Fire simulation.
    ///     Sets up the model description, loads configuration, and executes the simulation
    ///     until completion or the configured maximum number of iterations.
    /// </summary>
    private static void Main()
    {
        // Create a new model description and register simulation components
        var description = new ModelDescription();
        description.AddLayer<FireLayer>();
        description.AddAgent<HelperAgent, FireLayer>();

        // Load simulation configuration from JSON file
        // This includes parameters like density, start/end time, and grid file path
        var file = File.ReadAllText("config.json");
        var config = SimulationConfig.Deserialize(file);

        // Initialize the simulation with the model description and configuration
        var starter = SimulationStarter.Start(description, config);

        // Execute the simulation and wait for completion
        var handle = starter.Run();
        
        // Display results and clean up resources
        Console.WriteLine("Successfully executed iterations: " + handle.Iterations);
        starter.Dispose();
    }
}