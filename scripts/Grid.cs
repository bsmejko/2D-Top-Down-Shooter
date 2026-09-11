using Godot;
using System;

public partial class Grid : Node
{
    public int gridWidth;
    public int gridHeight;
    public float nodeSize;

    public Grid(int width, int height, float size)
    {
        gridWidth = width;
        gridHeight = height;
        nodeSize = size;
    }

    public Grid PathfindingGrid { get; private set; }

    [Export] private int exportWidth = 20;
    [Export] private int exportHeight = 20;
    [Export] private float exportSize = 32f;

    public override void _Ready()
    {
        PathfindingGrid = new Grid(exportWidth, exportHeight, exportSize);
        GD.Print("Grid initialized!");
    }
}

