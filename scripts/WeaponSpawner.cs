using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class WeaponSpawner : Node2D
{
    //Weapon and Item scenes
    [Export] public PackedScene ShotgunScene;
    [Export] public PackedScene ARScene;
    [Export] public PackedScene PistolScene;
    [Export] public PackedScene MedkitScene;
    [Export] public PackedScene InstaKillScene;
    [Export] public PackedScene AllyPowerupScene;
    [Export] public PackedScene SpeedBoostScene;
    [Export] public PackedScene FasterFireRateScene;
    [Export] public PackedScene ShieldScene;

    //Spawning Settings
    [Export] public float SpawnInterval = 3.3f;
    [Export] public float SpawnRadius = 800f;
    [Export] public int MaxSpawnAttempts = 10;
    [Export] public int MaxWeapons = 6, MaxMedkits = 11, MaxShields = 6, MaxInstaKills = 2;
    [Export] public int MaxAllyPowerups = 1, MaxSpeedBoosts = 8, MaxFasterFireRates = 6;
    [Export] public float WeaponSpawnChance = 0.7f;

    //References
    private Timer _spawnTimer, _moveTimer;
    private RandomNumberGenerator _rng = new();
    private GameManager _gameManager;
    private player _player;
    private Dictionary<string, List<Node2D>> _spawnedItems = new();

    public override void _Ready()
    {
        //Declare random number
        _rng.Randomize();

        //Get game manager and player nodes
        _gameManager = GetNode<GameManager>("../GameManager");
        _player = GetNode<player>("../Player");

        //Set up a dictionary for spawned weapons/items
        _spawnedItems["Weapons"] = new List<Node2D>();
        _spawnedItems["Medkits"] = new List<Node2D>();
        _spawnedItems["InstaKills"] = new List<Node2D>();
        _spawnedItems["AllyPowerups"] = new List<Node2D>();
        _spawnedItems["SpeedBoosts"] = new List<Node2D>();
        _spawnedItems["FasterFireRates"] = new List<Node2D>();
        _spawnedItems["Shields"] = new List<Node2D>();

        //Set up timers
        _spawnTimer = new Timer { WaitTime = SpawnInterval, Autostart = true, OneShot = false };
        AddChild(_spawnTimer);
        _spawnTimer.Timeout += SpawnRandomItem;

        _moveTimer = new Timer { WaitTime = 120.0f, Autostart = true, OneShot = false };
        AddChild(_moveTimer);
        _moveTimer.Timeout += MoveWeapons;
    }

    //Method to select a random weapon or item to spawn
    private void SpawnRandomItem()
    {
        float rand = _rng.Randf(); //declare random float from 0 to 1

        if (rand < 0.15f && _spawnedItems["Weapons"].Count < MaxWeapons)
            SpawnItem(SelectRandomWeapon(), "Weapons");
        else if (rand < 0.30f && _spawnedItems["Medkits"].Count < MaxMedkits)
            SpawnItem(MedkitScene, "Medkits");
        else if (rand < 0.40f && _spawnedItems["AllyPowerups"].Count < MaxAllyPowerups)
            SpawnItem(AllyPowerupScene, "AllyPowerups");
        else if (rand < 0.55f && _spawnedItems["SpeedBoosts"].Count < MaxSpeedBoosts)
            SpawnItem(SpeedBoostScene, "SpeedBoosts");
        else if (rand < 0.70f && _spawnedItems["Shields"].Count < MaxShields)
            SpawnItem(ShieldScene, "Shields");
        else if (rand < 0.85f && _spawnedItems["FasterFireRates"].Count < MaxFasterFireRates)
            SpawnItem(FasterFireRateScene, "FasterFireRates");
        else if (InstaKillScene != null && _spawnedItems["InstaKills"].Count < MaxInstaKills)
            SpawnItem(InstaKillScene, "InstaKills");
        else
            //output so I could see that the limit to weapon spawns was working
            GD.Print("Skipped spawn: Max limit reached."); 
    }

    private void SpawnItem(PackedScene scene, string itemType)
    {
        //Check if the scene is null (e.g., if the PackedScene is not assigned in the editor)
        if (scene == null) return;

        //Get a valid spawn position using the GetValidSpawnPosition method
        Vector2 spawnPosition = GetValidSpawnPosition();
        //If no valid position is found, exit the method
        if (spawnPosition == Vector2.Zero) return;

        //Instantiate the item from the provided PackedScene
        Node2D itemInstance = scene.Instantiate<Node2D>();
        //If instantiation fails, exit the method
        if (itemInstance == null) return;

        //If the item is a Weapon, set its WeaponHolder reference to the player's WeaponHolder node,
        //so it appears in the right position on the player
        if (itemInstance is Weapon weaponInstance)
            weaponInstance.WeaponHolder = _player.GetNode<Node2D>("WeaponHolder");

        //Add the item to Godot's scene tree
        AddChild(itemInstance);
        // Set the item's position to the valid spawn position
        itemInstance.GlobalPosition = spawnPosition;
        // Add the item to the corresponding list in the _spawnedItems dictionary
        _spawnedItems[itemType].Add(itemInstance);
    }

    //This method selects a random weapon, out of the pistol, shotgun and assault rifle.
    private PackedScene SelectRandomWeapon()
    {
        float rand = GD.Randf();
        if (rand < 0.33f) return ShotgunScene; //33% chance of each
        else if (rand < 0.66f) return ARScene;
        else return PistolScene;
    }

    //This method gets the valid spawn position by using the maze grid,
    //an alternative method is to use the pathfinding grid for this (like the enemy spawner)
    private Vector2 GetValidSpawnPosition()
    {
        //Get the maze grid from the MazeGenerator in the GameManager
        int[,] mazeGrid = _gameManager.MazeGenerator.GetMazeGrid();
        //Get the width and height of the maze grid
        int width = mazeGrid.GetLength(0), height = mazeGrid.GetLength(1);

        //Try to find a valid spawn position within a limited number of attempts
        for (int attempts = 0; attempts < MaxSpawnAttempts; attempts++)
        {
            //Generate random X and Y coordinates within the maze bounds
            int randomX = _rng.RandiRange(0, width - 1);
            int randomY = _rng.RandiRange(0, height - 1);

            //Check if the grid position is walkable (0 in the maze grid)
            if (mazeGrid[randomX, randomY] == 0)
            {
                //Convert the grid position to world coordinates using the Pathfinding system
                return _gameManager.Pathfinding.MapToWorld(new Vector2I(randomX, randomY));
            }
        }

        //If no valid position is found after all attempts, return a default position (top-left corner)
        return new Vector2(1000, 800);
    }

    //This method moves the spawned weapons after 120 seconds, to make it easier for the player to get a better weapon,
    //for if one is spawned deep into a maze far away
    private void MoveWeapons()
    {
        foreach (var weapon in _spawnedItems["Weapons"]) //It moves all the weapons that have been spawned at once
        {
            //Get a new valid spawn position
            Vector2 newPosition = GetValidSpawnPosition();
            //If a valid position is found, move the weapon to that position
            if (newPosition != Vector2.Zero)
                weapon.GlobalPosition = newPosition;
        }
    }
}