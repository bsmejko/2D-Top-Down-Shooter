using Godot;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class PauseMenu : Control
{
    public override void _Ready()
    {
        escPause();
        GetTree().Paused = false;
        ((AnimationPlayer)GetNode("AnimationPlayer")).Play("RESET");
    }


    private void resume()
    {
        GetTree().Paused = false;
        ((AnimationPlayer)GetNode("AnimationPlayer")).PlayBackwards("blur");
    } 
    private void pause()
    {
        GetTree().Paused = true;
        ((AnimationPlayer)GetNode("AnimationPlayer")).Play("blur");
    }
    private void escPause()
    {
        if (Input.IsActionJustPressed("Pause") && GetTree().Paused == false) 
        {
            pause();
        } 
        else if (Input.IsActionJustPressed("Pause") && GetTree().Paused == true) 
        {
        resume();
        }
    }
    private void _on_resume_pressed()
    {
        resume();
    }

    private void _on_restart_pressed()
    {
        GetTree().ReloadCurrentScene();
        Global.Score = 0;
        resume();
    }

    private void _on_main_menu_pressed()
    {
        GetTree().ChangeSceneToFile("Menu.tscn");
    }

    private void _on_quit_pressed()
    {
        GetTree().Quit();
    }

    public override void _PhysicsProcess(double delta)
    {
        escPause();
    }
}
