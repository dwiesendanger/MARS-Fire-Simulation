# NetLogo Fire Simulation using MARS Framework

A high-performance implementation of the [NetLogo Fire model](https://ccl.northwestern.edu/netlogo/models/Fire) using the [MARS Group Multi-Agent Simulation Framework](https://www.mars-group.org/). This simulation models forest fire spread from left to right through a 2D grid with configurable tree density.

## Overview

This project recreates the NetLogo Fire model, which demonstrates how fires spread through forests depending on tree density. The simulation starts with fires ignited along the entire left edge of the grid and observes how they spread eastward through the forest.

### Key Features

- **NetLogo-Accurate Simulation**: Faithful reproduction of the original NetLogo Fire model behavior
- **High Performance**: Optimized algorithms using queue-based processing for large grids (e.g. 250x250)
- **Real-time Visualization**: Python-based pygame visualization with a color-scheme based on the original NetLogo model
- **Configurable Parameters**: Adjustable tree density, grid sizes, and simulation duration
- **Automatic Completion**: Simulation stops automatically when no more cells are burning
- **Multiple Grid Sizes**: Support for various grid dimensions for experimentation

## Simulation Details

### Cell States
- **Empty (0)**: Black cells with no vegetation - fire cannot spread through these
- **Tree (1)**: Green cells containing trees - can be ignited by adjacent burning cells
- **Burning (2)**: Bright red cells currently on fire - spread fire to neighboring trees
- **Burned (3)**: Dark red cells that have finished burning - final state

### Fire Spread Rules
- Fire starts along the entire left column (x=0) of the grid
- Fire spreads to adjacent trees using 4-connected neighborhood (North, South, East, West)
- No diagonal spreading (consistent with NetLogo Fire model)
- Burning cells transition to burned state after one time step
- Simulation ends when no cells are actively burning

## Getting Started

### Prerequisites

**For the MARS Simulation:**
- .NET 8.0 or later

**For the Visualization:**
- Python 3.7+
- pygame
- websocket-client

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/dwiesendanger/MARS-Fire-Simulation.git
   cd MARS-Fire-Simulation
   ```

2. **Install Python dependencies:**
   ```bash
   cd Visualization
   pip install -r requirements.txt
   ```

3. **Build the .NET project:**
   ```bash
   cd ../GridBlueprint
   dotnet build
   ```

### Running the Simulation

**Important:** Always start the visualization first, then the simulation.

1. **Start the visualization (Terminal 1):**
   ```bash
   cd Visualization
   python main.py
   ```
   You should see "Waiting for MARS simulation to start..."

2. **Start the simulation (Terminal 2):**
   ```bash
   cd GridBlueprint
   dotnet run
   ```

The visualization will automatically connect and display the fire simulation in real-time.

## Configuration

### Simulation Parameters

Edit `GridBlueprint/config.json` to customize the simulation.
Here is a sample configuration file:

```json
{
  "globals": {
    "startTime": "2025-08-02T10:00",
    "endTime": "2025-08-02T10:10",
    "deltaTUnit": "seconds",
    "deltaT": 1,
    "output": "csv",
    "pythonVisualization": true
  },
  "layers": [
    {
      "name": "FireLayer",
      "pythonVisualization": true,
      "density": 0.65,
      "file": "Resources/grid_250x250.csv"
    }
  ],
  "agents": [
    {
      "name": "HelperAgent",
      "count": 1
    }
  ]
}
```

### Key Parameters

- **`density`**: Tree density (0.0 = no trees, 1.0 = all trees). Typical values: 0.3-0.9
- **`file`**: Grid size to use. Available options:
    - `Resources/grid_2x2.csv` (2×2 - minimal testing)
    - `Resources/grid.csv` (10×10 - small tests)
    - `Resources/grid_50x25.csv` (50×25 - rectangular)
    - `Resources/grid_50x50.csv` (50×50 - medium)
    - `Resources/grid_250x250.csv` (250×250 - NetLogo standard)
- **`endTime`**: Maximum simulation duration (simulation may end earlier if fire burns out)

### Creating Custom Grid Sizes

Use the included grid generator to create custom grid dimensions:

```bash
cd GridBlueprint/Resources
python grid_generator.py
```

The script will prompt for:
- Grid width (columns)
- Grid height (rows)
- Output filename

All generated grids contain only zeros and will be populated based on the density parameter.

## Performance Optimizations

The simulation includes several performance optimizations for handling large grids:

### Algorithmic Optimizations
- **Queue-based Processing**: Only processes actively burning cells instead of scanning the entire grid
- **HashSet Lookups**: O(1) duplicate detection for burning cells
- **Early Termination**: Automatic simulation completion when no cells are burning
- **Memory Efficiency**: Preallocated data structures to minimize garbage collection

### Performance Characteristics
- **Memory**: ~99% fewer allocations compared to naive grid-scanning approaches
- **CPU**: Processes only ~0.1% of cells in typical scenarios (burning cells vs. total cells)
- **Scalability**: Handles 250×250 grids (62,500 cells) efficiently
- **Typical Runtime**: 20-50 simulation steps for most density configurations

## Project Structure

```
MARS-Fire-Simulation/
├── GridBlueprint/                 # Main .NET simulation project
│   ├── Model/
│   │   ├── Agents/
│   │   │   └── HelperAgent.cs     # Coordinates simulation execution
│   │   ├── Layers/
│   │   │   └── FireLayer.cs       # Core fire simulation logic
│   │   └── Utils/
│   │       └── CellState.cs       # Cell state enumeration
│   ├── Resources/                 # Grid files and utilities
│   │   ├── grid_*.csv             # Various grid sizes
│   │   └── grid_generator.py      # Custom grid creation tool
│   ├── config.json                # Simulation configuration
│   └── Program.cs                 # Application entry point
├── Visualization/                 # Python visualization
│   └── main.py                    # Pygame-based visualization
└── README.md                      # This file
```

## Experimentation

### Density Studies
Experiment with different tree densities to observe percolation effects:

- **Low Density (0.3)**: Sparse trees, fire may not spread across
- **Critical Density (0.59)**: 50/50 chance of fire reaching the right edge
- **High Density (0.9)**: Dense forest, fire spreads easily

### Grid Size Comparisons
- **Small Grids (10×10)**: Quick testing and verification
- **Medium Grids (50×50)**: Detailed observation of spread patterns
- **Large Grids (250×250)**: Statistical analysis and percolation studies

## Technical Implementation

### Fire Spread Algorithm
1. **Initialization**: Generate random forest based on density parameter
2. **Ignition**: Set entire left column to burning state
3. **Propagation**: Each burning cell attempts to ignite adjacent trees
4. **Transition**: Burning cells become burned after spreading fire
5. **Termination**: Simulation ends when no cells remain burning

### MARS Framework Integration
- **RasterLayer**: Manages 2D grid data and spatial operations
- **Agent-Based**: HelperAgent coordinates simulation timing
- **Configuration-Driven**: JSON-based parameter management
- **Visualization Pipeline**: WebSocket communication with Python frontend

## Sample Results

### Typical Simulation Output
```
[FireLayer] Loaded density from config.json: 0.59
HelperAgent initialized
Fire simulation completed at tick 507. No more burning cells.
Burned area: 14326 of 36950 (38.77%)
Fire simulation completed. Stopping simulation...
Successfully executed iterations: 600
```

## References

- [Original NetLogo Fire Model](https://ccl.northwestern.edu/netlogo/models/Fire)
- [MARS Framework Documentation](https://www.mars-group.org/)
