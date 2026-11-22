using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Uses the existing Oficina scene and adds network avatars/name tags around it.</summary>
public partial class MultiplayerOffice : Node2D
{
    private NetworkSession _session = null!;
    private Node2D _office = null!;
    private Player _localPlayer = null!;
    private readonly System.Collections.Generic.Dictionary<long, Player> _avatars = new();
    private double _sendTimer;

    public override void _Ready()
    {
        _session = GetNode<NetworkSession>("/root/NetworkSession");
        var officeScene = GD.Load<PackedScene>("res://Scenes/Oficina.tscn");
        _office = officeScene.Instantiate<Node2D>(); AddChild(_office);
        _localPlayer = _office.GetNode<Player>("CharacterBody2D");
        _localPlayer.CanControl = true;
        long localId = Multiplayer.GetUniqueId();
        if (_session.NetworkPositions.TryGetValue(localId, out Vector2 localPosition)) _localPlayer.Position = localPosition;
        AddNameTag(_localPlayer, _session.Players.GetValueOrDefault(localId, _session.LocalPlayerName), true);
        _avatars[localId] = _localPlayer;

        var playerScene = GD.Load<PackedScene>("res://Scenes/Player.tscn");
        foreach (var entry in _session.Players)
        {
            if (entry.Key == localId) continue;
            var avatar = playerScene.Instantiate<Player>(); avatar.CanControl = false;
            avatar.Position = _session.NetworkPositions.GetValueOrDefault(entry.Key, new Vector2(500 + _avatars.Count * 45, 330));
            _office.AddChild(avatar); AddNameTag(avatar, entry.Value, false); _avatars[entry.Key] = avatar;
        }
        BuildHud();
    }

    public override void _Process(double delta)
    {
        _sendTimer += delta;
        if (_sendTimer >= 0.1 && _localPlayer != null)
        {
            _sendTimer = 0; _session.SendLocalPlayerPosition(_localPlayer.Position);
        }
        foreach (var pair in _avatars)
        {
            if (pair.Key == Multiplayer.GetUniqueId()) continue;
            if (_session.NetworkPositions.TryGetValue(pair.Key, out Vector2 pos)) pair.Value.Position = pair.Value.Position.Lerp(pos, 0.35f);
        }
    }

    private void BuildHud()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var root = new Control(); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); layer.AddChild(root);
        // La navegación debe estar disponible aunque se oculte el HUD.
        root.Visible = true;
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        var info = new PanelContainer { AnchorLeft = 0.02f, AnchorTop = 0.02f, AnchorRight = 0.39f, AnchorBottom = 0.20f };
        root.AddChild(info); info.Visible = PlayerPreferences.HudEnabled;
        var column = new VBoxContainer(); info.AddChild(column);
        column.AddChild(new Label { Text = $"RED FLAG · {_session.GameMode}" });
        column.AddChild(new Label { Text = $"Tu rol: {_session.LocalRole} · {_session.DurationMinutes} min · {_session.Difficulty}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var list = new PanelContainer { AnchorLeft = 0.73f, AnchorTop = 0.02f, AnchorRight = 0.98f, AnchorBottom = 0.32f };
        root.AddChild(list);
        list.Visible = PlayerPreferences.HudEnabled;
        var roster = new VBoxContainer(); list.AddChild(roster); roster.AddChild(new Label { Text = "EN LA OFICINA" });
        foreach (var player in _session.Players) roster.AddChild(new Label { Text = $"• {player.Value} [{_session.PlayerCodes.GetValueOrDefault(player.Key, "--------")}]" });
        var leave = new Button
        {
            Text = "← VOLVER AL MENÚ",
            AnchorLeft = 0.02f, AnchorTop = 0.90f,
            AnchorRight = 0.24f, AnchorBottom = 0.98f
        };
        leave.Pressed += VolverAlMenu;
        root.AddChild(leave);
        if (_session.GameMode == "Salva la red")
        {
            var listo = new Button
            {
                Text = "LISTO · ABRIR RED FLAG",
                AnchorLeft = 0.23f, AnchorTop = 0.92f,
                AnchorRight = 0.50f, AnchorBottom = 0.98f
            };
            listo.Pressed += () =>
            {
                listo.Disabled = true;
                listo.Text = "ESPERANDO AL EQUIPO...";
                _session.MarcarListoCooperativo();
            };
            root.AddChild(listo);
        }
    }

    // Salir siempre funciona desde el botón o desde Escape.
    private void VolverAlMenu()
    {
        _session.LeaveRoom();
        GetTree().ChangeSceneToFile("res://Scenes/MenuPrincipal.tscn");
    }

    public override void _UnhandledKeyInput(InputEvent evento)
    {
        if (evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo && tecla.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            VolverAlMenu();
        }
    }

    private static void AddNameTag(Player player, string name, bool local)
    {
        var label = new Label { Text = local ? $"{name} (TÚ)" : name, Position = new Vector2(-45, -32), Size = new Vector2(140, 24), HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 12);
        Color normal = PlayerPreferences.ColorblindMode ? new Color("ffe066") : new Color("ffffff");
        Color own = PlayerPreferences.ColorblindMode ? new Color("56b4e9") : new Color("a8f0ce");
        label.AddThemeColorOverride("font_color", local ? own : normal);
        player.AddChild(label);
    }
}
