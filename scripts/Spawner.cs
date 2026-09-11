using Godot;
using System;
using System.Collections.Generic;

public partial class Spawner : Node
{
    // Enemy Scenes exported to the editor
    [Export] public PackedScene AiEnemy5Scene;
    [Export] public PackedScene AiEnemy6Scene;
    [Export] public PackedScene AiEnemy7Scene;
    [Export] public PackedScene AiBoss2Scene;

    // Spawning Settings
    [Export] public int SpawnRadius = 8000; // Radius around the center to spawn enemies

    // UI
    [Export] public Label EnemyCountLabel; // Add this in the inspector
    [Export] public Label KillCountLabel; // Add this for kill tracker

    // Static variable for DeathScene to access - starts with 9 enemies + 1 player = 10 total
    public static int alivec = 0;
    public static int playerKills = 0; // Track player kills

    // References
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private GameManager _gameManager;
    private List<CharacterBody2D> _spawnedEnemies = new List<CharacterBody2D>();

    public override void _Ready()
    {
        // Set game mode to battle royale
        Global.IsBattleRoyaleMode = true;

        // Reset the alive count at the start of each battle
        ResetAliveCount();

        // Initialize random number generator
        _rng.Randomize();

        // Get reference to GameManager
        _gameManager = GetNode<GameManager>("../GameManager");

        // Debug for if the path is incorrect
        if (_gameManager == null)
        {
            GD.PrintErr("Game Manager not found");
            return;
        }

        // Start the battle royale spawn
        StartBattleRoyale();
    }

    private void ResetAliveCount()
    {
        alivec = 10; // 9 enemies + 1 player
        playerKills = 0; // Reset kills
        GD.Print($"Reset alive count to: {alivec}, kills to: {playerKills}");
    }

    private void StartBattleRoyale()
    {
        GD.Print("Starting Battle Royale Mode");

        // Spawn 9 enemies: enemy 5, 6, 7 randomly and one boss2
        SpawnBattleRoyaleEnemies();

        // Update enemy count display
        UpdateEnemyCountDisplay();
        UpdateKillCountDisplay();
    }

    private void SpawnBattleRoyaleEnemies()
    {
        // Spawn 8 random enemies (AiEnemy5, AiEnemy6, AiEnemy7)
        for (int i = 0; i < 8; i++)
        {
            SpawnRandomEnemy();
        }

        // Spawn 1 AiBoss2
        SpawnBoss2();
    }

    private void SpawnRandomEnemy()
    {
        // Randomly choose between enemy 5, 6, and 7
        float randomValue = GD.Randf();
        CharacterBody2D enemy;

        if (randomValue < 0.33f)
        {
            enemy = (CharacterBody2D)AiEnemy5Scene.Instantiate();
        }
        else if (randomValue < 0.66f)
        {
            enemy = (CharacterBody2D)AiEnemy6Scene.Instantiate();
        }
        else
        {
            enemy = (CharacterBody2D)AiEnemy7Scene.Instantiate();
        }

        SpawnEnemyAtPosition(enemy);
    }

    private void SpawnBoss2()
    {
        CharacterBody2D boss = (CharacterBody2D)AiBoss2Scene.Instantiate();
        SpawnEnemyAtPosition(boss);
    }

    private void SpawnEnemyAtPosition(CharacterBody2D enemy)
    {
        // Calculate a valid spawn position around the map
        Vector2 spawnPosition = GetValidSpawnPosition();

        // Add enemy to scene
        AddChild(enemy);
        enemy.GlobalPosition = spawnPosition;
        enemy.Visible = true;

        // Set Z index and collision properties after a short delay
        GetTree().CreateTimer(0.1f).Timeout += () =>
        {
            enemy.ZIndex = 21;
            enemy.CollisionLayer = 1 << 2; // Layer 3 (AIEnemies)
            enemy.CollisionMask = (1 << 0) | (1 << 2) | (1 << 3); // Layer 1 (Player) + Layer 3 (AIEnemies) + Layer 4 (PlayerProjectiles)
        };

        // Initialize the enemy with pathfinding and target
        InitializeEnemy(enemy);

        // Add to tracking list
        _spawnedEnemies.Add(enemy);

        GD.Print("Spawning enemy type: " + enemy.GetType().Name + " at " + spawnPosition);
    }

    private void InitializeEnemy(CharacterBody2D enemy)
    {
        if (_gameManager.Pathfinding == null)
        {
            GD.Print("Pathfinding grid is null in the Spawner");
            return;
        }

        // Handle enemy-specific initialization
        if (enemy is AiEnemy5 aiEnemy5)
        {
            aiEnemy5.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy5.UpdateTarget();
            aiEnemy5.Died += OnEnemyDied;
            aiEnemy5.AddToGroup("mobs");
        }
        else if (enemy is AiEnemy6 aiEnemy6)
        {
            aiEnemy6.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy6.UpdateTarget();
            aiEnemy6.Died += OnEnemyDied;
            aiEnemy6.AddToGroup("mobs");
        }
        else if (enemy is AiEnemy7 aiEnemy7)
        {
            aiEnemy7.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy7.UpdateTarget();
            aiEnemy7.Died += OnEnemyDied;
            aiEnemy7.AddToGroup("mobs");
        }
        else if (enemy is AiBoss2 aiBoss2)
        {
            aiBoss2.SetPathfindingGrid(_gameManager.Pathfinding);
            aiBoss2.UpdateTarget();
            aiBoss2.Died += OnEnemyDied;
            aiBoss2.AddToGroup("mobs");
        }
    }

    public void OnEnemyDied()
    {
        GD.Print("Enemy died in battle royale mode");

        // Decrease the alive count when an enemy dies
        alivec--;
        GD.Print($"Enemy died! Remaining contestants: {alivec}");

        // Find and hide the dead enemy
        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null && IsInstanceValid(enemy))
            {
                // Check if this enemy is dead by looking at its properties
                if (IsEnemyDead(enemy))
                {
                    HideEnemy(enemy);
                }
            }
        }

        // Update enemy count display
        UpdateEnemyCountDisplay();

        // Check victory condition
        CheckVictoryCondition();
    }

    // Call this method from player's bullet script when it kills an enemy
    public void RegisterPlayerKill()
    {
        playerKills++;
        GD.Print($"PLAYER GOT A KILL! Total kills: {playerKills}");
        UpdateKillCountDisplay();
    }

    private void UpdateEnemyCountDisplay()
    {
        // Calculate enemies remaining (excluding player)
        int enemiesRemaining = Math.Max(0, alivec - 1); // Subtract 1 for the player

        if (EnemyCountLabel != null)
        {
            EnemyCountLabel.Text = $"Enemies Remaining: {enemiesRemaining}";
        }
    }

    private void UpdateKillCountDisplay()
    {
        if (KillCountLabel != null)
        {
            KillCountLabel.Text = $"Kills: {playerKills}";
        }
    }

    private bool IsEnemyDead(CharacterBody2D enemy)
    {
        // Check common properties that indicate an enemy is dead
        if (enemy.CollisionLayer == 0) return true; // Collision disabled when dead

        // You might need to add specific checks based on your enemy implementation
        // For example, if enemies have an IsDead property:
        if (enemy.HasMethod("IsDead") && (bool)enemy.Call("IsDead")) return true;

        // Or check if they're not visible (already hidden)
        if (!enemy.Visible) return true;

        return false;
    }

    private void HideEnemy(CharacterBody2D enemy)
    {
        if (enemy == null || !IsInstanceValid(enemy)) return;

        // Hide the enemy
        enemy.Visible = false;

        // Disable collisions
        enemy.CollisionLayer = 0;
        enemy.CollisionMask = 0;

        // Stop any processing
        enemy.SetProcess(false);
        enemy.SetPhysicsProcess(false);

        GD.Print($"Hidden enemy: {enemy.GetType().Name}");
    }

    private Vector2 GetValidSpawnPosition()
    {
        // Use the center of the map (0,0) or player position if available
        Vector2 centerPosition = Vector2.Zero;

        // If player exists, use player position as center
        var player = GetNodeOrNull<player>("../Player");
        if (player != null)
        {
            centerPosition = player.GlobalPosition;
        }

        Vector2 spawnPosition;
        int attempts = 0;

        // Try to find a valid spawn position within a limited number of attempts
        do
        {
            // Generate a random angle and distance
            float angle = _rng.RandfRange(0, Mathf.Tau);
            float distance = _rng.RandfRange(SpawnRadius * 0.5f, SpawnRadius);

            // Calculate the spawn position relative to center
            spawnPosition = centerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            // Convert to grid coordinates and check if walkable
            Vector2I gridPosition = _gameManager.Pathfinding.WorldToMap(spawnPosition);
            bool walkable = _gameManager.Pathfinding.IsWalkable(gridPosition);

            if (walkable)
            {
                return spawnPosition;
            }

            attempts++;
        } while (attempts < 20);

        // If no valid position found, use a safe default
        GD.PrintErr("Failed to find a valid spawn position, using default");
        return new Vector2(1000, 800);
    }

    private void CheckVictoryCondition()
    {
        GD.Print($"Contestants remaining: {alivec}");

        // If only one contestant remains (the winner), trigger victory
        if (alivec <= 1)
        {
            GD.Print("Battle Royale complete! Winner found!");
            GD.Print($"Player finished with {playerKills} kills");

            // Optional: Show winner or transition scene
            // GetTree().ChangeSceneToFile("victory_scene.tscn");

            // Update display for final count
            UpdateEnemyCountDisplay();
            UpdateKillCountDisplay();
        }
    }

    // Cleanup method
    public override void _ExitTree()
    {
        // Clean up spawned enemies
        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null && IsInstanceValid(enemy))
            {
                enemy.QueueFree();
            }
        }
        _spawnedEnemies.Clear();
    }

    // Static method to reset the count (call this when starting a new battle)
    public static void ResetBattleRoyale()
    {
        alivec = 10;
        playerKills = 0;
        GD.Print("Battle Royale count reset to 10, kills reset to 0");
    }
}