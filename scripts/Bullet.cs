using Godot;

public partial class Bullet : Area2D
{
    [Export] public float Speed = 750.0f; //Speed of the bullet
    [Export] public float Lifetime = 10f; //Time before the bullet is removed from the scene

    private float _timeAlive = 0f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        //Move the bullet in the direction it is facing (transform.x)
        Position += Transform.X * (float)(Speed * delta);

        //Remove the bullet after its lifetime expires
        _timeAlive += (float)delta;
        if (_timeAlive >= Lifetime)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node body)
    {
        //This Handle collision with enemies
        //The shooter enemies take less damage to make gunfights longer and more engaging
        if (body.IsInGroup("mobs"))
        {
            // Register player kill in battle royale mode
            if (Global.IsBattleRoyaleMode)
            {
                var spawner = GetTree().Root.GetNodeOrNull<Spawner>("Spawner");
                if (spawner != null)
                {
                    spawner.RegisterPlayerKill();
                }
            }

            if (body is AiEnemy1 enemy1)
            {
                enemy1.TakeDamage(50);
            }
            else if (body is AiEnemy2 enemy2)
            {
                enemy2.TakeDamage(50);
            }
            else if (body is AiEnemy3 enemy3)
            {
                enemy3.TakeDamage(50);
            }
            else if (body is AiEnemy4 enemy4)
            {
                enemy4.TakeDamage(50);
            }
            else if (body is AiEnemy5 enemy5)
            {
                enemy5.TakeDamage(2);
            }
            else if (body is AiEnemy6 enemy6)
            {
                enemy6.TakeDamage(2);
            }
            else if (body is AiEnemy7 enemy7)
            {
                enemy7.TakeDamage(2);
            }
            else if (body is AiBoss1 aiBoss1)
            {
                aiBoss1.TakeDamage(50);
            }
            else if (body is AiBoss2 aiBoss2)
            {
                aiBoss2.TakeDamage(2);
            }
        }

        QueueFree(); //Destroy the bullet after collision
    }
}