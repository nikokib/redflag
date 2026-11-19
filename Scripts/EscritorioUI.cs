using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Escritorio de "Reds Flags OS" con el diseño pixel-art del juego.
/// El fondo (íconos, barra de tareas, "RED INTERNA") es una imagen; encima van zonas
/// clickeables invisibles, el HUD de la partida, el reloj y las ventanas de las apps.
/// Solo interfaz: la lógica del juego sigue en ComputerGameplay.
/// </summary>
public partial class EscritorioUI : Control
{
	public DatosEscritorio Datos;

	// Datos de la partida que ComputerGameplay actualiza
	public Label LblPuntos;
	public Label LblTiempo;
	public Label LblConexiones;
	public Label LblHora;
	public Label LblFecha;
	public Control Reloj;   // contenedor del reloj (lleva el tooltip con la fecha completa)

	/// <summary>Si se asigna, tocar el ícono "Red Flags" abre la ventana grande de Red Flag en vez de una ventana común.</summary>
	public Action AlAbrirRedFlag;

	private const string RutaFondo = "res://Interfaz/escritorio_fondo.png";

	// Posición de cada ícono sobre la imagen (en píxeles de una vista previa de 836x470)
	private const float PW = 836f;
	private const float PH = 470f;

	private static Rect2 Zona(float x0, float y0, float x1, float y1)
		=> new Rect2(x0 / PW, y0 / PH, (x1 - x0) / PW, (y1 - y0) / PH);

	private static readonly Dictionary<string, Rect2> ZonasIconos = new Dictionary<string, Rect2>
	{
		{ "mipc",        Zona(4,  22,  84,  70)  },
		{ "navegador",   Zona(4,  78,  84,  124) },
		{ "redflags",    Zona(4,  128, 84,  176) },
		{ "incidentes",  Zona(4,  180, 84,  226) },
		{ "logs",        Zona(4,  228, 84,  272) },
		{ "correo",      Zona(4,  274, 84,  316) },
		{ "chat",        Zona(4,  320, 84,  360) },
		{ "firewall",    Zona(768, 55,  832, 102) },
		{ "antivirus",   Zona(768, 104, 832, 152) },
		{ "camaras",     Zona(768, 154, 832, 198) },
		{ "notas",       Zona(768, 200, 832, 242) },
		{ "calculadora", Zona(768, 244, 832, 288) },
		{ "papelera",    Zona(768, 290, 832, 324) }
	};

	// Apps cuya ventana es directamente la imagen del diseño
	private class SkinVentana
	{
		public string Ruta;
		public Vector2 Tam;      // tamaño original de la imagen
		public float MinX;       // posición (0-1) del botón minimizar
		public float CerrarX;    // posición (0-1) del botón cerrar
		public IconoSistema.Clase Icono;   // ícono de la app
		public Rect2 Parche;     // zona (en px de la imagen) donde la imagen trae un ícono que no corresponde
		public Color ColorParche;
		public bool Reemplazar;  // si es false, el ícono de la imagen ya es el correcto
		public float AltoBarra = 45f;  // alto de la barra de título (px de la imagen)
	}

	private const float EscalaVentanas = 1.1f;
	private const float AltoBarraImg = 45f;

	private static readonly Dictionary<string, SkinVentana> Skins = new Dictionary<string, SkinVentana>
	{
		{ "mipc",      new SkinVentana { Ruta = "res://Interfaz/ventana_mipc.png",      Tam = new Vector2(477, 371), MinX = 0.836f, CerrarX = 0.950f,
			Icono = IconoSistema.Clase.Pc, Reemplazar = false, AltoBarra = 52f } },
        // Correo usa una ventana interactiva. La antigua imagen se conserva en Interfaz/.
        // Las otras ventanas con imagen no se modifican.
	};

	private class VentanaAbierta
	{
		public Control Ventana;
		public Button BotonTarea;
	}

	/// <summary>Se avisa cuando el JUGADOR abre una app (no cuando el juego la abre solo).</summary>
	public Action<string> AlAbrirAppUsuario;

	private MenuInicio menuInicio;
	private Control capaVentanas;
	private HBoxContainer barraTareas;
	private readonly Dictionary<string, VentanaAbierta> abiertas = new Dictionary<string, VentanaAbierta>();
	private int cascada = 0;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Fondo: escritorio del diseño
		Texture2D tex = ResourceLoader.Exists(RutaFondo) ? GD.Load<Texture2D>(RutaFondo) : null;
		if (tex != null)
		{
			TextureRect fondo = new TextureRect
			{
				Texture = tex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = MouseFilterEnum.Ignore
			};
			fondo.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			AddChild(fondo);
		}
		else
		{
			ColorRect respaldo = new ColorRect { Color = new Color("010a14"), MouseFilter = MouseFilterEnum.Ignore };
			respaldo.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			AddChild(respaldo);
		}

		// Zonas clickeables sobre los íconos de la imagen
		Control capaIconos = new Control { MouseFilter = MouseFilterEnum.Ignore };
		capaIconos.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(capaIconos);

		List<AppsEscritorio.AppDef> todas = new List<AppsEscritorio.AppDef>();
		todas.AddRange(AppsEscritorio.Izquierda);
		todas.AddRange(AppsEscritorio.Derecha);

		foreach (AppsEscritorio.AppDef app in todas)
		{
			if (!ZonasIconos.TryGetValue(app.Id, out Rect2 r)) continue;
			string id = app.Id;
			Button b = CrearZonaFraccion(r, app.Titulo);
			b.Pressed += () => { AlAbrirAppUsuario?.Invoke(id); AbrirApp(id); };
			capaIconos.AddChild(b);
		}

		// Capa donde viven las ventanas de las apps
		capaVentanas = new Control { MouseFilter = MouseFilterEnum.Ignore };
		capaVentanas.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(capaVentanas);

		CrearHud();
		CrearBarraTareas(todas);
	}

	// ------------------------------------------------------------------
	// Zonas clickeables invisibles
	// ------------------------------------------------------------------

	private static void EstiloZona(Button b)
	{
		StyleBoxEmpty vacio = new StyleBoxEmpty();
		b.AddThemeStyleboxOverride("normal", vacio);
		b.AddThemeStyleboxOverride("focus", vacio);
		b.AddThemeStyleboxOverride("hover", RedFlagUI.Caja(new Color(0.22f, 0.72f, 0.8f, 0.14f), RedFlagUI.BordeVivo, 1, 4));
		b.AddThemeStyleboxOverride("pressed", RedFlagUI.Caja(new Color(0.22f, 0.72f, 0.8f, 0.30f), RedFlagUI.BordeVivo, 1, 4));
	}

	private static Button CrearZonaFraccion(Rect2 r, string tip)
	{
		Button b = new Button
		{
			Flat = true,
			FocusMode = FocusModeEnum.None,
			TooltipText = tip,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			AnchorLeft = r.Position.X,
			AnchorTop = r.Position.Y,
			AnchorRight = r.End.X,
			AnchorBottom = r.End.Y
		};
		EstiloZona(b);
		return b;
	}

	/// <summary>Zona clickeable en píxeles, para botones dibujados en las imágenes.</summary>
	public static Button CrearZonaPixeles(Vector2 pos, Vector2 tam, string tip)
	{
		Button b = new Button
		{
			Flat = true,
			FocusMode = FocusModeEnum.None,
			TooltipText = tip,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			Position = pos,
			Size = tam
		};
		EstiloZona(b);
		return b;
	}

	// ------------------------------------------------------------------
	// HUD de la partida y barra de tareas
	// ------------------------------------------------------------------

	private void CrearHud()
	{
		HBoxContainer fila = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
		fila.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
		fila.OffsetTop = 8;
		fila.OffsetBottom = 42;
		AddChild(fila);

		PanelContainer panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkBegin };
		panel.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(new Color(0.0f, 0.04f, 0.08f, 0.88f), RedFlagUI.BordeVivo, 1, 3, 6));
		fila.AddChild(panel);

		HBoxContainer datos = new HBoxContainer();
		datos.AddThemeConstantOverride("separation", 10);
		panel.AddChild(datos);

		datos.AddChild(RedFlagUI.CrearLabel("PUNTOS:", 13, RedFlagUI.TextoApagado));
		LblPuntos = RedFlagUI.CrearLabel("0", 14, RedFlagUI.Acento);
		LblPuntos.CustomMinimumSize = new Vector2(40, 0);
		datos.AddChild(LblPuntos);

		datos.AddChild(RedFlagUI.CrearLabel("TIEMPO:", 13, RedFlagUI.TextoApagado));
		LblTiempo = RedFlagUI.CrearLabel("00:00", 14, RedFlagUI.Texto);
		LblTiempo.CustomMinimumSize = new Vector2(48, 0);
		datos.AddChild(LblTiempo);

		datos.AddChild(RedFlagUI.CrearLabel("CONEXIONES HOY:", 13, RedFlagUI.TextoApagado));
		LblConexiones = RedFlagUI.CrearLabel("0", 14, RedFlagUI.BordeVivo);
		LblConexiones.CustomMinimumSize = new Vector2(26, 0);
		datos.AddChild(LblConexiones);
	}

	private void CrearBarraTareas(List<AppsEscritorio.AppDef> todas)
	{
		// "Inicio": zona sobre el ícono de Windows de la imagen, abre el menú de inicio
		menuInicio = new MenuInicio
		{
			AlElegirApp = id => { AlAbrirAppUsuario?.Invoke(id); AbrirApp(id); },
			AlApagar = () => PantallaEnergia.Apagar(this),
			AlReiniciar = () => PantallaEnergia.Reiniciar(this)
		};
		AddChild(menuInicio);

		Button inicio = new Button
		{
			Flat = true,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			AnchorLeft = 4f / PW,
			AnchorRight = 82f / PW,
			AnchorTop = 441f / PH,
			AnchorBottom = 1f
		};
		StyleBoxEmpty vacio = new StyleBoxEmpty();
		inicio.AddThemeStyleboxOverride("normal", vacio);
		inicio.AddThemeStyleboxOverride("focus", vacio);
		inicio.AddThemeStyleboxOverride("hover", RedFlagUI.Caja(new Color(0.22f, 0.72f, 0.8f, 0.14f), RedFlagUI.BordeVivo, 1, 4));
		inicio.AddThemeStyleboxOverride("pressed", RedFlagUI.Caja(new Color(0.22f, 0.72f, 0.8f, 0.30f), RedFlagUI.BordeVivo, 1, 4));
		inicio.Pressed += () => menuInicio.Alternar();
		AddChild(inicio);

		// Botones de las apps abiertas (a la derecha de "Inicio", sobre la barra de la imagen)
		barraTareas = new HBoxContainer
		{
			AnchorLeft = 0.098f,
			AnchorRight = 0.86f,
			AnchorTop = 0.941f,
			AnchorBottom = 0.998f
		};
		barraTareas.AddThemeConstantOverride("separation", 6);
		AddChild(barraTareas);

		// Reloj: hora y fecha, encima del espacio que se dejó libre en la imagen
		VBoxContainer reloj = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Stop,
			TooltipText = "",
			AnchorLeft = 0.944f,
			AnchorRight = 0.999f,
			AnchorTop = 0.944f,
			AnchorBottom = 0.999f
		};
		reloj.AddThemeConstantOverride("separation", -2);
		reloj.Alignment = BoxContainer.AlignmentMode.Center;

		LblHora = RedFlagUI.CrearLabel("00:00", 13, RedFlagUI.Texto, HorizontalAlignment.Center);
		LblHora.MouseFilter = MouseFilterEnum.Ignore;
		reloj.AddChild(LblHora);

		LblFecha = RedFlagUI.CrearLabel("00/00/0000", 10, RedFlagUI.TextoApagado, HorizontalAlignment.Center);
		LblFecha.MouseFilter = MouseFilterEnum.Ignore;
		reloj.AddChild(LblFecha);

		Reloj = reloj;
		AddChild(reloj);
	}

	// ------------------------------------------------------------------
	// Ventanas
	// ------------------------------------------------------------------

	private void TraerAlFrente(Control ventana)
	{
		capaVentanas.MoveChild(ventana, -1);
	}

	/// <summary>Ventana cuyo aspecto es la imagen del diseño (barra de título arrastrable).</summary>
	private Control CrearVentanaImagen(SkinVentana skin, Action alMinimizar, Action alCerrar, Action<InputEvent> alEventoBarra)
	{
		Vector2 tam = skin.Tam * EscalaVentanas;

		Control v = new Control { CustomMinimumSize = tam, Size = tam, MouseFilter = MouseFilterEnum.Stop };

		Texture2D tex = ResourceLoader.Exists(skin.Ruta) ? GD.Load<Texture2D>(skin.Ruta) : null;
		if (tex != null)
		{
			TextureRect img = new TextureRect
			{
				Texture = tex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = MouseFilterEnum.Ignore
			};
			img.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			v.AddChild(img);
		}
		else
		{
			ColorRect respaldo = new ColorRect { Color = RedFlagUI.FondoVentana, MouseFilter = MouseFilterEnum.Ignore };
			respaldo.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			v.AddChild(respaldo);
		}

		// Ícono propio de la app: tapa el que trae dibujado la imagen cuando no corresponde
		if (skin.Reemplazar)
		{
			ColorRect parche = new ColorRect
			{
				Color = skin.ColorParche,
				MouseFilter = MouseFilterEnum.Ignore,
				Position = skin.Parche.Position * EscalaVentanas,
				Size = skin.Parche.Size * EscalaVentanas
			};
			v.AddChild(parche);

			float ladoIcono = 18f * EscalaVentanas;
			IconoSistema icono = IconoSistema.Crear(skin.Icono, ladoIcono);
			icono.Position = parche.Position + (parche.Size - new Vector2(ladoIcono, ladoIcono)) * 0.5f;
			icono.Size = new Vector2(ladoIcono, ladoIcono);
			v.AddChild(icono);
		}

		// Barra de título (para arrastrar)
		Control barra = new Control
		{
			MouseFilter = MouseFilterEnum.Stop,
			Position = Vector2.Zero,
			Size = new Vector2(tam.X, skin.AltoBarra * EscalaVentanas)
		};
		barra.GuiInput += (InputEvent e) => alEventoBarra(e);
		v.AddChild(barra);

		// Botones minimizar y cerrar dibujados en la imagen
		float lado = 26f * EscalaVentanas;
		float cy = skin.AltoBarra * EscalaVentanas * 0.5f;

		Button min = CrearZonaPixeles(new Vector2(tam.X * skin.MinX - lado * 0.5f, cy - lado * 0.5f), new Vector2(lado, lado), "Minimizar");
		min.Pressed += () => alMinimizar();
		v.AddChild(min);

		Button cerrar = CrearZonaPixeles(new Vector2(tam.X * skin.CerrarX - lado * 0.5f, cy - lado * 0.5f), new Vector2(lado, lado), "Cerrar");
		cerrar.Pressed += () => alCerrar();
		v.AddChild(cerrar);

		return v;
	}

	public void AbrirApp(string id)
	{
		// "Red Flags" no es una ventana común: abre la ventana centrada de Red Flag OS
		if (id == "redflags" && AlAbrirRedFlag != null)
		{
			AlAbrirRedFlag.Invoke();
			return;
		}

		if (abiertas.TryGetValue(id, out VentanaAbierta existente))
		{
			existente.Ventana.Visible = true;
			TraerAlFrente(existente.Ventana);
			return;
		}

		AppsEscritorio.AppDef app = AppsEscritorio.Buscar(id);
		if (app == null) return;

		bool arrastrando = false;
		Vector2 desfase = Vector2.Zero;
		Control ventana = null;

		Action<InputEvent> alEventoBarra = (InputEvent e) =>
		{
			if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
			{
				arrastrando = mb.Pressed;
				if (mb.Pressed)
				{
					desfase = ventana.GetGlobalMousePosition() - ventana.GlobalPosition;
					TraerAlFrente(ventana);
				}
			}
			else if (e is InputEventMouseMotion && arrastrando)
			{
				ventana.GlobalPosition = ventana.GetGlobalMousePosition() - desfase;

				Vector2 limite = capaVentanas.Size;
				Vector2 p = ventana.Position;
				p.X = Mathf.Clamp(p.X, -ventana.Size.X + 80f, limite.X - 80f);
				p.Y = Mathf.Clamp(p.Y, 0f, limite.Y * 0.93f - 30f);
				ventana.Position = p;
			}
		};

		bool maximizada = false;
		Vector2 posAntes = Vector2.Zero;
		Vector2 tamAntes = Vector2.Zero;
		Action alMaximizar = () =>
		{
			if (!maximizada)
			{
				posAntes = ventana.Position;
				tamAntes = ventana.Size;
				maximizada = true;
				Vector2 area = capaVentanas.Size;
				ventana.Position = new Vector2(4f, 4f);
				ventana.Size = new Vector2(area.X - 8f, area.Y * 0.93f - 8f);
			}
			else
			{
				maximizada = false;
				ventana.Size = tamAntes;
				ventana.Position = posAntes;
			}
			TraerAlFrente(ventana);
		};

		if (Skins.TryGetValue(id, out SkinVentana skin))
		{
			// La ventana es la imagen del diseño (contenido tal cual el diseño)
			ventana = CrearVentanaImagen(skin, () => ventana.Visible = false, () => CerrarApp(id), alEventoBarra);
		}
		else
		{
			ventana = RedFlagUI.CrearVentana(
				app.Titulo,
				app.Tamano,
				out PanelContainer contenido,
				null,
				IconoVentana.Clase.Logo,
				() => ventana.Visible = false,
				() => CerrarApp(id),
				alEventoBarra,
				IconoSistema.Crear(IconoSistema.DeApp(id), 20, app.Color),
				false,
				alMaximizar);

			contenido.AddChild(AppsEscritorio.Crear(id, Datos));
		}

		// Clic en cualquier parte de la ventana la trae al frente
		ventana.GuiInput += (InputEvent e) =>
		{
			if (e is InputEventMouseButton mb2 && mb2.Pressed)
				TraerAlFrente(ventana);
		};

		ventana.Position = new Vector2(110 + (cascada % 6) * 24, 48 + (cascada % 6) * 22);
		cascada++;
		capaVentanas.AddChild(ventana);

		// Botón en la barra de tareas
		Button boton = new Button
		{
			Text = app.Titulo,
			ClipText = true,
			CustomMinimumSize = new Vector2(120, 0),
			FocusMode = FocusModeEnum.None
		};
		RedFlagUI.EstilizarBoton(boton, false);
		boton.AddThemeFontSizeOverride("font_size", 13);
		boton.Pressed += () =>
		{
			if (!ventana.Visible)
			{
				ventana.Visible = true;
				TraerAlFrente(ventana);
			}
			else if (capaVentanas.GetChild(capaVentanas.GetChildCount() - 1) == ventana)
			{
				ventana.Visible = false;
			}
			else
			{
				TraerAlFrente(ventana);
			}
		};
		barraTareas.AddChild(boton);

		abiertas[id] = new VentanaAbierta { Ventana = ventana, BotonTarea = boton };
	}

	/// <summary>
	/// Agrega un botón fijo a la barra de tareas (lo usa la ventana de Red Flag
	/// para poder minimizarse y volver a abrirse).
	/// </summary>
	public Button AgregarBotonTarea(string texto, Action alPresionar)
	{
		Button boton = new Button
		{
			Text = texto,
			ClipText = true,
			CustomMinimumSize = new Vector2(150, 0),
			FocusMode = FocusModeEnum.None
		};
		RedFlagUI.EstilizarBoton(boton, true);
		boton.AddThemeFontSizeOverride("font_size", 13);
		boton.Pressed += () => alPresionar?.Invoke();
		barraTareas.AddChild(boton);
		return boton;
	}

	private void CerrarApp(string id)
	{
		if (!abiertas.TryGetValue(id, out VentanaAbierta v)) return;

		abiertas.Remove(id);
		v.Ventana.QueueFree();
		v.BotonTarea.QueueFree();
	}
}
