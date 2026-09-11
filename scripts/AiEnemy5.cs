using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class AiEnemy5 : CharacterBody2D
{
    [Signal] public delegate void DiedEventHandler();

    [Export] public PackedScene EnemyPistolScene;

    private AstarPathfinding _pathfinding;
    private Node2D _target;
    private List<Vector2> _path = new List<Vector2>();
    private int _pathIndex = 0;

    [Export] private float speed = 4000f;
    [Export] private int hp = 100;
    private bool isDead = false;
    private bool isHit = false;

    [Export] private float fireRate = 0.2f;
    [Export] private PackedScene projectileScene;
    [Export] private float shootRange = 20000f;
    [Export] private float stopDistance = 1200f;
    private Timer _fireTimer;

    private AnimatedSprite2D _animatedSprite;
    private Timer _deathTimer;
    private Timer _hitTimer;

    private Sprite2D _weaponSprite;
    private Marker2D _muzzle;
    private RayCast2D _rayCast;

    private Timer _targetUpdateTimer;

    public override void _Ready()
    {
        SetProcess(false);
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

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

        _targetUpdateTimer = new Timer();
        _targetUpdateTimer.WaitTime = 1.0f;
        _targetUpdateTimer.Autostart = true;
        _targetUpdateTimer.OneShot = false;
        _targetUpdateTimer.Timeout += UpdateTarget;
        AddChild(_targetUpdateTimer);

        _weaponSprite = GetNode<Sprite2D>("WeaponSprite");
        _muzzle = GetNode<Marker2D>("WeaponSprite/Muzzle");
        _rayCast = GetNode<RayCast2D>("RayCast2D");

        UpdateCollisionForGameMode();
    }

    private void UpdateCollisionForGameMode()
    {
        if (Global.IsBattleRoyaleMode)
            CollisionMask = (1 << 0) | (1 << 2) | (1 << 3);
        else
            CollisionMask = (1 << 0) | (1 << 3);
    }

    private CharacterBody2D FindClosestTargetIncludingEnemies()
    {
        CharacterBody2D closest = null;
        float closestDistance = float.MaxValue; 

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
        GD.Print("Pathfinding grid set for AiEnemy5.");
        CheckInitialization();
    }

    private void CheckInitialization()
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
            _target = FindClosestTargetIncludingEnemies();
        }
        else
        {
            var targets = GetTree().GetNodesInGroup("Players")
                .Concat(GetTree().GetNodesInGroup("Allies"))
                .ToList();

            if (targets.Count == 0)
            {
                _target = null;
                GD.Print("No targets found.");
                return;
            }

            GD.Print("Available targets:");
            foreach (var target in targets)
                GD.Print($"- {target.Name} at {((Node2D)target).GlobalPosition}");

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

        if (_pathIndex >= _path.Count || GlobalPosition.DistanceTo(_target.GlobalPosition) > 50f)
            UpdatePath();

        MoveAlongPath((float)delta);
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
        if (isDead) return;

        if (_pathIndex >= _path.Count)
        {
            GD.Print("Reached the end of the path.");
            return;
        }

        Vector2 targetPosition = _path[_pathIndex];
        Vector2 direction = (targetPosition - GlobalPosition).Normalized();
        float distanceToPlayer = GlobalPosition.DistanceTo(_target.GlobalPosition);

        if (distanceToPlayer > stopDistance)
            Velocity = direction * speed;
        else
            Velocity = Vector2.Zero;

        MoveAndSlide();
        PlayAnimation();

        if (GlobalPosition.DistanceTo(targetPosition) < 5f)
            _pathIndex++;
    }

    private void UpdateWeaponRotation()
    {
        if (_target == null || _weaponSprite == null) return;

        Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
        float angle = Mathf.Atan2(direction.Y, direction.X);

        if (direction.X < 0)
            _weaponSprite.Scale = new Vector2(0.972f, -0.918f);
        else
            _weaponSprite.Scale = new Vector2(0.972f, 0.918f);

        _weaponSprite.Rotation = angle;
    }

    private void Shoot()
    {
        if (isDead || _target == null || projectileScene == null) return;

        float distanceToPlayer = GlobalPosition.DistanceTo(_target.GlobalPosition);

        _rayCast.TargetPosition = _target.GlobalPosition - GlobalPosition;
        _rayCast.ForceRaycastUpdate();

        if (distanceToPlayer <= shootRange && !_rayCast.IsColliding())
        {
            Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
            GD.Print($"Shooting projectile in direction: {direction}");

            Projectile projectileInstance = projectileScene.Instantiate<Projectile>();
            GetParent().AddChild(projectileInstance);
            projectileInstance.GlobalPosition = _muzzle.GlobalPosition;
            projectileInstance.Direction = direction;
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

        CollisionLayer = 3;
        UpdateCollisionForGameMode();
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
            GD.Print("AiEnemy5 defeated!");
            isDead = true;
            Velocity = Vector2.Zero;
            _animatedSprite.Play("death");
            _fireTimer.Stop();
            _deathTimer.Start();
        }
    }

    private void OnHitTimerFinished()
    {
        isHit = false;
        PlayAnimation();
    }

    private void OnDeathTimerFinished()
    {
        GD.Print("AiEnemy5 death timer finished. Emitting Died signal.");
        EmitSignal(SignalName.Died);

        if (EnemyPistolScene != null)
        {
            Node2D enemyPistolInstance = EnemyPistolScene.Instantiate<Node2D>();
            GetParent().AddChild(enemyPistolInstance);
            enemyPistolInstance.GlobalPosition = GlobalPosition;
            GD.Print("Boss Shotgun spawned at enemy's position.");
        }

        var spawner = GetParent<EnemySpawner>();
        if (spawner != null)
            spawner.ReturnEnemyToPool(this);
        else
            GD.PrintErr("EnemySpawner not found as parent!");

        hp = 100;
        isDead = false;
    }
}