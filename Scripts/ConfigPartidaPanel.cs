using Godot;
using System;

/// <summary>
/// Panel que aparece al sentarse en una silla: elegir modo, dificultad y duración.
/// Ventana estilo Red Flag OS armada por código (barra de título con la bandera roja,
/// recuadro de configuración, objetivo y botones).
/// </summary>
public partial class ConfigPartidaPanel : CanvasLayer
{
	public Action AlComenzar;
	public Action AlCancelar;

	private OptionButton selModo;
	private OptionButton selNivel;
	private OptionButton selDuracion;
	private Label lblDescripcion;

	private static readonly int[] Duraciones = { 1, 2, 3, 5 };

	public override void _Ready()
	{
		Layer = 50;

		ColorRect fondo = new ColorRect { Color = new Color(0, 0, 0, 0.75f) };
		fondo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(fondo);

		CenterContainer centro = new CenterContainer();
		centro.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(centro);

		// Ventana con la bandera roja como logo de la barra de título
		PanelContainer ventana = RedFlagUI.CrearVentana(
			"INICIAR TURNO",
			new Vector2(520, 417),
			out PanelContainer contenido,
			null,
			IconoVentana.Clase.Logo,
			null,
			OnCancelar,
			null,
			IconoSistema.Crear(IconoSistema.Clase.RedFlag, 22));
		centro.AddChild(ventana);

		MarginContainer margen = new MarginContainer();
		margen.AddThemeConstantOverride("margin_left", 16);
		margen.AddThemeConstantOverride("margin_right", 16);
		margen.AddThemeConstantOverride("margin_top", 16);
		margen.AddThemeConstantOverride("margin_bottom", 14);
		contenido.AddChild(margen);

		VBoxContainer columna = new VBoxContainer();
		columna.AddThemeConstantOverride("separation", 12);
		margen.AddChild(columna);

		// --- Configuración del turno ---
		PanelContainer panelConfig = CrearRecuadro(RedFlagUI.Borde);
		columna.AddChild(panelConfig);

		VBoxContainer interior = new VBoxContainer();
		interior.AddThemeConstantOverride("separation", 12);
		panelConfig.AddChild(interior);
		interior.AddChild(RedFlagUI.CrearLabel("CONFIGURACIÓN DEL TURNO", 14, RedFlagUI.Texto));

		string[] textosDuracion = new string[Duraciones.Length];
		int indiceDuracion = 0;
		for (int i = 0; i < Duraciones.Length; i++)
		{
			textosDuracion[i] = Duraciones[i] + (Duraciones[i] == 1 ? " minuto" : " minutos");
			if (Duraciones[i] == PartidaConfig.DuracionMinutos) indiceDuracion = i;
		}

		selModo = CrearFila(interior, "Modo", new[] { "Turno de guardia", "Infinito" }, (int)PartidaConfig.Modo);
		selNivel = CrearFila(interior, "Dificultad", new[] { "Fácil", "Normal", "Difícil" }, (int)PartidaConfig.Nivel);
		selDuracion = CrearFila(interior, "Duración", textosDuracion, indiceDuracion);

		// --- Objetivo ---
		PanelContainer panelObjetivo = CrearRecuadro(RedFlagUI.Borde);
		panelObjetivo.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		columna.AddChild(panelObjetivo);

		VBoxContainer interiorObj = new VBoxContainer();
		interiorObj.AddThemeConstantOverride("separation", 8);
		panelObjetivo.AddChild(interiorObj);
		interiorObj.AddChild(RedFlagUI.CrearLabel("OBJETIVO", 14, RedFlagUI.Acento));

		lblDescripcion = RedFlagUI.CrearLabel("", 13, RedFlagUI.TextoApagado);
		lblDescripcion.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		lblDescripcion.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		lblDescripcion.CustomMinimumSize = new Vector2(0, 62);
		interiorObj.AddChild(lblDescripcion);

		// --- Botones ---
		HBoxContainer botones = new HBoxContainer();
		botones.AddThemeConstantOverride("separation", 12);
		columna.AddChild(botones);

		Button btnComenzar = RedFlagUI.CrearBoton("COMENZAR", new Vector2(0, 44), true);
		btnComenzar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		btnComenzar.AddThemeStyleboxOverride("normal", RedFlagUI.Caja(RedFlagUI.FondoPanel, RedFlagUI.BordeVivo, 2, 3, 8));
		btnComenzar.Pressed += OnComenzar;
		botones.AddChild(btnComenzar);

		Button btnCancelar = RedFlagUI.CrearBoton("CANCELAR", new Vector2(0, 44));
		btnCancelar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		btnCancelar.Pressed += OnCancelar;
		botones.AddChild(btnCancelar);

		selModo.ItemSelected += _ => ActualizarDescripcion();
		ActualizarDescripcion();
		btnComenzar.GrabFocus();
	}

	public override void _Input(InputEvent evento)
	{
		if (evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo && tecla.Keycode == Key.Escape)
		{
			GetViewport().SetInputAsHandled();
			OnCancelar();
		}
	}

	private static PanelContainer CrearRecuadro(Color borde)
	{
		PanelContainer p = new PanelContainer();
		StyleBoxFlat sb = RedFlagUI.Caja(RedFlagUI.FondoPanel, borde, 1, 3);
		sb.ContentMarginLeft = 14;
		sb.ContentMarginRight = 14;
		sb.ContentMarginTop = 12;
		sb.ContentMarginBottom = 12;
		p.AddThemeStyleboxOverride("panel", sb);
		return p;
	}

	private OptionButton CrearFila(VBoxContainer padre, string etiqueta, string[] opciones, int seleccionado)
	{
		HBoxContainer fila = new HBoxContainer();
		fila.AddThemeConstantOverride("separation", 10);
		padre.AddChild(fila);

		Label lbl = RedFlagUI.CrearLabel(etiqueta, 14, RedFlagUI.TextoApagado);
		lbl.CustomMinimumSize = new Vector2(112, 0);
		lbl.VerticalAlignment = VerticalAlignment.Center;
		fila.AddChild(lbl);

		OptionButton o = new OptionButton { Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 36) };
		o.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		RedFlagUI.EstilizarOpcion(o);
		o.AddThemeFontSizeOverride("font_size", 15);
		o.GetPopup().AddThemeFontSizeOverride("font_size", 15);
		foreach (string t in opciones) o.AddItem(t);
		o.Selected = seleccionado;
		fila.AddChild(o);
		return o;
	}

	private void ActualizarDescripcion()
	{
		bool infinito = selModo.Selected == (int)ModoJuego.Infinito;
		selDuracion.Disabled = infinito;

		lblDescripcion.Text = infinito
			? "Infinito: no hay tiempo límite. Cada 10 conexiones aparecen más virus y se acorta el tiempo para decidir. Aguantá lo más que puedas."
			: "Turno de guardia: sobrevivi el tiempo elegido filtrando conexiones. Si superás el turno, ganás.";
	}

	private void OnComenzar()
	{
		PartidaConfig.Modo = (ModoJuego)selModo.Selected;
		PartidaConfig.Nivel = (Dificultad)selNivel.Selected;
		PartidaConfig.DuracionMinutos = Duraciones[selDuracion.Selected];
		AlComenzar?.Invoke();
	}

	private void OnCancelar()
	{
		AlCancelar?.Invoke();
		QueueFree();
	}
}
