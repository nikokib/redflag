using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Define las apps del escritorio y arma el contenido de cada ventana.
/// Todo es solo interfaz: no modifica las reglas del juego.
/// </summary>
public static class AppsEscritorio
{
	public class AppDef
	{
		public string Id;
		public string Titulo;
		public string Mono;
		public Color Color;
		public Vector2 Tamano;

		public AppDef(string id, string titulo, string mono, string color, float ancho, float alto)
		{
			Id = id;
			Titulo = titulo;
			Mono = mono;
			Color = new Color(color);
			Tamano = new Vector2(ancho, alto);
		}
	}

	public static readonly AppDef[] Izquierda =
	{
		new AppDef("mipc", "Mi PC", "PC", "5aa9e6", 620, 400),
		new AppDef("navegador", "Navegador", "WWW", "39b7cc", 640, 420),
		new AppDef("redflags", "Red Flags", "RF", "ff3b34", 660, 400),
		new AppDef("incidentes", "Incidentes", "INC", "ffb347", 520, 340),
		new AppDef("logs", "Logs", "LOG", "8ec07c", 580, 380),
		new AppDef("correo", "Correo Interno", "@", "d6c7a1", 660, 380),
		new AppDef("chat", "Chat", "CH", "7ad7c1", 480, 380)
	};

	public static readonly AppDef[] Derecha =
	{
		new AppDef("firewall", "Firewall", "FW", "ff7a45", 480, 340),
		new AppDef("antivirus", "Antivirus", "AV", "4dd0a0", 480, 300),
		new AppDef("camaras", "Cámaras", "CAM", "9aa5b1", 640, 420),
		new AppDef("notas", "Notas", "N", "f2d94e", 440, 340),
		new AppDef("calculadora", "Calculadora", "123", "7ab8ff", 300, 400),
		new AppDef("papelera", "Papelera", "X", "86a1ab", 400, 240)
	};

	public static AppDef Buscar(string id)
	{
		foreach (AppDef a in Izquierda) if (a.Id == id) return a;
		foreach (AppDef a in Derecha) if (a.Id == id) return a;
		return null;
	}

	public static Control Crear(string id, DatosEscritorio d)
	{
		switch (id)
		{
			case "mipc": return Explorador();
			case "navegador": return Navegador();
			case "redflags": return Dashboard(d);
			case "incidentes": return Incidentes(d);
			case "logs": return Logs(d);
			case "correo": return Correo(d);
			case "chat": return Chat(d);
			case "firewall": return Firewall(d);
			case "antivirus": return Antivirus(d);
			case "camaras": return Camaras();
			case "notas": return Notas(d);
			case "calculadora": return Calculadora(d);
			case "papelera": return Papelera();
			default: return Margen(Txt("App no disponible"));
		}
	}

	// ------------------------------------------------------------------
	// Ayudas de interfaz
	// ------------------------------------------------------------------

	private static Label Txt(string texto, int tamano = 14, Color? color = null, bool ajustar = false,
		HorizontalAlignment alineacion = HorizontalAlignment.Left)
	{
		Label l = RedFlagUI.CrearLabel(texto, tamano, color ?? RedFlagUI.Texto, alineacion);
		if (ajustar)
		{
			l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			l.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			l.CustomMinimumSize = new Vector2(180, 0);
		}
		return l;
	}

	private static MarginContainer Margen(Control hijo, int m = 12)
	{
		MarginContainer mc = new MarginContainer();
		foreach (string lado in new[] { "left", "right", "top", "bottom" })
			mc.AddThemeConstantOverride("margin_" + lado, m);
		mc.AddChild(hijo);
		return mc;
	}

	private static PanelContainer Marco(Control hijo, int margen = 10)
	{
		PanelContainer p = new PanelContainer();
		p.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(RedFlagUI.FondoPanel, RedFlagUI.Borde, 1, 3, margen));
		p.AddChild(hijo);
		return p;
	}

	private static VBoxContainer Columna(int separacion = 8)
	{
		VBoxContainer v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", separacion);
		return v;
	}

	private static HBoxContainer Fila(int separacion = 8)
	{
		HBoxContainer h = new HBoxContainer();
		h.AddThemeConstantOverride("separation", separacion);
		return h;
	}

	private static ScrollContainer Desplazable(Control hijo)
	{
		ScrollContainer sc = new ScrollContainer
		{
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		sc.AddChild(hijo);
		return sc;
	}

	private static void ExpandirTodo(Control c)
	{
		c.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		c.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
	}

	private static void Suscribir(Control c, DatosEscritorio d, Action refrescar)
	{
		d.Cambio += refrescar;
		c.TreeExiting += () => d.Cambio -= refrescar;
		refrescar();
	}

	private static void EstilizarEntrada(LineEdit e)
	{
		e.AddThemeStyleboxOverride("normal", RedFlagUI.Caja(RedFlagUI.FondoPanel, RedFlagUI.Borde, 1, 2, 6));
		e.AddThemeStyleboxOverride("focus", RedFlagUI.Caja(RedFlagUI.FondoPanel, RedFlagUI.BordeVivo, 1, 2, 6));
		e.AddThemeFontOverride("font", RedFlagUI.Fuente);
		e.AddThemeFontSizeOverride("font_size", 15);
		e.AddThemeColorOverride("font_color", RedFlagUI.Texto);
		e.AddThemeColorOverride("font_placeholder_color", RedFlagUI.TextoApagado);
		e.AddThemeColorOverride("caret_color", RedFlagUI.BordeVivo);
	}

	private static ProgressBar CrearBarra(Color relleno)
	{
		ProgressBar b = new ProgressBar
		{
			MinValue = 0,
			MaxValue = 100,
			Value = 0,
			ShowPercentage = false,
			CustomMinimumSize = new Vector2(0, 16)
		};
		b.AddThemeStyleboxOverride("background", RedFlagUI.Caja(new Color("04101c"), RedFlagUI.Borde, 1, 6));
		b.AddThemeStyleboxOverride("fill", RedFlagUI.Caja(relleno, relleno, 0, 6));
		return b;
	}

	// ------------------------------------------------------------------
	// Mi PC (explorador de archivos)
	// ------------------------------------------------------------------

	private static Control Explorador()
	{
		VBoxContainer raiz = Columna(8);
		raiz.AddChild(Marco(Txt("C:\\Usuarios\\analopez\\", 14, RedFlagUI.TextoApagado), 8));

		HBoxContainer cuerpo = Fila(10);
		cuerpo.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		raiz.AddChild(cuerpo);

		VBoxContainer lateral = Columna(6);
		lateral.CustomMinimumSize = new Vector2(150, 0);
		lateral.AddChild(Txt("Acceso rápido", 14, RedFlagUI.Acento));
		foreach (string s in new[] { "Escritorio", "Documentos", "Descargas", "Imágenes" })
			lateral.AddChild(Txt("  " + s, 13));
		lateral.AddChild(Txt("Red Interna", 14, RedFlagUI.Acento));
		foreach (string s in new[] { "Departamentos", "Servidores" })
			lateral.AddChild(Txt("  " + s, 13));
		PanelContainer marcoLateral = Marco(lateral, 10);
		marcoLateral.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		cuerpo.AddChild(marcoLateral);

		GridContainer grilla = new GridContainer { Columns = 3, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		grilla.AddThemeConstantOverride("h_separation", 20);
		grilla.AddThemeConstantOverride("v_separation", 14);
		foreach (string nombre in new[] { "Documentos", "Descargas", "Reportes", "Plantillas", "Respaldos", "manual_reds.pdf" })
		{
			VBoxContainer celda = Columna(4);
			celda.AddChild(new IconoApp
			{
				Tipo = nombre.EndsWith(".pdf") ? IconoApp.Forma.Pdf : IconoApp.Forma.Carpeta,
				CustomMinimumSize = new Vector2(64, 64),
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
			});
			celda.AddChild(Txt(nombre, 12, RedFlagUI.Texto, false, HorizontalAlignment.Center));
			grilla.AddChild(celda);
		}
		PanelContainer marcoGrilla = Marco(grilla, 14);
		ExpandirTodo(marcoGrilla);
		cuerpo.AddChild(marcoGrilla);

		raiz.AddChild(Txt("6 elementos", 12, RedFlagUI.TextoApagado));
		return Margen(raiz, 10);
	}

	// ------------------------------------------------------------------
	// Navegador (intranet SecureNet)
	// ------------------------------------------------------------------

	private static Control Navegador()
	{
		VBoxContainer raiz = Columna(8);
		raiz.AddChild(Marco(Txt("https://intranet.securenet.local", 14, RedFlagUI.TextoApagado), 8));

		HBoxContainer cuerpo = Fila(12);
		cuerpo.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		raiz.AddChild(cuerpo);

		VBoxContainer contenido = Columna(8);
		Label titulo = Txt("SECURENET", 30, new Color("6fd3e8"));
		Label subtitulo = Txt("INTRANET CORPORATIVA", 13, RedFlagUI.TextoApagado);
		Label encabezado = Txt("", 16, RedFlagUI.Texto);
		Label lineas = Txt("", 14, RedFlagUI.TextoApagado, true);

		void Mostrar(string pagina)
		{
			switch (pagina)
			{
				case "Noticias":
					encabezado.Text = "NOTICIAS";
					lineas.Text = "- Se reforzó el firewall perimetral\n- Aumentaron los intentos de phishing\n- Recordatorio: no compartir credenciales";
					break;
				case "Documentos":
					encabezado.Text = "DOCUMENTOS";
					lineas.Text = "- manual_reds.pdf\n- Política de accesos v3\n- Protocolo de incidentes";
					break;
				case "Recursos":
					encabezado.Text = "RECURSOS";
					lineas.Text = "- Guía de seguridad para empleados\n- Lista de áreas habilitadas\n- Alta de dispositivos";
					break;
				case "Soporte":
					encabezado.Text = "SOPORTE";
					lineas.Text = "Mesa de ayuda interna: int. 4400\nHorario: 24/7 durante tu turno\nCorreo: soporte@securenet.local";
					break;
				default:
					encabezado.Text = "AVISOS IMPORTANTES";
					lineas.Text = "- Actualización de seguridad - 24/05\n- Reunión general - 26/05\n- Nueva política de contraseñas";
					break;
			}
		}

		VBoxContainer menu = Columna(4);
		menu.CustomMinimumSize = new Vector2(140, 0);
		foreach (string pagina in new[] { "Inicio", "Noticias", "Documentos", "Recursos", "Soporte" })
		{
			Button b = new Button { Text = pagina, Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 34) };
			RedFlagUI.EstilizarBoton(b, false);
			b.Pressed += () => Mostrar(pagina);
			menu.AddChild(b);
		}
		PanelContainer marcoMenu = Marco(menu, 8);
		marcoMenu.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		cuerpo.AddChild(marcoMenu);

		contenido.AddChild(titulo);
		contenido.AddChild(subtitulo);
		VBoxContainer avisos = Columna(10);
		avisos.AddChild(encabezado);
		avisos.AddChild(lineas);
		PanelContainer marcoAvisos = Marco(avisos, 14);
		marcoAvisos.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		contenido.AddChild(marcoAvisos);
		contenido.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		cuerpo.AddChild(contenido);

		Mostrar("Inicio");
		return Margen(raiz, 10);
	}

	// ------------------------------------------------------------------
	// Red Flags (whitelist / blacklist)
	// ------------------------------------------------------------------

	private static List<string> Ultimos(List<string> lista, int cantidad)
	{
		return lista.Skip(Math.Max(0, lista.Count - cantidad)).ToList();
	}

	private static Control Dashboard(DatosEscritorio d)
	{
		VBoxContainer raiz = Columna(10);
		raiz.AddChild(Txt("PANEL DE CONTROL - ACCESOS A LA RED", 15, RedFlagUI.Acento));

		Label lblBlanca = Txt("", 13, new Color("6fe3b0"));
		Label lblNegra = Txt("", 13, new Color("ff6b63"));

		HBoxContainer columnas = Fila(12);
		columnas.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		VBoxContainer colBlanca = Columna(6);
		colBlanca.AddChild(Txt("WHITELIST", 15, new Color("4dd0a0")));
		colBlanca.AddChild(lblBlanca);
		PanelContainer marcoBlanca = Marco(colBlanca, 12);
		ExpandirTodo(marcoBlanca);
		columnas.AddChild(marcoBlanca);

		VBoxContainer colNegra = Columna(6);
		colNegra.AddChild(Txt("BLACKLIST", 15, new Color("ff4a52")));
		colNegra.AddChild(lblNegra);
		PanelContainer marcoNegra = Marco(colNegra, 12);
		ExpandirTodo(marcoNegra);
		columnas.AddChild(marcoNegra);

		raiz.AddChild(columnas);

		Label resumen = Txt("", 13, RedFlagUI.TextoApagado);
		raiz.AddChild(resumen);

		string[] semillaBlanca = { "192.168.1.23  | Contabilidad", "192.168.1.45  | Marketing", "192.168.1.102 | Soporte" };
		string[] semillaNegra = { "192.168.1.66  | Desconocido", "192.168.1.77  | Externo", "192.168.1.200 | Invitado", "192.168.1.250 | Desconocido" };

		Suscribir(raiz, d, () =>
		{
			List<string> blancas = new List<string>(semillaBlanca);
			List<string> negras = new List<string>(semillaNegra);

			foreach (RegistroConexion r in d.Historial)
			{
				if (r.Infectada)
					negras.Add($"{r.IP,-15} | {r.Nombre}");
				else if (r.Decision != "RECHAZADA")
					blancas.Add($"{r.IP,-15} | {r.Area}");
			}

			lblBlanca.Text = string.Join("\n", Ultimos(blancas, 12));
			lblNegra.Text = string.Join("\n", Ultimos(negras, 12));
			resumen.Text = $"Solicitudes procesadas: {d.Historial.Count}   Amenazas bloqueadas: {d.AmenazasBloqueadas}   Virus aceptados: {d.VirusAceptados}";
		});

		return Margen(raiz, 10);
	}

	// ------------------------------------------------------------------
	// Incidentes
	// ------------------------------------------------------------------

	private static Control Incidentes(DatosEscritorio d)
	{
		VBoxContainer raiz = Columna(8);
		raiz.AddChild(Txt("INCIDENTES DE SEGURIDAD", 15, new Color("ff4a52")));

		Label resumen = Txt("", 13, RedFlagUI.TextoApagado);
		raiz.AddChild(resumen);

		Label lista = Txt("", 13, RedFlagUI.Texto, true);
		PanelContainer marco = Marco(Desplazable(lista), 10);
		ExpandirTodo(marco);
		raiz.AddChild(marco);

		Suscribir(raiz, d, () =>
		{
			List<RegistroConexion> incidentes = d.Historial
				.Where(r => r.Infectada && r.Decision != "RECHAZADA")
				.OrderByDescending(r => r.Hora)
				.ToList();

			resumen.Text = $"Infecciones activas: {incidentes.Count}   Amenazas bloqueadas: {d.AmenazasBloqueadas}";

			if (incidentes.Count == 0)
			{
				lista.Text = "Sin incidentes registrados.\nLa red está limpia.";
			}
			else
			{
				lista.Text = string.Join("\n\n", incidentes.Select(r =>
					$"[{r.Hora:HH:mm:ss}] INFECCIÓN ({r.Decision})\n   {r.Nombre} - {r.IP}\n   MAC {r.MAC}"));
			}
		});

		return Margen(raiz, 10);
	}

	// ------------------------------------------------------------------
	// Logs
	// ------------------------------------------------------------------

	private static Control Logs(DatosEscritorio d)
	{
		VBoxContainer raiz = Columna(8);
		raiz.AddChild(Txt("REGISTRO DE CONEXIONES", 15, RedFlagUI.Acento));

		Label lista = Txt("", 13, RedFlagUI.Texto, true);
		PanelContainer marco = Marco(Desplazable(lista), 10);
		ExpandirTodo(marco);
		raiz.AddChild(marco);

		Suscribir(raiz, d, () =>
		{
			string conexiones = string.Join("\n", d.Historial
				.OrderByDescending(r => r.Hora).Take(80)
				.Select(r => $"[{r.Hora:HH:mm:ss}] {r.Decision,-12} {r.Nombre,-16} {r.IP}"));
			string eventos = string.Join("\n", d.HistorialEmergencias
				.AsEnumerable().Reverse().Take(30));
			lista.Text = "EMERGENCIAS\n" +
				(eventos.Length == 0 ? "Todavía no hay correos urgentes." : eventos) +
				"\n\nCONEXIONES\n" +
				(conexiones.Length == 0 ? "Sin actividad registrada todavía." : conexiones);
		});

		return Margen(raiz, 10);
	}

	// ------------------------------------------------------------------
	// Correo interno
	// ------------------------------------------------------------------

	private static Control Correo(DatosEscritorio d)
	{
		// Los mensajes comunes siguen disponibles; arriba aparecen las urgencias.
		string[][] correos =
		{
			new[] { "Seguridad IT", "Cómo reconocer conexiones falsas",
				"Verificá las IP internas (192.168.x.x), los nombres de empleados y las direcciones MAC.\n\nAnte un mensaje sospechoso, evitá abrir enlaces externos." },
			new[] { "Recursos Humanos", "Reunión general - 26/05",
				"Se recuerda al personal la reunión general en la sala principal." },
			new[] { "Soporte", "Nueva política de contraseñas",
				"Desde esta semana las contraseñas deben tener al menos 12 caracteres." },
			new[] { "Administración", "Turno de guardia",
				"Las solicitudes sin responder se aceptan automáticamente.\n\nAtendé los correos de emergencia antes de que termine su cuenta regresiva." }
		};

		int seleccionado = 0; // -1 = correo urgente
		HBoxContainer raiz = Fila(8);
		raiz.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		VBoxContainer bandeja = Columna(5);
		bandeja.CustomMinimumSize = new Vector2(175, 0);
		bandeja.AddChild(Txt("BANDEJA DE ENTRADA", 12, RedFlagUI.Acento));
		Button urgente = RedFlagUI.CrearBoton("SIN EMERGENCIAS", new Vector2(0, 43));
		urgente.ClipText = true;
		urgente.AddThemeFontSizeOverride("font_size", 12);
		bandeja.AddChild(urgente);

		VBoxContainer lista = Columna(4);
		ScrollContainer listaScroll = Desplazable(lista);
		bandeja.AddChild(listaScroll);
		PanelContainer panelBandeja = Marco(bandeja, 8);
		panelBandeja.CustomMinimumSize = new Vector2(195, 0);
		panelBandeja.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		raiz.AddChild(panelBandeja);

		VBoxContainer lectura = Columna(6);
		lectura.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		lectura.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		Label de = Txt("", 12, RedFlagUI.TextoApagado);
		Label asunto = Txt("", 15, RedFlagUI.Acento, true);
		lectura.AddChild(de);
		lectura.AddChild(asunto);

		Label cuerpo = Txt("", 13, RedFlagUI.Texto, true);
		cuerpo.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		ScrollContainer scroll = Desplazable(cuerpo);
		scroll.CustomMinimumSize = new Vector2(0, 95);
		lectura.AddChild(scroll);

		Label tiempo = Txt("", 13, RedFlagUI.Rojo);
		ProgressBar barra = CrearBarra(RedFlagUI.Rojo);
		barra.CustomMinimumSize = new Vector2(0, 10);
		Label resultado = Txt("", 12, RedFlagUI.TextoApagado, true);
		lectura.AddChild(tiempo);
		lectura.AddChild(barra);
		lectura.AddChild(resultado);

		HBoxContainer respuestaNumero = Fila(6);
		LineEdit entrada = new LineEdit
		{
			PlaceholderText = "Resultado numérico",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		EstilizarEntrada(entrada);
		Button enviar = RedFlagUI.CrearBoton("RESPONDER", new Vector2(105, 34), true);
		enviar.AddThemeFontSizeOverride("font_size", 12);
		respuestaNumero.AddChild(entrada);
		respuestaNumero.AddChild(enviar);
		lectura.AddChild(respuestaNumero);

		HBoxContainer opciones = Fila(4);
		Button abrirEnlace = RedFlagUI.CrearBoton("ABRIR ENLACE", new Vector2(0, 32));
		Button reportar = RedFlagUI.CrearBoton("REPORTAR PHISHING", new Vector2(0, 32));
		Button verificar = RedFlagUI.CrearBoton("VERIFICAR EN IT", new Vector2(0, 32), true);
		foreach (Button b in new[] { abrirEnlace, reportar, verificar })
		{
			b.AddThemeFontSizeOverride("font_size", 10);
			b.ClipText = true;
			b.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			opciones.AddChild(b);
		}
		lectura.AddChild(opciones);
		PanelContainer panelLectura = Marco(lectura, 9);
		ExpandirTodo(panelLectura);
		raiz.AddChild(panelLectura);

		// Una única función devuelve la elección a ComputerGameplay.
		void Responder(string valor)
		{
			if (d.CorreoActual == null || !d.CorreoActual.Pendiente) return;
			d.ResponderCorreo?.Invoke(valor);
			entrada.Text = "";
			ActualizarVista();
		}
		enviar.Pressed += () => Responder(entrada.Text.Trim());
		entrada.TextSubmitted += (string texto) => Responder(texto.Trim());
		abrirEnlace.Pressed += () => Responder("ABRIR");
		reportar.Pressed += () => Responder("REPORTAR");
		verificar.Pressed += () => Responder("VERIFICAR");

		void ActualizarVista()
		{
			CorreoEmergencia e = d.CorreoActual;
			urgente.Disabled = e == null;
			urgente.Text = e == null ? "SIN EMERGENCIAS" :
				(e.Pendiente ? "!! URGENTE - " + Math.Ceiling(e.TiempoRestante) + "s" : "ÚLTIMA EMERGENCIA");
			bool esUrgente = seleccionado == -1 && e != null;
			if (esUrgente)
			{
				de.Text = "DE: " + e.Remitente;
				asunto.Text = e.Asunto;
				cuerpo.Text = e.Cuerpo;
				tiempo.Text = e.Pendiente ? "TIEMPO: " + Math.Ceiling(e.TiempoRestante) + " SEGUNDOS" : "EVENTO FINALIZADO";
				barra.Visible = e.Pendiente;
				barra.Value = e.TiempoTotal > 0 ? 100 * e.TiempoRestante / e.TiempoTotal : 0;
				resultado.Text = e.Pendiente ? "Respondé antes de que termine el tiempo." : e.Resultado;
				respuestaNumero.Visible = e.Pendiente &&
					e.Tipo != TipoCorreo.Phishing &&
					e.Tipo != TipoCorreo.CodigoCalculadora;
				opciones.Visible = e.Pendiente && e.Tipo == TipoCorreo.Phishing;
			}
			else
			{
				int i = Math.Max(0, seleccionado);
				de.Text = "DE: " + correos[i][0];
				asunto.Text = correos[i][1];
				cuerpo.Text = correos[i][2];
				tiempo.Text = "";
				barra.Visible = false;
				resultado.Text = "";
				respuestaNumero.Visible = false;
				opciones.Visible = false;
			}
		}

		// Los controles de lectura ya están creados antes de conectar estos botones.
		foreach (int i in new[] { 0, 1, 2, 3 })
		{
			int indice = i;
			Button b = RedFlagUI.CrearBoton(correos[i][1], new Vector2(0, 33));
			b.ClipText = true;
			b.Alignment = HorizontalAlignment.Left;
			b.AddThemeFontSizeOverride("font_size", 11);
			b.Pressed += () => { seleccionado = indice; ActualizarVista(); };
			lista.AddChild(b);
		}

		urgente.Pressed += () => { seleccionado = -1; ActualizarVista(); };
		void AlCambiarCorreo()
		{
			if (d.CorreoActual != null && d.CorreoActual.Pendiente && seleccionado != -1)
				seleccionado = -1;
			ActualizarVista();
		}
		d.CorreoCambio += AlCambiarCorreo;
		raiz.TreeExiting += () => d.CorreoCambio -= AlCambiarCorreo;
		if (d.CorreoActual != null) seleccionado = -1;
		ActualizarVista();
		return Margen(raiz, 7);
	}

	// ------------------------------------------------------------------
	// Chat
	// ------------------------------------------------------------------

	private static Control Chat(DatosEscritorio d)
	{
		if (d.Chat.Count == 0)
			d.Chat.Add("Soporte IT: Hola, ¿necesitás ayuda con algo durante tu turno?");

		string[] respuestas =
		{
			"Recibido. Revisá bien la IP y el nombre antes de aceptar.",
			"Todo en orden por acá. Seguimos monitoreando.",
			"Si dudás de una conexión, mejor rechazala.",
			"Anotado. Cualquier novedad te aviso por correo."
		};

		VBoxContainer raiz = Columna(8);

		Label mensajes = Txt("", 14, RedFlagUI.Texto, true);
		ScrollContainer scroll = Desplazable(mensajes);
		PanelContainer marco = Marco(scroll, 10);
		ExpandirTodo(marco);
		raiz.AddChild(marco);

		HBoxContainer envio = Fila(8);
		LineEdit entrada = new LineEdit
		{
			PlaceholderText = "Escribí un mensaje...",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		EstilizarEntrada(entrada);
		Button enviar = RedFlagUI.CrearBoton("ENVIAR", new Vector2(90, 36), true);
		envio.AddChild(entrada);
		envio.AddChild(enviar);
		raiz.AddChild(envio);

		void Refrescar()
		{
			mensajes.Text = string.Join("\n\n", d.Chat);
			scroll.SetDeferred("scroll_vertical", 100000);
		}

		void Enviar()
		{
			string texto = entrada.Text.Trim();
			if (texto.Length == 0) return;

			d.Chat.Add("Tú: " + texto);
			d.Chat.Add("Soporte IT: " + respuestas[d.Chat.Count % respuestas.Length]);
			entrada.Text = "";
			Refrescar();
		}

		enviar.Pressed += Enviar;
		entrada.TextSubmitted += (string _) => Enviar();

		Refrescar();
		return Margen(raiz, 10);
	}

	// ------------------------------------------------------------------
	// Firewall
	// ------------------------------------------------------------------

	private static Control Firewall(DatosEscritorio d)
	{
		VBoxContainer raiz = Columna(10);
		raiz.AddChild(Txt("FIREWALL: ACTIVO", 20, new Color("4dd0a0")));

		VBoxContainer reglas = Columna(6);
		reglas.AddChild(Txt("Puerto 22   (SSH)      BLOQUEADO", 14, new Color("ff6b63")));
		reglas.AddChild(Txt("Puerto 23   (Telnet)   BLOQUEADO", 14, new Color("ff6b63")));
		reglas.AddChild(Txt("Puerto 80   (HTTP)     FILTRADO", 14, new Color("ffb347")));
		reglas.AddChild(Txt("Puerto 443  (HTTPS)    PERMITIDO", 14, new Color("6fe3b0")));
		reglas.AddChild(Txt("Puerto 3389 (RDP)      BLOQUEADO", 14, new Color("ff6b63")));
		raiz.AddChild(Marco(reglas, 14));

		Label bloqueadas = Txt("", 14, RedFlagUI.TextoApagado);
		Label escudo = Txt("", 13, RedFlagUI.Acento);
		Label infeccion = Txt("", 13, RedFlagUI.Rojo);
		raiz.AddChild(bloqueadas);
		raiz.AddChild(escudo);
		raiz.AddChild(infeccion);

		Suscribir(raiz, d, () =>
		{
			bloqueadas.Text = $"Amenazas bloqueadas en este turno: {d.AmenazasBloqueadas}";
			escudo.Text = $"Protecciones extra activas: {d.ProteccionesFirewall}";
			infeccion.Text = d.InversionBotones > 0
				? $"ALERTA: botones invertidos ({d.InversionBotones})" : "";
		});

		return Margen(raiz, 12);
	}

	// ------------------------------------------------------------------
	// Antivirus
	// ------------------------------------------------------------------

	private static Control Antivirus(DatosEscritorio d)
	{
		VBoxContainer raiz = Columna(12);
		raiz.AddChild(Txt("ANTIVIRUS RED FLAG", 18, new Color("4dd0a0")));

		Label estado = Txt("Listo para analizar el sistema.", 14, RedFlagUI.TextoApagado, true);
		ProgressBar barra = CrearBarra(new Color("39b7cc"));
		Button escanear = RedFlagUI.CrearBoton("ESCANEAR SISTEMA", new Vector2(0, 42), true);

		escanear.Pressed += () =>
		{
			escanear.Disabled = true;
			barra.Value = 0;
			estado.Text = "Analizando archivos y conexiones...";

			Tween tween = escanear.CreateTween();
			tween.TweenProperty(barra, "value", 100.0, 3.0);
			tween.TweenCallback(Callable.From(() =>
			{
				int virus = d.VirusAceptados;
				estado.Text = virus == 0
					? "Análisis completo. Sistema limpio: no se detectaron amenazas activas."
					: $"Análisis completo. Se detectaron {virus} amenaza(s) activa(s) por conexiones aceptadas.";
				escanear.Disabled = false;
			}));
		};

		Label escaneoEmergencia = Txt("", 12, RedFlagUI.Acento, true);
		Suscribir(raiz, d, () => escaneoEmergencia.Text = d.EstadoAntivirus);
		raiz.AddChild(estado);
		raiz.AddChild(barra);
		raiz.AddChild(escanear);
		raiz.AddChild(escaneoEmergencia);
		return Margen(raiz, 16);
	}

	// ------------------------------------------------------------------
	// Cámaras
	// ------------------------------------------------------------------

	private static Control Camaras()
	{
		GridContainer grilla = new GridContainer { Columns = 2 };
		grilla.AddThemeConstantOverride("h_separation", 10);
		grilla.AddThemeConstantOverride("v_separation", 10);
		ExpandirTodo(grilla);

		string[] nombres =
		{
			"CAM 01 - Oficina principal", "CAM 02 - Pasillo",
			"CAM 03 - Sala de servidores", "CAM 04 - Entrada"
		};

		for (int i = 0; i < nombres.Length; i++)
		{
			bool sinSenal = i == 3;

			PanelContainer celda = new PanelContainer
			{
				CustomMinimumSize = new Vector2(280, 140),
				ClipContents = true
			};
			ExpandirTodo(celda);
			celda.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(new Color("02080c"), RedFlagUI.Borde, 1, 2));

			celda.AddChild(new RuidoGlitch
			{
				ColorBase = sinSenal ? new Color(0.7f, 0.75f, 0.8f) : new Color(0.25f, 0.85f, 0.75f),
				Densidad = sinSenal ? 0.5f : 0.12f
			});

			VBoxContainer capa = Columna(4);
			capa.AddChild(Txt(nombres[i], 12, RedFlagUI.Texto));
			Control espacio = new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
			capa.AddChild(espacio);
			if (sinSenal)
				capa.AddChild(Txt("SIN SEÑAL", 16, new Color("ff6b63"), false, HorizontalAlignment.Center));
			capa.AddChild(Txt(sinSenal ? "" : "REC", 12, new Color("ff4a52")));

			celda.AddChild(Margen(capa, 8));
			grilla.AddChild(celda);
		}

		return Margen(grilla, 10);
	}

	// ------------------------------------------------------------------
	// Notas
	// ------------------------------------------------------------------

	private static Control Notas(DatosEscritorio d)
	{
		TextEdit editor = new TextEdit
		{
			Text = d.TextoNotas,
			PlaceholderText = "Escribí tus notas acá...",
			WrapMode = TextEdit.LineWrappingMode.Boundary
		};
		ExpandirTodo(editor);
		editor.AddThemeStyleboxOverride("normal", RedFlagUI.Caja(RedFlagUI.FondoPanel, RedFlagUI.Borde, 1, 3, 8));
		editor.AddThemeStyleboxOverride("focus", RedFlagUI.Caja(RedFlagUI.FondoPanel, RedFlagUI.BordeVivo, 1, 3, 8));
		editor.AddThemeFontOverride("font", RedFlagUI.Fuente);
		editor.AddThemeFontSizeOverride("font_size", 15);
		editor.AddThemeColorOverride("font_color", RedFlagUI.Texto);
		editor.AddThemeColorOverride("font_placeholder_color", RedFlagUI.TextoApagado);
		editor.AddThemeColorOverride("caret_color", RedFlagUI.BordeVivo);
		editor.TextChanged += () => d.TextoNotas = editor.Text;

		return Margen(editor, 10);
	}

	// ------------------------------------------------------------------
	// Calculadora
	// ------------------------------------------------------------------

	private static Control Calculadora(DatosEscritorio d)
	{
		string pantalla = "0";
		double acumulado = 0;
		string operacion = null;
		bool nuevaEntrada = true;

		Label visor = Txt("0", 28, RedFlagUI.Texto, false, HorizontalAlignment.Right);

		double Leer()
		{
			return double.TryParse(pantalla, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
		}

		string Formatear(double v)
		{
			if (double.IsNaN(v) || double.IsInfinity(v)) return "Error";
			return v.ToString("0.########", CultureInfo.InvariantCulture);
		}

		double Calcular(double a, double b, string op)
		{
			switch (op)
			{
				case "+": return a + b;
				case "-": return a - b;
				case "×": return a * b;
				case "÷": return b == 0 ? double.NaN : a / b;
				default: return b;
			}
		}

		void Presionar(string t)
		{
			if (t.Length == 1 && char.IsDigit(t[0]))
			{
				if (nuevaEntrada || pantalla == "0" || pantalla == "Error") pantalla = t;
				else if (pantalla.Length < 14) pantalla += t;
				nuevaEntrada = false;
			}
			else if (t == ".")
			{
				if (nuevaEntrada || pantalla == "Error") { pantalla = "0."; nuevaEntrada = false; }
				else if (!pantalla.Contains('.')) pantalla += ".";
			}
			else if (t == "C")
			{
				pantalla = "0";
				acumulado = 0;
				operacion = null;
				nuevaEntrada = true;
			}
			else if (t == "=")
			{
				if (operacion != null)
				{
					pantalla = Formatear(Calcular(acumulado, Leer(), operacion));
					operacion = null;
					nuevaEntrada = true;
				}

				// Enviar el número al juego cuando se presiona =
				d.CodigoDesdeCalculadora?.Invoke(pantalla);
			}
			else
			{
				if (operacion != null && !nuevaEntrada)
				{
					double r = Calcular(acumulado, Leer(), operacion);
					acumulado = r;
					pantalla = Formatear(r);
				}
				else
				{
					acumulado = Leer();
				}
				operacion = t;
				nuevaEntrada = true;
			}

			visor.Text = pantalla;
		}

		VBoxContainer raiz = Columna(8);
		raiz.AddChild(Marco(visor, 10));

		GridContainer teclas = new GridContainer { Columns = 4 };
		teclas.AddThemeConstantOverride("h_separation", 6);
		teclas.AddThemeConstantOverride("v_separation", 6);
		ExpandirTodo(teclas);

		string[] orden = { "7", "8", "9", "÷", "4", "5", "6", "×", "1", "2", "3", "-", "0", ".", "=", "+" };
		foreach (string t in orden)
		{
			bool operador = t == "÷" || t == "×" || t == "-" || t == "+" || t == "=";
			Button b = RedFlagUI.CrearBoton(t, new Vector2(56, 48), operador);
			ExpandirTodo(b);
			b.Pressed += () => Presionar(t);
			teclas.AddChild(b);
		}
		raiz.AddChild(teclas);

		Button borrar = RedFlagUI.CrearBoton("C", new Vector2(0, 40));
		borrar.Pressed += () => Presionar("C");
		raiz.AddChild(borrar);

		return Margen(raiz, 10);
	}

	// ------------------------------------------------------------------
	// Papelera
	// ------------------------------------------------------------------

	private static Control Papelera()
	{
		VBoxContainer raiz = Columna(10);
		raiz.AddChild(Txt("PAPELERA", 16, RedFlagUI.TextoApagado));
		Label vacia = Txt("La papelera está vacía.\n0 elementos", 14, RedFlagUI.TextoApagado);
		PanelContainer marco = Marco(vacia, 16);
		ExpandirTodo(marco);
		raiz.AddChild(marco);
		return Margen(raiz, 10);
	}
}
