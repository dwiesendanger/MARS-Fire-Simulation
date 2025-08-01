using System;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Environments;
using GridBlueprint.Model.Layers;

namespace GridBlueprint.Model.Agents;

public class HelperAgent : IAgent<GridLayer>, IPositionable
{
    public void Init(GridLayer layer)
    {
        _layer = layer;
    }

    public void Tick()
    {
        // The simulation sends layer data to the visualization web socket only when a change in layer data occurs.
        // Therefore, we make an initial layer access to send layer data to web socket for visualization.
        _layer[0, 0] = _layer[0, 0];
    }

    public Guid ID { get; set; }
    public Position Position { get; set; }
    private GridLayer _layer;
}