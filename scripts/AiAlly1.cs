using Godot;
using System.Collections.Generic;

public partial class AiAlly1 : CharacterBody2D
{
    // Signals
    [Signal] public delegate void DiedEventHandler();

    // Pathfinding and Target
    private AstarPathfinding _pathfinding;
    private player _player; // Follow the player
    private Node2D _currentTarget; // Current target (player or enemy)
    private List<Vector2> _path = new List<Vector2>();
    private int _pathIndex = 0;

    // Ally Properties
    [Export] public int speed = 4000; // Movement speed
    [Export] public int hp = 100; // Health
    private bool isDead = false;
    private bool isHit = false;

    // Shooting
    [Export] public float fireRate = 0.12f; // Time between shots
    [Export] private PackedScene projectileScene; // Projectile scene
    [Export] private float shootRange = 20000f; // Range within which the ally can shoot
    [Export] private float followDistance = 1000f; // Distance to maintain from the player
    [Export] public PackedScene AllyPistolScene; // John Pistol scene to spawn on death
    private Timer _fireTimer;

    // Animation
    private AnimatedSprite2D _animatedSprite;
    private Timer _deathTimer;
    private Timer _hitTimer;

    //Shield
    private bool _isShielded = false; // Flag to track if the AI ally is shielded
    private Timer _shieldTimer; // Timer to manage the shield duration

    // Weapon
    private Sprite2D _weaponSprite;
    private Marker2D _muzzle;
    private RayCast2D _rayCast; // RayCast2D for line-of-sight check

    //Damage Handling
    //Track colliding enemies using hashset
    private HashSet<Node> collidingEnemies = new HashSet<Node>();
    //The Cooldown between taking damage (in seconds)
    private float damageCooldown = 0.1f; 
    private float timeSinceLastDamage = 0f;

    //Ally Mode
    //uses an enum to represent the mode (a group of constants)
    private enum AllyMode { FollowPlayer, ChaseEnemies }
    //This tracks the mode the AI is set to, by default its set to follow the player
    private AllyMode _currentMode = AllyMode.FollowPlayer; 
                                

    public override void _Ready()
    {
        AddToGroup("Allies");
        SetProcess(false); // Disable process initially
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        // Initialize the shield timer
        _shieldTimer = new Timer();
        _shieldTimer.WaitTime = 20f; // Default duration, can be overridden by the Shield
        _shieldTimer.OneShot = true;
        _shieldTimer.Timeout += OnShieldTimerFinished;
        AddChild(_shieldTimer);


        // Initialize timers and other components
        _deathTimer = new Timer();
        _deathTimer.WaitTime = 0.33f;
        _deathTimer.OneShot = true;
        _deathTimer.Timeout += OnDeathTimerFinished;
        AddChild(_deathTimer);

        _hitTimer = new Timer();
        _hitTimer.WaitTime = 0.12f;
        _hitTimer.OneShot = true;
        _hitTimer.Timeout += OnHitTimerFinished;
        AddChild(_hitTimer);

        _fireTimer = new Timer();
        _fireTimer.WaitTime = fireRate;
        _fireTimer.Autostart = true;
        _fireTimer.OneShot = false;
        _fireTimer.Timeout += Shoot;
        AddChild(_fireTimer);

        _weaponSprite = GetNode<Sprite2D>("WeaponSprite");
        _muzzle = GetNode<Marker2D>("WeaponSprite/Muzzle");
        _rayCast = GetNode<RayCast2D>("RayCast2D");

        // Enable process after initialization
        SetProcess(true);

        // Connect collision signals
        GetNode<Area2D>("Area2D").BodyEntered += OnBodyEntered;
        GetNode<Area2D>("Area2D").BodyExited += OnBodyExited;
    }

    public override void _Input(InputEvent @event)
    {
        //Toggles the ally mode when CTRL is pressed
        //CTRL is set to "toggle_ally_mode" in the editor
        if (@event.IsActionPressed("toggle_ally_mode")) 
        {
            ToggleAllyMode(); //Run the function to toggle it (below)
        }
    }

    private void ToggleAllyMode()
    {
        //Switch between FollowPlayer and ChaseEnemies modes
        _currentMode = (_currentMode == AllyMode.FollowPlayer) ? AllyMode.ChaseEnemies : AllyMode.FollowPlayer;
        GD.Print("Ally mode toggled to: " + _currentMode); //debug to check its actually swapping

        //Update the target based on the new mode
        //The target is either the player or the nearest enemy
        //(the "mobs" group or the player)
        UpdateTarget(); 

    }

    public void SetPathfindingGrid(AstarPathfinding pathfinding)
    {
        _pathfinding = pathfinding;
        GD.Print("Pathfinding grid set for AIAlly.");
        CheckInitialization();
    }

    public void SetPlayer(player player)
    {
        _player = player;
        GD.Print("Player set for AIAlly.");
        CheckInitialization();
    }

    private void CheckInitialization()
    {
        if (_pathfinding != null && _player != null)
        {
            SetProcess(true);
            UpdateTarget();
        }
    }

    public override void _Process(double delta)
    {
        UpdateTarget(); //Immediately update the target for the Ally
        _fireTimer.WaitTime = fireRate;
        if (isDead) return;

        if (_pathfinding == null || _currentTarget == null)
        {
            return;
        }

        //Update the path if the target moves significantly or we reach the end
        if (_pathIndex >= _path.Count || GlobalPosition.DistanceTo(_currentTarget.GlobalPosition) > 50f)
        {
            UpdatePath();
        }

        MoveAlongPath((float)delta);

        // Update weapon rotation to face the nearest enemy
        UpdateWeaponRotation();

        //This applies damage over time while colliding with enemies
        if (collidingEnemies.Count > 0)
        {
            timeSinceLastDamage += (float)delta;
            if (timeSinceLastDamage >= damageCooldown)
            {
                foreach (var enemy in collidingEnemies)
                {
                    //Apply 1 damage per tick (this is applied every damage cooldown)
                    TakeDamage(1); 
                }
                timeSinceLastDamage = 0f;
            }
        }
    }

    private void UpdateTarget()
    {
        if (_pathfinding == null || _player == null)
        {
            GD.PrintErr("Check player is set"); //Error log so I could see
            return; //Return if the player isnt set properly
        }

        if (_currentMode == AllyMode.FollowPlayer)
        {
            _currentTarget = _player; //Set the target to the player for this mode
        }
        else if (_currentMode == AllyMode.ChaseEnemies)
        {
            // Find the nearest enemy
            var enemies = GetTree().GetNodesInGroup("mobs"); //Get all enemies in the "mobs" group
            Node2D nearestEnemy = null; //Set there to no nearest enemy to start with
            //set to max value so we ensure that any valid distance to an enemy will be smaller than the initial value
            //It'll be updated when it finds a nearer enemy
            float nearestDistance = float.MaxValue; 

            foreach (Node2D enemy in enemies) //check each enemy
            {
                float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition); //Calculate the distance to enemy
                if (distance < nearestDistance) //Check against the current nearest enemy
                {
                    nearestEnemy = enemy; //Update nearest enemy
                    nearestDistance = distance; //Set the nearest to the new nearest distance
                }
            }

            _currentTarget = nearestEnemy; //Set the target to the nearest enemy
        }

        if (_currentTarget == null)
        {
            return; //Return when theres no enemy so that the AI stops moving/searching
        }
        UpdatePath(); //Update the path to the new target
    }

    private void UpdatePath()
    {
        if (_pathfinding == null || _currentTarget == null)
        {
            GD.PrintErr("Pathfinding or target is not set!");
            return;
        }

        Vector2I start = _pathfinding.WorldToMap(GlobalPosition);
        Vector2I target = _pathfinding.WorldToMap(_currentTarget.GlobalPosition);

        if (!_pathfinding.IsWalkable(start) || !_pathfinding.IsWalkable(target))
        {
            GD.PrintErr("Start or target position is not walkable!");
            return;
        }

        _path = _pathfinding.FindPath(start, target);
        if (_path == null || _path.Count == 0) 
        {
            GD.Print("No valid path found!");
            return;
        }

        _pathIndex = 1;
    }

    private void MoveAlongPath(float delta)
    {
        if (isDead) return;

        if (_pathIndex >= _path.Count)
        {
            return; //Return if path reached
        }

        Vector2 targetPosition = _path[_pathIndex];
        Vector2 direction = (targetPosition - GlobalPosition).Normalized();
        float distanceToTarget = GlobalPosition.DistanceTo(_currentTarget.GlobalPosition);

        if (_currentMode == AllyMode.FollowPlayer && distanceToTarget > followDistance)
        {
            Velocity = direction * speed;
        }
        else if (_currentMode == AllyMode.ChaseEnemies && distanceToTarget > 2000) //Slight gap so he keeps his distance
        {
            Velocity = direction * speed; //Always move toward the enemy in chase mode
        }
        else
        {
            Velocity = Vector2.Zero; //Stop moving if within followDistance (follow mode)
        }

        MoveAndSlide();
        PlayAnimation();

        if (GlobalPosition.DistanceTo(targetPosition) < 5f)
        {
            _pathIndex++;
        }
    }

    private void UpdateWeaponRotation()
    {
        if (_weaponSprite == null) return;

        // Find the nearest enemy
        var enemies = GetTree().GetNodesInGroup("mobs");
        Node2D nearestEnemy = null;
        float nearestDistance = shootRange;

        foreach (Node2D enemy in enemies)
        {
            float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distance < nearestDistance)
            {
                nearestEnemy = enemy;
                nearestDistance = distance;
            }
        }

        if (nearestEnemy == null)
        {
            GD.Print("No enemy found for weapon rotation.");
            return;
        }

        // Calculate the direction to the nearest enemy
        Vector2 direction = (nearestEnemy.GlobalPosition - GlobalPosition).Normalized();

        // Calculate the angle in radians
        float angle = Mathf.Atan2(direction.Y, direction.X);

        // Flip the weapon sprite by adjusting its scale
        if (direction.X < 0)
        {
            // Flip vertically when aiming left
            _weaponSprite.Scale = new Vector2(0.972f, -0.918f);
        }
        else
        {
            // Normal when aiming right
            _weaponSprite.Scale = new Vector2(0.972f, 0.918f);
        }

        // Set the weapon sprite's rotation
        _weaponSprite.Rotation = angle;
    }

    private void Shoot()
    {
        if (isDead || _player == null || projectileScene == null) return;

        // Find the nearest enemy
        var enemies = GetTree().GetNodesInGroup("mobs");
        Node2D nearestEnemy = null;
        float nearestDistance = shootRange;

        foreach (Node2D enemy in enemies)
        {
            float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distance < nearestDistance)
            {
                nearestEnemy = enemy;
                nearestDistance = distance;
            }
        }

        if (nearestEnemy == null)
        {
            GD.Print("No enemy found within range.");
            return;
        }

        // Check line of sight before shooting
        _rayCast.TargetPosition = nearestEnemy.GlobalPosition - GlobalPosition;
        _rayCast.ForceRaycastUpdate();

        if (nearestDistance <= shootRange && !_rayCast.IsColliding())
        {
            Vector2 direction = (nearestEnemy.GlobalPosition - GlobalPosition).Normalized();
            GD.Print($"Shooting projectile in direction: {direction}");

            // Instantiate the projectile
            ProjectileFriendly projectileInstance = projectileScene.Instantiate<ProjectileFriendly>();
            GetParent().AddChild(projectileInstance); // Add to the same parent as the ally

            // Set projectile position and direction
            projectileInstance.GlobalPosition = _muzzle.GlobalPosition; // Spawn from the muzzle
            projectileInstance.Direction = direction;

            // Debug: Ensure the projectile is moving
            GD.Print("Projectile spawned at:", projectileInstance.GlobalPosition, "with direction:", direction);
        }
        else
        {
            GD.Print("No line of sight to the nearest enemy.");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || _isShielded) return;

        // Play the hit animation
        _animatedSprite.Play("hit");
        isHit = true; // Set the hit state
        _hitTimer.Start(); // Start the hit timer

        hp -= damage;
        GD.Print($"AIAlly took {damage} damage! Health: {hp}");

        if (hp <= 0)
        {
            CollisionLayer = 0;
            GD.Print("AIAlly defeated!");
            isDead = true; // Mark the ally as dead
            Velocity = Vector2.Zero; // Stop movement
            _animatedSprite.Play("death"); // Play death animation

            // Stop the shooting timer
            _fireTimer.Stop();

            // Start the death timer
            _deathTimer.Start();
        }
    }

    private void PlayAnimation()
    {
        if (isDead || isHit) return; // Skip animation updates if dead or in hit state

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

    private void OnHitTimerFinished()
    {
        isHit = false; // Reset the hit state
        PlayAnimation(); // Resume normal animations
    }

private void OnDeathTimerFinished()
{
    GD.Print("AIAlly death timer finished. Emitting Died signal.");
    EmitSignal(SignalName.Died); // Emit the Died signal

    // Spawn the John Pistol at the ally's position
    if (AllyPistolScene != null)
    {
        Node2D allyPistolInstance = AllyPistolScene.Instantiate<Node2D>();
        GetParent().AddChild(allyPistolInstance); // Add to the same parent as the ally
        allyPistolInstance.GlobalPosition = GlobalPosition; // Set position to the ally's position
        GD.Print("John Pistol spawned at ally's position.");
    }

    // Reset health and state
    hp = 100;
    isDead = false;

    // Despawn the ally
    QueueFree();
}

    // Handle collision with enemies
    private void OnBodyEntered(Node body)
    {
        if (body is AiEnemy1 || body is AiEnemy2 || body is AiEnemy3 || body is AiEnemy4 || body is AiBoss1)
        {
            collidingEnemies.Add(body);
            GD.Print("AIAlly collided with enemy!");
        }
    }

    // Handle when the AI Ally stops colliding with an enemy
    private void OnBodyExited(Node body)
    {
        if (body is AiEnemy1 || body is AiEnemy2 || body is AiEnemy3 || body is AiEnemy4 || body is AiBoss1)
        {
            collidingEnemies.Remove(body); // Remove the enemy from the colliding set
            GD.Print("AIAlly stopped colliding with enemy!");
        }
    }

    public void RestoreHealth(int amount)
    {
        hp = Mathf.Min(hp + amount, 100); // Restore health, but don't exceed 100
        GD.Print($"Player health restored by {amount}. Current health: {hp}");
    }

    // Method to activate the shield
    public void ActivateShield(float duration)
    {
        _isShielded = true;
        _shieldTimer.WaitTime = duration;
        _shieldTimer.Start();
        GD.Print("Shield activated! AI Ally is now invulnerable.");
    }

    // Method to deactivate the shield
    private void OnShieldTimerFinished()
    {
        _isShielded = false;
        GD.Print("Shield deactivated. AI Ally is now vulnerable.");
    }
}