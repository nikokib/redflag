using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
#nullable enable annotations

public partial class MultiplayerMenu : Control
{
	private NetworkSession _session = null!;
	 
	private Node? _officeBackground;
	private PanelContainer _browserPanel = null!;
	private PanelContainer _joinIpPanel = null!;
	private PanelContainer _createPanel = null!;
	private PanelContainer _lobbyPanel = null!;
	private PanelContainer _settingsPanel = null!;
	private LineEdit _search = null!;
	private ItemList _roomList = null!;
	private LineEdit _joinAddress = null!;
	private SpinBox _joinPort = null!;
	private LineEdit _joinPassword = null!;
	private LineEdit _roomName = null!;
	private LineEdit _lobbyRoomName = null!;
	private OptionButton _bindMode = null!;
	private LineEdit _bindIp = null!;
	private SpinBox _port = null!;
	private SpinBox _maxPlayers = null!;
	private SpinBox _duration = null!;
	private OptionButton _difficulty = null!;
	private OptionButton _mode = null!;
	private CheckButton _privateRoom = null!;
	private LineEdit _hostPassword = null!;
	private LineEdit _lobbyPassword = null!;
	private Label _status = null!;
	private Label _lobbyInfo = null!;
	private Label _players = null!;
	private Button _startButton = null!;
	private Button _testButton = null!;
	private Label _playerCountLabel = null!;
	private Label _lobbyMessage = null!;
	private HBoxContainer _passwordRow = null!;
	private LineEdit _passwordPreview = null!;
	private readonly List<NetworkSession.PublicRoomInfo> _shownRooms = new();
	private bool _isEditingLobbySettings;

	public override void _Ready()
	{
		_session = GetNode<NetworkSession>("/root/NetworkSession");
		BuildUi(); _session.Changed += RefreshUi; RefreshUi();
		_session.RefreshPublicRooms();
	}
	public override void _ExitTree() { if (_session != null) _session.Changed -= RefreshUi; }

	private void BuildUi()
	{
		var office = GD.Load<PackedScene>("res://Scenes/Oficina.tscn");
		if (office != null) { _officeBackground = office.Instantiate(); AddChild(_officeBackground); MoveChild(_officeBackground, 0); var idlePlayer = _officeBackground.GetNodeOrNull<Player>("CharacterBody2D"); if (idlePlayer != null) idlePlayer.CanControl = false; }
		// La oficina contiene una Camera2D. La interfaz debe dibujarse
		// en una capa independiente para que la cámara no la desplace.
		var uiLayer = new CanvasLayer { Layer = 10 };
		AddChild(uiLayer);
		var uiRoot = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		uiLayer.AddChild(uiRoot);
		uiRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var shade = new ColorRect { Color = new Color(0.025f, 0.05f, 0.07f, 0.62f), MouseFilter = Control.MouseFilterEnum.Ignore };
		uiRoot.AddChild(shade);
		shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var brand = new VBoxContainer(); brand.AnchorLeft = 0.04f; brand.AnchorTop = 0.08f; brand.AnchorRight = 0.45f; brand.AnchorBottom = 0.40f; uiRoot.AddChild(brand);
		var title = new Label { Text = "RED FLAG", AutowrapMode = TextServer.AutowrapMode.WordSmart }; title.AddThemeFontSizeOverride("font_size", 42); brand.AddChild(title);
		var subtitle = new Label { Text = "MULTIJUGADOR  /  NEXATECH", AutowrapMode = TextServer.AutowrapMode.WordSmart }; subtitle.AddThemeFontSizeOverride("font_size", 19); subtitle.AddThemeColorOverride("font_color", new Color("9ed9e7")); brand.AddChild(subtitle);
		_status = new Label { Text = "Buscando salas…", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _status.AddThemeColorOverride("font_color", new Color("e4edf0")); brand.AddChild(_status);
		var back = new Button { Text = "← Volver al menú" }; back.Pressed += () => { _session.LeaveRoom(); GetTree().ChangeSceneToFile("res://Scenes/MenuPrincipal.tscn"); }; brand.AddChild(back);

		_browserPanel = MakePanel(0.53f, 0.07f, 0.97f, 0.93f); uiRoot.AddChild(_browserPanel);
		BuildBrowser();
		_joinIpPanel = MakePanel(0.54f, 0.18f, 0.96f, 0.82f); uiRoot.AddChild(_joinIpPanel); BuildJoinIp();
		// Formulario centrado, con espacio para todas las opciones.
		_createPanel = MakePanel(0.12f, 0.05f, 0.88f, 0.95f); uiRoot.AddChild(_createPanel); BuildCreateSettings();
		_lobbyPanel = MakePanel(0.54f, 0.07f, 0.97f, 0.93f); uiRoot.AddChild(_lobbyPanel); BuildLobby();
		_settingsPanel = MakePanel(0.12f, 0.05f, 0.88f, 0.95f); uiRoot.AddChild(_settingsPanel); BuildLobbySettings();
		ShowMode("browser");
	}

	private void BuildBrowser()
	{
		var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 10); _browserPanel.AddChild(box);
		AddTitle(box, "Navegador de salas públicas");
		_search = new LineEdit { PlaceholderText = "Buscar por IP, nombre, código de jugador o sala…", SizeFlagsHorizontal = SizeFlags.ExpandFill }; box.AddChild(_search);
		_search.TextChanged += _ => RefreshRoomResults();
		var refresh = new Button { Text = "↻ Actualizar salas" }; refresh.Pressed += () => _session.RefreshPublicRooms(_search.Text); box.AddChild(refresh);
		_roomList = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 230), SelectMode = ItemList.SelectModeEnum.Single }; box.AddChild(_roomList);
		_roomList.ItemSelected += _ => { if (_roomList.GetSelectedItems().Length > 0) { var room = _shownRooms[_roomList.GetSelectedItems()[0]]; _joinAddress.Text = room.Address; _joinPort.Value = room.Port; } };
		_lobbyPassword = AddLine(box, "Contraseña de la sala (si corresponde)", "");
		_lobbyPassword.Secret = true;
		_lobbyPassword.PlaceholderText = "Sólo para salas con contraseña";
		var revealBrowser = new CheckButton { Text = "Mostrar contraseña" };
		revealBrowser.Toggled += visible => _lobbyPassword.Secret = !visible;
		box.AddChild(revealBrowser);
		var actions = new HBoxContainer(); box.AddChild(actions);
		var join = new Button { Text = "Unirse a sala", SizeFlagsHorizontal = SizeFlags.ExpandFill }; join.Pressed += JoinSelectedRoom; actions.AddChild(join);
		var joinIp = new Button { Text = "Unirse a sala por IP", SizeFlagsHorizontal = SizeFlags.ExpandFill }; joinIp.Pressed += () => ShowMode("joinip"); actions.AddChild(joinIp);
		var create = new Button { Text = "Crear sala", SizeFlagsHorizontal = SizeFlags.ExpandFill }; create.Pressed += () => { PrepareCreateForm(); ShowMode("create"); }; actions.AddChild(create);
		box.AddChild(new Label { Text = "El navegador detecta salas públicas de la red local. Para Internet, usa la IP pública del anfitrión o un directorio online cuando esté desplegado.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
	}

	private void BuildJoinIp()
	{
		var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 10); _joinIpPanel.AddChild(box);
		AddTitle(box, "Unirse a sala por IP");
		_joinAddress = AddLine(box, "IP IPv4 del anfitrión", "127.0.0.1");
		_joinPort = AddSpin(box, "Puerto UDP", NetworkSession.DefaultPort, 1024, 65535, 1);
		_joinPassword = AddLine(box, "Contraseña (si corresponde)", "");
		_joinPassword.Secret = true;
		var revealJoin = new CheckButton { Text = "Mostrar contraseña" };
		revealJoin.Toggled += visible => _joinPassword.Secret = !visible;
		box.AddChild(revealJoin);
		var join = new Button { Text = "Conectar" }; join.Pressed += JoinByIp; box.AddChild(join);
		var back = new Button { Text = "Volver al navegador" }; back.Pressed += () => ShowMode("browser"); box.AddChild(back);
	}

	private void BuildCreateSettings()
	{
		// El formulario usa una zona desplazable; los botones quedan fijos abajo.
		var layout = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		layout.AddThemeConstantOverride("separation", 10);
		_createPanel.AddChild(layout);
		AddTitle(layout, "Crear sala · Configuración inicial");

		var scroll = new ScrollContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		layout.AddChild(scroll);
		var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		box.AddThemeConstantOverride("separation", 8);
		scroll.AddChild(box);

		_roomName = AddLine(box, "Nombre de la sala", "Sala de Red Flag");
		_bindMode = AddOption(box, "IP de escucha", new[] { "Automática (todas las interfaces)", "Personalizada" }, 0);
		_bindIp = AddLine(box, "IP local personalizada", "127.0.0.1"); _bindIp.Editable = false;
		_bindMode.ItemSelected += idx => _bindIp.Editable = idx == 1;
		_port = AddSpin(box, "Puerto UDP", NetworkSession.DefaultPort, 1024, 65535, 1);
		_maxPlayers = AddSpin(box, "Capacidad (4–15 jugadores)", 8, 4, 15, 1);
		_duration = AddSpin(box, "Duración (minutos)", 10, 1, 60, 1);
		_difficulty = AddOption(box, "Dificultad", new[] { "Fácil", "Normal", "Difícil" }, 1);
		// El modo Salva la red es el cooperativo que estamos probando.
		_mode = AddOption(box, "Modo de juego", new[] { "Salva la red", "Impostor", "Destruye la red", "Impostor reverso" }, 0);
		_privateRoom = new CheckButton { Text = "Sala privada (requiere contraseña)", ButtonPressed = false };
		box.AddChild(_privateRoom);
		_hostPassword = AddLine(box, "Contraseña de la sala", "");
		_hostPassword.MaxLength = 24;
		_hostPassword.Secret = true;
		_hostPassword.Editable = true; // Siempre deja escribir; activa "Sala privada" automáticamente.
		_hostPassword.PlaceholderText = "Escribí una clave o dejala vacía para generarla";
		_hostPassword.TextChanged += value =>
		{
			if (!string.IsNullOrWhiteSpace(value)) _privateRoom.ButtonPressed = true;
		};
		var showCreatePassword = new CheckButton { Text = "Mostrar contraseña" };
		showCreatePassword.Toggled += visible => _hostPassword.Secret = !visible;
		box.AddChild(showCreatePassword);
		box.AddChild(new Label
		{
			Text = "Si elegís IP automática, el servidor escucha en todas las interfaces. El modo cooperativo disponible es Salva la red.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		});

		var actions = new HBoxContainer();
		actions.AddThemeConstantOverride("separation", 10);
		layout.AddChild(actions);
		var create = new Button { Text = "CREAR SALA", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		create.Pressed += CreateRoom; actions.AddChild(create);
		var back = new Button { Text = "CANCELAR", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		back.Pressed += () => ShowMode("browser"); actions.AddChild(back);
	}

	private void BuildLobby()
	{
		// Título y botones siempre visibles; los datos de la sala se desplazan.
		var layout = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		layout.AddThemeConstantOverride("separation", 8);
		_lobbyPanel.AddChild(layout);

		var heading = new Label
		{
			Text = "OFICINA · SALA DE ESPERA",
			AutowrapMode = TextServer.AutowrapMode.Off,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		heading.AddThemeFontSizeOverride("font_size", 21);
		heading.AddThemeColorOverride("font_color", new Color("d8f4ff"));
		layout.AddChild(heading);

		var gear = new Button { Text = "⚙ Configuración de sala", SizeFlagsHorizontal = SizeFlags.ShrinkEnd };
		gear.Pressed += () => { SyncSettingsFromSession(); ShowMode("settings"); };
		layout.AddChild(gear);

		var scroll = new ScrollContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		layout.AddChild(scroll);
		var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 9);
		scroll.AddChild(body);

		_lobbyInfo = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		body.AddChild(_lobbyInfo);
		_players = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		body.AddChild(_players);

		// La contraseña sólo la ve el anfitrión de una sala privada.
		_passwordRow = new HBoxContainer();
		_passwordRow.AddChild(new Label { Text = "Clave:" });
		_passwordPreview = new LineEdit
		{
			Secret = true,
			Editable = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_passwordRow.AddChild(_passwordPreview);
		var reveal = new CheckButton { Text = "Ver" };
		reveal.Toggled += visible => _passwordPreview.Secret = !visible;
		_passwordRow.AddChild(reveal);
		body.AddChild(_passwordRow);

		_playerCountLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_playerCountLabel.AddThemeFontSizeOverride("font_size", 18);
		layout.AddChild(_playerCountLabel);

		_startButton = new Button { Text = "INICIAR PARTIDA (mín. 4)" };
		_startButton.Pressed += () => _session.StartMatch();
		layout.AddChild(_startButton);

		_testButton = new Button { Text = "PROBAR COOPERATIVO (mín. 2)" };
		_testButton.Pressed += () => _session.StartMatch(true);
		layout.AddChild(_testButton);

		_lobbyMessage = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_lobbyMessage.AddThemeColorOverride("font_color", new Color("c9d8e3"));
		layout.AddChild(_lobbyMessage);

		var leave = new Button { Text = "Salir de la sala" };
		leave.Pressed += () =>
		{
			_session.LeaveRoom();
			ShowMode("browser");
			_session.RefreshPublicRooms(_search.Text);
		};
		layout.AddChild(leave);
	}

	private void BuildLobbySettings()
	{
		var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; _settingsPanel.AddChild(scroll);
		var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; box.AddThemeConstantOverride("separation", 8); scroll.AddChild(box);
		AddTitle(box, "⚙ Configuración de sala");
		_lobbyRoomName = AddLine(box, "Nombre de la sala", "Sala de Red Flag");
		var note = new Label { Text = "Solo el anfitrión puede cambiar estos valores.", AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(note);
		// Reuse controls already created for initial setup; Godot nodes cannot have two parents, so settings are edited in-place through this panel's proxy controls.
		var max = AddSpin(box, "Capacidad (4–15)", 8, 4, 15, 1); max.Name = "LobbyMaxPlayers";
		var duration = AddSpin(box, "Duración (minutos)", 10, 1, 60, 1); duration.Name = "LobbyDuration";
		var diff = AddOption(box, "Dificultad", new[] { "Fácil", "Normal", "Difícil" }, 1); diff.Name = "LobbyDifficulty";
		var mode = AddOption(box, "Modo de juego", new[] { "Salva la red", "Impostor", "Destruye la red", "Impostor reverso" }, 1); mode.Name = "LobbyMode";
		var priv = new CheckButton { Text = "Sala privada", Name = "LobbyPrivate" }; box.AddChild(priv);
		var pass = AddLine(box, "Contraseña (vacío = generar)", "");
		pass.Name = "LobbyPassword";
		pass.Secret = true;
		pass.MaxLength = 24;
		pass.PlaceholderText = "Escribí una clave o dejala vacía para generarla";
		pass.TextChanged += value =>
		{
			if (!string.IsNullOrWhiteSpace(value) && _session.IsHost)
				priv.ButtonPressed = true;
		};
		var revealLobbyPassword = new CheckButton { Text = "Mostrar contraseña" };
		revealLobbyPassword.Toggled += visible => pass.Secret = !visible;
		box.AddChild(revealLobbyPassword);
		var apply = new Button { Text = "Guardar configuración" }; apply.Pressed += () =>
		{
			if (!_session.IsHost) return;
			string password = pass.Text.Trim(); if (priv.ButtonPressed && string.IsNullOrEmpty(password)) password = GeneratePassword(); pass.Text = password;
			_session.UpdateRoomSettings(_lobbyRoomName.Text, (int)max.Value, (int)duration.Value, diff.GetItemText(diff.Selected), mode.GetItemText(mode.Selected), priv.ButtonPressed, password);
			ShowMode("lobby");
		}; box.AddChild(apply);
		var back = new Button { Text = "Cancelar" }; back.Pressed += () => ShowMode("lobby"); box.AddChild(back);
	}

	private void CreateRoom()
	{
		if (string.IsNullOrWhiteSpace(PlayerPreferences.PlayerName)) { SetStatus("Antes de jugar online, configura tu nombre en Configuración → Personalización."); return; }
		string bind = "0.0.0.0";
		if (_bindMode.Selected == 1)
		{
			if (!IPAddress.TryParse(_bindIp.Text.Trim(), out IPAddress? ip) || ip.AddressFamily != AddressFamily.InterNetwork) { SetStatus("La IP personalizada no es válida. Usa una dirección IPv4."); return; }
			string[] local = IP.GetLocalAddresses();
			if (!local.Contains(ip.ToString())) { SetStatus("Esa IP no está asignada a este equipo. Usa una IP de una de sus interfaces o selecciona Automática."); return; }
			bind = ip.ToString();
		}
		string password = _hostPassword.Text.Trim(); if (_privateRoom.ButtonPressed && string.IsNullOrEmpty(password)) password = GeneratePassword(); _hostPassword.Text = password;
		Error result = _session.CreateRoom(PlayerPreferences.PlayerName, PlayerPreferences.PlayerId, _roomName.Text, (int)_port.Value,
			(int)_maxPlayers.Value, (int)_duration.Value, _difficulty.GetItemText(_difficulty.Selected), _mode.GetItemText(_mode.Selected), _privateRoom.ButtonPressed, password, bind);
		if (result == Error.Ok) { SyncSettingsFromSession(); ShowMode("lobby"); }
	}

	private void JoinSelectedRoom()
	{
		if (string.IsNullOrWhiteSpace(PlayerPreferences.PlayerName)) { SetStatus("Antes de jugar online, configura tu nombre en Configuración → Personalización."); return; }
		int[] selected = _roomList.GetSelectedItems(); if (selected.Length == 0 || selected[0] >= _shownRooms.Count) { SetStatus("Selecciona una sala pública de la lista."); return; }
		var room = _shownRooms[selected[0]];
		_session.JoinRoom(PlayerPreferences.PlayerName, PlayerPreferences.PlayerId, room.Address, room.Port, _lobbyPassword.Text.Trim());
	}
	private void JoinByIp()
	{
		if (string.IsNullOrWhiteSpace(PlayerPreferences.PlayerName)) { SetStatus("Antes de jugar online, configura tu nombre en Configuración → Personalización."); return; }
		if (!IPAddress.TryParse(_joinAddress.Text.Trim(), out IPAddress? ip) || ip.AddressFamily != AddressFamily.InterNetwork) { SetStatus("La IP no es válida. Introduce una dirección IPv4."); return; }
		_session.JoinRoom(PlayerPreferences.PlayerName, PlayerPreferences.PlayerId, ip.ToString(), (int)_joinPort.Value, _joinPassword.Text.Trim());
	}

	private void SyncSettingsFromSession()
	{
		if (_settingsPanel == null) return;
		var max = _settingsPanel.FindChild("LobbyMaxPlayers", true, false) as SpinBox;
		var duration = _settingsPanel.FindChild("LobbyDuration", true, false) as SpinBox;
		var diff = _settingsPanel.FindChild("LobbyDifficulty", true, false) as OptionButton;
		var mode = _settingsPanel.FindChild("LobbyMode", true, false) as OptionButton;
		var priv = _settingsPanel.FindChild("LobbyPrivate", true, false) as CheckButton;
		var pass = _settingsPanel.FindChild("LobbyPassword", true, false) as LineEdit;
		if (_lobbyRoomName != null) _lobbyRoomName.Text = _session.RoomName;
		if (max != null) max.Value = _session.MaxPlayers; if (duration != null) duration.Value = _session.DurationMinutes;
		if (diff != null) SelectOption(diff, _session.Difficulty); if (mode != null) SelectOption(mode, _session.GameMode);
		if (priv != null) priv.ButtonPressed = _session.IsPrivate;
		if (pass != null) { pass.Text = _session.PrivatePassword; pass.Editable = _session.IsHost; }
	}

	private void RefreshUi()
	{
		if (!IsInsideTree()) return;
		if (_session.IsInLobby)
		{
			if (_settingsPanel == null || !_settingsPanel.Visible) ShowMode("lobby");
			_lobbyInfo.Text = $"Sala: {_session.RoomName}\n{(_session.IsPrivate ? "Privada" : "Pública / descubrimiento LAN")} · {_session.GameMode}\nDuración: {_session.DurationMinutes} min · Dificultad: {_session.Difficulty}\nJugadores: {_session.ConnectedPlayerCount}/{_session.MaxPlayers} · Puerto UDP: {_session.Port}\nTu código: {PlayerPreferences.PlayerId}";
			_players.Text = "JUGADORES CONECTADOS\n" + string.Join("\n", _session.Players.Select(p => $"• {p.Value}  [{_session.PlayerCodes.GetValueOrDefault(p.Key, "--------")}]" + (p.Key == 1 ? "  (ANFITRIÓN)" : "")));

			int conectados = _session.ConnectedPlayerCount;
			int capacidad = _session.MaxPlayers;
			Color color = conectados <= 3 ? new Color("ff6b6b")
				: conectados <= 5 ? new Color("ffd166") : new Color("6ee7a7");
			_playerCountLabel.AddThemeColorOverride("font_color", color);
			bool cooperativo = _session.GameMode == "Salva la red";
			string detalle = conectados < 4
				? cooperativo
					? conectados < 2
						? "Falta 1 para probar. Mínimo normal: 4."
						: $"Prueba de 2 disponible · Faltan {4 - conectados} para normal."
					: $"Faltan {4 - conectados} para partida normal."
				: conectados <= 5 ? "Mínimo normal alcanzado." : "¡Sala lista!";
			_playerCountLabel.Text = $"JUGADORES: {conectados}/{capacidad} · {detalle}";
			_startButton.Disabled = !_session.IsHost;
			_testButton.Visible = _session.GameMode == "Salva la red";
			_testButton.Disabled = !_session.IsHost;
			_passwordRow.Visible = _session.IsHost && _session.IsPrivate;
			_passwordPreview.Text = _session.PrivatePassword;
			_lobbyMessage.Text = _session.Status;
			_status.Text = _session.Status;
		}
		else if (_session.IsConnected) { _status.Text = _session.Status; }
		else { _status.Text = _session.Status; RefreshRoomResults(); }
	}

	private void RefreshRoomResults()
	{
		if (_roomList == null || _session == null) return;
		string query = _search.Text.Trim(); _shownRooms.Clear(); _roomList.Clear();
		foreach (var room in _session.PublicRooms)
		{
			string haystack = $"{room.Name} {room.HostName} {room.HostCode} {room.PlayerSummary} {room.Address} {room.Port} {room.GameMode}";
			if (!string.IsNullOrEmpty(query) && !haystack.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
			_shownRooms.Add(room); _roomList.AddItem($"{room.Name}\nHost: {room.HostName} [{room.HostCode}]  ·  {room.Address}:{room.Port}\n{room.PlayerCount}/{room.MaxPlayers} jugadores  ·  {room.GameMode}  ·  {room.DurationMinutes} min");
		}
		if (_shownRooms.Count == 0) _roomList.AddItem("No se encontraron salas públicas. Pulsa Actualizar salas.");
	}

	private void ShowMode(string mode)
	{
		if (_browserPanel == null) return;
		_browserPanel.Visible = mode == "browser"; _joinIpPanel.Visible = mode == "joinip"; _createPanel.Visible = mode == "create";
		_lobbyPanel.Visible = mode == "lobby"; _settingsPanel.Visible = mode == "settings";
		if (mode == "settings") SyncSettingsFromSession();
	}
	private void PrepareCreateForm()
	{
		if (string.IsNullOrWhiteSpace(PlayerPreferences.PlayerName)) _status.Text = "Recuerda configurar tu nombre en Configuración → Personalización antes de crear la sala.";
		_bindMode.Select(0); _bindIp.Text = "127.0.0.1"; _port.Value = NetworkSession.DefaultPort; _privateRoom.ButtonPressed = false; _hostPassword.Text = "";
	}
	private void SetStatus(string message) { if (_status != null) _status.Text = message; }
	private static PanelContainer MakePanel(float left, float top, float right, float bottom)
	{
		var panel = new PanelContainer { AnchorLeft = left, AnchorTop = top, AnchorRight = right, AnchorBottom = bottom, OffsetLeft = 0, OffsetTop = 0, OffsetRight = 0, OffsetBottom = 0 };
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("111f28", 0.96f), BorderColor = new Color("4b7787"), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10, ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 16, ContentMarginBottom = 16 });
		return panel;
	}
	private static void AddTitle(Control parent, string text) { var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart }; l.AddThemeFontSizeOverride("font_size", 23); l.AddThemeColorOverride("font_color", new Color("d8f4ff")); parent.AddChild(l); }
	private static LineEdit AddLine(Container parent, string label, string value) { parent.AddChild(new Label { Text = label }); var input = new LineEdit { Text = value, SizeFlagsHorizontal = SizeFlags.ExpandFill }; parent.AddChild(input); return input; }
	private static SpinBox AddSpin(Container parent, string label, double value, double min, double max, double step) { parent.AddChild(new Label { Text = label }); var input = new SpinBox { Value = value, MinValue = min, MaxValue = max, Step = step, SizeFlagsHorizontal = SizeFlags.ExpandFill }; parent.AddChild(input); return input; }
	private static OptionButton AddOption(Container parent, string label, string[] values, int selected) { parent.AddChild(new Label { Text = label }); var input = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill }; foreach (string value in values) input.AddItem(value); input.Select(selected); parent.AddChild(input); return input; }
	private static void SelectOption(OptionButton option, string value) { for (int i = 0; i < option.ItemCount; i++) if (option.GetItemText(i) == value) { option.Select(i); return; } }
	private static string GeneratePassword() { const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; var rng = new Random(); return new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray()); }
}
