using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 180;	

	private AnimatedSprite2D anim;

	public override void _Ready()
	{
		anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}
	
	public override void _PhysicsProcess(double delta)
	{

		Vector2 direccion = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W))
		{
			direccion.Y --;
			anim.Play("walkup");
		}
		if (Input.IsKeyPressed(Key.S))
		{
			direccion.Y ++;
			anim.Play("walkdown");
		}
		if (Input.IsKeyPressed(Key.A))
		{
			direccion.X --;
			anim.Play("walkleft");
		}
		if (Input.IsKeyPressed(Key.D))
		{
			direccion.X ++;
			anim.Play("walkright");
		}
		else { anim.Play("idle"); }

		Position += direccion.Normalized() * Speed * (float) delta;
	}
	

}
