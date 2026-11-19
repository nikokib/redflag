using Godot;
using System;

/// <summary>
/// Ventana "!! EMERGENCIA !!" que avisa que el jugador acumuló demasiados errores.
/// Ofrece APAGAR EQUIPO o REINICIAR EQUIPO, y se puede cerrar sin elegir ninguna.
/// Solo interfaz: ComputerGameplay decide cuándo mostrarla y cuándo retirarla.
/// </summary>
public partial class AlertaEmergencia : CanvasLayer
{
	public Action AlApagar;
	public Action AlReiniciar;
	public Action AlCerrarManual;

	private static readonly Color Rojo = new Color("ff2e3f");
	private static readonly Color Texto = new Color("b9c9d6");

	private Control raiz;
	private MarcoNeon panel;

	public bool EstaVisible => raiz != null && raiz.Visible;

	public override void _Ready()
	{
		Layer = 60;

		raiz = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		AddChild(raiz);
		raiz.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		panel = new MarcoNeon
		{
			ColorInicio = Rojo,
			ColorFin = new Color("d81f33"),
			Fondo = new Color(0.045f, 0.0f, 0.02f, 0.97f),
			Chaflan = 16f,
			Grosor = 3f,
			Margen = 18,
			CustomMinimumSize = new Vector2(660, 0),
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		raiz.AddChild(panel);

		VBoxContainer caja = new VBoxContainer();
		caja.AddThemeConstantOverride("separation", 10);
		panel.AddChild(caja);

		// --- Título con los dos triángulos ---
		HBoxContainer cabecera = new HBoxContainer();
		cabecera.AddThemeConstantOverride("separation", 10);
		cabecera.AddChild(IconoSistema.Crear(IconoSistema.Clase.Advertencia, 46, Rojo));
		Label titulo = RedFlagUI.CrearLabel("!! EMERGENCIA !!", 38, Rojo, HorizontalAlignment.Center);
		titulo.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		titulo.VerticalAlignment = VerticalAlignment.Center;
		cabecera.AddChild(titulo);
		cabecera.AddChild(IconoSistema.Crear(IconoSistema.Clase.Advertencia, 46, Rojo));
		caja.AddChild(cabecera);

		caja.AddChild(new ColorRect { Color = Rojo, CustomMinimumSize = new Vector2(0, 3), MouseFilter = Control.MouseFilterEnum.Ignore });

		// --- Ícono y mensaje ---
		IconoSistema aviso = IconoSistema.Crear(IconoSistema.Clase.Info, 60, Rojo);
		aviso.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		caja.AddChild(aviso);

		Label mensaje = RedFlagUI.CrearLabel("El sistema presenta múltiples alertas.\nSe recomienda tomar una acción inmediata.", 20, Texto, HorizontalAlignment.Center);
		mensaje.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		caja.AddChild(mensaje);

		// --- Botones ---
		HBoxContainer botones = new HBoxContainer();
		botones.AddThemeConstantOverride("separation", 24);
		Button apagar = CrearBoton("APAGAR EQUIPO");
		apagar.Pressed += () => { Ocultar(); AlApagar?.Invoke(); };
		Button reiniciar = CrearBoton("REINICIAR EQUIPO");
		reiniciar.Pressed += () => { Ocultar(); AlReiniciar?.Invoke(); };
		botones.AddChild(apagar);
		botones.AddChild(reiniciar);
		caja.AddChild(botones);

		// --- Cerrar sin elegir ---
		Button cerrar = new Button
		{
			Text = "Cerrar",
			Flat = true,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
		};
		cerrar.AddThemeFontOverride("font", RedFlagUI.Fuente);
		cerrar.AddThemeFontSizeOverride("font_size", 16);
		cerrar.AddThemeColorOverride("font_color", new Color("86a1ab"));
		cerrar.AddThemeColorOverride("font_hover_color", Colors.White);
		cerrar.Pressed += () => { Ocultar(); AlCerrarManual?.Invoke(); };
		caja.AddChild(cerrar);
	}

	private static Button CrearBoton(string texto)
	{
		Button b = new Button
		{
			Text = texto,
			CustomMinimumSize = new Vector2(0, 66),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		b.AddThemeStyleboxOverride("normal", Estilo(new Color("0c0408"), Rojo));
		b.AddThemeStyleboxOverride("hover", Estilo(new Color("30060e"), Colors.White));
		b.AddThemeStyleboxOverride("pressed", Estilo(new Color("45091a"), Colors.White));
		b.AddThemeStyleboxOverride("focus", Estilo(new Color(0, 0, 0, 0), Rojo));
		b.AddThemeFontOverride("font", RedFlagUI.Fuente);
		b.AddThemeFontSizeOverride("font_size", 24);
		b.AddThemeColorOverride("font_color", new Color("a9bccb"));
		b.AddThemeColorOverride("font_hover_color", Colors.White);
		return b;
	}

	private static StyleBoxFlat Estilo(Color fondo, Color borde)
	{
		StyleBoxFlat sb = new StyleBoxFlat { BgColor = fondo, BorderColor = borde };
		sb.SetBorderWidthAll(3);
		sb.SetCornerRadiusAll(10);
		sb.ContentMarginLeft = 12;
		sb.ContentMarginRight = 12;
		return sb;
	}

	private double tiempo = 0;

	// El marco late en rojo mientras la ventana está abierta
	public override void _Process(double delta)
	{
		if (raiz == null || !raiz.Visible) return;
		tiempo += delta;
		float brillo = 0.75f + 0.25f * (float)Math.Sin(tiempo * 6.0);
		panel.SelfModulate = new Color(brillo, brillo, brillo, 1f);
	}

	public void Mostrar()
	{
		if (raiz == null || raiz.Visible) return;

		Vector2 pantalla = GetViewport().GetVisibleRect().Size;
		Vector2 tam = panel.GetCombinedMinimumSize();
		panel.Size = tam;
		panel.Position = new Vector2((pantalla.X - tam.X) * 0.5f, Mathf.Max(8f, (pantalla.Y - tam.Y) * 0.42f));

		raiz.Visible = true;
		panel.Modulate = new Color(1, 1, 1, 0);
		Tween t = CreateTween();
		t.TweenProperty(panel, "modulate:a", 1f, 0.2f);
	}

	public void Ocultar()
	{
		if (raiz != null) raiz.Visible = false;
	}
}
