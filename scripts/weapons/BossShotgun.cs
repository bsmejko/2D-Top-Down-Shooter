using Godot;

public partial class BossShotgun : Weapon
{
    [Export] public PackedScene BulletScene; // Scene for the bullet

    public override void _Ready()
    {
        WeaponName = "BossShotgun";
        WeaponTexture = GD.Load<Texture2D>("res://assets/Weapons/GoldShotgun.png");
        Automatic = false;
        SpreadShot = true;
        Firerate = 0.4f; // Slowest firerate

        // Set the sprite texture
        GetNode<Sprite2D>("Sprite2D").Texture = WeaponTexture;
    }

    public override bool Fire(Vector2 muzzlePosition)
    {
        // Call the base Fire method and check if it succeeded
        if (!base.Fire(muzzlePosition))
        {
            return false; // Exit if the weapon is on cooldown
        }



        // Spawn 3 bullets at different angles
        for (int i = -2; i <= 2; i++)
        {
            float angleOffset = Mathf.DegToRad(12 * i); // 15 degrees between bullets
            SpawnBullet(muzzlePosition, WeaponHolder.GlobalRotation + angleOffset);
        }

        return true; // Indicate that the weapon fired successfully
    }

    private void SpawnBullet(Vector2 position, float rotation)
    {
        if (BulletScene == null)
        {
            GD.PrintErr("BulletScene is not assigned.");
            return;
        }

        // Check if the weapon is still valid before spawning bullets
        if (!IsInstanceValid(this))
        {
            GD.PrintErr("Weapon is disposed. Cannot spawn bullets.");
            return;
        }

        // Instantiate the bullet
        var bulletInstance = BulletScene.Instantiate() as Bullet;
        bulletInstance.ZIndex = 21;
        if (bulletInstance == null)
        {
            GD.PrintErr("Failed to instantiate bullet.");
            return;
        }

        // Add the bullet to the scene
        GetTree().Root.AddChild(bulletInstance);

        // Set the bullet's position and rotation
        bulletInstance.GlobalPosition = position;
        bulletInstance.GlobalRotation = rotation;
    }
}