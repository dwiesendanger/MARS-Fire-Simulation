using System;
using System.Collections.Generic;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using GridBlueprint.Model.Layers;
using GridBlueprint.Model.Utils;

namespace GridBlueprint.Model.Agents;

public class SimpleAgent : IAgent<GridLayer>, IPositionable
{
    #region Init

    /// <summary>
    ///     The initialization method of the SimpleAgent is executed once at the beginning of a simulation.
    ///     It sets an initial Position and an initial State and generates a list of movement directions.
    /// </summary>
    /// <param name="layer">The GridLayer that manages the agents</param>
    public void Init(GridLayer layer)
    {
        _layer = layer;
        Position = new Position(StartX, StartY);
        _directions = CreateMovementDirectionsList();
        _layer.SimpleAgentEnvironment.Insert(this);
    }

    #endregion

    #region Tick

    /// <summary>
    ///     The tick method of the SimpleAgent is executed during each time step of the simulation.
    ///     A SimpleAgent can move randomly. It must stay within the bounds of the GridLayer and cannot move onto grid
    ///     cells that are not routable.
    ///     Near the end of the simulation, a SimpleAgent removes itself from the simulation.
    /// </summary>
    public void Tick()
    {
        MoveRandomly();
        
        if (_layer.GetCurrentTick() == 595)
        {
            RemoveFromSimulation();
        }
    }

    #endregion

    #region Methods
    
    /// <summary>
    ///     Generates a list of eight movement directions that the agent uses for random movement.
    /// </summary>
    /// <returns>The list of movement directions</returns>
    private static List<Position> CreateMovementDirectionsList()
    {
        return new List<Position>
        {
            MovementDirections.North,
            MovementDirections.Northeast,
            MovementDirections.East,
            MovementDirections.Southeast,
            MovementDirections.South,
            MovementDirections.Southwest,
            MovementDirections.West,
            MovementDirections.Northwest
        };
    }
    
    /// <summary>
    ///     Removes this agent from the simulation and, by extension, from the visualization.
    /// </summary>
    private void RemoveFromSimulation()
    {
        Console.WriteLine($"SimpleAgent {ID} is removing itself from the simulation.");
        _layer.SimpleAgentEnvironment.Remove(this);
        UnregisterAgentHandle.Invoke(_layer, this);
    }

    /// <summary>
    ///     Performs one random move, if possible, using the movement directions list.
    /// </summary>
    private void MoveRandomly()
    {
        var nextDirection = _directions[_random.Next(_directions.Count)];
        var newX = Position.X + nextDirection.X;
        var newY = Position.Y + nextDirection.Y;

        // Check if chosen move is within the bounds of the grid
        if (0 <= newX && newX < _layer.Width && 0 <= newY && newY < _layer.Height)
        {
            // Check if chosen move goes to a cell that is routable
            if (_layer.IsRoutable(newX, newY))
            {
                Position = new Position(newX, newY);
                _layer.SimpleAgentEnvironment.MoveTo(this, new Position(newX, newY));
                Console.WriteLine("{0} moved to a new cell: {1}", GetType().Name, Position);
            }
            else
            {
                Console.WriteLine("{0} tried to move to a blocked cell: ({1}, {2})", GetType().Name, newX, newY);
            }
        }
        else
        {
            Console.WriteLine("{0} tried to leave the world: ({1}, {2})", GetType().Name, newX, newY);
        }
    }

    /// <summary>
    ///     Increments the agent's MeetingCounter property value.
    /// </summary>
    public void IncrementCounter()
    {
        MeetingCounter += 1;
    }

    #endregion

    #region Fields and Properties

    public Guid ID { get; set; }
    
    public Position Position { get; set; }
    
    [PropertyDescription(Name = "StartX")]
    public int StartX { get; set; }
    
    [PropertyDescription(Name = "StartY")]
    public int StartY { get; set; }

    public int MeetingCounter { get; private set; }

    public UnregisterAgent UnregisterAgentHandle { get; set; }
    
    private GridLayer _layer;
    private List<Position> _directions;
    private readonly Random _random = new();

    #endregion
}