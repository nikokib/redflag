using Godot;
using System;

public partial class AudioControl : Node
{
	private string busName;
	private AudioBusLayout busLayout;
	private int modificador;
	private int busId;

	private float volumen;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		busLayout = GetNode<AudioBusLayout>("Control/SettingsPanel/OpcionesSonido/ScrollContainer/PanelOpciones/AudioControl");
		busName = busLayout.ResourceName;
		busId = AudioServer.GetBusIndex(busName);
		volumen = AudioServer.GetBusVolumeDb(busId);

		AudioServer.SetBusVolumeDb(busId, volumen);

	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

}
