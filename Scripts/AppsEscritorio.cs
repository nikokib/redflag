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
		new AppDef("navegador", "Navegador", "WWW", "39b7cc", 700, 460),
		new AppDef("redflags", "Red Flags", "RF", "ff3b34", 660, 400),
		new AppDef("incidentes", "Incidentes", "INC", "ffb347", 520, 340),
		new AppDef("logs", "Logs", "LOG", "8ec07c", 580, 380),
		new AppDef("correo", "Correo Interno", "@", "d6c7a1", 720, 470),
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
	// Navegador (intranet SecureNet) - con buscador y secciones que funcionan
	// ------------------------------------------------------------------

	private class ArticuloIntranet
	{
		public string Id;
		public string Seccion;   // noticias, documentos, recursos o soporte
		public string Titulo;
		public string Resumen;
		public string Claves;    // palabras para el buscador
		public string Cuerpo;
	}

	private static ArticuloIntranet Art(string id, string seccion, string titulo, string resumen, string claves, string cuerpo)
	{
		return new ArticuloIntranet { Id = id, Seccion = seccion, Titulo = titulo, Resumen = resumen, Claves = claves, Cuerpo = cuerpo };
	}

	private static readonly ArticuloIntranet[] ArticulosIntranet =
	{
		Art("virus", "documentos", "Qué hacer si detectás un virus",
			"Pasos para frenar una conexión infectada.",
			"virus infectado infeccion malware arreglar eliminar sacar remover solucionar amenaza",
			"Un virus entra a la red cuando se acepta una conexión infectada.\n\n" +
			"1. Antes de aceptar, revisá nombre, IP y MAC de la solicitud.\n" +
			"2. Si algo no coincide, RECHAZÁ la conexión.\n" +
			"3. Agregala a la blacklist para reconocerla la próxima vez.\n" +
			"4. Si ya entró, mirá la sección Logs e Incidentes para ver qué pasó.\n" +
			"5. Cuando llegue un correo del Antivirus, resolvelo rápido: analiza conexiones por vos.\n\n" +
			"Recordá: aceptar una conexión infectada suma errores y puede terminar el turno."),
		Art("reconocer", "documentos", "Cómo reconocer conexiones falsas",
			"Qué mirar en el nombre, la IP y la MAC.",
			"conexion falsa reconocer detectar pista ip mac nombre sospechoso infectada identificar revisar",
			"Una conexión limpia siempre cumple estas reglas:\n\n" +
			"NOMBRE: nombre y apellido normales, sin números ni símbolos. Ojo con letras cambiadas (por ejemplo una i por una l) y con usuarios de sistema como root_admin o guest_01.\n\n" +
			"IP: debe empezar con 192.168 y los dos últimos números van de 0 a 255 (el último, de 1 a 254). Una IP como 172.x.x.x, 10.x.x.x o con un número mayor a 255 es sospechosa.\n\n" +
			"MAC: seis pares de letras y números separados por \":\". Desconfiá de MAC como 00:00:00:... o DE:AD:BE:EF:...\n\n" +
			"Área y rol: si dice \"Desconocido\", es una muy mala señal."),
		Art("antivirus", "documentos", "Guía del Antivirus",
			"Cómo se activa y qué hace por vos.",
			"antivirus analisis escaneo proteccion codigo desbloqueo seguridad activar",
			"El Antivirus se activa con un correo de emergencia.\n\n" +
			"• Abrí el correo y resolvé la suma que aparece resaltada.\n" +
			"• Si acertás, el antivirus revisa y resuelve conexiones correctamente por vos.\n" +
			"• Si fallás, perdés puntos y se acorta el tiempo de la solicitud actual.\n\n" +
			"Podés ver el último análisis en la app Antivirus del escritorio."),
		Art("phishing", "documentos", "Protocolo contra el phishing",
			"Cómo distinguir una alerta real de un engaño.",
			"phishing correo falso engano estafa dominio enlace link reportar verificar sospechoso mail",
			"El phishing es un correo falso que intenta que abras un enlace o entregues datos.\n\n" +
			"1. Mirá el DOMINIO del remitente.\n" +
			"2. Si es securenet.local (interno), la alerta puede ser real: elegí VERIFICAR EN IT.\n" +
			"3. Si el dominio es externo (por ejemplo securenet-soporte.com) o apura con amenazas, es un engaño: elegí REPORTAR PHISHING.\n" +
			"4. Nunca uses ABRIR ENLACE: es la opción peligrosa.\n\n" +
			"El tiempo para decidir depende de la dificultad: cuanto más difícil, menos segundos tenés."),
		Art("autorizacion", "documentos", "Autorización masiva: cómo cancelarla",
			"Resolver la cuenta del correo urgente.",
			"autorizacion masiva cuenta matematica calcular multiplicar restar orden operaciones correo urgente",
			"Si llega un correo de autorización masiva, tenés que cancelar la orden resolviendo la cuenta.\n\n" +
			"Se resuelve en orden: primero la MULTIPLICACIÓN y después la resta.\n" +
			"Ejemplo: 6 x 7 - 5 = 42 - 5 = 37.\n\n" +
			"Escribí el resultado y presioná RESPONDER. Si fallás o se acaba el tiempo, se autoaceptan solicitudes sin revisar."),
		Art("calculadora", "documentos", "Usar la Calculadora con códigos",
			"Cómo ingresar un código de recuperación.",
			"calculadora codigo recuperar puntos numero igual limpiar teclas",
			"Cuando el correo pida un código:\n\n" +
			"1. Abrí la CALCULADORA del escritorio.\n" +
			"2. Si hay otro número en pantalla, presioná C para limpiar.\n" +
			"3. Ingresá el código resaltado en el correo.\n" +
			"4. Presioná = para confirmar.\n\n" +
			"Correcto: ganás puntos. Incorrecto o fuera de tiempo: perdés puntos."),
		Art("firewall", "recursos", "Firewall: cómo funciona",
			"Bloqueos automáticos contra conexiones infectadas.",
			"firewall bloqueo proteccion cortafuegos red defensa bloquear",
			"El firewall puede quedar protegido después de resolver bien una emergencia.\n\n" +
			"Mientras tenga bloqueos disponibles, bloquea automáticamente conexiones infectadas. Ves cuántos te quedan en el aviso del escritorio y en la app Firewall."),
		Art("listas", "recursos", "Whitelist y blacklist",
			"Para qué sirven las listas de conexiones.",
			"whitelist blacklist lista blanca negra confiables bloqueadas aprobar rechazar",
			"WHITELIST: conexiones que ya revisaste y son confiables.\nBLACKLIST: conexiones infectadas que ya detectaste.\n\n" +
			"Desde la app Red Flag podés agregar la solicitud actual a una de las dos. Te sirve para reconocerlas rápido si vuelven a aparecer."),
		Art("guia", "recursos", "Guía de seguridad para empleados",
			"Buenas prácticas del turno.",
			"guia seguridad empleados buenas practicas turno consejos basico",
			"• Revisá siempre nombre, IP y MAC antes de aceptar.\n• Ante la duda, rechazá.\n• Las solicitudes sin responder se aceptan solas: no te quedes mirando.\n• Atendé los correos de emergencia antes de que termine la cuenta regresiva.\n• No compartas credenciales ni abras enlaces externos."),
		Art("actualizacion", "noticias", "Actualización de seguridad - 24/05",
			"Se reforzaron las reglas de detección.",
			"actualizacion seguridad parche novedad deteccion",
			"Se actualizaron las reglas de detección de conexiones sospechosas. Ahora hay que prestar más atención a los detalles del nombre, la IP y la MAC."),
		Art("reunion", "noticias", "Reunión general - 26/05",
			"Se recuerda al personal la reunión en la sala principal.",
			"reunion general sala principal personal recordatorio",
			"Se recuerda al personal la reunión general en la sala principal. La asistencia es obligatoria para todo el turno."),
		Art("noticia-firewall", "noticias", "Se reforzó el firewall perimetral",
			"Mejoras en el bloqueo de intrusos.",
			"firewall perimetral reforzado novedad intrusos",
			"El equipo de redes reforzó el firewall perimetral. Cuando respondés bien una emergencia, el firewall puede bloquear conexiones infectadas por vos."),
		Art("noticia-phishing", "noticias", "Aumentaron los intentos de phishing",
			"Cuidado con los correos que apuran.",
			"phishing aumento intentos correos falsos alerta",
			"Esta semana aumentaron los correos falsos con amenazas y dominios externos. Revisá siempre el dominio del remitente y reportá lo sospechoso."),
		Art("contrasenas", "soporte", "Nueva política de contraseñas",
			"Mínimo de 12 caracteres.",
			"contrasena password clave politica caracteres seguridad cambiar",
			"Desde esta semana las contraseñas deben tener al menos 12 caracteres, con letras, números y símbolos. No las compartas ni las anotes en lugares visibles."),
		Art("mesa", "soporte", "Mesa de ayuda interna",
			"Contactos del equipo de soporte.",
			"soporte ayuda mesa contacto telefono interno mail problema",
			"Mesa de ayuda interna: int. 4400\nHorario: 24/7 durante tu turno\nCorreo: soporte@securenet.local\n\nSi dudás de un correo o de una conexión, consultá antes de actuar.")
	};

	private static readonly string[] PalabrasVacias =
	{
		"como", "que", "para", "por", "con", "los", "las", "una", "uno", "del", "mis", "hay", "esta", "este"
	};

	private static string Normalizar(string texto)
	{
		string s = (texto ?? "").ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		foreach (char c in s)
		{
			if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
				sb.Append(c);
		}
		return sb.ToString();
	}

	private static System.Collections.Generic.List<ArticuloIntranet> BuscarArticulos(string consulta)
	{
		string[] palabras = Normalizar(consulta).Split(
			new[] { ' ', ',', '.', '?', '¿', '!', '¡', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);

		System.Collections.Generic.List<ArticuloIntranet> articulos = new System.Collections.Generic.List<ArticuloIntranet>();
		System.Collections.Generic.List<int> puntajes = new System.Collections.Generic.List<int>();

		foreach (ArticuloIntranet a in ArticulosIntranet)
		{
			string titulo = Normalizar(a.Titulo);
			string claves = Normalizar(a.Claves);
			string texto = Normalizar(a.Resumen + " " + a.Cuerpo);
			int puntos = 0;

			foreach (string p in palabras)
			{
				if (p.Length < 3 || Array.IndexOf(PalabrasVacias, p) >= 0) continue;
				if (titulo.Contains(p)) puntos += 5;
				if (claves.Contains(p)) puntos += 4;
				if (texto.Contains(p)) puntos += 1;
			}

			if (puntos <= 0) continue;

			// Orden de mayor a menor puntaje
			int lugar = 0;
			while (lugar < puntajes.Count && puntajes[lugar] >= puntos) lugar++;
			articulos.Insert(lugar, a);
			puntajes.Insert(lugar, puntos);
		}

		return articulos;
	}

	private static ArticuloIntranet BuscarArticuloPorId(string id)
	{
		foreach (ArticuloIntranet a in ArticulosIntranet)
			if (a.Id == id) return a;
		return null;
	}

	private static Button BotonNavegador(IconoSistema.Clase icono)
	{
		Button b = new Button { CustomMinimumSize = new Vector2(36, 34), FocusMode = Control.FocusModeEnum.None };
		RedFlagUI.EstilizarBoton(b, false);
		IconoSistema i = IconoSistema.Crear(icono, 18, RedFlagUI.BordeVivo);
		i.Position = new Vector2(9, 8);
		i.Size = new Vector2(18, 18);
		b.AddChild(i);
		return b;
	}

	private static Control Navegador()
	{
		System.Collections.Generic.List<string> historial = new System.Collections.Generic.List<string> { "inicio" };
		int pos = 0;

		VBoxContainer raiz = Columna(8);

		// --- Barra: atrás, adelante, recargar, dirección y buscador ---
		HBoxContainer barra = Fila(6);
		Button atras = BotonNavegador(IconoSistema.Clase.FlechaIzq);
		Button adelante = BotonNavegador(IconoSistema.Clase.FlechaDer);
		Button recargar = BotonNavegador(IconoSistema.Clase.Reiniciar);
		LineEdit url = new LineEdit { Editable = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(120, 0) };
		EstilizarEntrada(url);
		LineEdit busqueda = new LineEdit
		{
			PlaceholderText = "Buscar: virus, phishing, firewall...",
			CustomMinimumSize = new Vector2(230, 0)
		};
		EstilizarEntrada(busqueda);
		Button buscar = BotonNavegador(IconoSistema.Clase.Lupa);
		barra.AddChild(atras);
		barra.AddChild(adelante);
		barra.AddChild(recargar);
		barra.AddChild(url);
		barra.AddChild(busqueda);
		barra.AddChild(buscar);
		raiz.AddChild(barra);

		// --- Cuerpo: secciones a la izquierda, contenido a la derecha ---
		HBoxContainer cuerpo = Fila(10);
		cuerpo.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		raiz.AddChild(cuerpo);

		VBoxContainer menu = Columna(6);
		menu.CustomMinimumSize = new Vector2(125, 0);
		cuerpo.AddChild(menu);

		VBoxContainer contenido = Columna(10);
		contenido.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		ScrollContainer scroll = Desplazable(contenido);
		PanelContainer marco = Marco(scroll, 10);
		ExpandirTodo(marco);
		cuerpo.AddChild(marco);

		string[][] secciones =
		{
			new[] { "Inicio", "inicio" }, new[] { "Noticias", "noticias" }, new[] { "Documentos", "documentos" },
			new[] { "Recursos", "recursos" }, new[] { "Soporte", "soporte" }
		};
		foreach (string[] s in secciones)
		{
			string destino = s[1];
			Button b = RedFlagUI.CrearBoton(s[0], new Vector2(0, 38));
			b.Alignment = HorizontalAlignment.Left;
			b.Pressed += () => Ir(destino);
			menu.AddChild(b);
		}

		void Ir(string pagina)
		{
			if (pos < historial.Count - 1) historial.RemoveRange(pos + 1, historial.Count - pos - 1);
			historial.Add(pagina);
			pos = historial.Count - 1;
			Mostrar(pagina);
		}

		void Buscar(string texto)
		{
			string t = (texto ?? "").Trim();
			if (t.Length == 0) return;
			Ir("buscar:" + t);
		}

		Button Enlace(string texto, string destino)
		{
			Button b = new Button
			{
				Text = texto,
				Flat = true,
				Alignment = HorizontalAlignment.Left,
				FocusMode = Control.FocusModeEnum.None,
				MouseDefaultCursorShape = Control.CursorShape.PointingHand,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			b.AddThemeFontOverride("font", RedFlagUI.Fuente);
			b.AddThemeFontSizeOverride("font_size", 16);
			b.AddThemeColorOverride("font_color", new Color("6fd3e8"));
			b.AddThemeColorOverride("font_hover_color", Colors.White);
			b.AddThemeColorOverride("font_pressed_color", Colors.White);
			b.Pressed += () => Ir(destino);
			return b;
		}

		void AgregarArticulo(ArticuloIntranet a)
		{
			contenido.AddChild(Enlace("> " + a.Titulo, "art:" + a.Id));
			contenido.AddChild(Txt("    " + a.Resumen, 13, RedFlagUI.TextoApagado, true));
		}

		void Mostrar(string pagina)
		{
			foreach (Node n in contenido.GetChildren())
			{
				contenido.RemoveChild(n);
				n.QueueFree();
			}

			atras.Disabled = pos <= 0;
			adelante.Disabled = pos >= historial.Count - 1;
			url.Text = "https://intranet.securenet.local/" + pagina.Replace(':', '/').Replace(' ', '-');

			if (pagina == "inicio")
			{
				contenido.AddChild(Txt("SECURENET", 32, new Color("6fd3e8")));
				contenido.AddChild(Txt("INTRANET CORPORATIVA", 13, RedFlagUI.TextoApagado));

				VBoxContainer avisos = Columna(4);
				avisos.AddChild(Txt("AVISOS IMPORTANTES", 17, RedFlagUI.Texto));
				avisos.AddChild(Enlace("- Actualización de seguridad - 24/05", "art:actualizacion"));
				avisos.AddChild(Enlace("- Reunión general - 26/05", "art:reunion"));
				avisos.AddChild(Enlace("- Nueva política de contraseñas", "art:contrasenas"));
				contenido.AddChild(Marco(avisos, 12));

				VBoxContainer ayuda = Columna(4);
				ayuda.AddChild(Txt("AYUDA RÁPIDA", 17, RedFlagUI.Acento));
				ayuda.AddChild(Enlace("- ¿Qué hago si detecto un virus?", "art:virus"));
				ayuda.AddChild(Enlace("- ¿Cómo reconozco una conexión falsa?", "art:reconocer"));
				ayuda.AddChild(Enlace("- ¿Cómo detecto un correo de phishing?", "art:phishing"));
				ayuda.AddChild(Enlace("- Cómo funciona el antivirus", "art:antivirus"));
				contenido.AddChild(Marco(ayuda, 12));

				contenido.AddChild(Txt("Tip: usá el buscador de arriba (por ejemplo \"virus\" o \"contraseña\").", 13, RedFlagUI.TextoApagado, true));
			}
			else if (pagina.StartsWith("art:"))
			{
				ArticuloIntranet a = BuscarArticuloPorId(pagina.Substring(4));
				if (a == null)
				{
					contenido.AddChild(Txt("Página no encontrada.", 18, RedFlagUI.Rojo));
				}
				else
				{
					contenido.AddChild(Txt(a.Seccion.ToUpper(), 12, RedFlagUI.TextoApagado));
					contenido.AddChild(Txt(a.Titulo, 22, new Color("6fd3e8"), true));
					contenido.AddChild(Marco(Txt(a.Cuerpo, 15, RedFlagUI.Texto, true), 12));
					contenido.AddChild(Enlace("< Volver al inicio", "inicio"));
				}
			}
			else if (pagina.StartsWith("buscar:"))
			{
				string consulta = pagina.Substring(7);
				System.Collections.Generic.List<ArticuloIntranet> resultados = BuscarArticulos(consulta);
				contenido.AddChild(Txt("RESULTADOS PARA \"" + consulta + "\"", 17, RedFlagUI.Acento, true));
				if (resultados.Count == 0)
				{
					contenido.AddChild(Txt("No se encontraron resultados.", 16, RedFlagUI.Texto));
					contenido.AddChild(Txt("Probá con: virus, antivirus, phishing, firewall, contraseña, calculadora.", 13, RedFlagUI.TextoApagado, true));
				}
				else
				{
					contenido.AddChild(Txt(resultados.Count + " resultado(s)", 12, RedFlagUI.TextoApagado));
					foreach (ArticuloIntranet a in resultados) AgregarArticulo(a);
				}
			}
			else
			{
				string titulo = pagina == "noticias" ? "NOTICIAS" : pagina == "documentos" ? "DOCUMENTOS"
					: pagina == "recursos" ? "RECURSOS" : "SOPORTE";
				contenido.AddChild(Txt(titulo, 24, new Color("6fd3e8")));
				foreach (ArticuloIntranet a in ArticulosIntranet)
					if (a.Seccion == pagina) AgregarArticulo(a);
			}
		}

		atras.Pressed += () =>
		{
			if (pos <= 0) return;
			pos--;
			Mostrar(historial[pos]);
		};
		adelante.Pressed += () =>
		{
			if (pos >= historial.Count - 1) return;
			pos++;
			Mostrar(historial[pos]);
		};
		recargar.Pressed += () => Mostrar(historial[pos]);
		buscar.Pressed += () => Buscar(busqueda.Text);
		busqueda.TextSubmitted += (string texto) => Buscar(texto);

		Mostrar("inicio");
		return Margen(raiz, 8);
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
	// Correo interno (bandeja de entrada + lectura con la tarea resaltada)
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

		Color naranja = new Color("ffb347");
		Color amarillo = new Color("ffd166");
		Color celeste = new Color("6fd3e8");

		int seleccionado = 0; // -1 = correo urgente
		HBoxContainer raiz = Fila(10);
		raiz.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		// --- Bandeja de entrada ---
		VBoxContainer bandeja = Columna(6);
		bandeja.AddChild(Txt("BANDEJA DE ENTRADA", 14, RedFlagUI.Acento));

		Button urgente = new Button
		{
			CustomMinimumSize = new Vector2(0, 50),
			ClipText = true,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		urgente.AddThemeStyleboxOverride("normal", RedFlagUI.Caja(new Color("0b2a3a"), celeste, 2, 6, 6));
		urgente.AddThemeStyleboxOverride("hover", RedFlagUI.Caja(new Color("0f3a50"), Colors.White, 2, 6, 6));
		urgente.AddThemeStyleboxOverride("pressed", RedFlagUI.Caja(new Color("0f3a50"), Colors.White, 2, 6, 6));
		urgente.AddThemeStyleboxOverride("disabled", RedFlagUI.Caja(new Color("06141d"), RedFlagUI.Borde, 1, 6, 6));
		urgente.AddThemeStyleboxOverride("focus", RedFlagUI.Caja(new Color(0, 0, 0, 0), celeste, 2, 6, 6));
		urgente.AddThemeFontOverride("font", RedFlagUI.Fuente);
		urgente.AddThemeFontSizeOverride("font_size", 16);
		urgente.AddThemeColorOverride("font_color", Colors.White);
		urgente.AddThemeColorOverride("font_disabled_color", RedFlagUI.TextoApagado);
		bandeja.AddChild(urgente);

		VBoxContainer lista = Columna(5);
		ScrollContainer listaScroll = Desplazable(lista);
		bandeja.AddChild(listaScroll);
		PanelContainer panelBandeja = Marco(bandeja, 8);
		panelBandeja.CustomMinimumSize = new Vector2(215, 0);
		panelBandeja.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		raiz.AddChild(panelBandeja);

		// --- Lectura ---
		VBoxContainer lectura = Columna(7);
		lectura.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		lectura.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		Label de = Txt("", 12, RedFlagUI.TextoApagado);
		Label asunto = Txt("", 17, RedFlagUI.Texto, true);
		lectura.AddChild(de);
		lectura.AddChild(asunto);

		// Recuadro con lo importante que tiene que hacer el jugador
		VBoxContainer contenidoTarea = Columna(4);
		Label etiquetaTarea = Txt("TU TAREA", 12, naranja);
		Label textoTarea = Txt("", 28, amarillo, true, HorizontalAlignment.Center);
		Label ayudaTarea = Txt("", 13, RedFlagUI.Texto, true, HorizontalAlignment.Center);
		contenidoTarea.AddChild(etiquetaTarea);
		contenidoTarea.AddChild(textoTarea);
		contenidoTarea.AddChild(ayudaTarea);
		PanelContainer cajaTarea = new PanelContainer();
		cajaTarea.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(new Color("1c1405"), naranja, 2, 6, 10));
		cajaTarea.AddChild(contenidoTarea);
		lectura.AddChild(cajaTarea);

		Label cuerpo = Txt("", 13, RedFlagUI.TextoApagado, true);
		cuerpo.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		ScrollContainer scroll = Desplazable(cuerpo);
		scroll.CustomMinimumSize = new Vector2(0, 70);
		lectura.AddChild(scroll);

		Label tiempo = Txt("", 15, RedFlagUI.Rojo);
		ProgressBar barra = CrearBarra(RedFlagUI.Rojo);
		barra.CustomMinimumSize = new Vector2(0, 14);
		Label resultado = Txt("", 13, RedFlagUI.TextoApagado, true);
		lectura.AddChild(tiempo);
		lectura.AddChild(barra);
		lectura.AddChild(resultado);

		HBoxContainer respuestaNumero = Fila(6);
		LineEdit entrada = new LineEdit
		{
			PlaceholderText = "Resultado numérico",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 38)
		};
		EstilizarEntrada(entrada);
		Button enviar = RedFlagUI.CrearBoton("RESPONDER", new Vector2(120, 38), true);
		enviar.AddThemeFontSizeOverride("font_size", 13);
		respuestaNumero.AddChild(entrada);
		respuestaNumero.AddChild(enviar);
		lectura.AddChild(respuestaNumero);

		HBoxContainer opciones = Fila(5);
		Button abrirEnlace = RedFlagUI.CrearBoton("ABRIR ENLACE", new Vector2(0, 38));
		Button reportar = RedFlagUI.CrearBoton("REPORTAR PHISHING", new Vector2(0, 38));
		Button verificar = RedFlagUI.CrearBoton("VERIFICAR EN IT", new Vector2(0, 38), true);
		foreach (Button b in new[] { abrirEnlace, reportar, verificar })
		{
			b.AddThemeFontSizeOverride("font_size", 11);
			b.ClipText = true;
			b.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			opciones.AddChild(b);
		}
		lectura.AddChild(opciones);
		PanelContainer panelLectura = Marco(lectura, 10);
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

		// Busca en el texto del correo lo más importante para resaltarlo
		bool BuscarTarea(CorreoEmergencia e, out string destacado, out string ayuda)
		{
			destacado = "";
			ayuda = "";
			string patron;
			switch (e.Tipo)
			{
				case TipoCorreo.Matematica:
					patron = @"\d+\s*x\s*\d+\s*-\s*\d+\s*=\s*\?";
					ayuda = "Resolvé la cuenta y escribí el resultado.";
					break;
				case TipoCorreo.Antivirus:
					patron = @"\d+\s*\+\s*\d+\s*=\s*\?";
					ayuda = "Resolvé la suma y escribí el resultado.";
					break;
				case TipoCorreo.CodigoCalculadora:
					patron = @"C[ÓO]DIGO:\s*\d+";
					ayuda = "Abrí la CALCULADORA, ingresá el número y presioná =";
					break;
				default:
					patron = @"Dominio del remitente:[^\n]*";
					ayuda = "Fijate en el dominio del remitente y decidí qué hacer.";
					break;
			}
			System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(e.Cuerpo ?? "", patron);
			if (!m.Success) return false;
			destacado = m.Value.Trim();
			return true;
		}

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

				string destacado = "";
				string ayuda = "";
				bool hayTarea = e.Pendiente && BuscarTarea(e, out destacado, out ayuda);
				cajaTarea.Visible = hayTarea;
				if (hayTarea)
				{
					textoTarea.Text = destacado;
					ayudaTarea.Text = ayuda;
				}

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
				cajaTarea.Visible = false;
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
			Button b = RedFlagUI.CrearBoton(correos[i][1], new Vector2(0, 38));
			b.ClipText = true;
			b.Alignment = HorizontalAlignment.Left;
			b.AddThemeFontSizeOverride("font_size", 12);
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
		return Margen(raiz, 8);
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

			string rutaImagen = $"res://Interfaz/camaras/cam_{i + 1:00}.png";
			Texture2D imagen = ResourceLoader.Exists(rutaImagen) ? GD.Load<Texture2D>(rutaImagen) : null;

			if (imagen != null)
			{
				// Foto de la oficina con efecto glitch (el texto de la cámara lo dibuja el propio control)
				FeedCamara feed = new FeedCamara { Imagen = imagen, Titulo = nombres[i], SinSenal = sinSenal };
				ExpandirTodo(feed);
				celda.AddChild(feed);
				grilla.AddChild(celda);
				continue;
			}

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
