using System;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Environments;
using GridBlueprint.Model.Layers;

namespace GridBlueprint.Model.Agents;

/// <summary>
///     The HelperAgent serves as a coordinator for the fire simulation by calling the FireLayer's tick method.
///     This agent is responsible for driving the simulation forward and detecting when the fire has completely burned out.
///     It implements automatic simulation completion detection to optimize performance once no cells are burning.
/// </summary>
public class HelperAgent : IAgent<FireLayer>, IPositionable
{
    #region Fields and Properties

    /// <summary>
    ///     Unique identifier for this agent instance.
    /// </summary>
    public Guid ID { get; set; }
    
    /// <summary>
    ///     Position of the agent in the simulation space (not used for fire simulation).
    /// </summary>
    public Position Position { get; set; }
    
    /// <summary>
    ///     Reference to the FireLayer that this agent manages.
    /// </summary>
    private FireLayer _layer;
    
    /// <summary>
    ///     Flag to ensure the completion message is only logged once.
    /// </summary>
    private bool _hasLogged = false;

    #endregion

    #region Initialization

    /// <summary>
    ///     Initializes the HelperAgent and establishes connection to the FireLayer.
    ///     This method is called once at the beginning of the simulation.
    /// </summary>
    /// <param name="layer">The FireLayer instance that this agent will coordinate</param>
    public void Init(FireLayer layer)
    {
        _layer = layer;
        Console.WriteLine("HelperAgent initialized");
    }

    #endregion

    #region Simulation Logic

    /// <summary>
    ///     Executes one simulation step by calling the FireLayer's tick method.
    ///     Also monitors for simulation completion and logs the completion message once.
    ///     This method is called once per simulation tick by the MARS framework.
    /// </summary>
    public void Tick()
    {
        // Drive the fire simulation forward
        _layer.Tick();
        
        // Check if fire simulation has completed and log completion message once
        if (_layer.IsSimulationComplete && !_hasLogged)
        {
            Console.WriteLine("Fire simulation completed. Stopping simulation...");
            _hasLogged = true;
        }
    }

    #endregion
}