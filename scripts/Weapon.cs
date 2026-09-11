using Godot;

public partial class Weapon : Area2D
{
    //AS the Weapons will inherit the weapon class, these settings will be able to be set for all the weapons
    //Name of the weapon (gonna be Pistol, Shotgun, AR)
    [Export] public string WeaponName;

    //Texture for the weapon sprite
    [Export] public Texture2D WeaponTexture;

    //Whether the weapon fires automatically (e.g., holding down the fire button)
    [Export] public bool Automatic;

    //Whether the weapon fires multiple bullets in a spread pattern
    [Export] public bool SpreadShot;

    //Time between shots (in seconds)
    [Export] public float Firerate = 0.5f;

    //Reference to the weapon's sprite (assigned in the Godot editor)
    [Export] public Sprite2D WeaponSprite;

    //Reference to the player's WeaponHolder (set externally when the weapon is picked up)
    public Node2D WeaponHolder { get; set; }

    //Signal to notify when the weapon is picked up by the player
    [Signal] public delegate void PickedUpEventHandler(player player);

    //Cooldown timer to track the time between shots
    private float _cooldown = 0f;

    //Called when the node enters the scene tree
    public override void _Ready()
    {
        //Connect the BodyEntered signal to detect when the player enters the weapons area
        BodyEntered += OnBodyEntered;

        //Set the sprite texture if the sprite and texture references are valid
        if (WeaponSprite != null && WeaponTexture != null)
        {
            WeaponSprite.Texture = WeaponTexture;
        }
        else
        {
            //Log an error if the sprite or texture is missing
            GD.PrintErr("Forgot to set texture for weapon");
        }
    }

    public override void _Process(double delta)
    {
        //Update the cooldown timer
        if (_cooldown > 0)
        {
            _cooldown -= (float)delta; //Decrease the cooldown by the time elapsed since the last frame
        }
    }

    //This method is triggered when a body enters the weapon's collision area
    private void OnBodyEntered(Node body)
    {
        //Check if the body that entered is the player
        //(AI ally and enemies cannot pick up weapons)
        if (body is player player)
        {
            //Call the OnPickup method to handle the pickup logic
            OnPickup(player);
        }
    }

    //Method to handle the weapon being picked up by the player
    public void OnPickup(player player)
    {
        //Emit the PickedUp signal to notify that the weapon has been picked up
        EmitSignal(SignalName.PickedUp, player);

        //Hide the weapon's visuals (e.g., sprite) since it's now in the player's inventory
        //I initialy tried to use the QueueFree() method to remove the weapon but this meant
        //it wouldn't shoot as its no longer in the scene!
        //So I instead chose to just hide the visuals and collisions
        HideVisuals();
    }

    //Method to hide the weapon's visual components (the sprite)
    public void HideVisuals()
    {
            //Disable the weapon's collision layer to prevent further interactions
            CollisionLayer = 0;

            //Hide the weapon sprite
            WeaponSprite.Visible = false;
    }

    //Virtual method to handle firing the weapon
    public virtual bool Fire(Vector2 muzzlePosition) //Pass in the muzzle loacation as a paramater
    {
        //Check if the weapon instance is still valid (not disposed)
        //I fixed this by just hiding the visuals instad
        if (!IsInstanceValid(this))
        {
            GD.PrintErr("Weapon is disposed. Can't fire.");
            return false; //Exit if the weapon is invalid
        }

        //Check if the weapon is on cooldown
        if (_cooldown > 0)
        {
            return false; //Exit if the weapon is on cooldown
        }

        //Reset the cooldown timer to the weapon's firerate
        _cooldown = Firerate;

        //Return true to indicate that the weapon fired successfully
        return true;
    }
}