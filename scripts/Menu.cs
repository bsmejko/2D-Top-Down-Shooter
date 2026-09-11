using Godot;
using System;

public partial class Menu : Control
{
    private void _on_play_pressed()
    {
        Global.Score = 0;
        GetTree().ChangeSceneToFile("game_mode.tscn");
    } 

    private void _on_options_pressed() 
    {
        GetTree().ChangeSceneToFile("Settings.tscn");
    }

    private void _on_quit_pressed()
    {
        GetTree().Quit();
    }
}
