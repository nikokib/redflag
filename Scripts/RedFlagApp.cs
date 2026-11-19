using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ventana "REDS FLAGS OS v1.0": el centro de control del jugador.
/// Se abre al tocar el ícono Red Flag, el botón "RED FLAG" de la barra de tareas
/// o la notificación emergente, y aparece centrada en la pantalla.
/// Tiene una barra lateral con seis secciones (Dashboard, Solicitudes, Whitelist,
/// Blacklist, Red y Ajustes). Solo es interfaz: la lógica sigue en ComputerGameplay,
/// que le pasa los datos por DatosEscritorio y recibe Aceptado / Rechazado.
/// </summary>
public partial class RedFlagApp : Control
{
	public DatosEscritorio Datos;

	public event Action Aceptado;
	public event Action Rechazado;

	/// <summary>Se dispara cuando el jugador la minimiza o la cierra (la solicitud sigue pendiente).</summary>
	public event Action Oculta;

	private enum Seccion { Dashboard, Solicitudes, Whitelist, Blacklist, Red, Ajustes }

	private static readonly Vector2 TamVentana = new Vector2(660, 392);

	private static readonly Color Cian = new Color("39b7cc");
	private static readonly Color CianOscuro = new Color("1b5f6e");
	private static readonly Color RojoVivo = new Color("ff3b34");
	private static readonly Color RojoOscuro = new Color("7a1d1d");
	private static readonly Color Verde = new Color("3ddc84");
	private static readonly Color FondoCaja = new Color("010a14");

	private PanelContainer ventana;
	private Control contenedorPaginas;
	private Label lblTitulo;
	private Label lblAuto;

	private readonly Dictionary<Seccion, Control> paginas = new Dictionary<Seccion, Control>();
	private readonly Dictionary<Seccion, Button> botonesNav = new Dictionary<Seccion, Button>();
	private readonly Dictionary<Seccion, IconoSistema> iconosNav = new Dictionary<Seccion, IconoSistema>();
	private Seccion actual = Seccion.Solicitudes;

	// Solicitudes
	private Label lblNombre, lblArea, lblIP, lblMAC, lblSinSolicitud;
	private Control tarjetaSolicitud;
	private Button btnAceptar, btnRechazar;

	// Dashboard
	private readonly Dictionary<string, Label> statDash = new Dictionary<string, Label>();
	private ProgressBar barraVirus, barraValidas, barraAuto;
	private Label lblVirus, lblValidas, lblAutoErr, lblRecientes;

	// Listas (la misma lista se dibuja en Solicitudes y en su página completa)
	private readonly List<Action> refrescarListas = new List<Action>();

	// Red
	private OptionButton selModo, selTipo, selSeguridad;
	private LineEdit entPuerto;
	private Label lblEstadoRed;

	private double acumulador = 0;

	public bool EstaAbierta => ventana != null && ventana.Visible;

	// ------------------------------------------------------------------
	// Construcción
	// ------------------------------------------------------------------

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;

		ventana = new PanelContainer
		{
			CustomMinimumSize = TamVentana,
			Size = TamVentana,
			MouseFilter = MouseFilterEnum.Stop,
			Visible = false
		};
		ventana.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(RedFlagUI.FondoVentana, Cian, 2, 4));
		AddChild(ventana);

		VBoxContainer raiz = new VBoxContainer();
		raiz.AddThemeConstantOverride("separation", 0);
		ventana.AddChild(raiz);

		raiz.AddChild(CrearBarraTitulo());

		MarginContainer margen = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		foreach (string lado in new[] { "left", "right", "top", "bottom" })
			margen.AddThemeConstantOverride("margin_" + lado, 8);
		raiz.AddChild(margen);

		HBoxContainer cuerpo = new HBoxContainer();
		cuerpo.AddThemeConstantOverride("separation", 8);
		margen.AddChild(cuerpo);

		cuerpo.AddChild(CrearLateral());

		VBoxContainer derecha = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		derecha.AddThemeConstantOverride("separation", 6);
		cuerpo.AddChild(derecha);

		derecha.AddChild(CrearCabecera());

		contenedorPaginas = new Control
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			ClipContents = true
		};
		derecha.AddChild(contenedorPaginas);

		AgregarPagina(Seccion.Dashboard, CrearPaginaDashboard());
		AgregarPagina(Seccion.Solicitudes, CrearPaginaSolicitudes());
		AgregarPagina(Seccion.Whitelist, CrearPanelLista(true, true));
		AgregarPagina(Seccion.Blacklist, CrearPanelLista(false, true));
		AgregarPagina(Seccion.Red, CrearPaginaRed());
		AgregarPagina(Seccion.Ajustes, CrearPaginaAjustes());

		raiz.AddChild(CrearPie());

		if (Datos != null)
		{
			Datos.ListasCambio += RefrescarListas;
			Datos.Cambio += RefrescarRecientes;
		}

		Mostrar(Seccion.Solicitudes);
		RefrescarListas();
	}

	private void AgregarPagina(Seccion s, Control pagina)
	{
		pagina.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		pagina.Visible = false;
		contenedorPaginas.AddChild(pagina);
		paginas[s] = pagina;
	}

	private Control CrearBarraTitulo()
	{
		PanelContainer barra = new PanelContainer { CustomMinimumSize = new Vector2(0, 32), MouseFilter = MouseFilterEnum.Stop };
		StyleBoxFlat estilo = new StyleBoxFlat { BgColor = RedFlagUI.FondoBarra, BorderColor = CianOscuro };
		estilo.BorderWidthBottom = 1;
		estilo.ContentMarginLeft = 10;
		estilo.ContentMarginRight = 10;
		estilo.ContentMarginTop = 6;
		estilo.ContentMarginBottom = 6;
		barra.AddThemeStyleboxOverride("panel", estilo);

		// Arrastrar la ventana desde la barra de título
		bool arrastrando = false;
		Vector2 desfase = Vector2.Zero;
		barra.GuiInput += (InputEvent e) =>
		{
			if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
			{
				arrastrando = mb.Pressed;
				if (mb.Pressed) desfase = ventana.GetGlobalMousePosition() - ventana.GlobalPosition;
			}
			else if (e is InputEventMouseMotion && arrastrando)
			{
				ventana.GlobalPosition = ventana.GetGlobalMousePosition() - desfase;
				Vector2 p = ventana.Position;
				p.X = Mathf.Clamp(p.X, -ventana.Size.X * ventana.Scale.X + 100f, Size.X - 100f);
				p.Y = Mathf.Clamp(p.Y, 0f, Size.Y - 60f);
				ventana.Position = p;
			}
		};

		HBoxContainer fila = new HBoxContainer();
		fila.AddThemeConstantOverride("separation", 10);
		barra.AddChild(fila);

		fila.AddChild(IconoSistema.Crear(IconoSistema.Clase.RedFlag, 20));
		Label titulo = RedFlagUI.CrearLabel("REDS FLAGS OS v1.0", 14, Colors.White);
		titulo.VerticalAlignment = VerticalAlignment.Center;
		fila.AddChild(titulo);
		fila.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
		fila.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Minimizar, AlHacerClic = Esconder });
		fila.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Maximizar, AlHacerClic = AlternarTamano });
		fila.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Cerrar, AlHacerClic = Esconder });
		return barra;
	}

	private Control CrearLateral()
	{
		PanelContainer lateral = new PanelContainer { CustomMinimumSize = new Vector2(124, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
		lateral.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(FondoCaja, Cian, 1, 3, 6));

		VBoxContainer lista = new VBoxContainer();
		lista.AddThemeConstantOverride("separation", 4);
		lateral.AddChild(lista);

		AgregarNav(lista, Seccion.Dashboard, "DASHBOARD", IconoSistema.Clase.Dashboard);
		AgregarNav(lista, Seccion.Solicitudes, "SOLICITUDES", IconoSistema.Clase.Solicitudes);
		AgregarNav(lista, Seccion.Whitelist, "WHITELIST", IconoSistema.Clase.Whitelist);
		AgregarNav(lista, Seccion.Blacklist, "BLACKLIST", IconoSistema.Clase.Blacklist);
		AgregarNav(lista, Seccion.Red, "RED", IconoSistema.Clase.Red);
		AgregarNav(lista, Seccion.Ajustes, "AJUSTES", IconoSistema.Clase.Ajustes);
		return lateral;
	}

	private void AgregarNav(VBoxContainer padre, Seccion s, string texto, IconoSistema.Clase icono)
	{
		Button b = new Button
		{
			Text = texto,
			Alignment = HorizontalAlignment.Left,
			CustomMinimumSize = new Vector2(0, 32),
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			ClipText = true
		};
		b.AddThemeFontOverride("font", RedFlagUI.Fuente);
		b.AddThemeFontSizeOverride("font_size", 12);
		b.AddThemeColorOverride("font_color", RedFlagUI.Texto);
		b.AddThemeColorOverride("font_hover_color", Colors.White);
		b.AddThemeColorOverride("font_pressed_color", Colors.White);

		IconoSistema ic = IconoSistema.Crear(icono, 16, new Color("5fa3b5"));
		ic.Position = new Vector2(9, 8);
		b.AddChild(ic);

		b.Pressed += () => Mostrar(s);
		padre.AddChild(b);

		botonesNav[s] = b;
		iconosNav[s] = ic;
	}

	private static StyleBoxFlat EstiloNav(bool seleccionado, bool encima)
	{
		StyleBoxFlat sb = new StyleBoxFlat
		{
			BgColor = seleccionado ? new Color("0a2a3a") : (encima ? new Color("06202e") : new Color(0, 0, 0, 0)),
			BorderColor = seleccionado ? Cian : (encima ? CianOscuro : new Color(0, 0, 0, 0))
		};
		sb.SetBorderWidthAll(seleccionado ? 2 : 1);
		sb.SetCornerRadiusAll(3);
		sb.ContentMarginLeft = 34;
		sb.ContentMarginRight = 4;
		return sb;
	}

	private Control CrearCabecera()
	{
		HBoxContainer fila = new HBoxContainer { CustomMinimumSize = new Vector2(0, 24) };
		fila.AddThemeConstantOverride("separation", 6);

		lblTitulo = RedFlagUI.CrearLabel("SOLICITUDES DE CONEXIÓN", 15, Colors.White);
		lblTitulo.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		lblTitulo.VerticalAlignment = VerticalAlignment.Center;
		fila.AddChild(lblTitulo);

		return fila;
	}

	private Control CrearPie()
	{
		PanelContainer pie = new PanelContainer { CustomMinimumSize = new Vector2(0, 28) };
		StyleBoxFlat estilo = new StyleBoxFlat { BgColor = RedFlagUI.FondoBarra, BorderColor = CianOscuro };
		estilo.BorderWidthTop = 1;
		estilo.ContentMarginLeft = 14;
		estilo.ContentMarginRight = 14;
		estilo.ContentMarginTop = 4;
		estilo.ContentMarginBottom = 4;
		pie.AddThemeStyleboxOverride("panel", estilo);

		HBoxContainer fila = new HBoxContainer();
		pie.AddChild(fila);

		// Puntos, tiempo y conexiones se muestran solo en la barra superior del escritorio.
		// Acá queda únicamente lo propio de las solicitudes: la cuenta regresiva de autoaceptación.
		HBoxContainer centro = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
		centro.AddThemeConstantOverride("separation", 6);
		centro.AddChild(RedFlagUI.CrearLabel("AUTO-ACEPTA EN:", 12, Cian));
		lblAuto = RedFlagUI.CrearLabel("0", 12, Colors.White);
		centro.AddChild(lblAuto);
		fila.AddChild(centro);
		return pie;
	}

	// ------------------------------------------------------------------
	// Ayudas
	// ------------------------------------------------------------------

	private static PanelContainer Caja(Control hijo, Color borde, int margen = 8, int grosor = 2)
	{
		PanelContainer p = new PanelContainer();
		p.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(FondoCaja, borde, grosor, 4, margen));
		p.AddChild(hijo);
		return p;
	}

	private static Button BotonIcono(IconoSistema.Clase clase, Color color, Vector2 tam, string tip)
	{
		Button b = new Button
		{
			CustomMinimumSize = tam,
			TooltipText = tip,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand
		};
		Color oscuro = new Color(color.R * 0.18f, color.G * 0.18f, color.B * 0.18f);
		b.AddThemeStyleboxOverride("normal", RedFlagUI.Caja(FondoCaja, color, 2, 3));
		b.AddThemeStyleboxOverride("hover", RedFlagUI.Caja(oscuro, color, 2, 3));
		b.AddThemeStyleboxOverride("pressed", RedFlagUI.Caja(new Color(color.R * 0.3f, color.G * 0.3f, color.B * 0.3f), color, 2, 3));
		b.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

		IconoSistema ic = IconoSistema.Crear(clase, 12, color);
		ic.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 5);
		b.AddChild(ic);
		return b;
	}

	private static Button BotonAccion(string texto, Color color, IconoSistema.Clase icono, Vector2 tam)
	{
		Button b = new Button
		{
			Text = texto,
			CustomMinimumSize = tam,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand
		};
		Color oscuro = new Color(color.R * 0.14f, color.G * 0.14f, color.B * 0.14f);
		StyleBoxFlat Estilo(Color fondo)
		{
			StyleBoxFlat sb = RedFlagUI.Caja(fondo, color, 2, 3);
			sb.ContentMarginLeft = 26;
			sb.ContentMarginRight = 6;
			return sb;
		}
		b.AddThemeStyleboxOverride("normal", Estilo(oscuro));
		b.AddThemeStyleboxOverride("hover", Estilo(new Color(color.R * 0.26f, color.G * 0.26f, color.B * 0.26f)));
		b.AddThemeStyleboxOverride("pressed", Estilo(new Color(color.R * 0.38f, color.G * 0.38f, color.B * 0.38f)));
		b.AddThemeStyleboxOverride("disabled", Estilo(oscuro));
		b.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		b.AddThemeFontOverride("font", RedFlagUI.Fuente);
		b.AddThemeFontSizeOverride("font_size", 13);
		b.AddThemeColorOverride("font_color", Colors.White);
		b.AddThemeColorOverride("font_hover_color", Colors.White);
		b.AddThemeColorOverride("font_pressed_color", Colors.White);

		IconoSistema ic = IconoSistema.Crear(icono, 14, color);
		ic.Position = new Vector2(10, (tam.Y - 14) * 0.5f);
		b.AddChild(ic);
		return b;
	}

	private static void EstilizarEntrada(LineEdit e)
	{
		e.AddThemeStyleboxOverride("normal", RedFlagUI.Caja(RedFlagUI.FondoPanel, CianOscuro, 1, 3, 6));
		e.AddThemeStyleboxOverride("focus", RedFlagUI.Caja(RedFlagUI.FondoPanel, Cian, 1, 3, 6));
		e.AddThemeFontOverride("font", RedFlagUI.Fuente);
		e.AddThemeFontSizeOverride("font_size", 13);
		e.AddThemeColorOverride("font_color", RedFlagUI.Texto);
		e.AddThemeColorOverride("caret_color", Cian);
	}

	private static ProgressBar CrearBarra(Color relleno)
	{
		ProgressBar b = new ProgressBar
		{
			MinValue = 0,
			MaxValue = 1,
			Value = 0,
			ShowPercentage = false,
			CustomMinimumSize = new Vector2(0, 10),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter
		};
		b.AddThemeStyleboxOverride("background", RedFlagUI.Caja(new Color("04101c"), CianOscuro, 1, 2));
		b.AddThemeStyleboxOverride("fill", RedFlagUI.Caja(relleno, relleno, 0, 2));
		return b;
	}

	private static Label Etiqueta(string texto, int tam, Color color)
	{
		Label l = RedFlagUI.CrearLabel(texto, tam, color);
		l.VerticalAlignment = VerticalAlignment.Center;
		return l;
	}

	private static HBoxContainer FilaDato(string clave, out Label valor, int anchoClave = 108)
	{
		HBoxContainer fila = new HBoxContainer();
		fila.AddThemeConstantOverride("separation", 4);
		Label k = Etiqueta(clave, 12, new Color("8fb3bd"));
		k.CustomMinimumSize = new Vector2(anchoClave, 0);
		fila.AddChild(k);
		valor = Etiqueta("", 13, Colors.White);
		valor.ClipText = true;
		valor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		fila.AddChild(valor);
		return fila;
	}

	// ------------------------------------------------------------------
	// Página: Solicitudes
	// ------------------------------------------------------------------

	private Control CrearPaginaSolicitudes()
	{
		VBoxContainer pagina = new VBoxContainer();
		pagina.AddThemeConstantOverride("separation", 8);

		// Tarjeta con la solicitud
		HBoxContainer interior = new HBoxContainer();
		interior.AddThemeConstantOverride("separation", 12);

		interior.AddChild(CrearAvatar());

		VBoxContainer datos = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		datos.AddThemeConstantOverride("separation", 1);
		datos.AddChild(FilaDato("Nombre:", out lblNombre));
		datos.AddChild(FilaDato("Departamento:", out lblArea));
		datos.AddChild(FilaDato("IP:", out lblIP));
		datos.AddChild(FilaDato("MAC:", out lblMAC));

		HBoxContainer botones = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		botones.AddThemeConstantOverride("separation", 14);
		btnAceptar = BotonAccion("ACEPTAR", Verde, IconoSistema.Clase.Check, new Vector2(122, 26));
		btnAceptar.Pressed += () => Aceptado?.Invoke();
		btnRechazar = BotonAccion("RECHAZAR", RojoVivo, IconoSistema.Clase.Cruz, new Vector2(122, 26));
		btnRechazar.Pressed += () => Rechazado?.Invoke();
		botones.AddChild(btnAceptar);
		botones.AddChild(btnRechazar);
		datos.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
		datos.AddChild(botones);
		interior.AddChild(datos);

		tarjetaSolicitud = interior;

		lblSinSolicitud = Etiqueta("Sin solicitudes pendientes.", 13, new Color("8fb3bd"));
		lblSinSolicitud.HorizontalAlignment = HorizontalAlignment.Center;
		lblSinSolicitud.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		lblSinSolicitud.Visible = false;

		VBoxContainer contenidoTarjeta = new VBoxContainer();
		contenidoTarjeta.AddChild(interior);
		contenidoTarjeta.AddChild(lblSinSolicitud);
		pagina.AddChild(Caja(contenidoTarjeta, Cian, 8, 2));

		// Whitelist y blacklist chicas, una al lado de la otra
		HBoxContainer listas = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		listas.AddThemeConstantOverride("separation", 10);
		Control blanca = CrearPanelLista(true, false);
		Control negra = CrearPanelLista(false, false);
		blanca.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		negra.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		listas.AddChild(blanca);
		listas.AddChild(negra);
		pagina.AddChild(listas);

		return pagina;
	}

	private Control CrearAvatar()
	{
		Panel marco = new Panel { CustomMinimumSize = new Vector2(88, 80), SizeFlagsVertical = SizeFlags.ShrinkBegin };
		marco.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(new Color("0b2236"), CianOscuro, 1, 2));

		// Foto del jugador: su personaje personalizado
		AvatarJugador foto = AvatarJugador.Crear(AvatarJugador.Modo.Busto, true);
		marco.AddChild(foto);
		foto.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 4);

		// Esquinas rojas, como en el diseño
		marco.Draw += () =>
		{
			Vector2 s = marco.Size;
			float l = 12f;
			Color r = RojoVivo;
			marco.DrawLine(new Vector2(0, 1), new Vector2(l, 1), r, 3f);
			marco.DrawLine(new Vector2(1, 0), new Vector2(1, l), r, 3f);
			marco.DrawLine(new Vector2(s.X - l, 1), new Vector2(s.X, 1), r, 3f);
			marco.DrawLine(new Vector2(s.X - 1, 0), new Vector2(s.X - 1, l), r, 3f);
			marco.DrawLine(new Vector2(0, s.Y - 1), new Vector2(l, s.Y - 1), r, 3f);
			marco.DrawLine(new Vector2(1, s.Y - l), new Vector2(1, s.Y), r, 3f);
			marco.DrawLine(new Vector2(s.X - l, s.Y - 1), new Vector2(s.X, s.Y - 1), r, 3f);
			marco.DrawLine(new Vector2(s.X - 1, s.Y - l), new Vector2(s.X - 1, s.Y), r, 3f);
		};
		return marco;
	}

	// ------------------------------------------------------------------
	// Whitelist / Blacklist
	// ------------------------------------------------------------------

	private Control CrearPanelLista(bool blanca, bool completa)
	{
		Color color = blanca ? Cian : RojoVivo;
		Color colorOscuro = blanca ? CianOscuro : RojoOscuro;

		VBoxContainer interior = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		interior.AddThemeConstantOverride("separation", 6);

		HBoxContainer cabecera = new HBoxContainer();
		cabecera.AddThemeConstantOverride("separation", 6);
		cabecera.AddChild(IconoSistema.Crear(blanca ? IconoSistema.Clase.Personas : IconoSistema.Clase.Prohibido, 18, color));
		Label titulo = Etiqueta(blanca ? "WHITELIST" : "BLACKLIST", 13, color);
		cabecera.AddChild(titulo);

		Label aviso = Etiqueta("", 11, new Color("8fb3bd"));
		aviso.HorizontalAlignment = HorizontalAlignment.Right;
		aviso.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		aviso.ClipText = true;
		cabecera.AddChild(aviso);

		Button mas = BotonIcono(IconoSistema.Clase.Mas, color, new Vector2(24, 24), "Agregar la solicitud actual a la lista");
		mas.Pressed += () =>
		{
			string mensaje;
			if (Datos == null || !Datos.Solicitud.Hay) mensaje = "No hay solicitud";
			else if (Datos.AgregarALista(blanca)) mensaje = "Agregada";
			else mensaje = "Ya está en una lista";

			aviso.Text = mensaje;
			GetTree().CreateTimer(1.6).Timeout += () =>
			{
				if (IsInstanceValid(aviso)) aviso.Text = "";
			};
		};
		cabecera.AddChild(mas);
		interior.AddChild(cabecera);

		VBoxContainer filas = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		filas.AddThemeConstantOverride("separation", 3);

		ScrollContainer scroll = new ScrollContainer
		{
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		scroll.AddChild(filas);

		PanelContainer recuadro = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		recuadro.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(new Color("02101a"), colorOscuro, 1, 3, 4));
		recuadro.AddChild(scroll);
		interior.AddChild(recuadro);

		refrescarListas.Add(() =>
		{
			foreach (Node n in filas.GetChildren())
			{
				filas.RemoveChild(n);
				n.QueueFree();
			}
			if (Datos == null) return;

			List<EntradaLista> entradas = blanca ? Datos.Whitelist : Datos.Blacklist;
			if (entradas.Count == 0)
			{
				filas.AddChild(Etiqueta(completa ? "Sin entradas. Usá + para agregar la solicitud actual." : "Sin entradas", 11, new Color("5f7f8a")));
				return;
			}

			foreach (EntradaLista e in entradas.ToList())
			{
				HBoxContainer fila = new HBoxContainer();
				fila.AddThemeConstantOverride("separation", 4);

				string texto = completa
					? $"{e.Nombre,-16} {e.IP,-15} {e.Area}"
					: $"{e.Nombre} · {e.IP}";
				Label l = Etiqueta(texto, 12, Colors.White);
				l.ClipText = true;
				l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
				fila.AddChild(l);

				EntradaLista captura = e;
				Button quitar = BotonIcono(IconoSistema.Clase.Cruz, colorOscuro, new Vector2(18, 18), "Quitar de la lista");
				quitar.Pressed += () => Datos.QuitarDeLista(blanca, captura);
				fila.AddChild(quitar);

				filas.AddChild(fila);
			}
		});

		PanelContainer panel = Caja(interior, color, 6, 2);
		panel.SizeFlagsVertical = SizeFlags.ExpandFill;
		return panel;
	}

	private void RefrescarListas()
	{
		foreach (Action a in refrescarListas) a();
	}

	// ------------------------------------------------------------------
	// Dashboard
	// ------------------------------------------------------------------

	private Control CrearPaginaDashboard()
	{
		VBoxContainer pagina = new VBoxContainer();
		pagina.AddThemeConstantOverride("separation", 8);

		GridContainer grilla = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		grilla.AddThemeConstantOverride("h_separation", 8);
		grilla.AddThemeConstantOverride("v_separation", 8);

		void Tarjeta(string clave, string titulo, Color color)
		{
			VBoxContainer v = new VBoxContainer();
			v.AddThemeConstantOverride("separation", 0);
			v.AddChild(Etiqueta(titulo, 10, new Color("8fb3bd")));
			Label valor = Etiqueta("0", 18, color);
			v.AddChild(valor);
			statDash[clave] = valor;
			PanelContainer c = Caja(v, CianOscuro, 6, 1);
			c.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			grilla.AddChild(c);
		}

		Tarjeta("puntos", "PUNTOS", Verde);
		Tarjeta("procesadas", "SOLICITUDES PROCESADAS", Colors.White);
		Tarjeta("bloqueadas", "AMENAZAS BLOQUEADAS", Cian);
		Tarjeta("aceptadas", "ACEPTADAS", Colors.White);
		Tarjeta("rechazadas", "RECHAZADAS", Colors.White);
		Tarjeta("auto", "AUTOACEPTADAS", new Color("ffb347"));
		pagina.AddChild(grilla);

		// Errores contra el límite de la dificultad
		VBoxContainer errores = new VBoxContainer();
		errores.AddThemeConstantOverride("separation", 4);
		errores.AddChild(Etiqueta("ERRORES (LÍMITE DEL TURNO)", 11, Cian));

		HBoxContainer FilaBarra(string nombre, out Label valor, out ProgressBar barra, Color colorBarra)
		{
			HBoxContainer f = new HBoxContainer();
			f.AddThemeConstantOverride("separation", 8);
			Label k = Etiqueta(nombre, 11, new Color("8fb3bd"));
			k.CustomMinimumSize = new Vector2(150, 0);
			f.AddChild(k);
			barra = CrearBarra(colorBarra);
			f.AddChild(barra);
			valor = Etiqueta("0/0", 11, Colors.White);
			valor.CustomMinimumSize = new Vector2(40, 0);
			valor.HorizontalAlignment = HorizontalAlignment.Right;
			f.AddChild(valor);
			return f;
		}

		errores.AddChild(FilaBarra("Virus aceptados", out lblVirus, out barraVirus, RojoVivo));
		errores.AddChild(FilaBarra("Válidas rechazadas", out lblValidas, out barraValidas, new Color("ffb347")));
		errores.AddChild(FilaBarra("Autoaceptadas", out lblAutoErr, out barraAuto, Cian));
		pagina.AddChild(Caja(errores, CianOscuro, 6, 1));

		// Últimas decisiones
		VBoxContainer recientes = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		recientes.AddThemeConstantOverride("separation", 2);
		recientes.AddChild(Etiqueta("ÚLTIMAS DECISIONES", 11, Cian));
		lblRecientes = Etiqueta("Sin actividad todavía.", 11, Colors.White);
		lblRecientes.ClipText = true;
		recientes.AddChild(lblRecientes);
		PanelContainer cajaRecientes = Caja(recientes, CianOscuro, 6, 1);
		cajaRecientes.SizeFlagsVertical = SizeFlags.ExpandFill;
		pagina.AddChild(cajaRecientes);

		return pagina;
	}

	private void RefrescarDashboard()
	{
		if (Datos == null || !statDash.ContainsKey("puntos")) return;

		int aceptadas = Datos.Historial.Count(r => r.Decision == "ACEPTADA");
		int rechazadas = Datos.Historial.Count(r => r.Decision == "RECHAZADA");
		int autos = Datos.Historial.Count(r => r.Decision == "AUTOACEPTADA");

		statDash["puntos"].Text = Datos.Puntos.ToString();
		statDash["procesadas"].Text = Datos.Historial.Count.ToString();
		statDash["bloqueadas"].Text = Datos.AmenazasBloqueadas.ToString();
		statDash["aceptadas"].Text = aceptadas.ToString();
		statDash["rechazadas"].Text = rechazadas.ToString();
		statDash["auto"].Text = autos.ToString();

		void Barra(ProgressBar b, Label l, int valor, int max)
		{
			int m = Math.Max(1, max);
			b.Value = Math.Min(1.0, valor / (double)m);
			l.Text = $"{valor}/{max}";
		}
		Barra(barraVirus, lblVirus, Datos.ErroresAceptar, Datos.MaxErroresAceptar);
		Barra(barraValidas, lblValidas, Datos.ErroresRechazar, Datos.MaxErroresRechazar);
		Barra(barraAuto, lblAutoErr, Datos.AutoAceptadas, Datos.MaxAutoAceptadas);
	}

	private void RefrescarRecientes()
	{
		if (Datos == null || lblRecientes == null) return;

		if (Datos.Historial.Count == 0)
		{
			lblRecientes.Text = "Sin actividad todavía.";
			return;
		}

		lblRecientes.Text = string.Join("\n", Datos.Historial
			.OrderByDescending(r => r.Hora)
			.Take(3)
			.Select(r => $"[{r.Hora:HH:mm:ss}] {r.Decision,-12} {r.Nombre,-16} {r.IP}"));
	}

	// ------------------------------------------------------------------
	// Red
	// ------------------------------------------------------------------

	private Control CrearPaginaRed()
	{
		VBoxContainer pagina = new VBoxContainer();
		pagina.AddThemeConstantOverride("separation", 8);

		HBoxContainer cuerpo = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		cuerpo.AddThemeConstantOverride("separation", 10);

		VBoxContainer form = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		form.AddThemeConstantOverride("separation", 8);

		OptionButton Opcion(string texto, string[] valores)
		{
			HBoxContainer fila = new HBoxContainer();
			fila.AddThemeConstantOverride("separation", 6);
			Label k = Etiqueta(texto, 12, RedFlagUI.Texto);
			k.CustomMinimumSize = new Vector2(120, 0);
			fila.AddChild(k);
			OptionButton o = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 26) };
			RedFlagUI.EstilizarOpcion(o);
			o.AddThemeFontSizeOverride("font_size", 13);
			foreach (string v in valores) o.AddItem(v);
			fila.AddChild(o);
			form.AddChild(fila);
			return o;
		}

		selModo = Opcion("Modo de conexión:", ConfigRed.Modos);
		selTipo = Opcion("Tipo de red:", ConfigRed.Tipos);

		HBoxContainer filaPuerto = new HBoxContainer();
		filaPuerto.AddThemeConstantOverride("separation", 6);
		Label kPuerto = Etiqueta("Puerto:", 12, RedFlagUI.Texto);
		kPuerto.CustomMinimumSize = new Vector2(120, 0);
		filaPuerto.AddChild(kPuerto);
		entPuerto = new LineEdit { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 26), MaxLength = 5 };
		EstilizarEntrada(entPuerto);
		filaPuerto.AddChild(entPuerto);
		form.AddChild(filaPuerto);

		selSeguridad = Opcion("Seguridad:", ConfigRed.Seguridades);

		lblEstadoRed = Etiqueta("", 11, Verde);
		form.AddChild(lblEstadoRed);

		cuerpo.AddChild(Caja(form, Cian, 10, 2));

		// Antena roja a la derecha
		PanelContainer cajaAntena = new PanelContainer { CustomMinimumSize = new Vector2(150, 0) };
		cajaAntena.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(FondoCaja, RojoOscuro, 1, 4, 10));
		IconoSistema antena = IconoSistema.Crear(IconoSistema.Clase.Antena, 100, RojoVivo);
		antena.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		antena.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		cajaAntena.AddChild(antena);
		cuerpo.AddChild(cajaAntena);
		pagina.AddChild(cuerpo);

		HBoxContainer botones = new HBoxContainer();
		botones.AddThemeConstantOverride("separation", 12);
		Button aplicar = BotonAccion("APLICAR", Cian, IconoSistema.Clase.Guardar, new Vector2(0, 30));
		aplicar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		aplicar.Pressed += AplicarRed;
		Button restablecer = BotonAccion("RESTABLECER", RojoVivo, IconoSistema.Clase.Reiniciar, new Vector2(0, 30));
		restablecer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		restablecer.Pressed += RestablecerRed;
		botones.AddChild(aplicar);
		botones.AddChild(restablecer);
		pagina.AddChild(botones);

		CargarRed();
		return pagina;
	}

	private void CargarRed()
	{
		if (Datos == null || selModo == null) return;
		selModo.Selected = Datos.Red.Modo;
		selTipo.Selected = Datos.Red.Tipo;
		selSeguridad.Selected = Datos.Red.Seguridad;
		entPuerto.Text = Datos.Red.Puerto;
	}

	private void AplicarRed()
	{
		if (Datos == null) return;

		string puerto = entPuerto.Text.Trim();
		if (!int.TryParse(puerto, out int numero) || numero < 1 || numero > 65535)
		{
			MostrarEstadoRed("Puerto inválido (1-65535).", RojoVivo);
			return;
		}

		Datos.Red.Modo = selModo.Selected;
		Datos.Red.Tipo = selTipo.Selected;
		Datos.Red.Puerto = numero.ToString();
		Datos.Red.Seguridad = selSeguridad.Selected;
		entPuerto.Text = Datos.Red.Puerto;
		MostrarEstadoRed("Configuración aplicada.", Verde);
	}

	private void RestablecerRed()
	{
		if (Datos == null) return;
		Datos.Red.Restablecer();
		CargarRed();
		MostrarEstadoRed("Valores restablecidos.", Cian);
	}

	private void MostrarEstadoRed(string texto, Color color)
	{
		lblEstadoRed.Text = texto;
		lblEstadoRed.AddThemeColorOverride("font_color", color);
		GetTree().CreateTimer(2.0).Timeout += () =>
		{
			if (IsInstanceValid(lblEstadoRed)) lblEstadoRed.Text = "";
		};
	}

	// ------------------------------------------------------------------
	// Ajustes (datos del turno en curso)
	// ------------------------------------------------------------------

	private Control CrearPaginaAjustes()
	{
		VBoxContainer pagina = new VBoxContainer();
		pagina.AddThemeConstantOverride("separation", 8);

		VBoxContainer turno = new VBoxContainer();
		turno.AddThemeConstantOverride("separation", 3);
		turno.AddChild(Etiqueta("TURNO ACTUAL", 12, Cian));
		bool infinito = PartidaConfig.Modo == ModoJuego.Infinito;
		turno.AddChild(FilaDato("Modo:", out Label vModo, 150));
		vModo.Text = infinito ? "Infinito" : "Turno de guardia";
		turno.AddChild(FilaDato("Dificultad:", out Label vNivel, 150));
		vNivel.Text = PartidaConfig.NombreNivel;
		turno.AddChild(FilaDato("Duración:", out Label vDur, 150));
		vDur.Text = infinito ? "Sin límite" : $"{PartidaConfig.DuracionMinutos} min";
		pagina.AddChild(Caja(turno, Cian, 10, 2));

		VBoxContainer limites = new VBoxContainer();
		limites.AddThemeConstantOverride("separation", 3);
		limites.AddChild(Etiqueta("LÍMITES DE ERRORES", 12, RojoVivo));
		limites.AddChild(FilaDato("Virus aceptados:", out Label l1, 150));
		l1.Text = PartidaConfig.MaxErroresAceptar.ToString();
		limites.AddChild(FilaDato("Válidas rechazadas:", out Label l2, 150));
		l2.Text = PartidaConfig.MaxErroresRechazar.ToString();
		limites.AddChild(FilaDato("Autoaceptadas:", out Label l3, 150));
		l3.Text = PartidaConfig.MaxAutoAceptadas.ToString();
		limites.AddChild(FilaDato("Tiempo por solicitud:", out Label l4, 150));
		l4.Text = $"{PartidaConfig.SegundosAutoAceptar:0} s";
		PanelContainer cajaLimites = Caja(limites, RojoVivo, 10, 2);
		cajaLimites.SizeFlagsVertical = SizeFlags.ExpandFill;
		pagina.AddChild(cajaLimites);

		return pagina;
	}

	// ------------------------------------------------------------------
	// Navegación y estado
	// ------------------------------------------------------------------

	private void Mostrar(Seccion s)
	{
		actual = s;

		foreach (KeyValuePair<Seccion, Control> kv in paginas)
			kv.Value.Visible = kv.Key == s;

		foreach (KeyValuePair<Seccion, Button> kv in botonesNav)
		{
			bool sel = kv.Key == s;
			kv.Value.AddThemeStyleboxOverride("normal", EstiloNav(sel, false));
			kv.Value.AddThemeStyleboxOverride("hover", EstiloNav(sel, true));
			kv.Value.AddThemeStyleboxOverride("pressed", EstiloNav(true, true));
			kv.Value.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
			iconosNav[kv.Key].ColorBase = sel ? Cian : new Color("5fa3b5");
			iconosNav[kv.Key].QueueRedraw();
		}

		switch (s)
		{
			case Seccion.Dashboard: lblTitulo.Text = "DASHBOARD"; break;
			case Seccion.Solicitudes: lblTitulo.Text = "SOLICITUDES DE CONEXIÓN"; break;
			case Seccion.Whitelist: lblTitulo.Text = "WHITELIST"; break;
			case Seccion.Blacklist: lblTitulo.Text = "BLACKLIST"; break;
			case Seccion.Red: lblTitulo.Text = "CONFIGURACIÓN DE RED"; break;
			default: lblTitulo.Text = "AJUSTES"; break;
		}

		if (s == Seccion.Dashboard) { RefrescarDashboard(); RefrescarRecientes(); }
		if (s == Seccion.Red) CargarRed();
	}

	/// <summary>Abre la ventana centrada y con la sección de solicitudes a la vista.</summary>
	public void Abrir()
	{
		if (ventana == null) return;

		Mostrar(Seccion.Solicitudes);
		ventana.Visible = true;
		CentrarVentana();

		Refrescar();
		ventana.Modulate = new Color(1, 1, 1, 0.2f);
		CreateTween().TweenProperty(ventana, "modulate:a", 1f, 0.15f);
	}

	private bool ampliada = false;

	private Vector2 AreaEscritorio()
	{
		Vector2 area = Size.X > 0 ? Size : GetViewportRect().Size;
		return new Vector2(area.X, Mathf.Max(100f, area.Y - 56f)); // deja lugar a la barra de tareas
	}

	private void CentrarVentana()
	{
		Vector2 area = AreaEscritorio();
		ventana.Position = ((area - ventana.Size * ventana.Scale) * 0.5f).Floor();
	}

	/// <summary>Botón del cuadradito: agranda la ventana (y todo su contenido) para leer mejor, o la vuelve a su tamaño.</summary>
	private void AlternarTamano()
	{
		ampliada = !ampliada;

		float factor = 1f;
		if (ampliada)
		{
			Vector2 area = AreaEscritorio();
			factor = Mathf.Clamp(Mathf.Min(area.X * 0.96f / ventana.Size.X, area.Y * 0.96f / ventana.Size.Y), 1f, 2.5f);
		}

		ventana.PivotOffset = Vector2.Zero;
		ventana.Scale = new Vector2(factor, factor);
		CentrarVentana();
	}

	/// <summary>Cierra la ventana sin avisar (lo usa el juego al terminar la partida).</summary>
	public void Cerrar()
	{
		if (ventana != null) ventana.Visible = false;
	}

	private void Esconder()
	{
		if (ventana != null) ventana.Visible = false;
		Oculta?.Invoke();
	}

	public override void _Process(double delta)
	{
		if (ventana == null || !ventana.Visible) return;

		acumulador += delta;
		if (acumulador < 0.1) return;
		acumulador = 0;
		Refrescar();
	}

	private void Refrescar()
	{
		if (Datos == null) return;

		lblAuto.Text = Math.Max(0, (int)Math.Ceiling(Datos.AutoAceptarRestante)).ToString();

		SolicitudActual s = Datos.Solicitud;
		tarjetaSolicitud.Visible = s.Hay;
		lblSinSolicitud.Visible = !s.Hay;
		lblNombre.Text = s.Nombre;
		lblArea.Text = s.Area;
		lblIP.Text = s.IP;
		lblMAC.Text = s.MAC;

		if (actual == Seccion.Dashboard) RefrescarDashboard();
	}
}
