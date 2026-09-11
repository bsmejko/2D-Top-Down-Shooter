using Godot;
using System;
using System.Collections.Generic;

public partial class EnemySpawner : Node
{
    //Enemy Scenes exported to the editor so that I could select them
    [Export] public PackedScene AiEnemy1Scene; 
    [Export] public PackedScene AiEnemy2Scene; 
    [Export] public PackedScene AiEnemy3Scene; 
    [Export] public PackedScene AiEnemy4Scene;
    [Export] public PackedScene AiEnemy5Scene;
    [Export] public PackedScene AiEnemy6Scene;
    [Export] public PackedScene AiEnemy7Scene;
    [Export] public PackedScene AiBoss1Scene;
    [Export] public PackedScene AiBoss2Scene;

    //Spawning Settings
    [Export] public int SpawnRadius = 8000; //Radius around the player to spawn enemies
    [Export] public float SpawnInterval = 0.5f; //Time between spawns -
                                                //we decrease this as the rounds increase to make it harder
    //Maximum number of enemies to use the pool for. The higher this is, the smoother the performance,
    //but less variation of enemies.
    //Basically, at the start of each round this amount of enemies are dequeued to be used in the next round.
    //For example if its round 1, the first ten enemies are added to the queue to be used at the start of round 2.
    //This is good as, it eases the player into the next round and improves performance,
    //however if its too high, then not many of the unique enemies for that round spawn.
    [Export] public int PoolEnemiesAmount = 10; 

    //Labels to update to represent score (kills) and the current round.
    [Export] public Label RoundLabel;
    [Export] public Label ScoreLabel;

    //Round/Score Variables
    private int kills = 0; //The score is measured as the number of kills
    public int currentRound = 1;
    private int enemiesToSpawn = 10; //This determines how many enemies to spawn each round.
                                     //It's updated later on but at the moment is 10 as thats the amount for round 1.
    private int enemiesAlive = 0;
    private int enemiesSpawnedThisRound = 0; //Track how many enemies have been spawned this round
    private float roundDelay = 3.0f; //Delay before starting the next round

    //References
    private Timer _spawnTimer;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    public Queue<CharacterBody2D> _enemyPool = new Queue<CharacterBody2D>(); //Pool for reusing enemies
    private GameManager _gameManager;
    private player _player;
    private AiAlly1 _aiAlly;

    public override void _Ready()
    {

        //Initialise a random number generator
        _rng.Randomize();

        //Get references to GameManager and Player
        _gameManager = GetNode<GameManager>("../GameManager"); //this is just their path in the scene
        _player = GetNode<player>("../Player"); //this is just their path in the scene


        //Debugs for if the path is incorrect
        if (_gameManager == null)
        {
            GD.PrintErr("Game Manager not found");
            return;
        }

        if (_player == null)
        {
            GD.PrintErr("Player not found"); 
            return;
        }

        //Preload enemies into the pool (skip for rounds 6 and above as these are 1v1 against other shooters)
        if (currentRound <= 5)
        {
            PreloadEnemies(PoolEnemiesAmount); //here we use the amount that we declared earlier for the pool
        }

        //Set up the spawn timer (This is a function in godot that allows you to time certain things)
        //I use it here for an interval to spawn enemies
        _spawnTimer = new Timer //The timer is technically a node that you can use and add to the scene,
                                //but I thought it'd be easier just to create it within the script
        {
            WaitTime = SpawnInterval,
            Autostart = true,
            OneShot = false
        };
        AddChild(_spawnTimer); //This adds the timer to the scene so that it's ran
        _spawnTimer.Timeout += SpawnEnemy;

        StartRound();         //Start the first round
        UpdateRoundDisplay(); //Ensure it updates at the start
        UpdateKillCount();    //Set the score to zero
    }

    private void StartRound()
    {
        GD.Print("Starting Round "+ currentRound);

        //Switch to set the number of enemies to spawn based on the current round
        switch (currentRound)
        {
            case 1: //just the round basically
                enemiesToSpawn = 10; 
                break;
            case 2:
                enemiesToSpawn = 20;
                break;
            case 3:
                enemiesToSpawn = 40; 
                break;
            case 4:
                enemiesToSpawn = 60; 
                break;
            case 5:
                enemiesToSpawn = 81; //The extra 1 is the boss that spawns last
                break;
            case 6:
                enemiesToSpawn = 1; 
                break;
            case 7:
                enemiesToSpawn = 1; 
                break;
            case 8:
                enemiesToSpawn = 1; 
                break;
            case 9:
                enemiesToSpawn = 4; 
                break;
            default:
                enemiesToSpawn = 50; //Just put the default to 50 enemies for if there were
                                     //any additional rounds but its not actually used
                break;
        }

        enemiesAlive = enemiesToSpawn; 
        enemiesSpawnedThisRound = 0; //Reset the counter for this round

        //Adjust spawn frequency based on the round
        SpawnInterval = Mathf.Max(0.1f, 1.5f - (currentRound - 1) * 0.25f);
        _spawnTimer.WaitTime = SpawnInterval;

        //Update the current round GUI
        UpdateRoundDisplay();

        //Refill the enemy pool with enemies for the current round (skip for rounds 6 and above)
        if (currentRound <= 5 && _enemyPool.Count < enemiesToSpawn)
        {
            int enemiesToAdd = enemiesToSpawn - _enemyPool.Count;
            PreloadEnemies(enemiesToAdd);
        }

        //Start spawning enemies
        _spawnTimer.Start();
    }

    private void SpawnEnemy()
    {
        //Stop spawning if we've reached the enemy limit for this round
        if (enemiesSpawnedThisRound >= enemiesToSpawn)
        {
            GD.Print("Enemy limit reached for this round. Stopping spawner."); //debug to check the enemy limit was working
            _spawnTimer.Stop(); //Stop the timer
            return;
        }

        //Get an enemy from the pool (or instantiate a new one for rounds 6 and above)
        CharacterBody2D enemy;
        if (currentRound <= 5)
        {
            enemy = _enemyPool.Dequeue();
        }
        else
        {
            enemy = GetRandomEnemyForRound(currentRound);
            AddChild(enemy);
        }

        //Calculate a valid spawn position around the player
        Vector2 spawnPosition = GetValidSpawnPosition();
        enemy.GlobalPosition = spawnPosition;

        enemy.Visible = true;
        GetTree().CreateTimer(0.1f).Timeout += () =>
        {
            //The Z index is a hierarchy of what appears over the what. I set the enemies to appear over weapon or item drops
            enemy.ZIndex = 21;
            //Set the collision layer (Layer 3)
            enemy.CollisionLayer = 1 << 2; //Layer 3 (AIEnemies)

            //Set the collision mask (Layer 1 and Layer 4)
            enemy.CollisionMask = (1 << 0) | (1 << 3); //Layer 1 is the Player and Layer 4 is PlayerProjectiles
        };

        //Initialise the enemy with pathfinding and target
        if (_gameManager.Pathfinding == null)
        {
            GD.Print("Pathfinding grid is null in the EnemySpawner");
            return;
        }

        //Handle enemy-specific initialisation -
        //I tried a more efficient way such as using a dictionary for doing this
        //but due to AiEnemy4 not requiring a grid and being unable to add the scenes to a dictionary, I couldn't.
        if (enemy is AiEnemy1 aiEnemy1)
        {
            aiEnemy1.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy1.UpdateTarget();
            aiEnemy1.Died -= OnEnemyDied; //Disconnect first to avoid multiple connections
            aiEnemy1.Died += OnEnemyDied; //Subscribe to death event
            aiEnemy1.AddToGroup("mobs");
        }
        else if (enemy is AiEnemy2 aiEnemy2)
        {
            aiEnemy2.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy2.UpdateTarget();
            aiEnemy2.Died -= OnEnemyDied;
            aiEnemy2.Died += OnEnemyDied;
            aiEnemy2.AddToGroup("mobs");
        }
        else if (enemy is AiEnemy3 aiEnemy3)
        {
            aiEnemy3.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy3.UpdateTarget();
            aiEnemy3.Died -= OnEnemyDied;
            aiEnemy3.Died += OnEnemyDied;
            aiEnemy3.AddToGroup("mobs");
        }
        else if (enemy is AiEnemy4 aiEnemy4)
        {
            //AiEnemy4 flies and so goes straight towards the player without requiring the grid.
            aiEnemy4.UpdateTarget();
            aiEnemy4.Died -= OnEnemyDied;
            aiEnemy4.Died += OnEnemyDied;
            aiEnemy4.AddToGroup("mobs");
        }
        else if (enemy is AiEnemy5 aiEnemy5)
        {
            aiEnemy5.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy5.UpdateTarget();
            aiEnemy5.Died -= OnEnemyDied;
            aiEnemy5.Died += OnEnemyDied;
            aiEnemy5.AddToGroup("mobs");
        }
        else if (enemy is AiEnemy6 aiEnemy6)
        {
            aiEnemy6.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy6.UpdateTarget();
            aiEnemy6.Died -= OnEnemyDied;
            aiEnemy6.Died += OnEnemyDied;
            aiEnemy6.AddToGroup("mobs");
        }
        else if (enemy is AiEnemy7 aiEnemy7)
        {
            aiEnemy7.SetPathfindingGrid(_gameManager.Pathfinding);
            aiEnemy7.UpdateTarget();
            aiEnemy7.Died -= OnEnemyDied;
            aiEnemy7.Died += OnEnemyDied;
            aiEnemy7.AddToGroup("mobs");
        }
        else if (enemy is AiBoss1 aiBoss1)
        {
            aiBoss1.SetPathfindingGrid(_gameManager.Pathfinding);
            aiBoss1.UpdateTarget();
            aiBoss1.Died -= OnEnemyDied;
            aiBoss1.Died += OnEnemyDied;
            aiBoss1.AddToGroup("mobs");
        }
        else if (enemy is AiBoss2 aiBoss2)
        {
            aiBoss2.SetPathfindingGrid(_gameManager.Pathfinding);
            aiBoss2.UpdateTarget();
            aiBoss2.Died -= OnEnemyDied;
            aiBoss2.Died += OnEnemyDied;
            aiBoss2.AddToGroup("mobs");
        }

        GD.Print("Spawning enemy type" + enemy.GetType().Name + "at" + spawnPosition);
        enemiesSpawnedThisRound++; //Increment the counter of enemies spawned
    }

    private CharacterBody2D GetRandomEnemyForRound(int round)
    {
        if (round == 1)
        {
            return (CharacterBody2D)AiEnemy1Scene.Instantiate(); //Only AiEnemy1 in round 1
        }
        else if (round == 2)
        {
            //Introduce AiEnemy2 in round 2
            float randomValue = GD.Randf(); //Godot's simpler system for random values between 0 and 1
            if (randomValue < 0.7f) //70% chance for AiEnemy1
            {
                return (CharacterBody2D)AiEnemy1Scene.Instantiate();
            }
            else //30% chance for AiEnemy2
            {
                return (CharacterBody2D)AiEnemy2Scene.Instantiate();
            }
        }
        else if (round == 3)
        {
            //Introduce AiEnemy3 in round 3
            float randomValue = GD.Randf();

            if (randomValue < 0.4f) //40% chance for AiEnemy1
            {
                return (CharacterBody2D)AiEnemy1Scene.Instantiate();
            }
            else if (randomValue < 0.7f) //30% chance for AiEnemy2
            {
                return (CharacterBody2D)AiEnemy2Scene.Instantiate();
            }
            else //30% chance for AiEnemy3
            {
                return (CharacterBody2D)AiEnemy3Scene.Instantiate();
            }
        }
        else if (round == 4)
        {
            //Introduce AiEnemy4 in round 4
            float randomValue = GD.Randf();
            if (randomValue < 0.2f)
            {
                return (CharacterBody2D)AiEnemy1Scene.Instantiate();
            }
            else if (randomValue < 0.4f)
            {
                return (CharacterBody2D)AiEnemy2Scene.Instantiate();
            }
            else if (randomValue < 0.8f)
            {
                return (CharacterBody2D)AiEnemy3Scene.Instantiate();
            }
            else
            {
                return (CharacterBody2D)AiEnemy4Scene.Instantiate();
            }
        }
        else if (round == 5)
        {
            //In Round 5: Spawn 80 random enemies and 1 AiBoss1 (melee boss)
            if (enemiesSpawnedThisRound < 80)
            {
                //Spawn random enemies (AiEnemy1 to AiEnemy4)
                float randomValue = GD.Randf();
                if (randomValue < 0.4f)
                {
                    return (CharacterBody2D)AiEnemy1Scene.Instantiate();
                }
                else if (randomValue < 0.6f)
                {
                    return (CharacterBody2D)AiEnemy2Scene.Instantiate();
                }
                else if (randomValue < 0.8f)
                {
                    return (CharacterBody2D)AiEnemy3Scene.Instantiate();
                }
                else
                {
                    return (CharacterBody2D)AiEnemy4Scene.Instantiate();
                }
            }
            else
            {
                //Spawn AiBoss1 after the 80 random enemies
                return (CharacterBody2D)AiBoss1Scene.Instantiate();
            }
        }
        //AiEnemy5 and onwards are enemies that fire projectiles.
        else if (round == 6)
        {
            return (CharacterBody2D)AiEnemy5Scene.Instantiate();
        }
        else if (round == 7)
        {
            return (CharacterBody2D)AiEnemy6Scene.Instantiate();
        }
        else if (round == 8)
        {
            return (CharacterBody2D)AiEnemy7Scene.Instantiate();
        }
        else if (round == 9)
        {
            //In Round 9: Spawn 1 AiEnemy5, 1 AiEnemy6, 1 AiEnemy7, and 1 AiBoss2
            if (enemiesSpawnedThisRound == 0)
            {
                return (CharacterBody2D)AiEnemy5Scene.Instantiate();
            }
            else if (enemiesSpawnedThisRound == 1)
            {
                return (CharacterBody2D)AiEnemy6Scene.Instantiate();
            }
            else if (enemiesSpawnedThisRound == 2)
            {
                return (CharacterBody2D)AiEnemy7Scene.Instantiate();
            }
            else
            {
                return (CharacterBody2D)AiBoss2Scene.Instantiate();
            }
        }
        else //there arent any extra rounds so this doesn't happen
        {
            return (CharacterBody2D)AiEnemy1Scene.Instantiate();
        }
    }

    public void OnEnemyDied()
    {
        enemiesAlive--;
        kills++;
        Global.Score++; //The score is stored in a global script,
                        //so we can display it on the death screen and the victory screen.
        UpdateKillCount();
        if (enemiesAlive <= 0)
        {
            //Start the next round after a delay
            GetTree().CreateTimer(roundDelay).Timeout += () =>
            {
                currentRound++;
                Global.LastRound = currentRound;
                if (currentRound <= 9)
                {
                    StartRound();
                }
                else
                {
                    GetTree().ChangeSceneToFile("survived_scene.tscn"); //The victory screen!
                    GD.Print("Game Complete");
                }
            };
        }
    }

    private void UpdateKillCount()
    {
        if (ScoreLabel != null)
        {
            ScoreLabel.Text = $"Kills: {kills}"; //update the text of the score label
        }
        else //debug to make sure I had actually assigned the score label in the scene
        {
            GD.PrintErr("Score Label is not assigned in the Inspector."); 
        }
    }

    //Same here but for the round.
    private void UpdateRoundDisplay()
    {
        if (RoundLabel != null)
        {
            RoundLabel.Text = $"Round: {currentRound}";
        }
        else
        {
            GD.PrintErr("RoundLabel is not assigned in the Inspector.");
        }
    }

    private Vector2 GetValidSpawnPosition()
    {
        //Get the player's current position in the world
        Vector2 playerPosition = _player.GlobalPosition;
        Vector2 spawnPosition;
        int attempts = 0;

        //Try to find a valid spawn position within a limited number of attempts (so performance isn't decreased)
        do
        {
            //Generate a random angle (0 to 2pi radians) and distance of half of the SpawnRadius to full SpawnRadius for variation
            float angle = _rng.RandfRange(0, Mathf.Tau); //Random angle in radians
            float distance = _rng.RandfRange(SpawnRadius * 0.5f, SpawnRadius); //Random distance within the spawn radius

            //Calculate the spawn position relative to the player using polar coordinates
            spawnPosition = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            //Convert the spawn position to grid coordinates for pathfinding
            Vector2I gridPosition = _gameManager.Pathfinding.WorldToMap(spawnPosition);

            //Check if the grid position is walkable (not blocked by walls or obstacles)
            bool walkable = _gameManager.Pathfinding.IsWalkable(gridPosition);

            //If the position is walkable, return it as a valid spawn position
            if (walkable)
            {
                return spawnPosition;
            }

            //Increment the attempt counter
            attempts++;
        } while (attempts < 20); //Limit to 20 attempts to prevent infinite loops and conserve performance
        //If no valid place is found, then just spawn it at the top left of the map (theres always a gap there for them to spawn)
        GD.PrintErr("Failed to find a valid spawn position");
        return new Vector2(1000,800); //this is the top left of the map. It is always a valid coordinate.
    }

    private void PreloadEnemies(int count)
    {
        //Skip preloading for rounds 6 and above
        if (currentRound >= 6) return;

        //Preload the specified number of enemies
        for (int i = 0; i < count; i++)
        {
            CharacterBody2D enemy;

            //For Round 5, I preloaded AiBoss1 as the last enemy
            if (currentRound == 5 && i == count - 1)
            {
                enemy = (CharacterBody2D)AiBoss1Scene.Instantiate();
            }
            else
            {
                //Preload random enemies for other rounds
                enemy = GetRandomEnemyForRound(currentRound);
            }

            //Hide the enemy initially and add it to the scene
            enemy.Visible = false;
            AddChild(enemy);

            //Add the enemy to the pool for reuse
            _enemyPool.Enqueue(enemy);
        }
    }
    //Called by all the AI enemies
    public void ReturnEnemyToPool(CharacterBody2D enemy)
    {
        //For rounds 6 and above, disable the enemy but don't add it to the pool
        if (currentRound >= 6)
        {
            enemy.Visible = false;
            enemy.CollisionLayer = 0; //Disable collisions
            enemy.CollisionMask = 0; //Prevent interactions
            return;
        }

        //For rounds 1-5, hide the enemy, disable collisions, and add it back to the pool
        enemy.Visible = false;
        enemy.CollisionLayer = 0; //Disable collisions
        enemy.CollisionMask = 0; //Prevent interactions
        _enemyPool.Enqueue(enemy);
    }
}