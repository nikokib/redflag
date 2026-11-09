using Godot;
using System;

public partial class SettingsMenu : Control
{
    private LineEdit _playerName = null!;
    private Label _playerId = null!;
    private HSlider _music = null!;
    private HSlider _sfx = null!;
    private OptionButton _resolution = null!;
    private CheckButton _fullscreen = null!;
    private CheckButton _vsync = null!;
    private CheckButton _colorblind = null!;
    private CheckButton _hud = null!;
    private Label _status = null!;
    private VBoxContainer _content = null!;
    private string _tab = "Pantalla";

    public override void _Ready() { BuildUi(); PlayerPreferences.ApplyDisplayAndGraphics(); }

    private void BuildUi()
    {
        var bg = new ColorRect { Color = new Color("101820") }; bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(bg);
        var margin = new MarginContainer(); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); margin.AddThemeConstantOverride("margin_left", 28); margin.AddThemeConstantOverride("margin_right", 28); margin.AddThemeConstantOverride("margin_top", 24); margin.AddThemeConstantOverride("margin_bottom", 24); AddChild(margin);
        var root = new VBoxContainer(); root.AddThemeConstantOverride("separation", 12); margin.AddChild(root);
        root.AddChild(new Label { Text = "RED FLAG  /  CONFIGURACIÓN", HorizontalAlignment = HorizontalAlignment.Center });
        var tabs = new HBoxContainer(); root.AddChild(tabs);
        foreach (string tab in new[] { "Pantalla", "Sonido", "Gráficos", "Personalización" }) { var b = new Button { Text = tab, SizeFlagsHorizontal = SizeFlags.ExpandFill }; b.Pressed += () => ShowTab(tab); tabs.AddChild(b); }
        _content = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; _content.AddThemeConstantOverride("separation", 12); root.AddChild(_content);
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; root.AddChild(_status);
        var bottom = new HBoxContainer(); root.AddChild(bottom);
        var save = new Button { Text = "Guardar cambios" }; save.Pressed += SaveSettings; bottom.AddChild(save);
        var back = new Button { Text = "Volver" }; back.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MenuPrincipal.tscn"); bottom.AddChild(back);
        ShowTab(_tab);
    }

    private void ShowTab(string tab)
    {
        _tab = tab; foreach (Node child in _content.GetChildren()) child.QueueFree();
        switch (tab)
        {
            case "Pantalla":
                _resolution = new OptionButton(); _resolution.AddItem("1280 × 720"); _resolution.AddItem("1600 × 900"); _resolution.AddItem("1920 × 1080");
                int idx = PlayerPreferences.WindowWidth >= 1900 ? 2 : PlayerPreferences.WindowWidth >= 1500 ? 1 : 0; _resolution.Select(idx);
                AddLabeled(_content, "Resolución", _resolution);
                _fullscreen = new CheckButton { Text = "Pantalla completa", ButtonPressed = PlayerPreferences.Fullscreen }; _content.AddChild(_fullscreen);
                _content.AddChild(new Label { Text = "La resolución se aplica al guardar." });
                break;
            case "Sonido":
                _music = AddSlider(_content, "Volumen de música", PlayerPreferences.MusicVolume);
                _sfx = AddSlider(_content, "Volumen de efectos (SFX)", PlayerPreferences.SfxVolume);
                _content.AddChild(new Label { Text = "Los niveles se guardan por separado. Los efectos futuros podrán asignarse al bus SFX." });
                break;
            case "Gráficos":
                _vsync = new CheckButton { Text = "Sincronización vertical (VSync)", ButtonPressed = PlayerPreferences.Vsync }; _content.AddChild(_vsync);
                _colorblind = new CheckButton { Text = "Paleta accesible para daltonismo", ButtonPressed = PlayerPreferences.ColorblindMode }; _content.AddChild(_colorblind);
                _hud = new CheckButton { Text = "Mostrar HUD durante la partida", ButtonPressed = PlayerPreferences.HudEnabled }; _content.AddChild(_hud);
                _content.AddChild(new HSeparator());
                _content.AddChild(new Label { Text = "Controles actuales" });
                _content.AddChild(new Label { Text = "Mover: W A S D   ·   Correr: Shift   ·   Interactuar: según la escena" });
                _content.AddChild(new Label { Text = "VSync reduce el tearing. El cambio de paleta y HUD se aplica en las escenas compatibles." });
                break;
            case "Personalización":
                _content.AddChild(new Label { Text = "Nombre visible en el juego" });
                _playerName = new LineEdit { PlaceholderText = "Escribe tu nombre (obligatorio para jugar online)", Text = PlayerPreferences.PlayerName, MaxLength = 24 }; _content.AddChild(_playerName);
                _playerId = new Label { Text = $"Identificador único: {PlayerPreferences.PlayerId}" }; _content.AddChild(_playerId);
                _content.AddChild(new HSeparator());
                _content.AddChild(new Label { Text = "Apariencia" });
                foreach (string option in new[] { "Peinados", "Trajes", "Ojos", "Piel" })
                {
                    var row = new HBoxContainer(); row.AddChild(new Label { Text = option, SizeFlagsHorizontal = SizeFlags.ExpandFill });
                    var placeholder = new Label { Text = "Próximamente", HorizontalAlignment = HorizontalAlignment.Right }; placeholder.AddThemeColorOverride("font_color", new Color("8a9aa4")); row.AddChild(placeholder); _content.AddChild(row);
                }
                break;
        }
    }

    private void SaveSettings()
    {
        if (_tab == "Personalización")
        {
            string name = _playerName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { _status.Text = "Debes configurar un nombre antes de jugar online."; return; }
            PlayerPreferences.SaveName(name);
        }
        if (_tab == "Sonido") PlayerPreferences.SaveAudio((float)_music.Value, (float)_sfx.Value);
        if (_tab == "Pantalla")
        {
            int idx = _resolution.Selected; int[] widths = { 1280, 1600, 1920 }; int[] heights = { 720, 900, 1080 };
            PlayerPreferences.SaveDisplay(widths[idx], heights[idx], _fullscreen.ButtonPressed);
        }
        if (_tab == "Gráficos") { PlayerPreferences.SaveGraphics(_vsync.ButtonPressed); PlayerPreferences.SaveAccessibility(_colorblind.ButtonPressed, _hud.ButtonPressed); }
        // Persist all currently visible tab values; settings are also loaded from disk on next launch.
        PlayerPreferences.ApplyDisplayAndGraphics();
        _status.Text = "Configuración guardada.";
    }

    private static HSlider AddSlider(VBoxContainer parent, string label, float value)
    {
        parent.AddChild(new Label { Text = $"{label}: {Math.Round(value * 100)}%" });
        var slider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = value, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        slider.ValueChanged += v => ((Label)parent.GetChild(parent.GetChildCount() - 2)).Text = $"{label}: {Math.Round(v * 100)}%";
        parent.AddChild(slider); return slider;
    }
    private static void AddLabeled(VBoxContainer parent, string label, Control control) { parent.AddChild(new Label { Text = label }); control.SizeFlagsHorizontal = SizeFlags.ExpandFill; parent.AddChild(control); }
}
