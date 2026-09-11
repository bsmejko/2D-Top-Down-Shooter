using Godot;
using System.Threading.Tasks;

public partial class Shield : Area2D
{
    [Export] public float BoostDuration = 20f;
    [Export] public Texture2D PowerUpIcon;

    private TextureRect _activeIcon;
    private static TextureRect _currentIcon; // Track the active UI icon

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is player player) OnPickup(player);
        else if (body is AiAlly1 aiAlly1) OnPickupAI(aiAlly1);
    }

    public async void OnPickup(player player)
    {
        // Remove previous icon if exists
        if (_currentIcon != null && _currentIcon.IsInsideTree())
            _currentIcon.QueueFree();

        player.ActivateShield(BoostDuration);

        // Create and show new icon
        ShowPowerUpIcon();
        _currentIcon = _activeIcon;

        QueueFree();

        // Wait for duration
        await Task.Delay((int)(BoostDuration * 1000));

        // Only remove if this icon is still active
        if (_currentIcon == _activeIcon)
        {
            RemovePowerUpIcon();
            _currentIcon = null;
        }
    }

    private void ShowPowerUpIcon()
    {
        if (PowerUpIcon == null) return;

        var iconContainer = GetNode<HBoxContainer>("/root/Game/CanvasLayer/PowerUpIcons");
        if (iconContainer == null)
        {
            GD.PrintErr("PowerUpIcons container not found");
            return;
        }

        _activeIcon = new TextureRect();
        _activeIcon.Texture = PowerUpIcon;
        _activeIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _activeIcon.CustomMinimumSize = new Vector2(40, 40);

        iconContainer.AddChild(_activeIcon);
    }

    private void RemovePowerUpIcon()
    {
        if (_activeIcon != null && _activeIcon.IsInsideTree())
        {
            _activeIcon.QueueFree();
            _activeIcon = null;
        }
    }

    public void OnPickupAI(AiAlly1 aiAlly1)
    {
        aiAlly1.ActivateShield(BoostDuration);
        QueueFree();
    }
}