using Godot;
using System;

public partial class MenuPrincipal : Node2D
{
	private AnimationPlayer anim;
	private AnimationPlayer anim2;

	public override void _Ready()
	{
		GetNode<Button>("Control/VBoxContainer/Singleplayer").Pressed += StartGame;
		GetNode<Button>("Control/VBoxContainer/Multiplayer").Pressed += Multijugador;
		GetNode<Button>("Control/VBoxContainer/Salir").Pressed += ExitGame;
		
		anim = GetNode<AnimationPlayer>("AnimationPlayer");
		anim2 = GetNode<AnimationPlayer>("AnimationPlayer2");

		anim.AnimationFinished += OnAnimationFinished;
		anim2.AnimationFinished += OnAnimationFinished;
		
		anim.Play("Animacion principal");
		anim2.Play("FadeIn");
	}

	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "Animacion principal")
		{
			anim.Play("Animacion backloop");
		}
		else if (animName == "Animacion backloop")
		{
			anim.Play("Animacion principal");
		}
		
		if (animName == "FadeIn") {
			anim2.Play("FadeInTitle");
		}
		
	}


	private void StartGame()
	{
		GetTree().ChangeSceneToFile("Scenes/Oficina.tscn");
	}

	private void Multijugador()
	{
		GD.Print("Próximamente...");
	}

	private void ExitGame()
	{
		GetTree().Quit();
	}
}
