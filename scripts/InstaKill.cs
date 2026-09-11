using Godot;

public partial class InstaKill : Area2D
{
    // Reference to the player
    private player _player;

    public override void _Ready()
    {
        // Connect the BodyEntered signal to detect when the player enters the InstaKill's area
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        // Check if the body that entered is the player
        if (body is player player)
        {
            OnPickup(player);
        }
        if (body is AiAlly1 aiAlly1)
        {
            OnPickupAI(aiAlly1);
        }
    }

    public void OnPickup(player player)
    {
        if (player != null)
        {
            player.TakeDamage(20);
            GD.Print("InstaKill applied to player!");
            // Optionally, remove the InstaKill from the scene after pickup
            QueueFree();
        }
    }
    public void OnPickupAI(AiAlly1 aiAlly1)
    {
        if (aiAlly1 != null)
        {
            aiAlly1.TakeDamage(10);
            GD.Print("InstaKill applied to player!");


            // Optionally, remove the InstaKill from the scene after pickup
            QueueFree();
        }
    }
}