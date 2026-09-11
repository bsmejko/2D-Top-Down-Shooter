using Godot;
using System;

public partial class GameMode : Control
{
    private void _on_survival_pressed()
    {
        Global.Score = 0;
        GetTree().ChangeSceneToFile("Main.tscn");
    }

    private void _on_battle_pressed()
    {
        Global.IsBattleRoyaleMode = true;
        GetTree().ChangeSceneToFile("Battle.tscn");
    }

    private void _on_back_pressed()
    {
        GetTree().ChangeSceneToFile("Menu.tscn");
    }
}


