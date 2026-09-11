using Godot;

public partial class Shotgun : Weapon //Inherits the weapon class from the previous script
{
    //Exported variable to assign the bullet scene in the Godot editor
    [Export] public PackedScene BulletScene; //The Scene for the bullet

    public override void _Ready()
    {
        //Initialise shotgun-specific properties (from the weapon class)
        WeaponName = "Shotgun"; 
        WeaponTexture = GD.Load<Texture2D>("res://assets/Weapons/shotgun.png"); 
        Automatic = false;
        //Shotgun fires multiple bullets in a spread (the other weapons don't do this)
        SpreadShot = true; 
        Firerate = 0.3f;

        //Set the sprite texture for the shotgun
        GetNode<Sprite2D>("Sprite2D").Texture = WeaponTexture;
    }

    //Override the Fire method to implement shotgun-specific firing behavior
    public override bool Fire(Vector2 muzzlePosition)
    {
        //Call the base Fire method and check if it succeeded
        if (!base.Fire(muzzlePosition))
        {
            return false; //Exit if the weapon is on cooldown
        }

        //Spawn 3 bullets at different angles to create a spread pattern (this is only for the shotgun)
        for (int i = -1; i <= 1; i++)
        {
            //Apply an offset for each bullet (15 degrees)
            float angleOffset = Mathf.DegToRad(15 * i);

            //Spawn a bullet with the calculated angle offset
            SpawnBullet(muzzlePosition, WeaponHolder.GlobalRotation + angleOffset);
        }

        return true; //Indicate that the weapon fired successfully
    }

    //Method to spawn a bullet at a specific position and rotation
    private void SpawnBullet(Vector2 position, float rotation)
    {
        //Check if the BulletScene is assigned
        if (BulletScene == null)
        {
            GD.PrintErr("BulletScene is not assigned.");
            return;
        }

        //Check if the weapon is still valid before spawning bullets
        if (!IsInstanceValid(this))
        {
            GD.PrintErr("Weapon is disposed. Cannot spawn bullets.");
            return;
        }

        //Instantiate the bullet from the BulletScene
        var bulletInstance = BulletScene.Instantiate() as Bullet;
        bulletInstance.ZIndex = 21; //Set the rendering order for the bullet
                                    //(so it isn't underneath the other enemies (clearer for player)

        //Check if the bullet was instantiated successfully
        if (bulletInstance == null)
        {
            GD.PrintErr("Failed to instantiate bullet.");
            return;
        }

        //Add the bullet to the scene
        GetTree().Root.AddChild(bulletInstance);

        //Set the bullet's position and rotation
        bulletInstance.GlobalPosition = position;
        bulletInstance.GlobalRotation = rotation;
    }
}