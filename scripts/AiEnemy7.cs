using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class AiEnemy7 : CharacterBody2D
{
    // Signals
    [Signal] public delegate void DiedEventHandler();

    //Weapon
    [Export] public PackedScene EnemyARScene; //ENemies AR scene to spawn on death
    private Sprite2D _weaponSprite;
    private Marker2D _muzzle;
    private RayCast2D _rayCast;

    //Shooting
    [Export] private float fireRate = 0.08f;
    [Export] private PackedScene projectileScene; //Projectile scene (the bullet)
    //Range within which the enemy can shoot (long range for this enemy)
    [Export] private float shootRange = 20000f;
    //Distance below which the enemy moves away -
    //this is a passive enemy, making it more complex,
    //as it tries to move away from the player
    //I added a min and max distance to prevent it from being undecisive when at an exact distance.
    [Export] private float minDistance = 5200f;
    [Export] private float maxDistance = 5400f;
    private Timer _fireTimer; //to control fire rate

    // Pathfinding and Target
    private AstarPathfinding _pathfinding;
    private Node2D _target;
    private List<Vector2> _path = new List<Vector2>();
    private int _pathIndex = 0;
    private Vector2 _safeLocation; //A predefined or random safe location to move to

    // Enemy Properties
    [Export] private float speed = 4500f; // Movement speed
    [Export] private int hp = 100; // Health
    private bool isDead = false;
    private bool isHit = false;

    // Animation
    private AnimatedSprite2D _animatedSprite;
    private Timer _deathTimer;
    private Timer _hitTimer;


    // Target Update Timer
    private Timer _targetUpdateTimer;

    public override void _Ready()
    {
        SetProcess(false);
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        // Initialize the death timer
        _deathTimer = new Timer();
        _deathTimer.WaitTime = 0.33f;
        _deathTimer.OneShot = true;
        _deathTimer.Timeout += OnDeathTimerFinished;
        AddChild(_deathTimer);

        // Initialize the hit timer
        _hitTimer = new Timer();
        _hitTimer.WaitTime = 0.12f;
        _hitTimer.OneShot = true;
        _hitTimer.Timeout += OnHitTimerFinished;
        AddChild(_hitTimer);

        // Initialize the shooting timer
        _fireTimer = new Timer();
        _fireTimer.WaitTime = fireRate;
        _fireTimer.Autostart = true;
        _fireTimer.OneShot = false;
        _fireTimer.Timeout += Shoot;
        AddChild(_fireTimer);

        // Initialize the target update timer
        _targetUpdateTimer = new Timer();
        _targetUpdateTimer.WaitTime = 1.0f; // Update target every 1 second
        _targetUpdateTimer.Autostart = true;
        _targetUpdateTimer.OneShot = false;
        _targetUpdateTimer.Timeout += UpdateTarget;
        AddChild(_targetUpdateTimer);

        // Get the weapon sprite and muzzle
        _weaponSprite = GetNode<Sprite2D>("WeaponSprite");
        _muzzle = GetNode<Marker2D>("WeaponSprite/Muzzle");

        // Get RayCast2D for line-of-sight checking
        _rayCast = GetNode<RayCast2D>("RayCast2D");

        // Initialize the safe location (you can set this to a predefined position or generate it randomly)
        _safeLocation = new Vector2(1000, 1000); // Example: Set to a predefined position

        // Set collision based on game mode
        UpdateCollisionForGameMode();
    }

    private void UpdateCollisionForGameMode()
    {
        if (Global.IsBattleRoyaleMode)
        {
            // Battle Royale: Can collide with players AND other enemies
            CollisionMask = (1 << 0) | (1 << 2) | (1 << 3); // Player + AIEnemies + PlayerProjectiles
        }
        else
        {
            // Survival: Only collide with players
            CollisionMask = (1 << 0) | (1 << 3); // Player + PlayerProjectiles only
        }
    }

    private CharacterBody2D FindClosestTargetIncludingEnemies()
    {
        CharacterBody2D closest = null;
        float closestDistance = float.MaxValue;

        // Check player
        var player = GetTree().GetFirstNodeInGroup("Players") as CharacterBody2D;
        if (player != null && IsInstanceValid(player))
        {
            float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (dist < closestDistance)
            {
                closest = player;
                closestDistance = dist;
            }
        }

        // Check other enemies (only in battle royale mode)
        if (Global.IsBattleRoyaleMode)
        {
            var enemies = GetTree().GetNodesInGroup("mobs");
            foreach (var enemy in enemies)
            {
                if (enemy != this && enemy is CharacterBody2D enemyBody && IsInstanceValid(enemyBody))
                {
                    float dist = GlobalPosition.DistanceTo(enemyBody.GlobalPosition);
                    if (dist < closestDistance)
                    {
                        closest = enemyBody;
                        closestDistance = dist;
                    }
                }
            }
        }

        return closest;
    }

    public void SetPathfindingGrid(AstarPathfinding pathfinding)
    {
        _pathfinding = pathfinding;
        GD.Print("Pathfinding grid set for AiEnemy7.");
        CheckInitialisation();
    }

    private void CheckInitialisation()
    {
        if (_pathfinding != null)
        {
            SetProcess(true);
            UpdateTarget();
        }
    }

    public void UpdateTarget()
    {
        if (Global.IsBattleRoyaleMode)
        {
            // Battle Royale: Target closest enemy OR player
            _target = FindClosestTargetIncludingEnemies();
        }
        else
        {
            // Original Survival mode logic
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
        }

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
        if (isDead) return;

        if (_pathfinding == null || _target == null)
        {
            GD.PrintErr("Pathfinding or target is not set!");
            return;
        }

        // Update the path if the target moves significantly or we reach the end
        if (_pathIndex >= _path.Count || GlobalPosition.DistanceTo(_target.GlobalPosition) > 0f)
        {
            UpdatePath();
        }

        MoveAlongPath((float)delta);

        // Update weapon rotation to face the player
        UpdateWeaponRotation();
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
            GD.Print("No valid path found!");
            return;
        }

        _pathIndex = 1;
    }
    private void MoveAlongPath(float delta)
    {
        if (isDead) return; //if dead then stop

        //Check if we've reached the current target position in the path
        if (_pathIndex >= _path.Count || GlobalPosition.DistanceTo(_target.GlobalPosition) > 0f)
        {
            UpdatePath();
        }
        //Set the distance to the player/ally
        float distanceToPlayer = GlobalPosition.DistanceTo(_target.GlobalPosition);

        //Update the RayCast target position
        _rayCast.TargetPosition = _target.GlobalPosition - GlobalPosition;
        _rayCast.ForceRaycastUpdate();

        //Check if there's a clear line of sight to the player
        bool hasLineOfSight = !_rayCast.IsColliding();

        if (hasLineOfSight)
        {
            if (distanceToPlayer < minDistance)
            {
                //Move to the safe location if too close to the player
                MoveAwayFromPlayer();
            }
            else if (distanceToPlayer > maxDistance)
            {
                //Move towards the player if too far
                MoveTowardsPlayer((float)delta);
            }
            else
            {
                //Stay idle if within the buffer zone and there's a clear line of sight
                Velocity = Vector2.Zero;
                PlayAnimation();
            }
        }
        else
        {
            //If there's no line of sight then always move towards the player
            MoveTowardsPlayer((float)delta);
        }
    }

    private void MoveTowardsPlayer(float delta)
    {
        if (_pathIndex >= _path.Count) return; //If already at player stop

        Vector2 targetPosition = _path[_pathIndex]; //Set position of the target on the path
        Vector2 direction = (targetPosition - GlobalPosition).Normalized(); //Set direction to this position
        Velocity = direction * speed; //Adjust the velcoity of the CharacterBody2D to this
        MoveAndSlide(); //This is an inbuilt function that moves the character based on the velocity
        PlayAnimation();

        //Move to the next path point
        if (GlobalPosition.DistanceTo(targetPosition) < 0f)
        {
            _pathIndex++;
        }
    }

    //This method makes sure the AI enemy maintains a distance from the player so that it uses its range advantage
    //This makes it more difficult for the player to kill them, especially with a close range gun.
    private void MoveAwayFromPlayer()
    {
        if (_target == null || _pathfinding == null) return;

        //Pathfind to the safe location (the _safeLocation is a variable set at the start)
        Vector2I start = _pathfinding.WorldToMap(GlobalPosition);
        Vector2I target = _pathfinding.WorldToMap(_safeLocation);

        //Check the target position is walkable
        if (!_pathfinding.IsWalkable(target))
        {
            //Find the nearest walkable position to the safe location
            target = FindNearestWalkablePosition(target);
        }

        //Calculate a new path to the safe location
        _path = _pathfinding.FindPath(start, target);
        if (_path == null || _path.Count == 0)
        {
            //Debug to check the path is valid and they can move to that position
            GD.Print("No valid path found to move to the safe location");
            return;
        }

        _pathIndex = 1; //Start moving along the new path

        //Move along the new path
        if (_pathIndex < _path.Count)
        {
            Vector2 nextPosition = _path[_pathIndex];
            Vector2 direction = (nextPosition - GlobalPosition).Normalized();
            Velocity = direction * speed;
            MoveAndSlide();
            PlayAnimation();

            //Move to the next path point
            if (GlobalPosition.DistanceTo(nextPosition) < 5f)
            {
                _pathIndex++;
            }
        }
        else
        {
            GD.Print("reached the safe location");
        }
    }

    private Vector2I FindNearestWalkablePosition(Vector2I target)
    {
        if (_pathfinding == null) return target;

        //Search for the nearest walkable position around the target
        //I set this to 10 for performance reasons and because the maze isn't too large for a higher one to be necessary
        int searchRadius = 10; //The radius for searching for a free path to the target (the safe spot)
        for (int x = -searchRadius; x <= searchRadius; x++) //iterates over the x axis
        {
            for (int y = -searchRadius; y <= searchRadius; y++) //iterates over the y axis
            {
                //For each combination of x and y,
                //a new position (checkPosition) is calculated by adding the current loop indices to the target position
                Vector2I checkPosition = new Vector2I(target.X + x, target.Y + y);
                if (_pathfinding.IsWalkable(checkPosition))
                {
                    return checkPosition; //if the position is found and valid then return it
                }
            }
        }

        //If no walkable position is found, just return the original target
        return target;
    }

    private void UpdateWeaponRotation()
    {
        if (_target == null || _weaponSprite == null) return;

        //Calculate the direction to the player
        Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();

        //Calculate the angle in radians
        float angle = Mathf.Atan2(direction.Y, direction.X);

        //Flip the weapon sprite by adjusting its scale
        if (direction.X < 0)
        {
            //Flip vertically when aiming left
            _weaponSprite.Scale = new Vector2(0.972f, -0.918f);
        }
        else
        {
            //Normal when aiming right
            _weaponSprite.Scale = new Vector2(0.972f, 0.918f);
        }

        //Set the weapon sprite's rotation
        _weaponSprite.Rotation = angle;
    }

    private void Shoot()
    {
        //Check if the enemy is dead, the target is missing, or the projectile scene isn't set
        if (isDead || _target == null || projectileScene == null) return;

        //Calculate the distance to the player
        float distanceToPlayer = GlobalPosition.DistanceTo(_target.GlobalPosition);

        //Check line of sight before shooting
        //Set the RayCast's target position to the player's position
        _rayCast.TargetPosition = _target.GlobalPosition - GlobalPosition;
        //This updates the position of the raycast to the latest set position, in this case it's the player's position
        _rayCast.ForceRaycastUpdate(); //Force the update

        //Check if the player is within shooting range and that there's a clear line of sight, if so then shoot
        if (distanceToPlayer <= shootRange && !_rayCast.IsColliding())
        {
            //Calculate the direction to the player
            Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
            GD.Print($"Shooting projectile in direction: {direction}");

            //Instantiate the projectile
            Projectile2 projectileInstance = projectileScene.Instantiate<Projectile2>();
            GetParent().AddChild(projectileInstance); //Add the projectile to the same parent as the enemy

            //Set the projectile's position and direction
            //Spawn the projectile at the muzzle position, this is set in the editor.
            projectileInstance.GlobalPosition = _muzzle.GlobalPosition;
            projectileInstance.Direction = direction; //Set the projectile's movement direction
        }
        else
        {
            //If the player is out of range or there's no line of sight, move away from the player
            MoveAwayFromPlayer();
        }
    }

    public void Reset()
    {
        _path.Clear();
        _pathIndex = 0;
        Velocity = Vector2.Zero;
        isDead = false;
        isHit = false;
        _animatedSprite.Play("Idle");

        // Reset collision layers and masks based on current game mode
        CollisionLayer = 3; // Set to the default collision layer for enemies
        UpdateCollisionForGameMode(); // Update mask based on current game mode
    }

    private void PlayAnimation()
    {
        if (isDead || isHit) return;

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
        if (isDead) return;

        hp -= damage;

        isHit = true;
        _animatedSprite.Play("hit");
        _hitTimer.Start();

        if (hp <= 0)
        {
            RemoveFromGroup("mobs");
            CollisionLayer = 0;
            GD.Print("AiEnemy7 defeated!");
            isDead = true; // Mark the enemy as dead
            Velocity = Vector2.Zero; // Stop movement
            _animatedSprite.Play("death"); // Play death animation

            // Stop the shooting timer
            _fireTimer.Stop();

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
        EmitSignal(SignalName.Died);

        //This sections spawns the AI's weapon into the scene when it dies. 
        if (EnemyARScene != null)
        {
            Node2D enemyARInstance = EnemyARScene.Instantiate<Node2D>();
            GetParent().AddChild(enemyARInstance); //Add to the same parent as the enemy
            enemyARInstance.GlobalPosition = GlobalPosition; //Set position to the enemy's position
        }

        //Return the enemy to the pool
        var spawner = GetParent<EnemySpawner>();
        if (spawner != null)
        {
            spawner.ReturnEnemyToPool(this);
        }
        //Reset health and state
        hp = 100;
        isDead = false;
    }
}