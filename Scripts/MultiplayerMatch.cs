using Godot;

public partial class MultiplayerMatch : Control
{
	public override void _Ready()
	{
		var session = GetNode<NetworkSession>("/root/NetworkSession");
		var background = new ColorRect { 
			Color = new Color("101820") //Es un azulado un poco oscuro, apagado
		}; 
		
		background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); 
		AddChild(background);
		var center = new CenterContainer(); center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(center);
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(560, 0) }; center.AddChild(panel);
		var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 16); panel.AddChild(column);
		var title = new Label { Text = "RED FLAG  /  TU ROL", HorizontalAlignment = HorizontalAlignment.Center }; title.AddThemeFontSizeOverride("font_size", 30); column.AddChild(title);
		bool hacker = session.LocalRole.Contains("Hacker");
		var role = new Label { Text = session.LocalRole.ToUpperInvariant(), HorizontalAlignment = HorizontalAlignment.Center }; role.AddThemeFontSizeOverride("font_size", 36); role.AddThemeColorOverride("font_color", PlayerPreferences.ColorblindMode ? (hacker ? new Color("d55e00") : new Color("0072b2")) : (hacker ? new Color("ff6565") : new Color("83e1b2"))); column.AddChild(role);
		column.AddChild(new Label { Text = $"Modo: {session.GameMode}\nDuración: {session.DurationMinutes} minutos\nDificultad: {session.Difficulty}\n\nMemoriza tu rol. Al continuar entrarás a la oficina con los demás jugadores.", HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart });
		var enter = new Button { Text = "Entrar a la oficina" }; enter.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MultiplayerOffice.tscn"); column.AddChild(enter);
		var leave = new Button { Text = "Abandonar partida" }; leave.Pressed += () => { session.LeaveRoom(); GetTree().ChangeSceneToFile("res://Scenes/MultiplayerMenu.tscn"); }; column.AddChild(leave);
	}
}
