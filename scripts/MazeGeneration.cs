using Godot;
using System;
using System.Collections.Generic;

public partial class MazeGeneration : Node
{
    //Dimensions of the maze grid
    public int width, height;

    //The maze grid itself: a 2D array where 1 represents a wall and 0 represents a carved path
    private int[,] grid;

    //Random number generator for selecting random neighbours
    private Random random;

    //Stack used for recursive backtracking during maze generation
    private Stack<Vector2I> backtrackingStack = new Stack<Vector2I>();

    //The four cardinal directions for neighbour checking: up, down, left, right
    private Vector2I[] directions = { new Vector2I(0, -1), new Vector2I(0, 1), new Vector2I(-1, 0), new Vector2I(1, 0) };

    //The width of corridors. A larger pathWidth creates thicker corridors, lower pathWidths may be used in higher rounds
    private int pathWidth = 3; //Starting with 3 (reasonably wide, so its easier for player)

    //Static variable to store the seed
    private static int _seed;



    public MazeGeneration() //This has to be parameterless due to Godot's requirements 
    {
        //Maze Dimensions, I have noted the specific values for if I need to alterate the pathWidth
        width = 73; //For pathWidth 3 (or 71 for pathWidth 1).

        height = 45; //For pathWidth 3 (or 41 for pathWidth 1).

        //Initialise the grid with the given dimensions.
        grid = new int[width, height];


        //Initialise the random generator with the static seed

        random = new Random();
        _seed = random.Next();


        random = new Random(_seed);
        GD.Print("MazeGeneration Parameterless Constructor Seed: " + _seed);
    }

    public int Seed()
    {
        return _seed;
    }

    //Constructor with parameters for manual initialisation (due to Godot requirement earlier)
    public MazeGeneration(int width, int height, int pathWidth = 3)
    {
        this.width = width;
        this.height = height;
        this.pathWidth = pathWidth;
        grid = new int[width, height];

        //Store the seed in the static variable

        //Use the provided seed for the random generator
        random = new Random(_seed);
        GD.Print("MazeGeneration Parameterized Constructor Seed: " + _seed);
    }

    //called when the node enters the scene tree for the first time
    //(Godot automatically runs this function in the scripts for each node).
    //So I am just simply calling the method I want to start with here.
    public override void _Ready()
    {
        //Post a message in the Godot Debug to show the Maze Grid has initiated and to declare which grids being printed
        //I print out the grid later on so this is just to make it clear in the debug that its the Maze Grid
        GD.Print("Maze Grid in MazeGeneration script: ");

        //Run the GenerateMaze function first
        GenerateMaze();

        //I added a print grid, during creation so that I could see if the Maze Grid and pathfinding grid matched up,
        //this is what lead me to using the seed (they didn't match up initially due to the randomness)
        PrintGrid();
    }

    //Generates the maze and returns the grid
    public int[,] GenerateMaze()
    {
        //Initialise the grid with walls (1) everywhere to start with
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = 1; //1 indicates a wall
            }
        }

        //Define the starting position for the maze generation
        //I set Starting at (1,1) to ensures that there is a border of walls around the maze
        //(initially it missed out walls on edges due to positioning)

        Vector2I startPosition = new Vector2I(1, 1);

        //Mark the starting cell as a path (0)
        grid[startPosition.X, startPosition.Y] = 0;

        //Push the starting cell onto the backtracking stack
        backtrackingStack.Push(startPosition);

        //Continue generation until there are no more cells to backtrack to
        while (backtrackingStack.Count > 0)
        {
            //Peek at the top of the stack to get the current cell
            Vector2I currentCell = backtrackingStack.Peek();

            //Retrieve a list of unvisited neighbouring cells
            List<Vector2I> unvisitedNeighbours = GetUnvisitedNeighbours(currentCell);

            //If there are any unvisited neighbours:
            if (unvisitedNeighbours.Count > 0)
            {
                //Randomly choose one neighbour from the list
                Vector2I chosenNeighbour = unvisitedNeighbours[random.Next(unvisitedNeighbours.Count)];

                //Call the CarvePath method to carve a path between the current cell and the chosen neighbour
                CarvePath(currentCell, chosenNeighbour);

                //Mark the chosen neighbour as visited (set it to 0, a path)
                grid[chosenNeighbour.X, chosenNeighbour.Y] = 0;

                //Push the chosen neighbour onto the stack
                backtrackingStack.Push(chosenNeighbour);
            }
            else
            {
                //If no unvisited neighbours exist, backtrack by popping the current cell off the stack
                backtrackingStack.Pop();
            }
        }

        //After the maze is generated, render it to the scene (placing walls)
        RenderMaze();

        //Return the generated maze grid. This is taken by the game manager to link the maze to the pathfinding
        return grid;
    }

    //Returns a list of unvisited neighbours (cells that are still walls) at a distance of pathWidth+1 from the given cell
    //The distance makes sure that corridors are carved correctly
    private List<Vector2I> GetUnvisitedNeighbours(Vector2I cell)
    {
        List<Vector2I> neighbours = new List<Vector2I>();

        foreach (Vector2I direction in directions)
        {
            //Calculate the neighbour position by moving (pathWidth + 1) cells in the current direction
            //This makes sure there is always a pathwidth even if its set to 1
            Vector2I neighbour = cell + direction * (pathWidth + 1);

            //Check if the neighbour is within the bounds and still a wall
            if (IsInBounds(neighbour) && grid[neighbour.X, neighbour.Y] == 1)
            {
                neighbours.Add(neighbour);
            }
        }

        return neighbours;
    }

    //Carves a path between two cells (start and end) in the grid
    private void CarvePath(Vector2I start, Vector2I end)
    {
        //Determine the direction to carve by subtracting the start from the end, then getting the sign
        Vector2I direction = (end - start).Sign();
        Vector2I current = start;

        //Continue moving from the start cell towards the end cell
        while (current != end)
        {
            //Move one step in the chosen direction
            current += direction;

            //For each cell within the corridor width, mark it as a path
            for (int i = 0; i < pathWidth; i++)
            {
                for (int j = 0; j < pathWidth; j++)
                {
                    int x = current.X + i;
                    int y = current.Y + j;

                    //Check if the position is in bounds
                    if (IsInBounds(new Vector2I(x, y)))
                    {
                        grid[x, y] = 0; //If it's in bounds mark as 0 to indicate a carved path
                    }
                }
            }
        }
    }

    //This function checks whether a given position is within the bounds of the grid
    private bool IsInBounds(Vector2I position)
    {
        //Simple bounds check to ensure x and y are within 0 and within the boundary of the maze grid
        return position.X >= 0 && position.X < width && position.Y >= 0 && position.Y < height;
    }

    //This function is used to actually render the maze by iterating through the grid
    //and placing wall nodes where grid values are 1
    private void RenderMaze()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //If the cell is marked as a wall (1), place a wall at that position
                if (grid[x, y] == 1)
                {
                    PlaceWall(x, y); //call this function for the walls
                }
            }
        }
    }

    //This just creates and places the walls into the scene at the given grid coordinates
    public void PlaceWall(int x, int y)
    {
        //First load the wall texture from the assets
        var wallTexture = (Texture2D)GD.Load("res://assets/Environment/Tile.png");
        if (wallTexture == null)
        {
            GD.PrintErr("Failed to load wall texture!");
            return;
        }

        //Create a StaticBody2D for the wall
        var wallBody = new StaticBody2D();

        //Create a Sprite2D to render the wall texture
        var wallSprite = new Sprite2D();
        wallSprite.Texture = wallTexture;
        wallSprite.Centered = false; //Allign the sprite to the top-left of the grid cell

        //Calculate the world position based on grid coordinates and texture size
        Vector2 position = new Vector2(x * wallTexture.GetWidth(), y * wallTexture.GetHeight());
        wallSprite.Position = position;

        //Create a collision shape for the wall
        var collisionShape = new CollisionShape2D();
        var shape = new RectangleShape2D();

        //Set the shape's size to match the texture
        shape.Size = wallTexture.GetSize();
        collisionShape.Shape = shape;

        //Position the collision shape to match the top-left alignment of the sprite
        collisionShape.Position = position + (shape.Size / 2); //Adjust for top-left alignment

        //Add the sprite and collision shape as children of the StaticBody2D
        wallBody.CollisionLayer = 1 << 1;
        wallBody.CollisionMask = 1 & 4 & 5;
        wallBody.AddChild(wallSprite);
        wallBody.AddChild(collisionShape);

        //Finally, add the wall node to the scene
        AddChild(wallBody);
    }

    //I created this just for if I needed to return just the maze grid. Otherwise, this isn't needed.
    public int[,] GetMazeGrid()
    {
        return grid;
    }

    //This prints the maze grid to the console for debugging (so I could compare the pathfinding grid to the maze one)
    private void PrintGrid()
    {
        for (int y = 0; y < height; y++)
        {
            string row = "";
            for (int x = 0; x < width; x++)
            {
                // "O" represents a path (0) and "X" represents a wall (1)
                row += (grid[x, y] == 0) ? "O " : "X ";
            }
            GD.Print(row);
        }
    }
}

//End of script. 