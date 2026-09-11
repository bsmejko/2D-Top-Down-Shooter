using Godot;

public partial class ProjectileFriendly : Area2D
{
    [Export] public float Speed = 5000f; // Speed of the projectile
    [Export] public float Lifetime = 10f; // Time before the projectile is removed

    private float _timeAlive = 0f;
    public Vector2 Direction = Vector2.Zero; // Direction of movement

    public override void _Ready()
    {
        // Connect the BodyEntered signal for collision detection
        BodyEntered += OnBodyEntered;
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
        // Handle collision with the player

        if (body is player)
        {

        }
        else if (body.IsInGroup("mobs"))
        {
            if (body is AiEnemy1 enemy1)
            {
                enemy1.TakeDamage(25); // Apply damage to the enemy
            }
            else if (body is AiEnemy2 enemy2)
            {
                enemy2.TakeDamage(25);
            }
            else if (body is AiEnemy3 enemy3)
            {
                enemy3.TakeDamage(25);
            }
            else if (body is AiEnemy4 enemy4)
            {
                enemy4.TakeDamage(25);
            }
            else if (body is AiEnemy5 enemy5)
            {
                enemy5.TakeDamage(1);
            }
            else if (body is AiEnemy6 enemy6)
            {
                enemy6.TakeDamage(1);
            }
            else if (body is AiEnemy7 enemy7)
            {
                enemy7.TakeDamage(1);
            }
            else if (body is AiBoss1 aiBoss1)
            {
                aiBoss1.TakeDamage(25);
            }
            else if (body is AiBoss2 aiBoss2)
            {
                aiBoss2.TakeDamage(1);
            }
            QueueFree();
        }
            QueueFree();
        

    }
}