using Godot;
using System;

public partial class GameManager : Node
{
    //Maze Settings
    [Export] public int mazeWidth = 73;  //73 for pathWidth 3 and 71 for pathWidth 1
    [Export] public int mazeHeight = 45; //45 for pathWidth 3 and 41 for pathWidth 1
    [Export] public int cellSize = 512; //This is the size of the wall texture I made
                                        //It was 512x512 pixels, so I use a cell size of 512

    //References
    private MazeGeneration _mazeGenerator; 
    private AstarPathfinding _pathfinding;

    //This is a reference for the AI characters to use to access the pathfinding grid
    public AstarPathfinding Pathfinding => _pathfinding; 


    //This is a reference to the Maze Generator to be used to find the maze grid
    //I use it in the weapon spawner to make sure they get valid spawn locations.
    public MazeGeneration MazeGenerator => _mazeGenerator;

    public override void _Ready() //Ran at start of scene by Godot
    {
        //Get the maze grid from the maze generation algorithm
        _mazeGenerator = new MazeGeneration(mazeWidth, mazeHeight, 3);
        int[,] mazeGrid = _mazeGenerator.GenerateMaze(); //I used this method as opposed to the GetMazeGrid
                                       //This just assured the maze was fully generated before returning it

        //Initialise the A* pathfinding with the maze grid
        _pathfinding = new AstarPathfinding();
        _pathfinding.Initialise(mazeGrid, mazeWidth, mazeHeight, cellSize);

        //Print the maze grid again to make sure that the game manager has a grid that matches the maze one.
        GD.Print("Maze Grid in GameManager script:");
        for (int y = 0; y < mazeHeight; y++)
        {
            string row = "";
            for (int x = 0; x < mazeWidth; x++)
            {
                row += (mazeGrid[x, y] == 0) ? "O " : "X "; //Check if the tile is actualy walkable
            }
            GD.Print(row);
        }
    }
}













