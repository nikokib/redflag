using Godot;
using System;

public partial class Chair : Node2D
{

	private bool playerNear = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<Area2D>("Area2D").BodyEntered += OnBodyEntered;
		GetNode<Area2D>("Area2D").BodyExited += OnBodyExited;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(playerNear && Input.IsKeyPressed(Key.E))
		{
			GD.Print("AAAAAAAAAAAAAAAAA");
		}
	}

	private void OnBodyEntered(Node body)
	{
		if(body is Player)
		{
			playerNear = true;
			GetNode<Sprite2D>("InteractPrompt").Visible = true;
		}
	}

	private void OnBodyExited(Node body)
	{
		if(body is Player)
		{
			playerNear = false;
			GetNode<Sprite2D>("InteractPrompt").Visible = false;
		}
	}
}
