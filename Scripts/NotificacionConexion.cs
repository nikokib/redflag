using Godot;
using System;

/// <summary>
/// Ventana "RED FLAG | SOLICITUD DE CONEXIÓN" con el diseño pixel-art del juego.
/// La imagen trae el marco, el avatar y los botones; acá se escriben encima los datos reales.
/// Solo muestra datos y avisa con eventos (Aceptado / Rechazado): la lógica queda en ComputerGameplay.
/// Se puede minimizar: queda un botón en la barra de tareas que parpadea si hay una solicitud esperando.
/// </summary>
public partial class NotificacionConexion : Control
{
	public event Action Aceptado;
	public event Action Rechazado;

	/// <summary>Se dispara al tocar la notificación (fuera de los botones): abre la ventana Red Flag.</summary>
	public event Action Clic;

	private const string RutaImagen = "res://Interfaz/notificacion_base.png";

	// Tamaño de la imagen original y ancho con el que se muestra en pantalla
	private const float BaseW = 1628f;
	private const float BaseH = 712f;
	private const float AnchoVentana = 440f;
	private const float E = AnchoVentana / BaseW;

	// Barra de tiempo (en coordenadas de la imagen original)
	private const float BarraX0 = 66f;
	private const float BarraX1 = 1560f;
	private const float BarraY0 = 546f;
	private const float BarraY1 = 582f;

	private Control ventana;
	private Label lblIP;
	private Label lblMAC;
	private Label lblRol;
	private Label lblArea;
	private Label lblAuto;
	private RichTextLabel lblPregunta;
	private ColorRect relleno;
	private readonly Color azulBarra = new Color("1a9bff");
	private readonly Color rojoBarra = new Color("ff3b34");
	private bool barraRoja = false;

	// Minimizar: la ventana se esconde y queda un botón en la barra de tareas
	private Button botonTarea;
	private bool minimizada = false;
	private bool haySolicitud = false;
	private bool appAbierta = false;
	private int segundosRestantes = 0;
	private double tiempoParpadeo = 0;

	/// <summary>Si es true, una solicitud nueva reabre la ventana aunque esté minimizada.</summary>
	public bool RestaurarAlLlegarSolicitud = false;

	public bool Minimizada => minimizada;

	private static Vector2 P(float x, float y) => new Vector2(x * E, y * E);

	private Label Texto(string texto, float x, float yCentro, int tamano, Color color)
	{
		Label l = RedFlagUI.CrearLabel(texto, tamano, color);
		l.MouseFilter = MouseFilterEnum.Ignore;
		l.VerticalAlignment = VerticalAlignment.Center;
		l.Position = new Vector2(x * E, yCentro * E - tamano * 0.75f);
		l.Size = new Vector2(BaseW * E - x * E, tamano * 1.5f);
		ventana.AddChild(l);
		return l;
	}

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;

		// Se ubica abajo a la derecha, por encima de la barra de tareas
		MarginContainer zona = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		zona.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		zona.AddThemeConstantOverride("margin_right", 12);
		zona.AddThemeConstantOverride("margin_bottom", 44);
		zona.AddThemeConstantOverride("margin_left", 12);
		zona.AddThemeConstantOverride("margin_top", 50);
		AddChild(zona);

		ventana = new Control
		{
			CustomMinimumSize = new Vector2(AnchoVentana, BaseH * E),
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
			SizeFlagsVertical = SizeFlags.ShrinkEnd,
			MouseFilter = MouseFilterEnum.Stop,
			Visible = false
		};
		zona.AddChild(ventana);

		// Imagen del diseño
		Texture2D tex = ResourceLoader.Exists(RutaImagen) ? GD.Load<Texture2D>(RutaImagen) : null;
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
			ventana.AddChild(img);
		}
		else
		{
			ColorRect respaldo = new ColorRect { Color = RedFlagUI.FondoVentana, MouseFilter = MouseFilterEnum.Ignore };
			respaldo.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			ventana.AddChild(respaldo);
		}

		Color valor = new Color("c9e3f2");
		Color acento = new Color("22e0c4");

		// --- Datos de quien se conecta ---
		lblIP = Texto("", 466f, 190f, 12, valor);
		lblMAC = Texto("", 466f, 257f, 12, valor);
		lblRol = Texto("", 1182f, 190f, 12, acento);
		lblArea = Texto("", 1182f, 257f, 12, acento);

		// --- Pregunta (el nombre va resaltado) ---
		lblPregunta = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = false,
			ScrollActive = false,
			AutowrapMode = TextServer.AutowrapMode.Off,
			MouseFilter = MouseFilterEnum.Ignore,
			Position = P(342f, 340f),
			Size = P(1230f, 140f)
		};
		lblPregunta.AddThemeFontOverride("normal_font", RedFlagUI.Fuente);
		lblPregunta.AddThemeFontSizeOverride("normal_font_size", 13);
		lblPregunta.AddThemeColorOverride("default_color", new Color("c9e3f2"));
		ventana.AddChild(lblPregunta);

		// --- Barra de tiempo ---
		relleno = new ColorRect
		{
			Color = azulBarra,
			MouseFilter = MouseFilterEnum.Ignore,
			Position = P(BarraX0, BarraY0),
			Size = P(BarraX1 - BarraX0, BarraY1 - BarraY0)
		};
		ventana.AddChild(relleno);

		// --- Texto de autoaceptar ---
		lblAuto = Texto("", 62f, 640f, 10, new Color("9db3c4"));

		// --- Toda la notificación abre la ventana Red Flag (los botones van encima) ---
		Button zonaAbrir = new Button
		{
			Flat = true,
			FocusMode = FocusModeEnum.None,
			TooltipText = "Abrir Red Flag",
			MouseDefaultCursorShape = CursorShape.PointingHand,
			Position = Vector2.Zero,
			Size = new Vector2(BaseW * E, BaseH * E)
		};
		StyleBoxEmpty sinEstilo = new StyleBoxEmpty();
		foreach (string estado in new[] { "normal", "hover", "pressed", "focus" })
			zonaAbrir.AddThemeStyleboxOverride(estado, sinEstilo);
		zonaAbrir.Pressed += () => Clic?.Invoke();
		ventana.AddChild(zonaAbrir);

		// --- Zonas clickeables sobre los botones dibujados en la imagen ---
		Button btnAceptar = EscritorioUI.CrearZonaPixeles(P(1223f, 410f), P(158f, 96f), "Permitir acceso");
		btnAceptar.Pressed += () => Aceptado?.Invoke();
		ventana.AddChild(btnAceptar);

		Button btnRechazar = EscritorioUI.CrearZonaPixeles(P(1415f, 410f), P(159f, 96f), "Rechazar");
		btnRechazar.Pressed += () => Rechazado?.Invoke();
		ventana.AddChild(btnRechazar);

		// Minimizar (_) y la X: una solicitud pendiente no se puede descartar, solo esconder
		Button btnMin = EscritorioUI.CrearZonaPixeles(P(1345f, 25f), P(90f, 80f), "Minimizar");
		btnMin.Pressed += () => Minimizar();
		ventana.AddChild(btnMin);

		Button btnCerrar = EscritorioUI.CrearZonaPixeles(P(1515f, 25f), P(95f, 80f), "Minimizar");
		btnCerrar.Pressed += () => Minimizar();
		ventana.AddChild(btnCerrar);

	}

	/// <summary>Muestra una nueva solicitud (reemplaza a la anterior).</summary>
	public void Mostrar(string nombre, string rol, string area, string ip, string mac)
	{
		lblIP.Text = ip;
		lblMAC.Text = mac;
		lblRol.Text = rol;
		lblArea.Text = area;

		string nombreSeguro = nombre.Replace("[", "[lb]");
		lblPregunta.Text = $"[color=#22e0c4]{nombreSeguro}[/color] quiere conectarse a tu red.\n¿Permitir acceso?";

		haySolicitud = true;
		if (minimizada && RestaurarAlLlegarSolicitud) minimizada = false;
		AplicarVisibilidad();
		ActualizarBotonTarea();

		// Pequeño parpadeo para que se note que llegó una solicitud nueva
		ventana.Modulate = new Color(1, 1, 1, 0.2f);
		CreateTween().TweenProperty(ventana, "modulate:a", 1f, 0.25f);
	}

	/// <summary>Actualiza la barra y el texto con el tiempo que falta para la autoaceptación.</summary>
	public void ActualizarTiempo(double restante, double total)
	{
		if (relleno == null) return;

		double r = Math.Max(0, restante);
		double proporcion = total > 0 ? r / total : 0;

		float anchoMax = (BarraX1 - BarraX0) * E;
		relleno.Size = new Vector2(anchoMax * (float)proporcion, (BarraY1 - BarraY0) * E);

		int segundos = (int)Math.Ceiling(r);
		segundosRestantes = segundos;
		if (minimizada && !appAbierta) ActualizarBotonTarea();
		lblAuto.Text = $"Se aceptará la solicitud automáticamente en {segundos} {(segundos == 1 ? "segundo" : "segundos")}.";

		bool urgente = proporcion < 0.3;
		if (urgente != barraRoja)
		{
			barraRoja = urgente;
			relleno.Color = urgente ? rojoBarra : azulBarra;
			lblAuto.AddThemeColorOverride("font_color", urgente ? new Color("ff6b63") : new Color("9db3c4"));
		}
	}

	public void Ocultar()
	{
		haySolicitud = false;
		AplicarVisibilidad();
		ActualizarBotonTarea();
	}

	/// <summary>
	/// Avisa si la ventana grande de Red Flag está abierta: mientras lo está, la notificación
	/// emergente se esconde (la solicitud ya se ve ahí) y el botón de la barra no parpadea.
	/// </summary>
	public void AppAbierta(bool abierta)
	{
		appAbierta = abierta;
		AplicarVisibilidad();
		ActualizarBotonTarea();
	}

	private void AplicarVisibilidad()
	{
		if (ventana != null) ventana.Visible = haySolicitud && !minimizada && !appAbierta;
	}

	// ------------------------------------------------------------------
	// Minimizar / restaurar
	// ------------------------------------------------------------------

	/// <summary>Enlaza el botón de la barra de tareas que representa a esta ventana.</summary>
	public void ConectarBotonTarea(Button boton)
	{
		botonTarea = boton;
		ActualizarBotonTarea();
	}

	public void Minimizar()
	{
		minimizada = true;
		AplicarVisibilidad();
		ActualizarBotonTarea();
	}

	public void Restaurar()
	{
		minimizada = false;
		AplicarVisibilidad();
		ActualizarBotonTarea();
	}

	/// <summary>Para el botón de la barra de tareas: abre o minimiza la ventana.</summary>
	public void Alternar()
	{
		if (minimizada) Restaurar();
		else Minimizar();
	}

	private void ActualizarBotonTarea()
	{
		if (botonTarea == null) return;

		bool parpadea = minimizada && haySolicitud && !appAbierta;

		if (parpadea)
			botonTarea.Text = $"RED FLAG ({segundosRestantes}s)";
		else
			botonTarea.Text = "RED FLAG";

		if (!parpadea)
		{
			botonTarea.Modulate = Colors.White;
			botonTarea.AddThemeColorOverride("font_color", RedFlagUI.Texto);
		}
	}

	// Mientras está minimizada y esperando respuesta, el botón parpadea en rojo
	public override void _Process(double delta)
	{
		if (botonTarea == null || !(minimizada && haySolicitud && !appAbierta)) return;

		tiempoParpadeo += delta;
		bool encendido = Math.Sin(tiempoParpadeo * 8.0) > 0;
		botonTarea.AddThemeColorOverride("font_color", encendido ? new Color("ff6b63") : Colors.White);
		botonTarea.Modulate = encendido ? new Color(1f, 0.85f, 0.85f) : Colors.White;
	}
}
