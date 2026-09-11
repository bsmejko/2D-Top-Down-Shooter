using Godot;
using System;

public partial class GameUi : Control
{

	int score = 0;

	private Score _score;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		score++;
		_score.Text =  score.ToString();
	}


}
