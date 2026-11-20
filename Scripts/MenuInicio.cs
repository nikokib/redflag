using Godot;
using System;

/// <summary>
/// Menú de Inicio del escritorio con el diseño neón (lista de apps a la izquierda y
/// "Opciones del sistema" con Apagar / Reiniciar a la derecha).
/// Solo interfaz: abrir una app, apagar o reiniciar se avisa con acciones.
/// </summary>
public partial class MenuInicio : CanvasLayer
{
	public Action<string> AlElegirApp;
	public Action AlApagar;
	public Action AlReiniciar;

	// Posición de la barra de tareas en la imagen del escritorio (para abrir el menú justo encima)
	private const float PH = 470f;
	private const float PW = 836f;
	private const float YBarra = 441f;

	private static readonly Color Cian = new Color("39b7cc");
	private static readonly Color CianOscuro = new Color("1b5f6e");
	private static readonly Color Texto = new Color("d4e6ea");
	private static readonly Color TextoSuave = new Color("86a1ab");

	private Control raiz;
	private MarcoNeon panel;
	private bool abierto = false;

	public bool EstaAbierto => abierto;

	public override void _Ready()
	{
		Layer = 50;

		raiz = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		AddChild(raiz);
		raiz.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		// Zona invisible que cierra el menú al tocar afuera
		Button captador = new Button
		{
			Flat = true,
			FocusMode = Control.FocusModeEnum.None,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		StyleBoxEmpty vacio = new StyleBoxEmpty();
		foreach (string estado in new[] { "normal", "hover", "pressed", "focus" })
			captador.AddThemeStyleboxOverride(estado, vacio);
		captador.Pressed += Cerrar;
		raiz.AddChild(captador);
		captador.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		panel = new MarcoNeon
		{
			ColorInicio = Cian,
			ColorFin = new Color("2a8fb0"),
			Chaflan = 10f,
			Margen = 8,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		raiz.AddChild(panel);
		panel.AddChild(ConstruirContenido());
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (abierto && evento.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			Cerrar();
		}
	}

	public void Alternar()
	{
		if (abierto) Cerrar();
		else Abrir();
	}

	public void Abrir()
	{
		if (abierto) return;
		abierto = true;

		Vector2 pantalla = GetViewport().GetVisibleRect().Size;
		Vector2 tam = panel.GetCombinedMinimumSize();
		panel.Size = tam;
		float x = pantalla.X * (4f / PW);
		float y = pantalla.Y * (YBarra / PH) - tam.Y - 4f;
		panel.Position = new Vector2(x, Mathf.Max(4f, y));

		raiz.Visible = true;
		panel.Modulate = new Color(1, 1, 1, 0);
		Tween t = CreateTween();
		t.TweenProperty(panel, "modulate:a", 1f, 0.12f);
	}

	public void Cerrar()
	{
		if (!abierto) return;
		abierto = false;
		raiz.Visible = false;
	}

	// ------------------------------------------------------------------
	// Contenido
	// ------------------------------------------------------------------

	private Control ConstruirContenido()
	{
		VBoxContainer principal = new VBoxContainer();
		principal.AddThemeConstantOverride("separation", 0);

		// --- Barra de título "Mi PC" ---
		HBoxContainer barra = new HBoxContainer();
		barra.AddThemeConstantOverride("separation", 10);
		barra.CustomMinimumSize = new Vector2(0, 40);
		barra.AddChild(IconoSistema.Crear(IconoSistema.Clase.Pc, 28, Cian));
		barra.AddChild(RedFlagUI.CrearLabel("Mi PC", 22, Texto));
		barra.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
		barra.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Minimizar, CustomMinimumSize = new Vector2(22, 22), AlHacerClic = Cerrar });
		barra.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Maximizar, CustomMinimumSize = new Vector2(22, 22) });
		barra.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Cerrar, CustomMinimumSize = new Vector2(22, 22), AlHacerClic = Cerrar });
		principal.AddChild(Margen(barra, 10, 4, 10, 4));
		principal.AddChild(Linea(CianOscuro, 1, true));

		// --- Cuerpo ---
		HBoxContainer cuerpo = new HBoxContainer();
		cuerpo.AddThemeConstantOverride("separation", 0);
		principal.AddChild(cuerpo);

		VBoxContainer lista = new VBoxContainer { CustomMinimumSize = new Vector2(250, 0) };
		lista.AddThemeConstantOverride("separation", 2);
		AgregarApps(lista, AppsEscritorio.Izquierda);
		AgregarApps(lista, AppsEscritorio.Derecha);
		cuerpo.AddChild(Margen(lista, 10, 10, 10, 10));

		cuerpo.AddChild(Linea(CianOscuro, 1, false));

		VBoxContainer sistema = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
		sistema.AddThemeConstantOverride("separation", 6);

		HBoxContainer cabecera = new HBoxContainer();
		cabecera.AddThemeConstantOverride("separation", 10);
		cabecera.CustomMinimumSize = new Vector2(0, 40);
		cabecera.AddChild(IconoSistema.Crear(IconoSistema.Clase.Ajustes, 26, Cian));
		Label titulo = RedFlagUI.CrearLabel("Opciones del sistema", 15, TextoSuave);
		titulo.VerticalAlignment = VerticalAlignment.Center;
		cabecera.AddChild(titulo);
		sistema.AddChild(cabecera);
		sistema.AddChild(Linea(CianOscuro, 1, true));

		Button apagar = CrearFila("Apagar", IconoSistema.Clase.Apagar, 40, true);
		apagar.Pressed += () => { Cerrar(); AlApagar?.Invoke(); };
		sistema.AddChild(apagar);

		Button reiniciar = CrearFila("Reiniciar", IconoSistema.Clase.Reiniciar, 40, false);
		reiniciar.Pressed += () => { Cerrar(); AlReiniciar?.Invoke(); };
		sistema.AddChild(reiniciar);

		cuerpo.AddChild(Margen(sistema, 10, 8, 10, 10));

		return principal;
	}

	private void AgregarApps(VBoxContainer destino, AppsEscritorio.AppDef[] apps)
	{
		foreach (AppsEscritorio.AppDef app in apps)
		{
			// "Mi PC" es el título del menú: sigue abriéndose desde su ícono del escritorio
			if (app.Id == "mipc") continue;

			string id = app.Id;
			Button b = CrearFila(app.Titulo, IconoSistema.DeApp(id), 34, false);
			b.Pressed += () =>
			{
				Cerrar();
				AlElegirApp?.Invoke(id);
			};
			destino.AddChild(b);
		}
	}

	private Button CrearFila(string texto, IconoSistema.Clase icono, float alto, bool destacado)
	{
		Button b = new Button
		{
			Text = texto,
			Alignment = HorizontalAlignment.Left,
			CustomMinimumSize = new Vector2(0, alto),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};

		Color transparente = new Color(0, 0, 0, 0);
		StyleBoxFlat normal = EstiloFila(destacado ? new Color("051a28") : transparente, destacado ? Cian : transparente);
		StyleBoxFlat hover = EstiloFila(new Color("0a2d40"), Cian);
		b.AddThemeStyleboxOverride("normal", normal);
		b.AddThemeStyleboxOverride("hover", hover);
		b.AddThemeStyleboxOverride("pressed", hover);
		b.AddThemeStyleboxOverride("focus", normal);
		b.AddThemeFontOverride("font", RedFlagUI.Fuente);
		b.AddThemeFontSizeOverride("font_size", 18);
		b.AddThemeColorOverride("font_color", Texto);
		b.AddThemeColorOverride("font_hover_color", Colors.White);
		b.AddThemeColorOverride("font_pressed_color", Colors.White);

		float lado = 22f;
		IconoSistema ico = IconoSistema.Crear(icono, lado);
		ico.Position = new Vector2(14, (alto - lado) * 0.5f);
		ico.Size = new Vector2(lado, lado);
		b.AddChild(ico);

		Label flecha = RedFlagUI.CrearLabel(">", 20, Cian);
		flecha.MouseFilter = Control.MouseFilterEnum.Ignore;
		flecha.Visible = false;
		flecha.AnchorLeft = 1f;
		flecha.AnchorRight = 1f;
		flecha.AnchorTop = 0.5f;
		flecha.AnchorBottom = 0.5f;
		flecha.OffsetLeft = -26f;
		flecha.OffsetRight = -8f;
		flecha.OffsetTop = -14f;
		flecha.OffsetBottom = 14f;
		b.AddChild(flecha);

		b.MouseEntered += () => flecha.Visible = true;
		b.MouseExited += () => flecha.Visible = false;
		return b;
	}

	private static StyleBoxFlat EstiloFila(Color fondo, Color borde)
	{
		StyleBoxFlat sb = new StyleBoxFlat { BgColor = fondo, BorderColor = borde };
		sb.SetBorderWidthAll(2);
		sb.SetCornerRadiusAll(8);
		sb.ContentMarginLeft = 50;
		sb.ContentMarginRight = 30;
		sb.ContentMarginTop = 2;
		sb.ContentMarginBottom = 2;
		return sb;
	}

	private static MarginContainer Margen(Control hijo, int izq, int arriba, int der, int abajo)
	{
		MarginContainer mc = new MarginContainer();
		mc.AddThemeConstantOverride("margin_left", izq);
		mc.AddThemeConstantOverride("margin_top", arriba);
		mc.AddThemeConstantOverride("margin_right", der);
		mc.AddThemeConstantOverride("margin_bottom", abajo);
		mc.AddChild(hijo);
		return mc;
	}

	private static ColorRect Linea(Color color, float grosor, bool horizontal)
	{
		return new ColorRect
		{
			Color = color,
			CustomMinimumSize = horizontal ? new Vector2(0, grosor) : new Vector2(grosor, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}
}
