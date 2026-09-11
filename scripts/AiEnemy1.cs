using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class AiEnemy1 : CharacterBody2D
{
    //Signals
    //This is a signal that is emitted when the enemy dies
    [Signal] public delegate void DiedEventHandler(); 

    //Pathfinding and Target properties
    private AstarPathfinding _pathfinding; //The reference to the pathfinding system
    private Node2D _target; //The current target (player or AI ally)
    private List<Vector2> _path = new List<Vector2>(); //List of points in the path to the target
    private int _pathIndex = 0; //Current index in the path (set to 0 initially)

    //Enemy Properties
    private float speed = 2750f; //Movement speed of the enemy - this changes for each enemy
    private int hp = 100; //Health points of the enemy - also changes for each enemy
    private bool isDead = false; //Tracks if the enemy is dead
    private bool isHit = false; //Tracks if the enemy is currently in the "hit" state

    //Animation
    //The reference to the animated sprite for visuals and animations
    private AnimatedSprite2D _animatedSprite; 

    //Timers
    private Timer _deathTimer; //this is death animation delay 
    private Timer _hitTimer; //this is for hit animation delay (both to give it time to actually play)
    private Timer _targetUpdateTimer; //this timer is for updating the target periodically out of the player and AI ally


    //Called when enters scene - so just when the enemy is spawned in by the EnemySpawner
    public override void _Ready() 
    {
        //Disable the processing until pathfinding and target are set (this is _PhysicsProcess() function usually)
        SetProcess(false);
        //Get the animated sprite (just the AnimatedSprite2D as a child of the node)
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        //Add the enemy to the mobs group for easy management - the groups are used for target setting
        AddToGroup("mobs"); 
        //I used it so that I could simply set the target for the AI ally to "mobs" as opposed to each enemy seperately.


        //Initialise the death timer
        _deathTimer = new Timer();

        //0.33 second delay for death animation -
        //changes between each enemy script, to make chunkier enemy animations longer
        _deathTimer.WaitTime = 0.33f; 
                                      
        _deathTimer.OneShot = true; //Indicates that Timer only runs once
        _deathTimer.Timeout += OnDeathTimerFinished; //Callback for when the timer finishes
        AddChild(_deathTimer); //Add the timer to the scene

        //Initialise the hit timer
        _hitTimer = new Timer();
        _hitTimer.WaitTime = 0.12f; //0.12 second delay for hit animation
        _hitTimer.OneShot = true; //Timer only runs once
        _hitTimer.Timeout += OnHitTimerFinished; //Callback when the timer finishes
        AddChild(_hitTimer); //Add the timer to the scene

        //Initialise the target update timer, this checks for who out of the player and AI ally is closer to attack
        _targetUpdateTimer = new Timer();
        _targetUpdateTimer.WaitTime = 1.0f; //Update target every 1 second
        _targetUpdateTimer.Autostart = true; //Start the timer automatically
        _targetUpdateTimer.OneShot = false; //Timer repeats
        _targetUpdateTimer.Timeout += UpdateTarget; //Callback to update the target
        AddChild(_targetUpdateTimer); //Add the timer to the scene
    }

    //This sets the pathfinding grid for the enemy
    public void SetPathfindingGrid(AstarPathfinding pathfinding)
    {
        _pathfinding = pathfinding; //Assign the pathfinding system
        CheckInitialisation(); //Check if the enemy is ready to move
    }

    //Check if the enemy is fully Initialised and ready to move
    private void CheckInitialisation()
    {
        if (_pathfinding != null)
        {
            SetProcess(true); //Enable processing if pathfinding is set properly
            UpdateTarget(); //Update the target immediately
        }
    }

    //Update the current target to either player or AI ally
    public void UpdateTarget()
    {
        //Get all targets in the "Players" and "Allies" groups
        var targets = GetTree().GetNodesInGroup("Players")
            .Concat(GetTree().GetNodesInGroup("Allies"))
            .ToList();

        if (targets.Count == 0)
        {
            _target = null; //If there are no targets found, then return
            return;
        }

        //Log to check all available targets are being found
        //Initially the AI didn't recognise the "Allies" family due to a typo.
        GD.Print("Available targets:");
        foreach (var target in targets)
        {
            GD.Print(target.Name + "at" + ((Node2D)target).GlobalPosition);
        }

        //Find the closest target
        _target = targets
            //use orderby to order by closest target
            .OrderBy(t => GlobalPosition.DistanceTo(((Node2D)t).GlobalPosition))
            //Returns the default definition if no targets are found - just set as Node2D.
            .FirstOrDefault() as Node2D; 
    }

    //Called every frame
    public override void _Process(double delta)
    {
        if (isDead) return; //Stop processing if the enemy is dead

        //If the player hasnt got a target or the pathfinding isn't set then update the target in order to find one
        if (_pathfinding == null || _target == null) 
        {
            UpdateTarget(); //Update the target for if it's not set
            return;
        }

        //Update the path if the target has moved significantly or if we've reached the end of the current path
        if (_pathIndex >= _path.Count || GlobalPosition.DistanceTo(_target.GlobalPosition) > 50f)
        {
            UpdatePath();
        }

        MoveAlongPath((float)delta); //Call the move along path function to make the AI actually move through the maze
    }

    //This function updates the path to the target
    private void UpdatePath()
    {
        if (_pathfinding == null || _target == null)
        {
            GD.PrintErr("Pathfinding or target isn't set"); //check the path and target are set first
            return;
        }

        //Convert current position and target position to grid coordinates using the pathfinding functions
        Vector2I start = _pathfinding.WorldToMap(GlobalPosition);
        Vector2I target = _pathfinding.WorldToMap(_target.GlobalPosition);

        if (!_pathfinding.IsWalkable(start) || !_pathfinding.IsWalkable(target))
        {
            //debug to check when it was that the pathfinding wouldn't work
            GD.PrintErr("start or target position isn't walkable - check if player is partly in wall"); 
            return;
        }

        //Find the path using the pathfinding system
        _path = _pathfinding.FindPath(start, target);
        if (_path == null || _path.Count == 0)
        {
            return; //No valid path found
        }

        _pathIndex = 1; //Start moving to the first point in the path
    }

    //Move the enemy along the path
    private void MoveAlongPath(float delta) 
    {
        if (isDead) return; //Stop moving if the enemy is dead

        if (_pathIndex >= _path.Count)
        {
            return; //Stop if we've reached the end of the path
        }

        //Calculate direction to the next point in the path
        Vector2 targetPosition = _path[_pathIndex];
        Vector2 direction = (targetPosition - GlobalPosition).Normalized();
        Velocity = direction * speed; //Apply speed to the velocity
                                      //(an inbuilt variable for CharacterBody2Ds for Godot)
        MoveAndSlide(); //Move the enemy

        //Check if we've reached the current target position in the path
        if (GlobalPosition.DistanceTo(targetPosition) < 5f)
        {
            _pathIndex++; //Move to the next point in the path
        }

        PlayAnimation(); //Update the animation based on movement
    }

    //This method resets the enemy's state
    public void Reset() //not currently used, but I added it incase of use in enemySpawner
    {
        _path.Clear(); //Clear the current path
        _pathIndex = 0; //Reset the path index
        Velocity = Vector2.Zero; //Stop movement
        isDead = false; //Reset the dead state
        isHit = false; //Reset the hit state
        _animatedSprite.Play("Idle"); //Reset animation to idle
    }

    //Play the appropriate animation based on movement direction
    private void PlayAnimation()
    {
        if (isDead || isHit) return; //Stop playing movement animations if the enemy is dead or hit

        float directionX = Velocity.X;
        float directionY = Velocity.Y;
        //This just another Godot variable for CharacterBody2D used as an away of flipping the node horizontally
        bool flip = _animatedSprite.FlipH;

        if (Velocity.Length() > 0)
        {
            if (directionX > 0)
            {
                _animatedSprite.Play("right"); 
                _animatedSprite.FlipH = false;
            }
            else if (directionX < 0)
            {
                _animatedSprite.Play("left"); 
                _animatedSprite.FlipH = true;
            }
            else if (directionY != 0 && !flip)
            {
                _animatedSprite.Play("right"); 
            }
            else if (directionY != 0 && flip)
            {
                _animatedSprite.Play("left"); 
            }
        }
        else
        {
            _animatedSprite.Play("Idle"); 
        }
    }

    //Apply damage to the enemy - called by player bullet scene when in contact with the AI Enemy
    public void TakeDamage(int damage)
    {
        if (isDead) return; //Ignore damage if already dead

        hp -= damage; //Reduce health
        GD.Print($"AiEnemy1 took {damage} damage, remaining health: {hp}");

        //Play the "hit" animation
        isHit = true;
        _animatedSprite.Play("hit");
        _hitTimer.Start(); //Start the hit timer

        if (hp <= 0)
        {
            //Remove from the "mobs" group so that the AI ally doesn't carry on trying to chase/shoot it
            RemoveFromGroup("mobs"); 
            CollisionLayer = 0; //Disable collisions
            isDead = true; //Mark the enemy as dead
            Velocity = Vector2.Zero; //Stop movement
            _animatedSprite.Play("death"); //Play death animation

            _deathTimer.Start(); //Start the death timer
        }
    }

    //Called when the hit timer finishes
    private void OnHitTimerFinished()
    {
        isHit = false; //Reset the hit state
        PlayAnimation(); //Resume normal animations
    }

    //This gets called when the death timer finishes
    private void OnDeathTimerFinished()
    {
        EmitSignal(SignalName.Died); 
        var spawner = GetParent<EnemySpawner>(); //Get the spawner
        //Return the enemy to the pool by calling the funciton from the EnemySpawner script.
        spawner.ReturnEnemyToPool(this); 
        hp = 100; //Reset health
        isDead = false; //Reset the dead state
    }
}