using Godot;
using System;

public partial class AiPowerUp : Area2D
{
    [Export] private PackedScene allyScene;
    private GameManager _gameManager;
    private player _player;

    public override void _Ready()
    {
        //Reference the player and GameManager node
        _gameManager = GetNode<GameManager>("/root/Game/GameManager");
        _player = GetNode<player>("/root/Game/Player");
        BodyEntered += OnBodyEntered;
    }

    //The method is triggered when a body enters the power-up's collision area
    //specifically, the player
    private void OnBodyEntered(Node body)
    {
        if (body is player playerRef) //Ensure only the player can pick it up
        {
            SpawnAlly(playerRef);
            QueueFree();
        }
    }

    private void SpawnAlly(player playerRef)
    {
        if (allyScene == null)
        {
            //Make sure I actually assigned it in the editor
            GD.PrintErr("Ally scene not assigned in the editor");
            return;
        }
        //Instantiate the ally scene ready for spawning
        Node2D allyInstance = allyScene.Instantiate<Node2D>();
        //Add the ally instance to the current scene
        GetTree().CurrentScene.CallDeferred("add_child", allyInstance);
        //Set the ally's position to the player's position
        allyInstance.GlobalPosition = playerRef.GlobalPosition;

        //Check if the instantiated object is of type AiAlly1
        if (allyInstance is AiAlly1 ally)
        {
            //Initialise the ally with the pathfinding grid and player reference
            //by calling these functions from the ally script.
            ally.SetPathfindingGrid(_gameManager.Pathfinding); 
            ally.SetPlayer(_player);
        }
    }
}
