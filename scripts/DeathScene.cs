using Godot;
using System;
using System.Text;

public partial class DeathScene : Control
{
    [Export] public Label RoundLabel;
    [Export] public Label ScoreLabel;

    public override void _Ready()
    {
        if (Global.IsBattleRoyaleMode)
        {
            // When you die, alivec represents: remaining enemies + you
            // So your placement = alivec
            int placement = Spawner.alivec;
            RoundLabel.Text = $"You came {placement}{GetOrdinalSuffix(placement)}!";
            ScoreLabel.Text = $"You got {Global.Score} kills!";
        }
        else
        {
            // Survival mode: show rounds and kills
            RoundLabel.Text = $"You defeated {Global.LastRound - 1} rounds!";
            ScoreLabel.Text = $"You got {Global.Score} kills!";
        }
    }

    private string GetOrdinalSuffix(int number)
    {
        int mod100 = number % 100;
        if (mod100 >= 11 && mod100 <= 13) return "th";
        switch (number % 10)
        {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }

    private void _on_quit_pressed()
    {
        GetTree().Quit();
    }

    private void _on_main_menu_pressed()
    {
        GetTree().ChangeSceneToFile("Menu.tscn");
    }

    private void _on_restart_pressed()
    {
        Global.Score = 0;
        if (Global.IsBattleRoyaleMode)
        {
            // Reset the battle royale count before starting new game
            Spawner.ResetBattleRoyale();
            GetTree().ChangeSceneToFile("Battle.tscn");
        }
        else
        {
            GetTree().ChangeSceneToFile("Main.tscn");
        }
    }
}