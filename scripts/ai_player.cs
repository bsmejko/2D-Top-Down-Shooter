using Godot;
using System;

public partial class ai_player : CharacterBody2D
{
    [Export]
    public CharacterBody2D targetToChase;  // Ensure this is set to the player node in the inspector
    private NavigationAgent2D _navigationAgent;

    private const float SPEED = 2000f;
    private const float STOP_DISTANCE = 1600f;  // Distance at which AI should stop
    private const float REACTIVATE_DISTANCE = 1200f;  // Distance to reactivate AI if it gets too close
    private Random _random = new Random();
    private Vector2 _randomOffset = Vector2.Zero;

    private Vector2 _previousTargetPosition;  // To track the player's last position
    private bool _isPlayerMoving = false;     // Flag to detect player movement

    public override void _Ready()
    {
        _navigationAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        SetPhysicsProcess(false);
        CallDeferred(nameof(WaitForPhysics));

    }

    private async void WaitForPhysics()
    {
        await ToSignal(GetTree(), "physics_frame");
        SetPhysicsProcess(true);
    }

    private bool IsTargetMoving()
    {
        // Check if the target is moving by comparing current and previous positions
        return !targetToChase.GlobalPosition.IsEqualApprox(_previousTargetPosition);
    }

    private void PlayAnimation()
    {
        var animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        float directionX = Velocity.X;
        float directionY = Velocity.Y;
        bool flip = animatedSprite.FlipH;

        // Only update direction if AI is moving
        if (Velocity.Length() > 0)
        {
            if (directionX > 0)
            {
                animatedSprite.Play("right");
                animatedSprite.FlipH = false;
            }
            else if (directionX < 0)
            {
                animatedSprite.Play("left");
                animatedSprite.FlipH = true;
            }
            else if (directionY != 0 && !flip)
            {
                animatedSprite.Play("right");
            }
            else if (directionY != 0 && flip)
            {
                animatedSprite.Play("left");
            }
        }
        else
        {
            animatedSprite.Play("Idle");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (targetToChase == null)
        {
            GD.Print("No target to chase!");
            return;
        }

        float distanceToTarget = GlobalPosition.DistanceTo(targetToChase.GlobalPosition);


        // Check if target is moving
        if (distanceToTarget <= STOP_DISTANCE)
        {
            if (!IsTargetMoving())
            {
                // Maintain stopping distance if target isn't moving
                Velocity = Vector2.Zero;
            }
        }
        else
        {
            if (_navigationAgent.IsNavigationFinished() && targetToChase.GlobalPosition == _navigationAgent.TargetPosition)
            {
                return;

            }

            // Set new target and update pathfinding
            _navigationAgent.TargetPosition = targetToChase.GlobalPosition;
            Velocity = GlobalPosition.DirectionTo(_navigationAgent.GetNextPathPosition()) * SPEED;
        }

        _previousTargetPosition = targetToChase.GlobalPosition; // Update previous position

        MoveAndSlide();
        PlayAnimation();
    }
}
