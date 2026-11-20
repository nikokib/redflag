using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

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

	// Los tres eventos especiales son exclusivos del modo single player.
	private AvisoEmergencia avisoEmergencia;

	// Ventana "EMERGENCIA": sale cuando el jugador acumuló errores Y está a punto de perder (solo interfaz)
	private const int ERRORES_MINIMOS_ALERTA = 3;  // errores totales para que pueda salir
	private const int MARGEN_PARA_PERDER = 1;      // le falta esta cantidad de errores (o menos) para perder
	private const int BUENAS_PARA_RECUPERAR = 3;   // decisiones correctas para que desaparezca...
	private const int TAREAS_PARA_RECUPERAR = 1;   // ...más esta cantidad de correos urgentes resueltos a tiempo
	private const double ENFRIAMIENTO_REINICIO = 20; // segundos sin alerta tras elegir "Reiniciar equipo"
	private AlertaEmergencia alertaSistema;
	private bool alertaActiva = false;
	private bool reinicioEmergenciaUsado = false;
	private bool reinicioEmergenciaEnCurso = false;
	private bool alertaCerrada = false;
	private double alertaEnfriamiento = 0;
	private int ultimosErrores = 0;
	private int ultimasBuenas = 0;
	private int ultimasTareas = 0;
	private int rachaBuenas = 0;
	private int rachaTareas = 0;
	private double proximoCorreo = 10;
	private double tiempoAviso = 0;
	private string ultimoMensaje = "";
	private int inversionesPendientes = 0;
	private int proteccionesPendientes = 0;
	private int correctosCorreo = 0;
	private int fallidosCorreo = 0;
	private readonly List<TipoCorreo> tiposSinUsar = new List<TipoCorreo>();

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
		ConfigurarModoCooperativo();
		datos = new DatosEscritorio();
		if (modoCooperativo && sesionCoop.IsHost) datos.ListasCambio += ContarCambioListasCoop;

		escritorio = new EscritorioUI { Datos = datos,
			AlSalirMultijugador = modoCooperativo ? SalirCooperativo : null };
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

		// Un botón en el aviso abre la bandeja interactiva del escritorio.
		avisoEmergencia = new AvisoEmergencia { AlAbrirCorreo = () => { AbrirCorreoUrgente(); avisoEmergencia.OcultarPendiente(); } };
		AddChild(avisoEmergencia);
		// Si el jugador abre el correo desde el escritorio, el aviso de emergencia desaparece
		escritorio.AlAbrirAppUsuario = (string appId) => { if (appId == "correo") avisoEmergencia.OcultarPendiente(); };
		alertaSistema = new AlertaEmergencia
		{
			AlApagar = () => PantallaEnergia.Apagar(this),
			AlReiniciar = () => ReiniciarParaRecuperar(),
			AlCerrarManual = () => alertaCerrada = true
		};
		AddChild(alertaSistema);
		datos.ResponderCorreo = ResponderCorreo;
		datos.CodigoDesdeCalculadora = RevisarCodigoCalculadora;
		if (modoCooperativo && !sesionCoop.IsHost)
		{
			datos.AgregarListaRemota = blanca =>
			{
				if (!datos.Solicitud.Hay || juegoTerminado) return false;
				sesionCoop.EnviarAccionCooperativa("AGREGAR_LISTA",
					idSolicitudCliente.ToString() + "|" + (blanca ? "B" : "N"));
				return true;
			};
			datos.QuitarListaRemota = (blanca, ip) =>
			{
				if (juegoTerminado) return;
				sesionCoop.EnviarAccionCooperativa("QUITAR_LISTA",
					(blanca ? "B" : "N") + "|" + ip);
			};
		}

		AplicarConfig();
		if (modoCooperativo && !sesionCoop.IsHost)
		{
			lblTiempo.Text = "SINCRONIZANDO...";
			notif.Ocultar();
		}
		else InicializarPantalla();
	}
	
	
	private void ReiniciarParaRecuperar()
	{
		if (juegoTerminado || reinicioEmergenciaUsado)
			return;

		reinicioEmergenciaUsado = true;
		reinicioEmergenciaEnCurso = true;

		alertaActiva = false;
		alertaCerrada = true;
		alertaEnfriamiento = ENFRIAMIENTO_REINICIO;
		rachaBuenas = 0;
		rachaTareas = 0;

		// Buscar el tipo de error más cercano a la derrota
		int menorMargen = int.MaxValue;
		int tipoError = -1;

		if (erroresAceptar > 0 &&
			maxErroresAceptar - erroresAceptar < menorMargen)
		{
			menorMargen = maxErroresAceptar - erroresAceptar;
			tipoError = 0;
		}

		if (erroresRechazar > 0 &&
			maxErroresRechazar - erroresRechazar < menorMargen)
		{
			menorMargen = maxErroresRechazar - erroresRechazar;
			tipoError = 1;
		}

		if (autoAceptadas > 0 &&
			maxAutoAceptadas - autoAceptadas <= menorMargen)
		{
			tipoError = 2;
		}

		string errorCorregido = "ninguno";

		switch (tipoError)
		{
			case 0:
				erroresAceptar--;
				errorCorregido = "virus aceptado";
				break;

			case 1:
				erroresRechazar--;
				errorCorregido = "conexión válida rechazada";
				break;

			case 2:
				autoAceptadas--;
				errorCorregido = "solicitud autoaceptada";
				break;
		}

		// Penalización por el reinicio
		SumarPuntos(-20);

		ultimoMensaje = "Sistema recuperado: -20 puntos.";
		tiempoAviso = 5;

		// Guardar el resultado en el historial
		datos.HistorialEmergencias.Add(
			DateTime.Now.ToString("HH:mm:ss") +
			" - Reinicio de emergencia. Corregido: " +
			errorCorregido + ". Penalización: -20 puntos."
		);

		SincronizarDatos();
		datos.AvisarCambio();

		// Animación sin reiniciar la partida
		PantallaEnergia.ReiniciarEmergencia(this, () =>
		{
			reinicioEmergenciaEnCurso = false;
		});
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
		if (modoCooperativo && sesionCoop.IsHost && bloqueoDecisionesCoop > 0)
			bloqueoDecisionesCoop = Math.Max(0, bloqueoDecisionesCoop - delta);
		if (modoCooperativo && !sesionCoop.IsHost)
		{
			ActualizarInterfazClienteCoop();
			return;
		}
		ActualizarTiempoPartida(delta);

		if (!reinicioEmergenciaEnCurso)
		{
			ActualizarAutoAceptar(delta);
			ActualizarCorreos(delta);
			ActualizarAlertaSistema(delta);
		}

		SincronizarDatos();
		if (modoCooperativo) EnviarEstadoCoop(delta);
	}

	// ------------------------------------------------------------------
	// ALERTA "EMERGENCIA": el jugador cometió varios errores y está a punto de perder.
	// Solo muestra una ventana: no cambia puntos ni reglas.
	//  - Se puede cerrar sin elegir (reaparece si comete otro error).
	//  - Desaparece si se recupera: decisiones correctas + un correo urgente resuelto a tiempo.
	// ------------------------------------------------------------------
	private int TotalErrores => erroresAceptar + erroresRechazar + autoAceptadas + fallidosCorreo;

	// Cuántos errores más puede cometer antes de perder (el contador más cercano al límite)
	private int MargenParaPerder()
	{
		int m = maxErroresAceptar - erroresAceptar;
		m = Math.Min(m, maxErroresRechazar - erroresRechazar);
		m = Math.Min(m, maxAutoAceptadas - autoAceptadas);
		return m;
	}

	private void ActualizarAlertaSistema(double delta)
	{
		if (juegoTerminado)
		{
			alertaSistema.Ocultar();
			return;
		}
		if (reinicioEmergenciaUsado)
		{
			alertaSistema.Ocultar();
			return;
		}

		if (alertaEnfriamiento > 0) alertaEnfriamiento -= delta;

		int errores = TotalErrores;
		int buenas = validasAceptadas + virusRechazados;
		int tareas = correctosCorreo;

		if (errores > ultimosErrores)
		{
			// Un error nuevo: se corta la racha de recuperación y la ventana puede volver a salir
			rachaBuenas = 0;
			rachaTareas = 0;
			alertaCerrada = false;
		}
		else
		{
			if (buenas > ultimasBuenas) rachaBuenas += buenas - ultimasBuenas;
			if (tareas > ultimasTareas) rachaTareas += tareas - ultimasTareas;
		}
		ultimosErrores = errores;
		ultimasBuenas = buenas;
		ultimasTareas = tareas;

		bool enPeligro = errores >= ERRORES_MINIMOS_ALERTA && MargenParaPerder() <= MARGEN_PARA_PERDER;

		if (!alertaActiva && enPeligro && alertaEnfriamiento <= 0)
		{
			alertaActiva = true;
			alertaCerrada = false;
			rachaBuenas = 0;
			rachaTareas = 0;
		}

		if (alertaActiva && rachaBuenas >= BUENAS_PARA_RECUPERAR && rachaTareas >= TAREAS_PARA_RECUPERAR)
		{
			// El jugador se recuperó antes de perder: la alerta se retira
			alertaActiva = false;
			alertaSistema.Ocultar();
			return;
		}

		if (alertaActiva && !alertaCerrada && !alertaSistema.EstaVisible)
			alertaSistema.Mostrar();
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
		datos.InversionBotones = inversionesPendientes;
		datos.ProteccionesFirewall = proteccionesPendientes;
		datos.EmergenciasCorrectas = correctosCorreo;
		datos.EmergenciasFallidas = fallidosCorreo;
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
		CrearConexionAleatoria();
		// El firewall protege contra las próximas 3 conexiones infectadas.
		for (int intento = 0; intento < 100; intento++)
		{
			if (!conexionActual.Infectada || proteccionesPendientes <= 0)
				break;

			proteccionesPendientes--;
			RegistrarDecision("RECHAZADA");
			SumarPuntos(20);
			virusRechazados++;
			AgregarConexion();
			datos.HistorialEmergencias.Add("Firewall: amenaza bloqueada automáticamente.");
			CrearConexionAleatoria();
		}
		MostrarConexion();
		AgregarConexion();
		SincronizarDatos();
		datos.AvisarCambio();
	}

	// Construye una conexión, sin mostrarla, para el antivirus y el firewall.
	private Conexion CrearConexionAleatoria()
	{
		Conexion nueva = new Conexion();
		nueva.Infectada = rnd.NextDouble() < probInfectada;
		nueva.Nombre = nombres[rnd.Next(nombres.Length)];
		nueva.IP = GenerarIP();
		nueva.MAC = GenerarMAC();
		nueva.Departamento = departamentos[rnd.Next(departamentos.Length)];
		nueva.Rol = roles[rnd.Next(roles.Length)];
		conexionActual = nueva;
		if (nueva.Infectada) AplicarPistasDeVirus();
		return nueva;
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
		if (modoCooperativo && !sesionCoop.IsHost)
		{
			EnviarDecisionCoop("ACEPTAR");
			return;
		}
		if (juegoTerminado ||
			(modoCooperativo && sesionCoop.IsHost && bloqueoDecisionesCoop > 0))
			return;
		if (modoCooperativo) bloqueoDecisionesCoop = 0.25;

		// Durante el phishing, un fallo puede invertir las próximas 3 decisiones.
		if (inversionesPendientes > 0)
		{
			inversionesPendientes--;
			Rechazar();
			if (inversionesPendientes == 0)
				alertaRoja.Pulso(erroresAceptar / (float)maxErroresAceptar);
		}
		else Aceptar();

		if (juegoTerminado)
			return;

		GenerarSolicitud();
		autoAceptar = segundosAuto;
	}

	private void OnRechazarPressed()
	{
		if (modoCooperativo && !sesionCoop.IsHost)
		{
			EnviarDecisionCoop("RECHAZAR");
			return;
		}
		if (juegoTerminado ||
			(modoCooperativo && sesionCoop.IsHost && bloqueoDecisionesCoop > 0))
			return;
		if (modoCooperativo) bloqueoDecisionesCoop = 0.25;

		if (inversionesPendientes > 0)
		{
			inversionesPendientes--;
			Aceptar();
			if (inversionesPendientes == 0)
				alertaRoja.Pulso(erroresAceptar / (float)maxErroresAceptar);
		}
		else Rechazar();

		if (juegoTerminado)
			return;

		GenerarSolicitud();
		autoAceptar = segundosAuto;
	}

	// ------------------------------------------------------------------
	// CORREOS DE EMERGENCIA - SOLO SINGLE PLAYER
	// ------------------------------------------------------------------

	private void AbrirCorreoUrgente()
	{
		// Red Flag OS se dibuja por encima del escritorio: hay que ocultarlo
		// para que la ventana de correo no quede detrás de él.
		if (redFlagApp.EstaAbierta)
		{
			redFlagApp.Cerrar();
			notif.AppAbierta(false);
		}
		escritorio.AbrirApp("correo");
	}

	private void ActualizarCorreos(double delta)
	{
		if (juegoTerminado) return;
		if (tiempoAviso > 0) tiempoAviso -= delta;

		CorreoEmergencia correo = datos.CorreoActual;
		if (correo != null && correo.Pendiente)
		{
			int antes = (int)Math.Ceiling(correo.TiempoRestante);
			correo.TiempoRestante = Math.Max(0, correo.TiempoRestante - delta);
			if (correo.TiempoRestante == 0)
				FinalizarCorreo(false, true);
			else if ((int)Math.Ceiling(correo.TiempoRestante) != antes)
				datos.AvisarCorreo();
		}
		else
		{
			proximoCorreo -= delta;
			if (proximoCorreo <= 0)
				CrearCorreo();
		}

		if (!juegoTerminado)
			avisoEmergencia.Actualizar(datos.CorreoActual, inversionesPendientes,
				proteccionesPendientes, tiempoAviso, ultimoMensaje);
	}

	// Todos los tipos aparecen una vez por ronda, en orden aleatorio.
	private TipoCorreo ElegirTipo()
	{
		if (tiposSinUsar.Count == 0)
		{
			tiposSinUsar.Add(TipoCorreo.Matematica);
			tiposSinUsar.Add(TipoCorreo.Antivirus);
			tiposSinUsar.Add(TipoCorreo.Phishing);
			tiposSinUsar.Add(TipoCorreo.CodigoCalculadora);
		}
		int indice = rnd.Next(tiposSinUsar.Count);
		TipoCorreo tipo = tiposSinUsar[indice];
		tiposSinUsar.RemoveAt(indice);
		return tipo;
	}

	private void CrearCorreo()
	{
		CorreoEmergencia correo = new CorreoEmergencia();
		numeroCorreoCoop++;
		correo.Tipo = ElegirTipo();
		// Tiempo para completar la tarea del correo según la dificultad elegida
		switch (PartidaConfig.Nivel)
		{
			case Dificultad.Facil: correo.TiempoTotal = 15; break;
			case Dificultad.Normal: correo.TiempoTotal = 10; break;
			default: correo.TiempoTotal = 7; break;
		}
		int segundosCorreo = (int)correo.TiempoTotal;
		correo.TiempoRestante = correo.TiempoTotal;

		switch (correo.Tipo)
		{
			case TipoCorreo.Matematica:
			{
				int a = rnd.Next(4, 13);
				int b = rnd.Next(3, 10);
				int c = rnd.Next(2, 15);
				correo.RespuestaCorrecta = a * b - c;
				correo.Remitente = "Autorizaciones <autorizaciones@securenet.local>";
				correo.Asunto = "URGENTE: detener autorización masiva";
				correo.Cuerpo = $"Se intentan aprobar 2 accesos sin revisión.\n\nPara cancelar la orden, calculá:  {a} x {b} - {c} = ?\n\nSi fallás o vencen los {segundosCorreo} segundos, se autoaceptan hasta 2 solicitudes. ¡Respondé ya!";
				break;
			}
			case TipoCorreo.Antivirus:
			{
				int a = rnd.Next(11, 30);
				int b = rnd.Next(2, 11);
				correo.RespuestaCorrecta = a + b;
				correo.Remitente = "Seguridad IT <seguridad@securenet.local>";
				correo.Asunto = "URGENTE: activar análisis de 2 conexiones";
				correo.Cuerpo = $"El antivirus necesita autorización inmediata.\n\nCódigo de desbloqueo: {a} + {b} = ?\n\nSi acertás, revisará y resolverá correctamente 2 conexiones por vos. Si fallás, perdés 15 puntos y se acorta el tiempo de la solicitud actual.";
				break;
			}
			case TipoCorreo.Phishing:
			{
				correo.AlertaReal = rnd.Next(2) == 0;
				correo.Remitente = correo.AlertaReal
					? "Seguridad IT <seguridad@securenet.local>"
					: "Soporte <seguridad@securenet-soporte.com>";
				correo.Asunto = correo.AlertaReal ? "Alerta real: validación de incidente" : "BLOQUEO INMEDIATO: verificá tu cuenta";
				correo.Cuerpo = correo.AlertaReal
					? "Detectamos un intento de ingreso extraño.\nDominio del remitente: securenet.local (interno).\n\nNo abras links: elegí VERIFICAR EN IT para gestionar el incidente por el canal oficial."
					: "TU CUENTA SE ELIMINARÁ EN SEGUNDOS.\nIngresá al enlace de restauración.\nDominio del remitente: securenet-soporte.com (externo).\n\nSi es phishing, elegí REPORTAR PHISHING.";
				break;
			}
			case TipoCorreo.CodigoCalculadora:
			{
				int codigo;
				if (rnd.Next(2) == 0)
					codigo = rnd.Next(100, 1000);
				else
					codigo = rnd.Next(1000, 10000);
				correo.RespuestaCorrecta = codigo;
				correo.Remitente = "Seguridad IT <seguridad@securenet.local>";
				correo.Asunto = "URGENTE: recuperar puntos del sistema";
				correo.Cuerpo =
					$"Se detectaron registros pendientes de validación.\n\n" +
					$"Para recuperar los puntos, abrí la CALCULADORA.\n\n" +
					$"CÓDIGO: {codigo}\n\n" +
					$"Ingresá el número y presioná = para confirmar.\n" +
					$"Si hay otro número en pantalla, primero presioná C.\n\n" +
					$"Tenés {segundosCorreo} segundos.\n\n" +
					$"CORRECTO: +30 puntos.\n" +
					$"INCORRECTO O FUERA DE TIEMPO: -10 puntos.";
				break;
			}
		}

		datos.CorreoActual = correo;
		datos.AvisarCorreo();
		// Primero aparece la notificación de emergencia; el correo se abre al tocar su botón.
		ultimoMensaje = correo.Asunto;
		tiempoAviso = 4;
	}
	private void RevisarCodigoCalculadora(string codigo)
	{
		CorreoEmergencia correo = datos.CorreoActual;
		if (juegoTerminado || correo == null || !correo.Pendiente)
			return;
		if (correo.Tipo != TipoCorreo.CodigoCalculadora)
			return;
		ResponderCorreo(codigo);
	}
	private void ResponderCorreo(string respuesta)
	{
		if (modoCooperativo && !sesionCoop.IsHost)
		{
			EnviarRespuestaCoop(respuesta);
			return;
		}
		CorreoEmergencia correo = datos.CorreoActual;
		if (juegoTerminado || correo == null || !correo.Pendiente) return;
		bool correcto;
		if (correo.Tipo == TipoCorreo.Phishing)
			correcto = (correo.AlertaReal && respuesta == "VERIFICAR") ||
					   (!correo.AlertaReal && respuesta == "REPORTAR");
		else
			correcto = int.TryParse(respuesta, out int numero) && numero == correo.RespuestaCorrecta;

		FinalizarCorreo(correcto, false);
	}

	private void FinalizarCorreo(bool correcto, bool vencido)
	{
		CorreoEmergencia correo = datos.CorreoActual;
		if (juegoTerminado || correo == null || !correo.Pendiente) return;
		correo.Pendiente = false;
		proximoCorreo = rnd.Next(10, 15);
		tiempoAviso = 5;

		if (correcto)
		{
			correctosCorreo++;
			switch (correo.Tipo)
			{
				case TipoCorreo.Matematica:
					SumarPuntos(25);
					ultimoMensaje = "Cancelaste el acceso masivo. +25 puntos.";
					break;
				case TipoCorreo.Antivirus:
					ultimoMensaje = "Antivirus activado: 5 solicitudes analizadas.";
					AnalizarCincoSolicitudes();
					break;
				case TipoCorreo.CodigoCalculadora:
					SumarPuntos(30);
					ultimoMensaje = "Código validado correctamente. +30 puntos.";
					break;
				case TipoCorreo.Phishing:
					SumarPuntos(20);
					proteccionesPendientes += 3;
					ultimoMensaje = "Alerta gestionada. Firewall: 3 bloqueos extra. +20 puntos.";
					break;
			}
		}
		else
		{
			fallidosCorreo++;
			switch (correo.Tipo)
			{
				case TipoCorreo.Matematica:
					ultimoMensaje = "FALLO: se autoaceptaron solicitudes masivas.";
					AceptarCincoAutomaticas();
					break;
				case TipoCorreo.Antivirus:
					SumarPuntos(-15);
					autoAceptar = Math.Min(autoAceptar, Math.Max(2, segundosAuto - 5));
					ultimoMensaje = "Antivirus desactivado: -15 puntos y menos tiempo.";
					break;
				case TipoCorreo.CodigoCalculadora:
					SumarPuntos(-10);
					ultimoMensaje = "Código incorrecto o tiempo agotado. -10 puntos.";
					break;
				case TipoCorreo.Phishing:
					if (vencido)
					{
						SumarPuntos(-10);
						ultimoMensaje = "Alerta sin atender: -10 puntos.";
					}
					else
					{
						inversionesPendientes = 3;
						alertaRoja.Pulso(0.75f);
						ultimoMensaje = "INFECTADO: botones invertidos durante 3 decisiones.";
					}
					break;
			}
		}

		correo.Resultado = (correcto ? "CORRECTO: " : vencido ? "TIEMPO AGOTADO: " : "ERROR: ") + ultimoMensaje;
		datos.HistorialEmergencias.Add(DateTime.Now.ToString("HH:mm:ss") + " - " + correo.Resultado);
		SincronizarDatos();
		datos.AvisarCorreo();
		datos.AvisarCambio();
		VerificarFinDelJuego();
	}

	private void AnalizarCincoSolicitudes()
	{
		int limpias = 0;
		int infectadas = 0;
		// Cuenta la solicitud pendiente y otras cuatro, siempre usando
		// las funciones normales Aceptar/Rechazar para mantener puntos y logs.
		for (int i = 0; i < 5 && !juegoTerminado; i++)
		{
			if (conexionActual.Infectada)
			{
				Rechazar();
				infectadas++;
			}
			else
			{
				Aceptar();
				limpias++;
			}
			if (!juegoTerminado) GenerarSolicitud();
		}
		autoAceptar = segundosAuto;
		datos.EstadoAntivirus = $"Último escaneo urgente: {limpias} seguras y {infectadas} amenazas bloqueadas.";
		datos.AvisarCambio();
	}

	private void AceptarCincoAutomaticas()
	{
		// La dificultad establece cuántas autoaceptadas bastan para perder.
		// Por eso puede terminar antes de registrar las 5.
		for (int i = 0; i < 2 && !juegoTerminado; i++)
		{
			RegistrarDecision("AUTOACEPTADA");
			autoAceptadas++;
			VerificarFinDelJuego();
			if (!juegoTerminado) GenerarSolicitud();
		}
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
		resultadoCoopVictoria = victoria;
		resultadoCoopMotivo = motivo;
		if (datos.CorreoActual != null) datos.CorreoActual.Pendiente = false;
		avisoEmergencia.Ocultar();
		alertaSistema.Ocultar();
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
			$"Correos urgentes correctos: {correctosCorreo}\n" +
			$"Correos urgentes fallidos: {fallidosCorreo}\n" +
			$"Tiempo: {TimeSpan.FromSeconds(tiempoJugado):mm\\:ss}";

		resultadoCoopResumen = resumen;
		AddChild(new GameOverScreen
		{
			Victoria = victoria,
			Motivo = motivo,
			Resumen = resumen,
			EsCooperativo = modoCooperativo
		});
		if (modoCooperativo) EnviarEstadoCoop(1);
	}

	// ----------------------------------------------------------
	// MULTIJUGADOR COOPERATIVO: anfitrión controla el estado
	// ----------------------------------------------------------
	private bool modoCooperativo = false;
	private NetworkSession sesionCoop;
	private double envioCoopAcumulado = 0;
	private double bloqueoDecisionesCoop = 0;
	private bool desconexionNotificada = false;
	private int idSolicitudCliente = 0;
	private int numeroCorreoCoop = 0;
	private int numeroCorreoCliente = -1;
	private int ultimoIdMostrado = -1;
	private int ultimoErrorVirusCliente = 0;
	private int versionListasCoop = 0;
	private int ultimaVersionListasCoop = -1;
	private bool resultadoCoopVictoria = false;
	private string resultadoCoopMotivo = "";
	private string resultadoCoopResumen = "";
	private static readonly JsonSerializerOptions OpcionesCoop = new JsonSerializerOptions
	{
		IncludeFields = true
	};

	private void ConfigurarModoCooperativo()
	{
		sesionCoop = GetNodeOrNull<NetworkSession>("/root/NetworkSession");
		modoCooperativo = sesionCoop != null && sesionCoop.IsConnected &&
						  !sesionCoop.IsInLobby && sesionCoop.GameMode == "Salva la red";
		if (!modoCooperativo) return;

		PartidaConfig.Modo = ModoJuego.TurnoDeGuardia;
		PartidaConfig.DuracionMinutos = sesionCoop.DurationMinutes;
		switch (sesionCoop.Difficulty.Trim().ToLowerInvariant())
		{
			case "fácil":
			case "facil": PartidaConfig.Nivel = Dificultad.Facil; break;
			case "difícil":
			case "dificil": PartidaConfig.Nivel = Dificultad.Dificil; break;
			default: PartidaConfig.Nivel = Dificultad.Normal; break;
		}

		if (sesionCoop.IsHost)
		{
			sesionCoop.AccionCooperativaRecibida += ProcesarAccionCoop;
			// La suscripción se completa en _Ready, después de construir DatosEscritorio.
		}
		else
			sesionCoop.EstadoCooperativoRecibido += AplicarEstadoCoop;
	}

	public override void _ExitTree()
	{
		if (!modoCooperativo || sesionCoop == null) return;
		if (sesionCoop.IsHost)
		{
			sesionCoop.AccionCooperativaRecibida -= ProcesarAccionCoop;
			if (datos != null) datos.ListasCambio -= ContarCambioListasCoop;
		}
		else
			sesionCoop.EstadoCooperativoRecibido -= AplicarEstadoCoop;
	}

	private void ContarCambioListasCoop() { versionListasCoop++; }

	private void SalirCooperativo()
	{
		if (sesionCoop == null) return;
		sesionCoop.LeaveRoom();
		GetTree().ChangeSceneToFile("res://Scenes/MenuPrincipal.tscn");
	}

	private void EnviarDecisionCoop(string accion)
	{
		if (juegoTerminado || idSolicitudCliente <= 0 || !datos.Solicitud.Hay) return;
		sesionCoop.EnviarAccionCooperativa(accion,
			idSolicitudCliente.ToString(CultureInfo.InvariantCulture));
	}

	private void EnviarRespuestaCoop(string respuesta)
	{
		if (juegoTerminado || numeroCorreoCliente <= 0 ||
			datos.CorreoActual == null || !datos.CorreoActual.Pendiente) return;
		sesionCoop.EnviarAccionCooperativa("CORREO",
			numeroCorreoCliente.ToString(CultureInfo.InvariantCulture) + "|" + respuesta);
	}

	private void ProcesarAccionCoop(long remitente, string accion, string valor)
	{
		// Las llamadas sólo llegan desde NetworkSession, que verifica el peer registrado.
		if (!modoCooperativo || !sesionCoop.IsHost || juegoTerminado) return;
		if (bloqueoDecisionesCoop > 0 &&
			(accion == "ACEPTAR" || accion == "RECHAZAR")) return;
		if (accion == "ACEPTAR" || accion == "RECHAZAR")
		{
			if (!int.TryParse(valor, out int id) || id != conexionesHoy ||
				!datos.Solicitud.Hay || conexionActual == null) return;
			if (accion == "ACEPTAR") OnAceptarPressed();
			else OnRechazarPressed();
		}
		else if (accion == "CORREO")
		{
			int separador = valor.IndexOf('|');
			if (separador <= 0 || separador > 12) return;
			if (!int.TryParse(valor[..separador], out int id) ||
				id != numeroCorreoCoop || datos.CorreoActual == null ||
				!datos.CorreoActual.Pendiente) return;
			string respuesta = valor[(separador + 1)..];
			if (respuesta.Length > 32) return;
			// La calculadora y el correo verifican las respuestas con las mismas reglas
			// que el modo individual. Nunca confiamos en una respuesta "correcta" del cliente.
			ResponderCorreo(respuesta);
		}
		else if (accion == "AGREGAR_LISTA")
		{
			string[] partes = valor.Split('|', 2);
			if (partes.Length == 2 && int.TryParse(partes[0], out int id) &&
				id == conexionesHoy && datos.Solicitud.Hay &&
				(partes[1] == "B" || partes[1] == "N"))
				datos.AgregarALista(partes[1] == "B");
		}
		else if (accion == "QUITAR_LISTA")
		{
			string[] partes = valor.Split('|', 2);
			if (partes.Length == 2 && partes[1].Length < 64 &&
				(partes[0] == "B" || partes[0] == "N"))
			{
				bool blanca = partes[0] == "B";
				EntradaLista entrada = (blanca ? datos.Whitelist : datos.Blacklist)
					.FirstOrDefault(x => x.IP == partes[1]);
				if (entrada != null) datos.QuitarDeLista(blanca, entrada);
			}
		}
		// Refrescar también al pulsar una acción, sin esperar al siguiente pulso periódico.
		EnviarEstadoCoop(1.0);
	}

	private void EnviarEstadoCoop(double delta)
	{
		if (!modoCooperativo || sesionCoop == null || !sesionCoop.IsHost) return;
		envioCoopAcumulado += delta;
		if (envioCoopAcumulado < 0.25) return;
		envioCoopAcumulado = 0;

		CorreoEmergencia c = datos.CorreoActual;
		CorreoEmergencia correoPublico = c == null ? null : new CorreoEmergencia
		{
			Tipo = c.Tipo, Remitente = c.Remitente, Asunto = c.Asunto,
			Cuerpo = c.Cuerpo, Resultado = c.Resultado, Pendiente = c.Pendiente,
			TiempoRestante = c.TiempoRestante, TiempoTotal = c.TiempoTotal,
			// No transmitir la respuesta ni la clasificación secreta en el paquete.
			RespuestaCorrecta = 0, AlertaReal = false
		};

		EstadoCooperativo e = new EstadoCooperativo
		{
			NumeroSolicitud = conexionesHoy,
			NumeroCorreo = numeroCorreoCoop,
			VersionListas = versionListasCoop,
			Solicitud = new SolicitudActual
			{
				Hay = datos.Solicitud.Hay, Nombre = datos.Solicitud.Nombre,
				Area = datos.Solicitud.Area, Rol = datos.Solicitud.Rol,
				IP = datos.Solicitud.IP, MAC = datos.Solicitud.MAC
			},
			Correo = correoPublico,
			Tiempo = datos.TiempoTurno,
			AutoRestante = Math.Max(0, autoAceptar),
			AutoMaximo = segundosAuto,
			Puntos = puntos,
			Conexiones = conexionesHoy,
			ErroresAceptar = erroresAceptar,
			ErroresRechazar = erroresRechazar,
			AutoAceptadas = autoAceptadas,
			MaxErroresAceptar = maxErroresAceptar,
			MaxErroresRechazar = maxErroresRechazar,
			MaxAutoAceptadas = maxAutoAceptadas,
			Inversiones = inversionesPendientes,
			Protecciones = proteccionesPendientes,
			CorreosCorrectos = correctosCorreo,
			CorreosFallidos = fallidosCorreo,
			EstadoAntivirus = datos.EstadoAntivirus,
			Aviso = ultimoMensaje,
			DuracionAviso = tiempoAviso,
			Historial = datos.Historial.TakeLast(25).ToList(),
			Blancas = datos.Whitelist.TakeLast(60).ToList(),
			Negras = datos.Blacklist.TakeLast(60).ToList(),
			HistorialEmergencias = datos.HistorialEmergencias.TakeLast(25).ToList(),
			Terminada = juegoTerminado,
			Victoria = resultadoCoopVictoria,
			MotivoFinal = resultadoCoopMotivo,
			ResumenFinal = resultadoCoopResumen
		};
		sesionCoop.EnviarEstadoCooperativo(JsonSerializer.Serialize(e, OpcionesCoop));
	}

	private void ActualizarInterfazClienteCoop()
	{
		if (!sesionCoop.IsConnected)
		{
			if (!desconexionNotificada)
			{
				desconexionNotificada = true;
				GetTree().ChangeSceneToFile("res://Scenes/MultiplayerMenu.tscn");
			}
			return;
		}
		if (datos.Solicitud.Hay && !juegoTerminado)
			notif.ActualizarTiempo(datos.AutoAceptarRestante, segundosAuto);
		if (!juegoTerminado)
			avisoEmergencia.Actualizar(datos.CorreoActual, inversionesPendientes,
				proteccionesPendientes, tiempoAviso, ultimoMensaje);
	}

	private void AplicarEstadoCoop(string json)
	{
		if (!modoCooperativo || sesionCoop.IsHost || !IsInsideTree() ||
			json == null || json.Length > 120000) return;
		EstadoCooperativo e;
		try { e = JsonSerializer.Deserialize<EstadoCooperativo>(json, OpcionesCoop); }
		catch (JsonException) { return; }
		if (e == null) return;

		idSolicitudCliente = e.NumeroSolicitud;
		segundosAuto = e.AutoMaximo;
		puntos = e.Puntos;
		conexionesHoy = e.Conexiones;
		erroresAceptar = e.ErroresAceptar;
		erroresRechazar = e.ErroresRechazar;
		autoAceptadas = e.AutoAceptadas;
		maxErroresAceptar = e.MaxErroresAceptar;
		maxErroresRechazar = e.MaxErroresRechazar;
		maxAutoAceptadas = e.MaxAutoAceptadas;
		inversionesPendientes = e.Inversiones;
		proteccionesPendientes = e.Protecciones;
		correctosCorreo = e.CorreosCorrectos;
		fallidosCorreo = e.CorreosFallidos;
		ultimoMensaje = e.Aviso ?? "";
		tiempoAviso = e.DuracionAviso;
		datos.EstadoAntivirus = e.EstadoAntivirus ?? "";
		datos.TiempoTurno = e.Tiempo;
		autoAceptar = e.AutoRestante;
		lblTiempo.Text = TimeSpan.FromSeconds(Math.Max(0, e.Tiempo)).ToString(@"mm\:ss");
		lblPuntos.Text = e.Puntos.ToString();
		lblConexiones.Text = e.Conexiones.ToString();

		SolicitudActual s = e.Solicitud ?? new SolicitudActual();
		datos.Solicitud.Hay = s.Hay && !e.Terminada;
		datos.Solicitud.Nombre = s.Nombre ?? "";
		datos.Solicitud.Area = s.Area ?? "";
		datos.Solicitud.Rol = s.Rol ?? "";
		datos.Solicitud.IP = s.IP ?? "";
		datos.Solicitud.MAC = s.MAC ?? "";

		if (e.NumeroSolicitud != ultimoIdMostrado)
		{
			ultimoIdMostrado = e.NumeroSolicitud;
			if (datos.Solicitud.Hay)
				notif.Mostrar(s.Nombre, s.Rol, s.Area, s.IP, s.MAC);
		}
		if (!datos.Solicitud.Hay) notif.Ocultar();
		if (erroresAceptar > ultimoErrorVirusCliente)
			alertaRoja.Pulso(erroresAceptar / (float)Math.Max(1, maxErroresAceptar));
		ultimoErrorVirusCliente = erroresAceptar;

		// Mantener el mismo objeto de correo para no reiniciar la interfaz de lectura
		// en cada paquete de sincronización.
		CorreoEmergencia c = e.Correo;
		bool nuevoCorreo = e.NumeroCorreo != numeroCorreoCliente;
		bool refrescarCorreo = nuevoCorreo;
		if (c != null)
		{
			if (nuevoCorreo || datos.CorreoActual == null)
				datos.CorreoActual = new CorreoEmergencia();
			CorreoEmergencia local = datos.CorreoActual;
			if (local.Pendiente != c.Pendiente || local.Resultado != c.Resultado ||
				(int)Math.Ceiling(local.TiempoRestante) != (int)Math.Ceiling(c.TiempoRestante))
				refrescarCorreo = true;
			local.Tipo = c.Tipo;
			local.Remitente = c.Remitente;
			local.Asunto = c.Asunto;
			local.Cuerpo = c.Cuerpo;
			local.Resultado = c.Resultado;
			local.Pendiente = c.Pendiente;
			local.TiempoRestante = c.TiempoRestante;
			local.TiempoTotal = c.TiempoTotal;
		}
		else if (datos.CorreoActual != null)
		{
			datos.CorreoActual = null;
			refrescarCorreo = true;
		}
		numeroCorreoCliente = e.NumeroCorreo;
		if (refrescarCorreo) datos.AvisarCorreo();

		// Las listas y logs sólo se copian cuando cambia una conexión.
		if (e.NumeroSolicitud != ultimoRegistroCoop ||
			e.VersionListas != ultimaVersionListasCoop ||
			(refrescarCorreo && c != null && !c.Pendiente))
		{
			ultimoRegistroCoop = e.NumeroSolicitud;
			ultimaVersionListasCoop = e.VersionListas;
			datos.Historial.Clear();
			if (e.Historial != null) datos.Historial.AddRange(e.Historial);
			datos.Whitelist.Clear();
			if (e.Blancas != null) datos.Whitelist.AddRange(e.Blancas);
			datos.Blacklist.Clear();
			if (e.Negras != null) datos.Blacklist.AddRange(e.Negras);
			datos.HistorialEmergencias.Clear();
			if (e.HistorialEmergencias != null)
				datos.HistorialEmergencias.AddRange(e.HistorialEmergencias);
			datos.AvisarListas();
		}
		SincronizarDatos();
		datos.AvisarCambio();

		if (e.Terminada && !juegoTerminado)
		{
			juegoTerminado = true;
			avisoEmergencia.Ocultar();
			alertaSistema.Ocultar();
			datos.Solicitud.Hay = false;
			redFlagApp.Cerrar();
			notif.AppAbierta(false);
			notif.Ocultar();
			AddChild(new GameOverScreen
			{
				Victoria = e.Victoria,
				Motivo = e.MotivoFinal,
				Resumen = e.ResumenFinal,
				EsCooperativo = true
			});
		}
	}

	private int ultimoRegistroCoop = -1;
}
