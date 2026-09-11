using Godot;

public partial class Medkit : Area2D
{
    [Export] public int HealthRestoreAmount = 30; 

    public override void _Ready()
    {
        //Connect the BodyEntered signal to detect when the player enters the medkit's area
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        //Check if the body that entered is the player
        if (body is player player)
        {
            OnPickup(player);
        }
        //Or if it's the Ally
        if (body is AiAlly1 aiAlly1)
        {
            OnPickupAI(aiAlly1);
        }
    }

    public void OnPickup(player player)
    {
        //Restore the player's health
        player.RestoreHealth(HealthRestoreAmount); //Call the function from the player

        //Remove the medkit from the scene
        QueueFree();
        GD.Print("+30 hp"); //make sure its working
    }
    public void OnPickupAI(AiAlly1 aiAlly1)
    {
        aiAlly1.RestoreHealth(HealthRestoreAmount);

        QueueFree();
        GD.Print("AI health + 30hp");
    }

}