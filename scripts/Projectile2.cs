using Godot;

public partial class Projectile2 : Area2D
{
    [Export] public float Speed = 10000f; // Speed of the projectile
    [Export] public float Lifetime = 2f; // Time before the projectile is removed

    private float _timeAlive = 0f;
    public Vector2 Direction = Vector2.Zero; // Direction of movement

    public override void _Ready()
    {
        // Set collision layers and masks based on game mode
        UpdateCollisionForGameMode();

        // Connect the BodyEntered signal for collision detection
        BodyEntered += OnBodyEntered;
    }

    private void UpdateCollisionForGameMode()
    {
        if (Global.IsBattleRoyaleMode)
        {
            // Battle Royale: Can hit players AND other enemies
            CollisionMask = (1 << 0) | (1 << 1) | (1 << 2) |  (1 << 3); // Layer 1 (Player) + Layer 3 (AIEnemies) + Layer 4 (PlayerProjectiles)
        }
        else
        {
            // Survival: Only hit players and allies
            CollisionMask = (1 << 0) | (1 << 3); // Layer 1 (Player) + Layer 4 (PlayerProjectiles)
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Move the projectile in the specified direction
        Position += Direction * (float)(Speed * delta);

        // Remove the projectile after its lifetime expires
        _timeAlive += (float)delta;
        if (_timeAlive >= Lifetime)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (Global.IsBattleRoyaleMode)
        {
            // Battle Royale Mode: Damage both players and enemies
            if (body is player player)
            {
                GD.Print("Projectile hit the player!");
                player.TakeDamage(2);
                QueueFree();
            }
            else if (body is AiAlly1 aiAlly)
            {
                aiAlly.TakeDamage(2);
                QueueFree();
            }
            else if (body is AiEnemy1 || body is AiEnemy2 || body is AiEnemy3 ||
                     body is AiEnemy4 || body is AiEnemy5 || body is AiEnemy6 ||
                     body is AiEnemy7 || body is AiBoss1 || body is AiBoss2)
            {
                // Apply damage to enemy in battle royale mode
                GD.Print($"Projectile hit enemy: {body.GetType().Name}");
                if (body.HasMethod("TakeDamage"))
                {
                    body.Call("TakeDamage", 2);
                }
                QueueFree();
            }
            else
            {
                // Hit something else (wall, obstacle, etc.)
                QueueFree();
            }
        }
        else
        {
            // Survival Mode: Original logic - only damage players and allies
            if (body is AiEnemy5 || body is AiEnemy6 || body is AiEnemy7 || body is AiBoss2)
            {
                // Ignore enemy collisions in survival mode
                return;
            }
            else if (body is player player)
            {
                GD.Print("Projectile hit the player!");
                player.TakeDamage(2);
                QueueFree();
            }
            else if (body is AiAlly1 aiAlly)
            {
                aiAlly.TakeDamage(2);
                QueueFree();
            }
            else
            {
                QueueFree();
            }
        }
    }
}