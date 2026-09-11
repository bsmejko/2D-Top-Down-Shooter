using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class AiEnemy4 : CharacterBody2D
{
    // Signal to notify when the enemy dies.
    [Signal] public delegate void DiedEventHandler();

    // Target reference (can be a player or ally)
    private Node2D _target; // Changed from 'player' to 'Node2D' to handle any target type

    // Enemy properties: adjust as needed for difficulty.
    private float speed = 2000f; // Flying enemies can be faster.
    private int hp = 150;        // Health for AiEnemy4
    private bool isDead = false; // Track if the enemy is dead

    // Animation
    private AnimatedSprite2D _animatedSprite;

    // Timer for death delay
    private Timer _deathTimer;

    // Timer to periodically update the target
    private Timer _targetUpdateTimer;

    public override void _Ready()
    {
        // Disable processing until the target is set.
        SetProcess(false);

        // Get the animated sprite
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        // Initialize the death timer
        _deathTimer = new Timer();
        _deathTimer.WaitTime = 1.0f; // 1 second delay
        _deathTimer.OneShot = true; // Timer only runs once
        _deathTimer.Timeout += OnDeathTimerFinished; // Connect timeout signal
        AddChild(_deathTimer); // Add the timer to the scene

        // Initialize the target update timer
        _targetUpdateTimer = new Timer();
        _targetUpdateTimer.WaitTime = 1.0f; // Update target every 1 second
        _targetUpdateTimer.Autostart = true;
        _targetUpdateTimer.OneShot = false;
        _targetUpdateTimer.Timeout += UpdateTarget;
        AddChild(_targetUpdateTimer);
    }

    // Updates the target to the closest player or ally.
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
        SetProcess(true); // Enable processing once the target is available
    }

    // Called every frame.
    public override void _Process(double delta)
    {
        if (isDead) return; // Stop processing if the enemy is dead

        if (_target == null)
        {
            GD.PrintErr("Target not set for AiEnemy4!");
            return;
        }

        // Calculate the direction directly toward the target's position.
        Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();

        // Update the enemy's velocity based on its speed.
        Velocity = direction * speed;

        // Move the enemy. (Note: MoveAndSlide handles delta internally.)
        MoveAndSlide();

        // Play the appropriate animation based on movement.
        PlayAnimation();
    }

    // Plays animations based on the enemy's velocity.
    private void PlayAnimation()
    {
        if (isDead) return; // Stop playing movement animations if the enemy is dead

        float directionX = Velocity.X;
        float directionY = Velocity.Y;
        bool flip = _animatedSprite.FlipH;

        if (Velocity.Length() > 0)
        {
            // Simple horizontal-based animation selection.
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
    }

    // Handles damage taken by the enemy.
    public void TakeDamage(int damage)
    {
        if (isDead) return; // Ignore damage if already dead

        hp -= damage;
        GD.Print($"AiEnemy4 took {damage} damage, remaining health: {hp}");

        // Check if the enemy's health has dropped to or below zero.
        if (hp <= 0)
        {
            RemoveFromGroup("mobs");
            CollisionLayer = 0;
            GD.Print("AiEnemy4 defeated!");
            isDead = true; // Mark the enemy as dead
            Velocity = Vector2.Zero; // Stop movement
            _animatedSprite.Play("death"); // Play death animation

            // Start the death timer
            _deathTimer.Start();
        }
    }

    // Called when the death timer finishes.
    private void OnDeathTimerFinished()
    {
        // Emit the death signal to notify GameManager/Spawner.
        EmitSignal(SignalName.Died);

        // Return this enemy to the spawner's pool.
        var spawner = GetParent<EnemySpawner>();
        spawner.ReturnEnemyToPool(this);

        // Reset health and state for future reuse.
        hp = 150;
        isDead = false;
    }

    // Resets the enemy's state (for pooling purposes).
    public void Reset()
    {
        _target = null;
        Velocity = Vector2.Zero;
        SetProcess(false);
        isDead = false; // Reset the dead state
        _animatedSprite.Play("Idle"); // Reset animation to idle
    }
}