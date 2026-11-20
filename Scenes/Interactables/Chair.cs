using Godot;
using System;

public partial class Chair : Node2D
{
	private bool playerNear = false;
	private Player jugador;
	private ConfigPartidaPanel panelConfig;
    private bool listoMultijugador = false;

	public override void _Ready()
	{
		GetNode<Area2D>("Area2D").BodyEntered += OnBodyEntered;
		GetNode<Area2D>("Area2D").BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		if (playerNear && panelConfig == null && Input.IsKeyPressed(Key.E))
		{
			AbrirConfig();
		}
	}

	private void AbrirConfig()
	{
        NetworkSession sesion = GetNodeOrNull<NetworkSession>("/root/NetworkSession");
        if (sesion != null && sesion.IsConnected && !sesion.IsInLobby)
        {
            if (sesion.GameMode == "Salva la red" && !listoMultijugador)
            {
                listoMultijugador = true;
                sesion.MarcarListoCooperativo();
            }
            // En una partida online la silla nunca abre ComputerGameplay individual.
            return;
        }
		if (jugador != null) jugador.CanControl = false;

		panelConfig = new ConfigPartidaPanel();
		panelConfig.AlComenzar = () => GetTree().ChangeSceneToFile("res://Scenes/ComputerGameplay.tscn");
		panelConfig.AlCancelar = () =>
		{
			panelConfig = null;
			if (jugador != null) jugador.CanControl = true;
		};
		AddChild(panelConfig);
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player p)
		{
			jugador = p;
			playerNear = true;
			GetNode<Sprite2D>("InteractPrompt").Visible = true;
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body is Player)
		{
			playerNear = false;
			GetNode<Sprite2D>("InteractPrompt").Visible = false;
		}
	}
}
