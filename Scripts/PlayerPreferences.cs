using Godot;
using System;
using System.Linq;

/// <summary>Persisted local profile and client options.</summary>
public static class PlayerPreferences
{
    #nullable enable annotations
    private const string Path = "user://red_flag_settings.cfg";
    private static ConfigFile? _config;
    private static ConfigFile Config
    {
        get
        {
            if (_config != null) return _config;
            _config = new ConfigFile();
            _config.Load(Path);
            if (string.IsNullOrWhiteSpace((string)_config.GetValue("profile", "player_name", "")))
                _config.SetValue("profile", "player_name", "");
            if (string.IsNullOrWhiteSpace((string)_config.GetValue("profile", "player_id", "")))
                _config.SetValue("profile", "player_id", Guid.NewGuid().ToString("N")[..8].ToUpperInvariant());
            _config.Save(Path);
            return _config;
        }
    }

    public static string PlayerName => (string)Config.GetValue("profile", "player_name", "");
    public static string PlayerId => (string)Config.GetValue("profile", "player_id", "" );
    public static float MusicVolume => (float)Config.GetValue("audio", "music", 0.8f);
    public static float SfxVolume => (float)Config.GetValue("audio", "sfx", 0.8f);
    public static int WindowWidth => (int)Config.GetValue("display", "width", 1280);
    public static int WindowHeight => (int)Config.GetValue("display", "height", 720);
    public static bool Fullscreen => (bool)Config.GetValue("display", "fullscreen", false);
    public static bool Vsync => (bool)Config.GetValue("graphics", "vsync", true);
    public static bool ColorblindMode => (bool)Config.GetValue("accessibility", "colorblind", false);
    public static bool HudEnabled => (bool)Config.GetValue("accessibility", "hud", true);

    public static void SaveName(string name) => Set("profile", "player_name", (name ?? "").Trim()[..Math.Min((name ?? "").Trim().Length, 24)]);
    public static void SaveAudio(float music, float sfx) { Config.SetValue("audio", "music", Mathf.Clamp(music, 0f, 1f)); Config.SetValue("audio", "sfx", Mathf.Clamp(sfx, 0f, 1f)); Save(); }
    public static void SaveDisplay(int width, int height, bool fullscreen) { Config.SetValue("display", "width", width); Config.SetValue("display", "height", height); Config.SetValue("display", "fullscreen", fullscreen); Save(); }
    public static void SaveGraphics(bool vsync) { Config.SetValue("graphics", "vsync", vsync); Save(); }
    public static void SaveAccessibility(bool colorblind, bool hud) { Config.SetValue("accessibility", "colorblind", colorblind); Config.SetValue("accessibility", "hud", hud); Save(); }
    private static void Set(string section, string key, Variant value) { Config.SetValue(section, key, value); Save(); }
    private static void Save() => Config.Save(Path);

    public static void ApplyDisplayAndGraphics()
    {
        DisplayServer.WindowSetSize(new Vector2I(WindowWidth, WindowHeight));
        DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetVsyncMode(Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        ApplyAudio();
    }

    public static void ApplyAudio()
    {
        int master = AudioServer.GetBusIndex("Master");
        int music = AudioServer.GetBusIndex("Music");
        int sfx = AudioServer.GetBusIndex("SFX");
        if (master >= 0 && music < 0 && sfx < 0) AudioServer.SetBusVolumeDb(master, Mathf.LinearToDb(Mathf.Max(0.001f, (MusicVolume + SfxVolume) * 0.5f)));
        else if (master >= 0) AudioServer.SetBusVolumeDb(master, 0f);
        if (music >= 0) AudioServer.SetBusVolumeDb(music, Mathf.LinearToDb(Mathf.Max(0.001f, MusicVolume)));
        if (sfx >= 0) AudioServer.SetBusVolumeDb(sfx, Mathf.LinearToDb(Mathf.Max(0.001f, SfxVolume)));
    }
}
