using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class MenuPrincipal : Node2D
{

	///-------------------------------TRANSICIONES------------------------------------
	private AnimationPlayer anim;
	private AnimationPlayer anim2;
	///-------------------------------------------------------------------------------

	///--------------------------------BOTONES----------------------------------------
	private Button singleplayerButton;
	private Button multiplayerButton;
	private Button optionsButton;
	private Button exitSettings;
	private Button exitButton;
	///--------------------------------------------------------------------------------

	/// --------------------------------LISTAS-----------------------------------------
	/// Aclaración: System.Action es un tipo de dato que guarda una acción del sistema, no todos
	/// los métodos que realiza el sistema se pueden meter en esta lista (algunos son void, ej: AudioServer.ChangeAudioBusDb(), 
	/// es una acción del sistema que cambia el nivel de volumen de un bus de audio, pero es tipo "void" en lugar de "System.Action")
	private List<Button> listaBotones;
	private List<System.Action> listaFunciones;
	///---------------------------------------------------------------------------------

	/// -------------------------------CONFIGURACION------------------------------------
	private TextureRect optionsImage;
	private Panel panel;
	private Panel panelOpciones;
	private AudioBusLayout busLayout;
	private Panel multiplayerPanel;
	///---------------------------------------------------------------------------------



	public override void _Ready()
	{

		optionsImage = GetNode<TextureRect>("Control/SettingsImage");
		singleplayerButton = GetNode<Button>("Control/VBoxContainer/Singleplayer");
		multiplayerButton = GetNode<Button>("Control/VBoxContainer/Multiplayer");
		optionsButton = GetNode<Button>("Control/VBoxContainer/Opciones");
		exitButton = GetNode<Button>("Control/VBoxContainer/Salir");
		exitSettings = GetNode<Button>("Control/SettingsPanel/ExitSettingsButton");
		panel = GetNode<Panel>("Control/SettingsPanel");
		panelOpciones = GetNode<Panel>("Control/SettingsPanel/OpcionesSonido/ScrollContainer/PanelOpciones");
		multiplayerPanel = GetNode<Panel>("Control/MultiPlayerPanel");

		listaBotones = [singleplayerButton, multiplayerButton, optionsButton, exitButton, exitSettings];
		listaFunciones = [StartGame, Multijugador, OpenSettings, ExitGame, ExitSettings];

		for(int i = 0; i < listaBotones.Count; i++)
		{
			listaBotones[i].Pressed += listaFunciones[i];
		}

		PlayerPreferences.ApplyDisplayAndGraphics();
		
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
		multiplayerPanel.Visible = true;
	}

	private void OpenSettings()
	{
		panel.Visible = true;
		optionsImage.Visible = true;

	}

	private void ExitSettings()
	{	
		panel.Visible = false;
		optionsImage.Visible = false;
	}

	private void ActualizarVolumen(AudioBusLayout bus)
	{
		string busName = bus.ResourceName;
		int busId = AudioServer.GetBusIndex(busName);
		float volumen = AudioServer.GetBusVolumeDb(busId);
		AudioServer.SetBusVolumeDb(busId, volumen);
	}

	private void ExitGame()
	{
		GetTree().Quit();
	}
}
