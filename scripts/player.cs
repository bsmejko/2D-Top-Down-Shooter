using Godot;
using System;
using System.Collections.Generic;

public partial class player : CharacterBody2D
{
    //Player movement speeds
    [Export]
    public int Speed = 2000; //Base movement speed - lower so the player can be more precise 
    //Sprint speed (faster than base speed to allow player to quickly move round map if needs be)
    public int SprintSpeed = 4000; 

    //References to other nodes and scenes
    [Export]
    private EnemySpawner spawner; 

    private ProgressBar health; //Health bar UI element
    private AnimatedSprite2D _animatedSprite; //Animated sprite for changes in player animation
    public bool IsMoving { get; private set; } //Tracks if the player is currently moving

    //Player health and damage settings
    public int hp = 100; //Player's health
    private const int damagePerSecond = 10; //Base damage per second when colliding with enemies 
    private float damageCooldown = 0.1f; //Cooldown between damage ticks
    private float timeSinceLastDamage = 0f; //Tracks time since last damage tick

    //Weapon and camera references
    private Node2D weaponHolder; //Node to hold the weapon
    private Sprite2D weaponSprite; //Sprite for the equipped weapon
    private Camera2d camera; //Camera for screen shake effects

    //Timers for hit animation and shield duration
    private Timer _hitTimer; //Timer for hit animation cooldown
                             //(allows the animation to play for long enough without being interrupted
    private bool _isShielded = false; //Tracks if the player is currently shielded (by the shield powerup)
    private Timer _shieldTimer; //Timer for shield duration

    //Tracks enemies the player is currently colliding with using a hashset.
    private HashSet<Node> collidingEnemies = new HashSet<Node>();

    //Currently equipped weapon
    public Weapon CurrentWeapon { get; private set; }

    //Dictionary to store damage values for different enemy types
    private Dictionary<Type, int> enemyDamage = new Dictionary<Type, int>
    {
        //The damages for each of the enemies that can collide with the player
        { typeof(AiEnemy1), 1 }, 
        { typeof(AiEnemy2), 3 }, 
        { typeof(AiEnemy3), 5 }, 
        { typeof(AiEnemy4), 2 }, 
        { typeof(AiBoss1), 6 }  
    };

    //Called when the node enters the scene tree
    public override void _Ready()
    {
        //Add the player to the "Players" group for easy access
        AddToGroup("Players");

        //Get references to nodes
        camera = GetNode<Camera2d>("Camera2D"); //Camera for screen shake
        weaponHolder = GetNode<Node2D>("WeaponHolder"); //Node to hold the weapon
        weaponSprite = GetNode<Sprite2D>("WeaponHolder/WeaponSprite"); //Weapon sprite

        spawner = GetNode<EnemySpawner>("../EnemySpawner"); //The enemy spawner
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D"); //Player's animated sprite
        health = GetNode<ProgressBar>("../CanvasLayer/HealthBar"); //Health bar UI
        health.Value = hp; //Set initial health value to value declared above

        //Initialise the timers:

        _hitTimer = new Timer();
        _hitTimer.WaitTime = 0.12f; //Duration of the hit animation
        _hitTimer.OneShot = true;
        _hitTimer.Timeout += OnHitTimerFinished; //Callback when the timer finishes
        AddChild(_hitTimer);

        _shieldTimer = new Timer();
        _shieldTimer.WaitTime = 20f; //Default shield duration
        _shieldTimer.OneShot = true;
        _shieldTimer.Timeout += OnShieldTimerFinished; //Callback when the shield expires
        AddChild(_shieldTimer);

        //Connect signals for collision detection (could've been done in the editor but clearer if I manually do it here)
        GetNode<Area2D>("Area2D").BodyEntered += _on_Area2D_body_entered; //When an enemy enters the collision area -
                                                                          //an area set using a collision shape in the editor
        GetNode<Area2D>("Area2D").BodyExited += _on_Area2D_body_exited; //When an enemy exits the collision area -
                                                                        //uses same collision shape

        //Connect to the weapon pickup area - this uses a different collision shape -
        //called WeaponPickUpArea in the editor
        GetNode<Area2D>("WeaponPickupArea").BodyEntered += OnWeaponBodyEntered; //When a weapon is picked up
        //No exit one required

        //Equip the starting pistol
        //This is a pistol in the scene under the player -
        //the player has a weaponholder child and then the pistol
        //as a child of that
        var startingPistol = GetNode<Weapon>("WeaponHolder/Pistol"); 
        if (startingPistol != null)
        {
            EquipWeapon(startingPistol); //Equip the pistol
        }
        else
        {
            GD.PrintErr("Starting pistol not found"); //Debug error if pistol is missing
            //(this happened a lot as I struggled to make it recognise the starting pistol as part of the weapon class)
        }
    }

    //Called when a weapon enters the pickup area, which is the signal I created earlier
    private void OnWeaponBodyEntered(Node body)
    {
        if (body is Weapon weapon)
        {
            //Equip the new weapon
            EquipWeapon(weapon);

            //Hide the weapon's visuals instead of disposing of it
            //because when disposed the shoot function wouldn't work as the weapon no longer existed
            //This was an issue I had for a while
            weapon.HideVisuals();
        }
        else
        {
            GD.Print("Objects not a weapon"); //Debug if the object isn't a weapon
        }
    }

    //Equips a weapon and updates the player's visuals
    public void EquipWeapon(Weapon weapon)
    {
        if (CurrentWeapon != null && IsInstanceValid(CurrentWeapon))
        {
            CurrentWeapon.HideVisuals(); //Hide the old weapon's visuals
        }

        //Equip the new weapon
        CurrentWeapon = weapon;
        weapon.WeaponHolder = GetNode<Node2D>("WeaponHolder"); //Assign WeaponHolder

        //Update the weapon sprite texture
        if (weaponSprite != null)
        {
            weaponSprite.Texture = weapon.WeaponTexture;
        }
        else
        {
            GD.PrintErr("WeaponSprite is null!!"); //Debug error if weapon sprite is missing
        }
    }

    //Fires the equipped weapon - called when left mouse button is pressed
    public void Shoot()
    {
        if (CurrentWeapon != null)
        {
            if (IsInstanceValid(CurrentWeapon))
            {
                //Get the muzzle position in the weapon's local space
                Vector2 muzzleLocalPosition = GetNode<Node2D>("WeaponHolder/WeaponSprite/Muzzle").Position;

                //Convert the local muzzle position to global space
                Vector2 muzzleGlobalPosition = weaponHolder.ToGlobal(muzzleLocalPosition);

                //Call the weapon's Fire method with the muzzle's global position
                CurrentWeapon.Fire(muzzleGlobalPosition);

                //Unfortunately this method means the muzzle is in the same position for each weapon,
                //despite the length of the weapon.
            }
        }
        else
        {
            GD.Print("No weapon equipped!"); //Debug if no weapon is actually equipped
        }
    }

    //Handles player input for movement
    public void GetInput()
    {
        Vector2 inputDirection = Input.GetVector("left", "right", "up", "down"); //Get input direction
        IsMoving = inputDirection != Vector2.Zero; //Check if the player is moving

        //Set velocity based on input and sprint state
        if (Input.IsActionPressed("Sprint")) //for actions like this, you use the editor to assign keys to a name.
                                             //in this case I used the LEFT SHIFT key and called it "Sprint".
        {
            Velocity = inputDirection * SprintSpeed; 
        }
        else
        {
            Velocity = inputDirection * Speed; 
        }
    }

    //Plays the appropriate animation based on movement direction
    public void PlayAnimation()
    {
        if (hp <= 0) return; //If dead, dont play

        float directionx = Velocity.X;
        float directiony = Velocity.Y;
        bool flip = _animatedSprite.FlipH;

        if (directionx > 0)
        {
            _animatedSprite.Play("right"); 
            _animatedSprite.FlipH = false;
        }
        else if (directionx < 0)
        {
            _animatedSprite.Play("left"); 
            _animatedSprite.FlipH = true;
        }
        else if (directiony != 0 && !flip)
        {
            _animatedSprite.Play("right"); 
        }
        else if (directiony != 0 && flip)
        {
            _animatedSprite.Play("left"); 
        }
        else
        {
            _animatedSprite.Play("Idle"); //Idle animation by default
        }
    }

    //Called when an enemy enters the collision area (set at start)
    public void _on_Area2D_body_entered(Node body)
    {
        if (body is AiEnemy1 || body is AiEnemy2 || body is AiEnemy3 || body is AiEnemy4 || body is AiBoss1)
        {
            collidingEnemies.Add(body); //Add the enemy to the colliding set
        }
    }

    //Called when an enemy exits the collision area
    public void _on_Area2D_body_exited(Node body)
    {
        if (body is AiEnemy1 || body is AiEnemy2 || body is AiEnemy3 || body is AiEnemy4 || body is AiBoss1)
        {
            collidingEnemies.Remove(body); //Remove the enemy from the colliding set
        }
    }

    //Rotates the weapon to face the mouse position
    private void RotateWeapon()
    {
        Vector2 mousePosition = GetGlobalMousePosition(); //Get mouse position
        weaponHolder.LookAt(mousePosition); //Rotate the weapon holder

        //Flip the weapon sprite based on mouse position
        if (mousePosition.X < GlobalPosition.X)
        {
            weaponSprite.Scale = new Vector2((float)0.972, (float)-0.918); //Flip vertically when aiming left
        }
        else
        {
            weaponSprite.Scale = new Vector2((float)0.972, (float)0.918); //Normal when aiming right
        }
    }

    //Called every physics frame by Godot
    public override void _PhysicsProcess(double delta)
    {
        GetInput(); //Handle player input
        MoveAndSlide(); //Move the player

        //Shoot if the shoot action is pressed
        if (Input.IsActionPressed("shoot")) //another key assigned in the editor left mouse button => "shoot"
        {
            Shoot(); //call the function to shoot!
        }

        RotateWeapon(); //Rotate the weapon to face the mouse

        if (!_hitTimer.IsStopped()) return; //Skip animation updates if the hit animation is playing

        PlayAnimation(); //Update player animations - all animations are set in the editor with unique names

        //Apply damage over time while colliding with enemies
        if (collidingEnemies.Count > 0 && !_isShielded)
        {
            camera.ShakeCamera(30); //This applies a shake effect to the camera
            timeSinceLastDamage += (float)delta;
            if (timeSinceLastDamage >= damageCooldown)
            {
                _animatedSprite.Play("hit"); //Play hit animation while in collision
                foreach (var enemy in collidingEnemies)
                {
                    int enemySpecificDamage = enemyDamage.ContainsKey(enemy.GetType()) ? enemyDamage[enemy.GetType()] : damagePerSecond;
                    hp -= enemySpecificDamage; //Apply damage
                }

                health.Value = hp; //Update health bar
                timeSinceLastDamage = 0f;

                if (hp <= 0)
                {
                    HandlePlayerDeath();
                }
            }
        }
    }

    //This method handles the player's death by displaying the death screen etc...
    private async void HandlePlayerDeath()
    {
        CollisionLayer = 0; //Disable collisions
        _animatedSprite.Play("death"); //Play death animation

        //Disable player movement and input
        SetProcess(false);
        SetPhysicsProcess(false);

        //Wait for 1 second before transitioning to the death scene, to allow time for death animation to play.
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

        //Change to the DeathScene
        GetTree().ChangeSceneToFile("death_scene.tscn");
    }

    //Restores the player's health - used by the medkit powerup
    public void RestoreHealth(int amount)
    {
        hp = Mathf.Min(hp + amount, 100); //Restore health, but don't exceed 100
        health.Value = hp;
    }

    //Applies damage to the player when shot by a projectile (from the shooter opponents)
    public void TakeDamage(int damage)
    {
        if (hp <= 0 || _isShielded) return; //If already dead or shielded, ignore

        _animatedSprite.Play("hit"); //Play hit animation
        camera.ShakeCamera(50); //Shake the camera, at a higher amount to represent higher damage
        hp -= damage; //Apply damage (specific to the AI opponent's projectile that hit)
        health.Value = hp; //Update health bar

        _hitTimer.Start(); //Start the hit timer

        if (hp <= 0)
        {
            HandlePlayerDeath(); //Handle player death
        }
    }

    //Called when the hit timer finishes
    private void OnHitTimerFinished()
    {
        if (hp > 0)
        {
            PlayAnimation(); //Resume normal animations
        }
    }

    //Activates the shield power up
    public void ActivateShield(float duration)
    {
        _isShielded = true;
        _shieldTimer.WaitTime = duration;
        _shieldTimer.Start();
    }

    //Called when the shield timer finishes
    private void OnShieldTimerFinished()
    {
        _isShielded = false;
    }
}