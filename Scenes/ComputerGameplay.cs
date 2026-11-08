using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

public partial class ComputerGameplay : Control
{
	// Interfaz (se arma por código: ver EscritorioUI y NotificacionConexion)
	private EscritorioUI escritorio;
	private NotificacionConexion notif;
	private RedFlagApp redFlagApp;
	private DatosEscritorio datos;

	private Label lblTiempo;
	private Label lblHora;
	private Label lblFecha;
	private Label lblPuntos;
	private Label lblConexiones;

	private Random rnd = new Random();
	private Conexion conexionActual;

	private class Conexion
	{
		public string Nombre { get; set; }
		public string Departamento { get; set; }
		public string IP { get; set; }
		public string MAC { get; set; }
		public string Rol { get; set; }
		public bool Infectada { get; set; }
	}

	// --- Valores que se cargan desde PartidaConfig en _Ready ---
	private double tiempoPartida;      // cuenta regresiva (Turno de guardia)
	private double segundosAuto;       // segundos por solicitud antes de autoaceptar
	private double probInfectada;      // 0.0 a 1.0
	private int maxErroresAceptar;
	private int maxErroresRechazar;
	private int maxAutoAceptadas;

	private double autoAceptar;
	private double tiempoJugado = 0;

	// Datos inferiores
	private int puntos = 0;
	private int conexionesHoy = 0;

	// Detectar final del juego
	private int erroresAceptar = 0;
	private int erroresRechazar = 0;
	private int autoAceptadas = 0;
	private int virusRechazados = 0;   // decisiones correctas (solo para el resumen final)
	private int validasAceptadas = 0;

	private bool juegoTerminado = false;

	// Capa roja que avisa de impostores aceptados (solo visual)
	private AlertaRoja alertaRoja;

	// Modo Infinito: cada tantas conexiones sube la dificultad
	private const int CONEXIONES_POR_NIVEL = 10;

	// Listas de datos
	private string[] nombres =
	{
		"Ana Lopez", "Juan Perez", "Maria Gomez", "Lucas Diaz", "Sofia Torres",
		"Pedro Ruiz", "Lucia Fernandez", "Martin Gomez", "Camila Alvarez", "Agustin Romero"
	};

	private string[] departamentos =
	{
		"Finanzas", "RRHH", "Marketing", "IT", "Ventas", "Soporte", "Seguridad", "Administracion"
	};

	private string[] roles =
	{
		"Analista", "Gerente", "Desarrollador", "Contador", "Pasante", "Supervisor", "Diseñador"
	};

	private string[] nombresInfectados =
	{
		"VOID", "null_user", "adm1n", "syst3m", "ERROR404"
	};

	private string[] macsInfectadas =
	{
		"00:00:00:00:00:00", "FF:FF:FF:FF:FF:FF", "DE:AD:BE:EF:66:66", "66:66:66:66:66:66", "XX:XX:XX:XX:XX:XX"
	};

	private string[] ipsInfectadas =
	{
		"10.66.6.66", "192.168.0.131313", "192.168.1.666", "0.0.0.0", "255.255.255.255"
	};

	public override void _Ready()
	{
		datos = new DatosEscritorio();

		escritorio = new EscritorioUI { Datos = datos };
		AddChild(escritorio);
		lblTiempo = escritorio.LblTiempo;
		lblHora = escritorio.LblHora;
		lblFecha = escritorio.LblFecha;
		lblPuntos = escritorio.LblPuntos;
		lblConexiones = escritorio.LblConexiones;

		// Ventana grande "REDS FLAGS OS": se abre centrada desde el ícono Red Flags,
		// el botón de la barra de tareas o tocando la notificación emergente
		redFlagApp = new RedFlagApp { Datos = datos };
		AddChild(redFlagApp);
		redFlagApp.Aceptado += OnAceptarPressed;
		redFlagApp.Rechazado += OnRechazarPressed;
		redFlagApp.Oculta += () => notif.AppAbierta(false);
		escritorio.AlAbrirRedFlag = AbrirRedFlag;

		// La solicitud de conexión llega como notificación sobre el escritorio
		notif = new NotificacionConexion();
		AddChild(notif);
		notif.Aceptado += OnAceptarPressed;
		notif.Rechazado += OnRechazarPressed;
		notif.Clic += AbrirRedFlag;

		// Botón "RED FLAG" en la barra de tareas: abre / esconde la ventana de Red Flag
		notif.ConectarBotonTarea(escritorio.AgregarBotonTarea("RED FLAG", AlternarRedFlag));

		alertaRoja = new AlertaRoja();
		AddChild(alertaRoja);

		AplicarConfig();
		InicializarPantalla();
	}

	private void AplicarConfig()
	{
		tiempoPartida = PartidaConfig.DuracionSegundos;
		segundosAuto = PartidaConfig.SegundosAutoAceptar;
		probInfectada = PartidaConfig.ProbInfectada;
		maxErroresAceptar = PartidaConfig.MaxErroresAceptar;
		maxErroresRechazar = PartidaConfig.MaxErroresRechazar;
		maxAutoAceptadas = PartidaConfig.MaxAutoAceptadas;
		autoAceptar = segundosAuto;

		datos.MaxErroresAceptar = maxErroresAceptar;
		datos.MaxErroresRechazar = maxErroresRechazar;
		datos.MaxAutoAceptadas = maxAutoAceptadas;
	}

	public override void _Process(double delta)
	{
		ActualizarHora();
		ActualizarTiempoPartida(delta);
		ActualizarAutoAceptar(delta);
		SincronizarDatos();
	}

	// Copia el estado de la partida para que la ventana Red Flag lo muestre
	private void SincronizarDatos()
	{
		datos.Puntos = puntos;
		datos.ConexionesHoy = conexionesHoy;
		datos.ErroresAceptar = erroresAceptar;
		datos.ErroresRechazar = erroresRechazar;
		datos.AutoAceptadas = autoAceptadas;
		datos.AutoAceptarRestante = Math.Max(0, autoAceptar);
	}

	private void AbrirRedFlag()
	{
		if (juegoTerminado)
			return;

		redFlagApp.Abrir();
		notif.AppAbierta(true);
	}

	private void AlternarRedFlag()
	{
		if (redFlagApp.EstaAbierta)
		{
			redFlagApp.Cerrar();
			notif.AppAbierta(false);
		}
		else
		{
			AbrirRedFlag();
		}
	}

	private void InicializarPantalla()
	{
		puntos = 0;
		conexionesHoy = 0;

		lblPuntos.Text = puntos.ToString();
		lblConexiones.Text = conexionesHoy.ToString();

		GenerarSolicitud();
	}

	private void ActualizarTiempoPartida(double delta)
	{
		if (juegoTerminado)
			return;

		tiempoJugado += delta;

		double aMostrar;
		if (PartidaConfig.Modo == ModoJuego.TurnoDeGuardia)
		{
			tiempoPartida -= delta;
			aMostrar = Math.Max(0, tiempoPartida);
		}
		else
		{
			// Infinito: el reloj cuenta hacia arriba
			aMostrar = tiempoJugado;
		}

		datos.TiempoTurno = aMostrar;
		lblTiempo.Text = TimeSpan.FromSeconds(aMostrar).ToString(@"mm\:ss");

		VerificarFinDelJuego();
	}

	private int ultimoSegundoReloj = -1;

	// Reloj de la barra de tareas: hora arriba, fecha abajo (y la fecha completa al pasar el mouse)
	private void ActualizarHora()
	{
		DateTime ahora = DateTime.Now;

		lblHora.Text = ahora.ToString("HH:mm");
		lblFecha.Text = ahora.ToString("dd/MM/yyyy");

		if (ahora.Second == ultimoSegundoReloj)
			return;
		ultimoSegundoReloj = ahora.Second;

		try
		{
			escritorio.Reloj.TooltipText = ahora.ToString("dddd d 'de' MMMM 'de' yyyy, HH:mm:ss", new CultureInfo("es-AR"));
		}
		catch (CultureNotFoundException)
		{
			escritorio.Reloj.TooltipText = ahora.ToString("yyyy-MM-dd HH:mm:ss");
		}
	}

	private void ActualizarAutoAceptar(double delta)
	{
		if (juegoTerminado)
			return;

		autoAceptar -= delta;

		notif.ActualizarTiempo(autoAceptar, segundosAuto);

		if (autoAceptar <= 0)
		{
			RegistrarDecision("AUTOACEPTADA");
			autoAceptadas++;

			VerificarFinDelJuego();

			if (juegoTerminado)
				return;

			GenerarSolicitud();

			autoAceptar = segundosAuto;
		}
	}

	private void GenerarSolicitud()
	{
		conexionActual = new Conexion();

		conexionActual.Infectada = rnd.NextDouble() < probInfectada;

		// Datos de una conexión limpia (siempre válidos)
		conexionActual.Nombre = nombres[rnd.Next(nombres.Length)];
		conexionActual.IP = GenerarIP();
		conexionActual.MAC = GenerarMAC();
		conexionActual.Departamento = departamentos[rnd.Next(departamentos.Length)];
		conexionActual.Rol = roles[rnd.Next(roles.Length)];

		// Un virus se "delata" según la dificultad
		if (conexionActual.Infectada)
			AplicarPistasDeVirus();

		MostrarConexion();
		AgregarConexion();
	}

	// ------------------------------------------------------------------
	// Pistas de un virus. Las conexiones limpias nunca las tienen:
	//   Nombre: nombre y apellido normales, sin números ni símbolos.
	//   IP:     192.168.x.y con x entre 0 y 255 e y entre 1 y 254.
	//   MAC:    seis pares hexadecimales válidos.
	// Fácil:   todo está mal y a la vista (nombre, IP, MAC y departamento).
	// Normal:  1 o 2 pistas, de dificultad media.
	// Difícil: una sola pista y muy sutil (una letra cambiada, un número fuera de rango...).
	// ------------------------------------------------------------------
	private void AplicarPistasDeVirus()
	{
		switch (PartidaConfig.Nivel)
		{
			case Dificultad.Facil:
				conexionActual.Nombre = nombresInfectados[rnd.Next(nombresInfectados.Length)];
				conexionActual.IP = ipsInfectadas[rnd.Next(ipsInfectadas.Length)];
				conexionActual.MAC = macsInfectadas[rnd.Next(macsInfectadas.Length)];
				conexionActual.Departamento = "Desconocido";
				conexionActual.Rol = "Desconocido";
				break;

			case Dificultad.Normal:
			{
				// 1 pista (40%) o 2 pistas (60%) entre nombre, IP y MAC
				int cantidad = rnd.NextDouble() < 0.4 ? 1 : 2;
				List<int> pistas = new List<int> { 0, 1, 2 };
				for (int i = 0; i < cantidad; i++)
				{
					int idx = rnd.Next(pistas.Count);
					int pista = pistas[idx];
					pistas.RemoveAt(idx);
					if (pista == 0) conexionActual.Nombre = NombreSospechosoMedio(conexionActual.Nombre);
					else if (pista == 1) conexionActual.IP = IPSospechosaMedia();
					else conexionActual.MAC = MACSospechosaMedia();
				}
				break;
			}

			default: // Difícil: una única pista sutil
				switch (rnd.Next(3))
				{
					case 0: conexionActual.Nombre = NombreConLetraCambiada(conexionActual.Nombre); break;
					case 1: conexionActual.IP = IPSutil(); break;
					default: conexionActual.MAC = MACSutil(); break;
				}
				break;
		}
	}

	// --- Normal ---
	private string NombreSospechosoMedio(string nombreReal)
	{
		// Nombre real escrito con números ("m4ria_g0mez") o un usuario de sistema
		if (rnd.NextDouble() < 0.5)
		{
			string l = nombreReal.ToLower().Replace(' ', '_').Replace('a', '4').Replace('e', '3').Replace('o', '0');
			return l;
		}
		string[] sistema = { "root_admin", "svc_backup", "guest_01", "admin2", "tmp_user" };
		return sistema[rnd.Next(sistema.Length)];
	}

	private string IPSospechosaMedia()
	{
		switch (rnd.Next(3))
		{
			case 0: return $"172.{rnd.Next(30, 99)}.{rnd.Next(0, 256)}.{rnd.Next(1, 255)}"; // fuera de la red interna
			case 1: return $"192.168.{rnd.Next(0, 256)}.{rnd.Next(256, 400)}";              // último número > 255
			default: return $"10.{rnd.Next(60, 70)}.{rnd.Next(0, 256)}.{rnd.Next(1, 255)}";
		}
	}

	private string MACSospechosaMedia()
	{
		switch (rnd.Next(2))
		{
			case 0: return $"00:00:00:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}";
			default: return $"DE:AD:BE:EF:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}";
		}
	}

	// --- Difícil ---
	private string NombreConLetraCambiada(string nombreReal)
	{
		// Cambia UNA letra por otra parecida: Lucas Diaz -> Lucas Dlaz, Sofia -> Sofla...
		string[] pares = { "il", "li", "rn", "ao", "ei", "sz", "uv" };
		List<int> posibles = new List<int>();
		for (int i = 0; i < nombreReal.Length; i++)
			foreach (string par in pares)
				if (char.ToLower(nombreReal[i]) == par[0]) { posibles.Add(i); break; }

		if (posibles.Count == 0)
			return nombreReal.Substring(0, nombreReal.Length - 1) + "z";

		int pos = posibles[rnd.Next(posibles.Count)];
		char original = char.ToLower(nombreReal[pos]);
		char nuevo = original;
		foreach (string par in pares)
			if (par[0] == original) { nuevo = par[1]; break; }

		if (char.IsUpper(nombreReal[pos])) nuevo = char.ToUpper(nuevo);
		char[] letras = nombreReal.ToCharArray();
		letras[pos] = nuevo;
		return new string(letras);
	}

	private string IPSutil()
	{
		switch (rnd.Next(3))
		{
			case 0: return $"192.186.{rnd.Next(0, 256)}.{rnd.Next(1, 255)}";                  // 168 y 186 dados vuelta
			case 1: return $"192.168.{rnd.Next(0, 256)}.{rnd.Next(256, 300)}";                // un octeto apenas fuera de rango
			default: return $"192.168.{rnd.Next(0, 256)}.{(rnd.Next(2) == 0 ? 0 : 255)}";     // dirección de red / broadcast
		}
	}

	private string MACSutil()
	{
		// MAC con un carácter que no es hexadecimal (G, Z, O...) o un par de más/menos
		string mac = GenerarMAC();
		char[] letras = mac.ToCharArray();
		int pos;
		do { pos = rnd.Next(letras.Length); } while (letras[pos] == ':');
		char[] malos = { 'G', 'Z', 'O', 'S' };
		letras[pos] = malos[rnd.Next(malos.Length)];
		return new string(letras);
	}

	private void MostrarConexion()
	{
		datos.Solicitud.Hay = true;
		datos.Solicitud.Nombre = conexionActual.Nombre;
		datos.Solicitud.Area = conexionActual.Departamento;
		datos.Solicitud.Rol = conexionActual.Rol;
		datos.Solicitud.IP = conexionActual.IP;
		datos.Solicitud.MAC = conexionActual.MAC;

		notif.Mostrar(conexionActual.Nombre, conexionActual.Rol, conexionActual.Departamento,
			conexionActual.IP, conexionActual.MAC);
	}

	// Guarda la decisión para mostrarla en las apps (Logs, Incidentes, Red Flags...)
	private void RegistrarDecision(string decision)
	{
		RegistroConexion registro = new RegistroConexion
		{
			Hora = DateTime.Now,
			Nombre = conexionActual.Nombre,
			IP = conexionActual.IP,
			MAC = conexionActual.MAC,
			Area = conexionActual.Departamento,
			Rol = conexionActual.Rol,
			Decision = decision,
			Infectada = conexionActual.Infectada
		};

		datos.Registrar(registro);

		// Limpia -> whitelist, virus -> blacklist (automático)
		datos.AgregarAutomatico(registro);
	}

	public bool ConexionEsInfectada()
	{
		return conexionActual.Infectada;
	}

	private string GenerarIP()
	{
		return $"192.168.{rnd.Next(0, 256)}.{rnd.Next(1, 255)}";
	}

	private string GenerarMAC()
	{
		return $"{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}";
	}

	private void ActualizarPuntos()
	{
		lblPuntos.Text = puntos.ToString();
	}

	public void SumarPuntos(int cantidad)
	{
		puntos += cantidad;

		if (puntos < 0)
			puntos = 0;

		ActualizarPuntos();
	}

	public void AgregarConexion()
	{
		conexionesHoy++;
		lblConexiones.Text = conexionesHoy.ToString();

		// Modo Infinito: la dificultad sube cada CONEXIONES_POR_NIVEL conexiones
		if (PartidaConfig.Modo == ModoJuego.Infinito && conexionesHoy > 1 && (conexionesHoy - 1) % CONEXIONES_POR_NIVEL == 0)
		{
			probInfectada = Math.Min(0.6, probInfectada + 0.03);
			segundosAuto = Math.Max(5, segundosAuto - 0.5);
		}
	}

	public void Aceptar()
	{
		RegistrarDecision("ACEPTADA");

		if (ConexionEsInfectada())
		{
			// Aceptó un virus
			SumarPuntos(-20);
			erroresAceptar++;
			alertaRoja.Pulso(erroresAceptar / (float)maxErroresAceptar);
		}
		else
		{
			SumarPuntos(10);
			validasAceptadas++;
		}

		VerificarFinDelJuego();
	}

	public void Rechazar()
	{
		RegistrarDecision("RECHAZADA");

		if (ConexionEsInfectada())
		{
			SumarPuntos(20);
			virusRechazados++;
		}
		else
		{
			SumarPuntos(-10);
			erroresRechazar++;
		}

		VerificarFinDelJuego();
	}

	private void OnAceptarPressed()
	{
		if (juegoTerminado)
			return;

		Aceptar();

		if (juegoTerminado)
			return;

		GenerarSolicitud();
		autoAceptar = segundosAuto;
	}

	private void OnRechazarPressed()
	{
		if (juegoTerminado)
			return;

		Rechazar();

		if (juegoTerminado)
			return;

		GenerarSolicitud();
		autoAceptar = segundosAuto;
	}

	private void VerificarFinDelJuego()
	{
		if (juegoTerminado)
			return;

		if (erroresAceptar >= maxErroresAceptar)
		{
			FinDelJuego(false, "¡Aceptaste demasiados virus!");
			return;
		}

		if (erroresRechazar >= maxErroresRechazar)
		{
			FinDelJuego(false, "¡Rechazaste demasiadas conexiones válidas!");
			return;
		}

		if (autoAceptadas >= maxAutoAceptadas)
		{
			FinDelJuego(false, "¡Se autoaceptaron demasiadas conexiones!");
			return;
		}

		// En Turno de guardia, aguantar hasta el final del tiempo es ganar
		if (PartidaConfig.Modo == ModoJuego.TurnoDeGuardia && tiempoPartida <= 0)
		{
			FinDelJuego(true, "Sobreviviste al turno. El firewall resistió.");
		}
	}

	private void FinDelJuego(bool victoria, string motivo)
	{
		juegoTerminado = true;
		datos.Solicitud.Hay = false;
		SincronizarDatos();
		redFlagApp.Cerrar();
		notif.AppAbierta(false);
		notif.Ocultar();

		string modoTxt = PartidaConfig.Modo == ModoJuego.TurnoDeGuardia ? "Turno de guardia" : "Infinito";
		string resumen =
			$"Modo: {modoTxt} ({PartidaConfig.NombreNivel})\n" +
			$"Puntos: {puntos}\n" +
			$"Conexiones revisadas: {Math.Max(0, conexionesHoy - 1)}\n" +
			$"Virus rechazados: {virusRechazados}\n" +
			$"Virus aceptados: {erroresAceptar}\n" +
			$"Válidas aceptadas: {validasAceptadas}\n" +
			$"Válidas rechazadas: {erroresRechazar}\n" +
			$"Autoaceptadas: {autoAceptadas}\n" +
			$"Tiempo: {TimeSpan.FromSeconds(tiempoJugado):mm\\:ss}";

		AddChild(new GameOverScreen
		{
			Victoria = victoria,
			Motivo = motivo,
			Resumen = resumen
		});
	}
}
