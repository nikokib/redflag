using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>Host-authoritative ENet session. Public room discovery currently targets the local network.</summary>

public partial class NetworkSession : Node
{   
    #nullable enable annotations
    /// Usamos esta línea para permitir poner el "?" al lado de las clases de las variables.
    /// Este símbolo ("?") sirve para perimtir que una variable sea nula. 
    public const int DefaultPort = 24567;
    public const int DiscoveryPort = 24568;
    public event Action? Changed;
    // Puente entre el servidor y la pantalla cooperativa.
    public event Action<string>? EstadoCooperativoRecibido;
    public event Action<long, string, string>? AccionCooperativaRecibida;
    private readonly HashSet<long> _jugadoresListos = new();
    private bool _cooperativoComenzado;


    public sealed class PublicRoomInfo
    {
        public string Name = "Sala";
        public string HostName = "Jugador";
        public string HostCode = "";
        public string Address = "127.0.0.1";
        public string PlayerSummary = "";
        public int Port = DefaultPort;
        public int PlayerCount = 1;
        public int MaxPlayers = 8;
        public int DurationMinutes = 10;
        public string Difficulty = "Normal";
        public string GameMode = "Impostor";
        public bool IsPrivate;
        public string Key => $"{Address}:{Port}:{HostCode}";
        public override string ToString() => $"{Name} · {HostName} · {Address}:{Port} · {PlayerCount}/{MaxPlayers} · {HostCode}";
    }

    new public bool IsConnected { get; private set; } 
    /// Usamos "new" al inicio porque Godot.Object ya tiene una propiedad llamada "IsConnected".
    public bool IsHost { get; private set; }
    public bool IsInLobby { get; private set; }
    public bool IsPrivate { get; private set; }
    public string RoomName { get; private set; } = "Sala de Red Flag";
    public string LocalPlayerName { get; private set; } = "Jugador";
    public string LocalPlayerId { get; private set; } = "";
    public string PrivatePassword { get; private set; } = "";
    public string Difficulty { get; private set; } = "Normal";
    public string GameMode { get; private set; } = "Impostor";
    public string BindAddress { get; private set; } = "0.0.0.0";
    public int Port { get; private set; } = DefaultPort;
    public int MaxPlayers { get; private set; } = 8;
    public int DurationMinutes { get; private set; } = 10;
    public string Status { get; private set; } = "Desconectado";
    public string LocalRole { get; private set; } = "Sin asignar";
    public int ConnectedPlayerCount => _players.Count;
    public IReadOnlyDictionary<long, string> Players => _players;
    public IReadOnlyDictionary<long, string> PlayerCodes => _playerCodes;
    public IReadOnlyList<PublicRoomInfo> PublicRooms => _publicRooms.Values.ToList();

    private ENetMultiplayerPeer? _peer;
    private PacketPeerUdp? _discoverySocket;
    private PacketPeerUdp? _browserSocket;
    private readonly SortedDictionary<long, string> _players = new();
    private readonly SortedDictionary<long, string> _playerCodes = new();
    private readonly Dictionary<string, PublicRoomInfo> _publicRooms = new();
    private readonly Dictionary<long, Vector2> _networkPositions = new();
    public IReadOnlyDictionary<long, Vector2> NetworkPositions => _networkPositions;
    private bool _joinAccepted;
    private string _pendingPassword = "";
    private double _browserTimeLeft;
    private string _browserQuery = "";

    // Callables persistentes: permiten conectar y desconectar sin generar errores
    // al entrar o salir de una sala repetidas veces.
    private Callable _alConectarJugador;
    private Callable _alDesconectarJugador;
    private Callable _alConectarServidor;
    private Callable _alFallarConexion;
    private Callable _alDesconectarServidor;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _alConectarJugador = Callable.From<long>(OnPeerConnected);
        _alDesconectarJugador = Callable.From<long>(OnPeerDisconnected);
        _alConectarServidor = Callable.From(OnConnectedToServer);
        _alFallarConexion = Callable.From(OnConnectionFailed);
        _alDesconectarServidor = Callable.From(OnServerDisconnected);
    }

    private void ConectarSenal(StringName nombre, Callable accion)
    {
        if (!Multiplayer.IsConnected(nombre, accion)) Multiplayer.Connect(nombre, accion);
    }

    private void DesconectarSenal(StringName nombre, Callable accion)
    {
        if (Multiplayer.IsConnected(nombre, accion)) Multiplayer.Disconnect(nombre, accion);
    }

    public override void _Process(double delta)
    {
        PollDiscovery();
        PollBrowser();
        if (_browserSocket != null)
        {
            _browserTimeLeft -= delta;
            if (_browserTimeLeft <= 0) { _browserSocket.Close(); _browserSocket = null; EmitChanged(); }
        }
    }

    public Error CreateRoom(string playerName, string playerId, string roomName, int requestedPort, int maxPlayers,
        int durationMinutes, string difficulty, string gameMode, bool isPrivate, string password, string bindAddress)
    {
        LeaveRoom(false);
        LocalPlayerName = CleanName(playerName, "Anfitrión");
        LocalPlayerId = CleanCode(playerId);
        RoomName = CleanName(roomName, "Sala de Red Flag");
        MaxPlayers = Math.Clamp(maxPlayers, 4, 15);
        DurationMinutes = Math.Clamp(durationMinutes, 1, 60);
        Difficulty = difficulty; GameMode = gameMode; IsPrivate = isPrivate;
        PrivatePassword = isPrivate ? password : "";
        BindAddress = string.IsNullOrWhiteSpace(bindAddress) ? "0.0.0.0" : bindAddress.Trim();
        LocalRole = "Sin asignar";

        Error error = Error.InvalidParameter;
        int startPort = Math.Clamp(requestedPort, 1024, 65535);
        for (int candidate = startPort; candidate <= 65535; candidate++)
        {
            var candidatePeer = new ENetMultiplayerPeer();
            candidatePeer.SetBindIP(BindAddress);
            error = candidatePeer.CreateServer(candidate, MaxPlayers - 1);
            if (error == Error.Ok) { _peer = candidatePeer; Port = candidate; break; }
            candidatePeer.Close();
        }
        if (_peer == null)
        {
            SetStatus($"No se encontró un puerto disponible desde {startPort} hasta 65535 en {BindAddress}. Error: {error}");
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        IsConnected = true; IsHost = true; IsInLobby = true; _joinAccepted = true;
        _players.Clear(); _playerCodes.Clear(); _networkPositions.Clear(); _players[1] = LocalPlayerName; _playerCodes[1] = LocalPlayerId; _networkPositions[1] = new Vector2(520, 340);
        ConectarSenal(MultiplayerApi.SignalName.PeerConnected, _alConectarJugador);
        ConectarSenal(MultiplayerApi.SignalName.PeerDisconnected, _alDesconectarJugador);
        StartDiscoveryListener();
        SetStatus($"Sala creada en el puerto UDP {Port}. Comparte la IP de tu equipo para conexiones fuera de la red local.");
        BroadcastRoomInfo(); EmitChanged(); return Error.Ok;
    }

    public Error JoinRoom(string playerName, string playerId, string address, int port, string password)
    {
        LeaveRoom(false);
        LocalPlayerName = CleanName(playerName, "Jugador"); LocalPlayerId = CleanCode(playerId);
        if (!IPAddress.TryParse(address.Trim(), out IPAddress? parsed) || parsed.AddressFamily != AddressFamily.InterNetwork)
        { SetStatus("La IP no es válida. Introduce una dirección IPv4, por ejemplo 192.168.1.20."); return Error.InvalidParameter; }
        Port = Math.Clamp(port, 1024, 65535); _joinAccepted = false; _players.Clear(); _playerCodes.Clear();
        _peer = new ENetMultiplayerPeer(); Error error = _peer.CreateClient(parsed.ToString(), Port);
        if (error != Error.Ok) { _peer = null; SetStatus($"No se pudo conectar: {error}"); return error; }
        Multiplayer.MultiplayerPeer = _peer; IsConnected = false; IsHost = false; IsInLobby = false;
        ConectarSenal(MultiplayerApi.SignalName.ConnectedToServer, _alConectarServidor);
        ConectarSenal(MultiplayerApi.SignalName.ConnectionFailed, _alFallarConexion);
        ConectarSenal(MultiplayerApi.SignalName.ServerDisconnected, _alDesconectarServidor);
        _pendingPassword = password; SetStatus($"Conectando a {parsed}:{Port}…"); EmitChanged(); return Error.Ok;
    }

    public void UpdateRoomSettings(string roomName, int maxPlayers, int durationMinutes, string difficulty, string gameMode, bool isPrivate, string password)
    {
        if (!IsHost || !IsInLobby) return;
        RoomName = CleanName(roomName, "Sala de Red Flag"); MaxPlayers = Math.Clamp(maxPlayers, 4, 15);
        DurationMinutes = Math.Clamp(durationMinutes, 1, 60); Difficulty = difficulty; GameMode = gameMode;
        IsPrivate = isPrivate; PrivatePassword = isPrivate ? password : "";
        BroadcastRoomInfo(); EmitChanged();
    }

    public void RefreshPublicRooms(string query = "")
    {
        _browserQuery = (query ?? "").Trim(); _publicRooms.Clear();
        _browserSocket?.Close(); _browserSocket = new PacketPeerUdp();
        Error bind = _browserSocket.Bind(0);
        if (bind != Error.Ok) { _browserSocket = null; SetStatus("No se pudo abrir el socket para buscar salas."); EmitChanged(); return; }
        _browserSocket.SetBroadcastEnabled(true);
        _browserSocket.SetDestAddress("255.255.255.255", DiscoveryPort);
        _browserSocket.PutPacket(Encoding.UTF8.GetBytes("DISCOVER|" + Guid.NewGuid().ToString("N")));
        // Also query loopback, useful for local multi-instance tests.
        _browserSocket.SetDestAddress("127.0.0.1", DiscoveryPort);
        _browserSocket.PutPacket(Encoding.UTF8.GetBytes("DISCOVER|LOCAL"));
        _browserTimeLeft = 1.5; SetStatus("Buscando salas públicas de la red local…"); EmitChanged();
    }

    public void StartMatch(bool modoPrueba = false)
    {
        if (!IsHost || !IsInLobby) return;
        if (modoPrueba && GameMode != "Salva la red")
        {
            SetStatus("La prueba con 2 jugadores sólo está disponible en Salva la red.");
            return;
        }
        int minimo = modoPrueba ? 2 : 4;
        if (_players.Count < minimo)
        {
            int faltan = minimo - _players.Count;
            SetStatus($"Faltan {faltan} jugador(es) para {(modoPrueba ? "la prueba cooperativa" : "la partida normal")}. Conectados: {_players.Count}/{MaxPlayers}.");
            return;
        }
        var ids = _players.Keys.ToList(); var roles = new Dictionary<long, string>();
        foreach (long id in ids) roles[id] = "Hacker";
        var rng = new Random();
        switch (GameMode)
        {
            case "Salva la red": foreach (long id in ids) roles[id] = "Administrador"; break;
            case "Impostor": foreach (long id in ids) roles[id] = "Administrador"; roles[ids[rng.Next(ids.Count)]] = "Hacker"; break;
            case "Destruye la red": break;
            case "Impostor reverso": roles[ids[rng.Next(ids.Count)]] = "Administrador infiltrado"; break;
            default: foreach (long id in ids) roles[id] = "Administrador"; break;
        }
        _jugadoresListos.Clear(); _cooperativoComenzado = false;
        IsInLobby = false; SetStatus("Partida iniciada."); StopDiscoveryListener();
        string roster = string.Join("\n", _players.Select(p => $"{p.Key}|{SafeField(p.Value)}|{SafeField(_playerCodes.GetValueOrDefault(p.Key, ""))}"));
        foreach (long id in ids)
        {
            string role = roles[id];
            if (id == 1) EnterMatch(GameMode, DurationMinutes, Difficulty, role, roster);
            else RpcId(id, nameof(ReceiveMatchStart), GameMode, DurationMinutes, Difficulty, role, roster);
        }
        EmitChanged();
    }

    public void LeaveRoom(bool updateStatus = true)
    {
        StopDiscoveryListener(); _browserSocket?.Close(); _browserSocket = null;
        DesconectarSenal(MultiplayerApi.SignalName.PeerConnected, _alConectarJugador);
        DesconectarSenal(MultiplayerApi.SignalName.PeerDisconnected, _alDesconectarJugador);
        DesconectarSenal(MultiplayerApi.SignalName.ConnectedToServer, _alConectarServidor);
        DesconectarSenal(MultiplayerApi.SignalName.ConnectionFailed, _alFallarConexion);
        DesconectarSenal(MultiplayerApi.SignalName.ServerDisconnected, _alDesconectarServidor);
        if (_peer != null) { _peer.Close(); _peer = null; }
        Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer(); IsConnected = false; IsHost = false; IsInLobby = false; IsPrivate = false; _joinAccepted = false;
        _players.Clear(); _playerCodes.Clear(); _networkPositions.Clear(); LocalRole = "Sin asignar";
        _jugadoresListos.Clear(); _cooperativoComenzado = false;
        if (updateStatus) SetStatus("Desconectado"); EmitChanged();
    }

    private void StartDiscoveryListener()
    {
        _discoverySocket?.Close(); _discoverySocket = new PacketPeerUdp();
        Error err = _discoverySocket.Bind(DiscoveryPort);
        if (err != Error.Ok) { _discoverySocket.Close(); _discoverySocket = null; SetStatus(Status + " (aviso: no se pudo abrir el puerto de descubrimiento LAN)"); }
        else _discoverySocket.SetBroadcastEnabled(true);
    }
    private void StopDiscoveryListener() { _discoverySocket?.Close(); _discoverySocket = null; }

    private void PollDiscovery()
    {
        if (_discoverySocket == null) return;
        while (_discoverySocket.GetAvailablePacketCount() > 0)
        {
            string packet = Encoding.UTF8.GetString(_discoverySocket.GetPacket());
            string sender = _discoverySocket.GetPacketIP(); int senderPort = _discoverySocket.GetPacketPort();
            if (packet.StartsWith("DISCOVER|", StringComparison.Ordinal) && IsHost && IsInLobby && !IsPrivate)
            {
                string response = string.Join("|", "ROOM", Port, SafeField(RoomName), SafeField(LocalPlayerName), SafeField(LocalPlayerId), _players.Count, MaxPlayers, DurationMinutes, SafeField(Difficulty), SafeField(GameMode), "0", GetAdvertisedIp(), SafeField(string.Join(",", _players.Values.Concat(_playerCodes.Values))));
                _discoverySocket.SetDestAddress(sender, senderPort); _discoverySocket.PutPacket(Encoding.UTF8.GetBytes(response));
            }
        }
    }

    private void PollBrowser()
    {
        if (_browserSocket == null) return;
        while (_browserSocket.GetAvailablePacketCount() > 0)
        {
            string packet = Encoding.UTF8.GetString(_browserSocket.GetPacket());
            if (!packet.StartsWith("ROOM|", StringComparison.Ordinal)) continue;
            string[] p = packet.Split('|'); if (p.Length < 12) continue;
            if (!int.TryParse(p[1], out int port) || !int.TryParse(p[5], out int count) || !int.TryParse(p[6], out int cap) || !int.TryParse(p[7], out int duration)) continue;
            var room = new PublicRoomInfo { Port = port, Name = p[2], HostName = p[3], HostCode = p[4], PlayerCount = count, MaxPlayers = cap, DurationMinutes = duration, Difficulty = p[8], GameMode = p[9], IsPrivate = p[10] == "1", Address = p[11], PlayerSummary = p.Length > 12 ? p[12] : p[3] };
            if (room.IsPrivate) continue;
            string haystack = $"{room.Name} {room.HostName} {room.HostCode} {room.PlayerSummary} {room.Address} {room.Port} {room.GameMode}";
            if (!string.IsNullOrWhiteSpace(_browserQuery) && !haystack.Contains(_browserQuery, StringComparison.OrdinalIgnoreCase)) continue;
            _publicRooms[room.Key] = room;
        }
    }

    private string GetAdvertisedIp()
    {
        if (BindAddress != "0.0.0.0" && BindAddress != "*") return BindAddress;
        return IP.GetLocalAddresses().FirstOrDefault(a => IPAddress.TryParse(a, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) ?? "127.0.0.1";
    }

    private void OnPeerConnected(long id) { if (IsHost) SetStatus($"Jugador {id} conectándose…"); }
    private void OnPeerDisconnected(long id)
    {
        if (!IsHost) return;

        _jugadoresListos.Remove(id);
        bool wasRegistered = _players.Remove(id);
        _playerCodes.Remove(id);
        _networkPositions.Remove(id);

        // Los intentos rechazados nunca se agregaron a la lista; no deben
        // anunciarse como si un jugador de la sala se hubiera desconectado.
        if (!wasRegistered) return;

        BroadcastPlayerList();
        BroadcastRoomInfo();
        SetStatus($"El jugador {id} se desconectó.");
        EmitChanged();
    }
    private void OnConnectedToServer() { IsConnected = true; SetStatus("Conectado. Validando acceso a la sala…"); RpcId(1, nameof(RegisterPlayer), _pendingPassword, LocalPlayerName, LocalPlayerId); EmitChanged(); }
    private void OnConnectionFailed() { LeaveRoom(false); SetStatus("No se pudo entrar. Comprueba IP, puerto y conexión; si la sala es privada, verifica la contraseña."); EmitChanged(); }
    private void OnServerDisconnected() { LeaveRoom(false); SetStatus("Se perdió la conexión con el anfitrión."); EmitChanged(); }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RegisterPlayer(string password, string playerName, string playerId)
    {
        if (!IsHost) return;
        long senderId = Multiplayer.GetRemoteSenderId();
        if (senderId <= 1) return;

        if (!IsInLobby)
        {
            RpcId(senderId, nameof(JoinRejected), "La partida ya comenzó.");
            return;
        }

        if (IsPrivate && password != PrivatePassword)
        {
            RpcId(senderId, nameof(JoinRejected), "La contraseña es incorrecta.");
            return;
        }

        // El identificador persistente identifica al perfil, no al nombre visible.
        // No aceptamos IDs vacíos: CleanCode generaría uno nuevo y permitiría eludir
        // la comprobación de duplicados.
        if (string.IsNullOrWhiteSpace(playerId))
        {
            RpcId(senderId, nameof(JoinRejected), "El perfil no tiene un identificador válido. Reinicia el juego para generar uno nuevo.");
            return;
        }

        string normalizedPlayerId = CleanCode(playerId);

        // Una retransmisión del registro desde el mismo peer no debe contar como
        // un segundo ingreso ni alterar el jugador que ya fue registrado.
        if (_players.ContainsKey(senderId))
        {
            if (_playerCodes.TryGetValue(senderId, out string? registeredId) &&
                string.Equals(registeredId, normalizedPlayerId, StringComparison.OrdinalIgnoreCase))
            {
                RpcId(senderId, nameof(JoinAccepted), RoomName);
                BroadcastPlayerList();
            }
            else
            {
                RpcId(senderId, nameof(JoinRejected), "Esta conexión ya está asociada a otro perfil.");
            }
            return;
        }

        // Un mismo perfil no puede ocupar dos lugares de la misma sala,
        // aunque intente conectarse desde otra instancia o dispositivo.
        bool alreadyConnected = _playerCodes.Any(entry =>
            entry.Key != senderId &&
            string.Equals(entry.Value, normalizedPlayerId, StringComparison.OrdinalIgnoreCase));
        if (alreadyConnected)
        {
            RpcId(senderId, nameof(JoinRejected), "Este perfil ya está conectado a la sala. Desconecta la otra sesión antes de volver a entrar.");
            return;
        }

        if (_players.Count >= MaxPlayers)
        {
            RpcId(senderId, nameof(JoinRejected), "La sala está llena. Elige otra sala o espera a que se libere un lugar.");
            return;
        }

        _players[senderId] = CleanName(playerName, $"Jugador {senderId}");
        _playerCodes[senderId] = normalizedPlayerId;
        _networkPositions[senderId] = new Vector2(520 + (_players.Count * 36), 340);

        RpcId(senderId, nameof(JoinAccepted), RoomName);
        BroadcastPlayerList();
        BroadcastRoomInfo();
        SetStatus($"{_players[senderId]} se unió a la sala.");
        EmitChanged();
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void JoinAccepted(string roomName) { _joinAccepted = true; RoomName = roomName; IsInLobby = true; SetStatus($"Te uniste a «{roomName}»."); EmitChanged(); }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void JoinRejected(string message) { LeaveRoom(false); SetStatus(message); EmitChanged(); }

    private void BroadcastPlayerList()
    {
        string serialized = string.Join("\n", _players.Select(p => $"{p.Key}|{SafeField(p.Value)}|{SafeField(_playerCodes.GetValueOrDefault(p.Key, ""))}"));
        Rpc(nameof(ReceivePlayerList), serialized);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ReceivePlayerList(string serialized)
    {
        if (IsHost) return; _players.Clear(); _playerCodes.Clear();
        foreach (string line in serialized.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Split('|', 3);
            if (parts.Length >= 2 && long.TryParse(parts[0], out long id)) { _players[id] = parts[1]; _playerCodes[id] = parts.Length > 2 ? parts[2] : ""; }
        }
        EmitChanged();
    }
    private void BroadcastRoomInfo() => Rpc(nameof(ReceiveRoomInfo), RoomName, IsPrivate, MaxPlayers, DurationMinutes, Difficulty, GameMode, _players.Count);
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ReceiveRoomInfo(string roomName, bool isPrivate, int maxPlayers, int durationMinutes, string difficulty, string gameMode, int playerCount)
    { if (IsHost) return; RoomName = roomName; IsPrivate = isPrivate; MaxPlayers = maxPlayers; DurationMinutes = durationMinutes; Difficulty = difficulty; GameMode = gameMode; EmitChanged(); }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ReceiveMatchStart(string gameMode, int durationMinutes, string difficulty, string role, string roster) => EnterMatch(gameMode, durationMinutes, difficulty, role, roster);
    private void EnterMatch(string gameMode, int durationMinutes, string difficulty, string role, string roster)
    {
        _players.Clear(); _playerCodes.Clear(); _networkPositions.Clear();
        foreach (string line in roster.Split('\n', StringSplitOptions.RemoveEmptyEntries)) { string[] parts = line.Split('|', 3); if (parts.Length >= 2 && long.TryParse(parts[0], out long id)) { _players[id] = parts[1]; _playerCodes[id] = parts.Length > 2 ? parts[2] : ""; _networkPositions[id] = new Vector2(500 + ((_networkPositions.Count % 5) - 2) * 48, 320 + ((_networkPositions.Count / 5) * 45)); } }
        GameMode = gameMode; DurationMinutes = durationMinutes; Difficulty = difficulty; LocalRole = role; IsInLobby = false;
        MatchRoster = roster; EmitChanged(); GetTree().ChangeSceneToFile("res://Scenes/MultiplayerMatch.tscn");
    }
    public string MatchRoster { get; private set; } = "";
    public void SendLocalPlayerPosition(Vector2 position)
    {
        if (!IsConnected || IsInLobby) return;
        if (IsHost) { _networkPositions[1] = position; Rpc(nameof(ReceivePlayerPosition), 1L, position); }
        else RpcId(1, nameof(SubmitPlayerPosition), position);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SubmitPlayerPosition(Vector2 position)
    {
        if (!IsHost) return; long sender = Multiplayer.GetRemoteSenderId(); if (!_players.ContainsKey(sender)) return;
        _networkPositions[sender] = position; Rpc(nameof(ReceivePlayerPosition), sender, position);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void ReceivePlayerPosition(long peerId, Vector2 position) { _networkPositions[peerId] = position; }

    // El anfitrión comienza el escritorio SOLO cuando todos confirmaron que están listos.
    public void MarcarListoCooperativo()
    {
        if (!IsConnected || IsInLobby || GameMode != "Salva la red") return;
        if (IsHost) { _jugadoresListos.Add(1); ComprobarListos(); }
        else RpcId(1, nameof(RecibirListoCooperativo));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RecibirListoCooperativo()
    {
        if (!IsHost || IsInLobby || GameMode != "Salva la red") return;
        long id = Multiplayer.GetRemoteSenderId();
        if (!_players.ContainsKey(id)) return;
        _jugadoresListos.Add(id);
        ComprobarListos();
    }

    private void ComprobarListos()
    {
        if (_cooperativoComenzado || _players.Count < 2) return;
        if (!_players.Keys.All(id => _jugadoresListos.Contains(id))) return;
        _cooperativoComenzado = true;
        Rpc(nameof(AbrirConsolaCooperativa));
        AbrirConsolaCooperativa();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void AbrirConsolaCooperativa()
    {
        if (GameMode != "Salva la red" || !IsConnected) return;
        GetTree().ChangeSceneToFile("res://Scenes/ComputerGameplay.tscn");
    }

    public void EnviarEstadoCooperativo(string json)
    {
        if (!IsConnected || !IsHost || GameMode != "Salva la red") return;
        Rpc(nameof(RecibirEstadoCooperativo), json);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RecibirEstadoCooperativo(string json)
    {
        if (IsHost || !IsConnected || GameMode != "Salva la red") return;
        EstadoCooperativoRecibido?.Invoke(json);
    }

    // El cliente envía una acción; el anfitrión la comprueba contra la solicitud/correo actual.
    public void EnviarAccionCooperativa(string tipo, string valor)
    {
        if (!IsConnected || IsHost || GameMode != "Salva la red") return;
        RpcId(1, nameof(RecibirAccionCooperativa), tipo, valor);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RecibirAccionCooperativa(string tipo, string valor)
    {
        if (!IsHost || IsInLobby || GameMode != "Salva la red") return;
        long id = Multiplayer.GetRemoteSenderId();
        if (!_players.ContainsKey(id)) return;
        AccionCooperativaRecibida?.Invoke(id, tipo, valor);
    }

    private void SetStatus(string status) { Status = status; EmitChanged(); }
    private void EmitChanged() => Changed?.Invoke();
    private static string SafeField(string value) => (value ?? "").Replace("|", " ").Replace("\n", " ").Replace("\r", " ");
    private static string CleanName(string value, string fallback) { string cleaned = SafeField(value).Trim(); return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned[..Math.Min(cleaned.Length, 24)]; }
    private static string CleanCode(string value) { string cleaned = SafeField(value).Trim().ToUpperInvariant(); return string.IsNullOrWhiteSpace(cleaned) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : cleaned[..Math.Min(cleaned.Length, 8)]; }
}
