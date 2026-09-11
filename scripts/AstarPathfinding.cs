using Godot;
using System;
using System.Collections.Generic;

public partial class AstarPathfinding : Node
{
    public int[,] grid; //2D array representing the grid which will match the maze's one
                        //each cell can be walkable (0) or represent a wall from the maze, so blocked (1)
    public int width, height; //Dimensions of the grid. Width and height define the size of the grid
    public int cellSize; //This represents the size of each cell in the grid (maze wall textre size which was 64x64)
                         //I use this to convert between grid and world coordinates

    public AstarPathfinding() { } //Here is the default constructor
                                  //Like MazeGeneration it's Parameterless because of Godot requirements and matches class name

    //This initialises the A* pathfinding algorithm with the grid and its properties - Called by the GameManager.
    public void Initialise(int[,] grid, int width, int height, int cellSize)
    {
        this.grid = grid; //Assign the grid to the class-level variable
        this.width = width; //Assign the width of the grid
        this.height = height; //Assign the height of the grid
        this.cellSize = cellSize; //Assign the cell size, which matches the size of the walls in the maze

        GD.Print("Maze in A* : ");
        //I printed the grid to the debug console to compare it with the maze grid
        for (int y = 0; y < height; y++)
        {
            string row = "";
            for (int x = 0; x < width; x++)
            {
                //"O" represents a walkable path, "X" represents a blocked cell (same as in maze algorithm!)
                row += IsWalkable(new Vector2I(x, y)) ? "O " : "X ";
            }
            GD.Print(row); //Print each row of the grid to compare with that of the maze
        }
    }

    //This method finds the shortest path from the AI's current position to the target
    //(either the player or another enemy depending on their family) using the A* algorithm
    //It is called by all AI enemies and allies to find the shortest path to the target
    public List<Vector2> FindPath(Vector2I start, Vector2I target)
    {
        var openSet = new PriorityQueue<Node>(); //These are the nodes to be evaluated, prioritised by their fScore
                                                 //(just the total distance)
        var closedSet = new HashSet<Vector2I>(); //The set of nodes that have already been evaluated
        var cameFrom = new Dictionary<Vector2I, Vector2I>(); //Tracks the best path by storing the previous node for each node
        var gScore = new Dictionary<Vector2I, float>(); //Cost from the start node to each node (basically how far away it is)
        var fScore = new Dictionary<Vector2I, float>(); //Estimated total cost from start to target through each node
                                                        //(gScore + heuristic (AKA hScore))
        //hScore (the estimate of cost to move from current to target node) isnt stored seperately
        //this is because it's calculated whenever it's needed (when updating the fscore).
        //It's also combined with the Gscore anyway so theres no point

        gScore[start] = 0; //Obviously, the cost from the start node to itself is 0
        fScore[start] = Heuristic(start, target); //We use this as the estimated total cost from start to target
        openSet.Enqueue(new Node(start, fScore[start])); //Then we begin by adding the start node to the open set

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue().Position; //Now while the open set has values,
                                                      //we get the node with the lowest fScore from it

            if (current == target) //Once the target node is reached, then reconstruct and return the path
            {
                return ReconstructPath(cameFrom, current);
            }

            closedSet.Add(current); //Then mark the current node as evaluated by adding it to the closed set

            //Now evaluate each neighbour of the current node
            foreach (var neighbour in GetNeighbours(current))
            {
                //Skip the neighbour if it has already been evaluated or is not walkable
                if (closedSet.Contains(neighbour) || !IsWalkable(neighbour))
                {
                    continue;
                }

                float tentativeGScore = gScore[current] + 1; //Calculate the tentative cost to reach the neighbour
                                                             //(assuming a uniform cost)

                //See if this path to the neighbour is better than any previous path then update the scores
                if (!gScore.ContainsKey(neighbour) || tentativeGScore < gScore[neighbour])
                {
                    cameFrom[neighbour] = current; //Record the best path to the neighbour
                    gScore[neighbour] = tentativeGScore; //Then Update the gScore for the neighbour
                    fScore[neighbour] = tentativeGScore + Heuristic(neighbour, target); //Also update the fScore for the neighbour

                    //If the neighbour is not already in the open set, then we can add it
                    if (!openSet.Contains(new Node(neighbour, fScore[neighbour])))
                    {
                        openSet.Enqueue(new Node(neighbour, fScore[neighbour])); //Add to the open set
                    }
                }
            }
        }

        return null; //This is if no path is found
    }

    //This method reconstructs the path from the `cameFrom` dictionary.
    private List<Vector2> ReconstructPath(Dictionary<Vector2I, Vector2I> cameFrom, Vector2I current)
    {
        var path = new List<Vector2> { MapToWorld(current) }; //Start with the target position in world coordinates
        while (cameFrom.ContainsKey(current)) //Then, traverse back to the start node
        {
            current = cameFrom[current]; //Move to the previous node in the path
            path.Insert(0, MapToWorld(current)); //Insert the node at the beginning of the path to maintain the order
        }
        return path; //Return the reconstructed path in world coordinates
    }

    //This method returns the walkable neighbours of a given position.
    private IEnumerable<Vector2I> GetNeighbours(Vector2I position)
    {
        var directions = new Vector2I[] //Define the four possible directions
        {
            new Vector2I(0, -1), //Up
            new Vector2I(0, 1),  //Down
            new Vector2I(-1, 0), //Left
            new Vector2I(1, 0)   //Right
        };

        //Check each direction for valid neighbours.
        foreach (var dir in directions)
        {
            var neighbour = position + dir; //Calculate the neighbour's position
            if (IsInBounds(neighbour)) //Ensure the neighbour is within the grid bounds
            {
                yield return neighbour; //If all met, return the valid neighbour
            }
        }
    }

    //This method checks if a position is within the grid bounds
    private bool IsInBounds(Vector2I position)
    {
        return position.X >= 0 && position.X < width && position.Y >= 0 && position.Y < height;
    }

    //This method checks if a position is walkable and not blocked,
    //making sure that the AI can navigate to the player through a valid path.
    public bool IsWalkable(Vector2I position)
    {
        //Check if the position is actually within the grid bounds
        if (!IsInBounds(position))
        {
            GD.Print("Position isnt walkable"); //Debug so that I could actually check when the position was outside the bounds.
                                                //Initially the bounds weren't set right
            return false; //not walkable
        }
        return grid[position.X, position.Y] == 0; //If it is in bounds set it to 0 to represent a walkable path for the AI
    }

    //This method calculates the heuristic (Manhattan distance) between two positions
    private float Heuristic(Vector2I a, Vector2I b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); //This is the Manhattan distance formula
        //It pretty much just calculates the actual distance between the two points (x1-x2) + (y1-y2)
    }

    //This method converts world coordinates to grid coordinates.
    public Vector2I WorldToMap(Vector2 worldPosition) //takes a value for the current world position when called
                                                      //(Just the global position of the AI node)
    {
        return new Vector2I((int)(worldPosition.X / cellSize), (int)(worldPosition.Y / cellSize));
    }

    //This method converts grid coordinates to world coordinates.
    public Vector2 MapToWorld(Vector2I mapPosition)
    {
        return new Vector2(mapPosition.X * cellSize + cellSize / 2, mapPosition.Y * cellSize + cellSize / 2);
    }

    //This class represents a node in the A* algorithm.
    private class Node : IComparable<Node>
    {
        public Vector2I Position { get; } //Grid position of the node
        public float Priority { get; } //Priority (fScore) for the priority queue

        public Node(Vector2I position, float priority)
        {
            Position = position;
            Priority = priority;
        }

        //This method just compares nodes based on their priority
        public int CompareTo(Node other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }

    //This class implements a priority queue for the A* algorithm.
    private class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> _elements = new List<T>(); //Internal list to store elements

        public int Count => _elements.Count; //Number of elements in the queue

        //This method adds an item to the priority queue.
        public void Enqueue(T item)
        {
            _elements.Add(item);
            int childIndex = _elements.Count - 1;
            //Maintain the heap property by bubbling up the new item
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (_elements[childIndex].CompareTo(_elements[parentIndex]) >= 0)
                {
                    break;
                }
                (_elements[childIndex], _elements[parentIndex]) = (_elements[parentIndex], _elements[childIndex]);
                childIndex = parentIndex;
            }
        }

        //This method removes and returns the item with the highest priority.
        public T Dequeue()
        {
            //Get the index of the last item
            int lastIndex = _elements.Count - 1;

            T frontItem = _elements[0]; //We grab the item with the highest priority (always at root so index is 0)

            _elements[0] = _elements[lastIndex]; //Move the last item to the front (root postion)
                                                 //this will maintains the structure after removing the root

            //Remove the last item since it has been moved to the root
            _elements.RemoveAt(lastIndex); 

            //The new root item may be larger than its children,
            //so we need to move it down to its correct position
            int parentIndex = 0; // Start at the root (0)

            //This loop continues until its structures fully restored
            while (true)
            {
                //Calculate the indices of the left and right children of the current parent
                int leftChildIndex = parentIndex * 2 + 1; //Left child index
                int rightChildIndex = leftChildIndex + 1; //Right child index

                //Check if the left child exists
                //If the left child index is out of bounds, the parent has no children, so we can stop
                if (leftChildIndex >= _elements.Count)
                {
                    break; //Exit
                }

                
                //If the right child exists and is smaller than the left child, use the right child
                //Otherwise, we just use the left child
                int minChildIndex = (rightChildIndex < _elements.Count && _elements[rightChildIndex].CompareTo(_elements[leftChildIndex]) < 0)
                    ? rightChildIndex //Right child is smaller
                    : leftChildIndex; //Left child is smaller (or is the only child)

                //Check if the parent is smaller than or equal to the smallest child
                if (_elements[parentIndex].CompareTo(_elements[minChildIndex]) <= 0)
                {
                    break; //Exit the loop if so
                }

                //Swap the parent with the smallest child
                //This moves the parent down and the child up
                (_elements[parentIndex], _elements[minChildIndex]) = (_elements[minChildIndex], _elements[parentIndex]);

                //Update the parent index to the position of the smallest child
                //This allows the loop to continue checking and restoring the heap property at the new level
                parentIndex = minChildIndex;
            }

            //Return the item that was originally at the root (this is the highest priority item)
            return frontItem;
        }

        //This method just checks if the queue contains a specific item.
        public bool Contains(T item)
        {
            return _elements.Contains(item); 
        }
    }
}