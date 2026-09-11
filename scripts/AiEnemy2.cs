using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class AiEnemy2 : CharacterBody2D
{
    // Signals
    [Signal] public delegate void DiedEventHandler();

    // Pathfinding and Target
    private AstarPathfinding _pathfinding;
    private Node2D _target; // Changed from 'player' to 'Node2D' to handle any target type
    private List<Vector2> _path = new List<Vector2>();
    private int _pathIndex = 0;

    // Enemy Properties
    private float speed = 1500f; // Adjusted speed for testing
    private int hp = 500;
    private bool isDead = false; // Track if the enemy is dead
    private bool isHit = false; // Track if the enemy is currently in the "hit" state

    // Animation
    private AnimatedSprite2D _animatedSprite;

    // Timers
    private Timer _deathTimer;
    private Timer _hitTimer;
    private Timer _targetUpdateTimer; // Timer to periodically update the target

    public override void _Ready()
    {
        SetProcess(false); // Disable processing until we have pathfinding & target
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        // Initialize the death timer
        _deathTimer = new Timer();
        _deathTimer.WaitTime = 0.66f; // 2x delay to Enemy1 to represent their health increase
        _deathTimer.OneShot = true; // Timer only runs once
        _deathTimer.Timeout += OnDeathTimerFinished; // Connect timeout signal
        AddChild(_deathTimer); // Add the timer to the scene

        // Initialize the hit timer
        _hitTimer = new Timer();
        _hitTimer.WaitTime = 0.12f; // 0.25 second delay for hit animation
        _hitTimer.OneShot = true; // Timer only runs once
        _hitTimer.Timeout += OnHitTimerFinished; // Connect timeout signal
        AddChild(_hitTimer); // Add the timer to the scene

        // Initialize the target update timer
        _targetUpdateTimer = new Timer();
        _targetUpdateTimer.WaitTime = 1.0f; // Update target every 1 second
        _targetUpdateTimer.Autostart = true;
        _targetUpdateTimer.OneShot = false;
        _targetUpdateTimer.Timeout += UpdateTarget;
        AddChild(_targetUpdateTimer);
    }

    public void SetPathfindingGrid(AstarPathfinding pathfinding)
    {
        _pathfinding = pathfinding;
        GD.Print("Pathfinding grid set for AiEnemy2.");
        CheckInitialization();
    }

    private void CheckInitialization()
    {
        if (_pathfinding != null)
        {
            SetProcess(true);
            GD.Print("AiEnemy2 initialized and ready to move.");
            UpdateTarget(); // Only update the path when pathfinding is assigned
        }
    }

    public void UpdateTarget()
    {
        // Get all targets in the "Players" and "Allies" groups
        var targets = GetTree().GetNodesInGroup("Players")
            .Concat(GetTree().GetNodesInGroup("Allies"))
            .ToList();

        if (targets.Count == 0)
        {
            _target = null;
            GD.Print("No targets found.");
            return;
        }

        // Log all targets
        GD.Print("Available targets:");
        foreach (var target in targets)
        {
            GD.Print($"- {target.Name} at {((Node2D)target).GlobalPosition}");
        }

        // Find the closest target
        _target = targets
            .OrderBy(t => GlobalPosition.DistanceTo(((Node2D)t).GlobalPosition))
            .FirstOrDefault() as Node2D;

        if (_target == null)
        {
            GD.Print("No valid target found.");
            return;
        }

        GD.Print($"Target set to: {_target.Name}");
        UpdatePath();
    }

    public override void _Process(double delta)
    {
        if (isDead) return; // Stop processing if the enemy is dead or hit

        if (_pathfinding == null || _target == null)
        {
            GD.PrintErr("Pathfinding or target is not set!");
            return;
        }

        // Update the path if the target has moved significantly or if we've reached the end of the current path
        if (_pathIndex >= _path.Count || GlobalPosition.DistanceTo(_target.GlobalPosition) > 50f)
        {
            UpdatePath();
        }

        MoveAlongPath((float)delta);
    }

    private void UpdatePath()
    {
        if (_pathfinding == null || _target == null)
        {
            GD.PrintErr("Pathfinding or target is not set!");
            return;
        }

        Vector2I start = _pathfinding.WorldToMap(GlobalPosition);
        Vector2I target = _pathfinding.WorldToMap(_target.GlobalPosition);

        if (!_pathfinding.IsWalkable(start) || !_pathfinding.IsWalkable(target))
        {
            GD.PrintErr("Start or target position is not walkable!");
            return;
        }

        _path = _pathfinding.FindPath(start, target);
        if (_path == null || _path.Count == 0)
        {
            GD.Print("No valid path found.");
            return;
        }

        _pathIndex = 1;
    }

    private void MoveAlongPath(float delta)
    {
        if (isDead) return; // Stop moving if the enemy is dead or hit

        if (_pathIndex >= _path.Count)
        {
            return;
        }

        Vector2 targetPosition = _path[_pathIndex];
        Vector2 direction = (targetPosition - GlobalPosition).Normalized();
        Velocity = direction * speed; // Apply speed directly (delta is handled in MoveAndSlide)
        MoveAndSlide();

        // Check if we've reached the current target position in the path
        if (GlobalPosition.DistanceTo(targetPosition) < 5f)
        {
            _pathIndex++; // Move to the next point in the path
        }

        PlayAnimation();
    }

    public void Reset()
    {
        _path.Clear();
        _pathIndex = 0;
        Velocity = Vector2.Zero;
        isDead = false; // Reset the dead state
        isHit = false; // Reset the hit state
        _animatedSprite.Play("Idle"); // Reset animation to idle
    }

    private void PlayAnimation()
    {
        if (isDead || isHit) return; // Stop playing movement animations if the enemy is dead or hit

        float directionX = Velocity.X;
        float directionY = Velocity.Y;
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

    public void TakeDamage(int damage)
    {
        if (isDead) return; // Ignore damage if already dead or hit

        hp -= damage;
        GD.Print($"AiEnemy2 took {damage} damage, remaining health: {hp}");

        // Play the "hit" animation
        isHit = true;
        _animatedSprite.Play("hit");
        _hitTimer.Start(); // Start the hit timer

        if (hp <= 0)
        {
            RemoveFromGroup("mobs");
            CollisionLayer = 0;
            GD.Print("AiEnemy2 defeated!");
            isDead = true; // Mark the enemy as dead
            Velocity = Vector2.Zero; // Stop movement
            _animatedSprite.Play("death"); // Play death animation

            // Start the death timer
            _deathTimer.Start();
        }
    }

    private void OnHitTimerFinished()
    {
        isHit = false; // Reset the hit state
        PlayAnimation(); // Resume normal animations
    }

    private void OnDeathTimerFinished()
    {
        // Emit the Died signal
        EmitSignal(SignalName.Died);

        // Return the enemy to the pool
        var spawner = GetParent<EnemySpawner>();
        spawner.ReturnEnemyToPool(this);

        // Reset health and state
        hp = 500;
        isDead = false;
    }
}