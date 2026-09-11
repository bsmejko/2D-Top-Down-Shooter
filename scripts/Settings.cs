using Godot;
using System;

public partial class Settings : Control
{
	private void _on_back_pressed()
    {
		GetTree().ChangeSceneToFile("Menu.tscn");
    }
	
	private void _on_screen_size_pressed() 
	{
        if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
    }
}
