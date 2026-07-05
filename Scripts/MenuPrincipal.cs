using Godot;
using System;

public partial class MenuPrincipal : Node2D
{
	public override void _Ready()
	{
		GetNode<Button>("Control/VBoxContainer/Singleplayer").Pressed += StartGame;
		GetNode<Button>("Control/VBoxContainer/Multiplayer").Pressed += Multiplayer;
		GetNode<Button>("Control/VBoxContainer/Salir").Pressed += ExitGame;
	}

	private void StartGame()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Oficina.tscn");
	}

	private void Multiplayer()
	{
		GD.Print("Próximamente...");
	}

	private void ExitGame()
	{
		GetTree().Quit();
	}
}
