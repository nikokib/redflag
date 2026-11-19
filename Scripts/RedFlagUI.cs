using Godot;
using System;

/// <summary>
/// Estilo visual compartido ("Red Flag OS"): colores, fuente, ventana con barra
/// de título y botones. Solo estética: no contiene lógica de juego.
/// </summary>
public static class RedFlagUI
{
	// --- Paleta (sacada de los diseños) ---
	public static readonly Color FondoVentana = new Color("010b16");
	public static readonly Color FondoBarra = new Color("00111a");
	public static readonly Color FondoPanel = new Color("020f1c");
	public static readonly Color Borde = new Color("1b5f6e");
	public static readonly Color BordeVivo = new Color("39b7cc");
	public static readonly Color Texto = new Color("d4e6ea");
	public static readonly Color TextoApagado = new Color("86a1ab");
	public static readonly Color Acento = new Color("4dd0a0");
	public static readonly Color Rojo = new Color("ff3b34");

	private static SystemFont fuente;

	/// <summary>
	/// Fuente monoespaciada del sistema (Consolas en Windows). Si no existe,
	/// Godot usa la siguiente de la lista.
	/// </summary>
	public static Font Fuente
	{
		get
		{
			if (fuente == null)
			{
				fuente = new SystemFont
				{
					FontNames = new[] { "Consolas", "Lucida Console", "DejaVu Sans Mono", "Courier New", "monospace" },
					FontWeight = 500
				};
			}
			return fuente;
		}
	}

	public static StyleBoxFlat Caja(Color fondo, Color borde, int grosor = 1, int radio = 2, int margen = 0)
	{
		StyleBoxFlat sb = new StyleBoxFlat { BgColor = fondo, BorderColor = borde };
		sb.SetBorderWidthAll(grosor);
		sb.SetCornerRadiusAll(radio);
		if (margen > 0) sb.SetContentMarginAll(margen);
		return sb;
	}

	public static Label CrearLabel(string texto, int tamano, Color color, HorizontalAlignment alineacion = HorizontalAlignment.Left)
	{
		Label l = new Label { Text = texto, HorizontalAlignment = alineacion };
		l.AddThemeFontOverride("font", Fuente);
		l.AddThemeFontSizeOverride("font_size", tamano);
		l.AddThemeColorOverride("font_color", color);
		return l;
	}

	public static Button CrearBoton(string texto, Vector2 minimo, bool primario = false)
	{
		Button b = new Button { Text = texto, CustomMinimumSize = minimo };
		EstilizarBoton(b, primario);
		return b;
	}

	public static void EstilizarBoton(Button b, bool primario)
	{
		Color borde = primario ? BordeVivo : Borde;
		Color transparente = new Color(0, 0, 0, 0);

		b.AddThemeStyleboxOverride("normal", Caja(FondoPanel, borde, 1, 2, 8));
		b.AddThemeStyleboxOverride("hover", Caja(new Color("06202e"), BordeVivo, 1, 2, 8));
		b.AddThemeStyleboxOverride("pressed", Caja(new Color("0a3445"), Acento, 1, 2, 8));
		b.AddThemeStyleboxOverride("disabled", Caja(FondoPanel, new Color("12303a"), 1, 2, 8));
		b.AddThemeStyleboxOverride("focus", Caja(transparente, BordeVivo, 2, 2, 8));

		b.AddThemeFontOverride("font", Fuente);
		b.AddThemeFontSizeOverride("font_size", 16);
		b.AddThemeColorOverride("font_color", Texto);
		b.AddThemeColorOverride("font_hover_color", Colors.White);
		b.AddThemeColorOverride("font_pressed_color", Acento);
		b.AddThemeColorOverride("font_focus_color", Colors.White);
		b.AddThemeColorOverride("font_disabled_color", new Color("4a6670"));
	}

	public static void EstilizarOpcion(OptionButton o)
	{
		Color transparente = new Color(0, 0, 0, 0);

		o.AddThemeStyleboxOverride("normal", Caja(FondoPanel, Borde, 1, 2, 8));
		o.AddThemeStyleboxOverride("hover", Caja(new Color("06202e"), BordeVivo, 1, 2, 8));
		o.AddThemeStyleboxOverride("pressed", Caja(new Color("0a3445"), Acento, 1, 2, 8));
		o.AddThemeStyleboxOverride("disabled", Caja(FondoPanel, new Color("12303a"), 1, 2, 8));
		o.AddThemeStyleboxOverride("focus", Caja(transparente, BordeVivo, 2, 2, 8));

		o.AddThemeFontOverride("font", Fuente);
		o.AddThemeFontSizeOverride("font_size", 16);
		o.AddThemeColorOverride("font_color", Texto);
		o.AddThemeColorOverride("font_hover_color", Colors.White);
		o.AddThemeColorOverride("font_pressed_color", Acento);
		o.AddThemeColorOverride("font_focus_color", Colors.White);
		o.AddThemeColorOverride("font_disabled_color", new Color("4a6670"));

		// Lista desplegable
		PopupMenu popup = o.GetPopup();
		popup.AddThemeStyleboxOverride("panel", Caja(FondoVentana, BordeVivo, 1, 2, 4));
		popup.AddThemeStyleboxOverride("hover", Caja(new Color("0a3445"), BordeVivo, 1, 2));
		popup.AddThemeFontOverride("font", Fuente);
		popup.AddThemeFontSizeOverride("font_size", 16);
		popup.AddThemeColorOverride("font_color", Texto);
		popup.AddThemeColorOverride("font_hover_color", Colors.White);
	}

	/// <summary>
	/// Ventana con barra de título "RED FLAG | TÍTULO". Devuelve la ventana
	/// (para agregarla donde quieras) y, en <paramref name="contenido"/>, el
	/// contenedor interior donde va el contenido.
	/// </summary>
	public static PanelContainer CrearVentana(
		string titulo,
		Vector2 minimo,
		out PanelContainer contenido,
		Color? fondoContenido = null,
		IconoVentana.Clase logo = IconoVentana.Clase.Logo,
		Action alMinimizar = null,
		Action alCerrar = null,
		Action<InputEvent> alEventoBarra = null,
		Control icono = null,
		bool mostrarMarca = true,
		Action alMaximizar = null)
	{
		PanelContainer ventana = new PanelContainer { CustomMinimumSize = minimo };
		ventana.AddThemeStyleboxOverride("panel", Caja(FondoVentana, Borde, 1, 3));

		VBoxContainer raiz = new VBoxContainer();
		raiz.AddThemeConstantOverride("separation", 0);
		ventana.AddChild(raiz);

		// Barra de título
		PanelContainer barra = new PanelContainer();
		StyleBoxFlat estiloBarra = new StyleBoxFlat { BgColor = FondoBarra, BorderColor = Borde };
		estiloBarra.BorderWidthBottom = 1;
		estiloBarra.ContentMarginLeft = 10;
		estiloBarra.ContentMarginRight = 10;
		estiloBarra.ContentMarginTop = 7;
		estiloBarra.ContentMarginBottom = 7;
		barra.AddThemeStyleboxOverride("panel", estiloBarra);
		raiz.AddChild(barra);

		if (alEventoBarra != null)
		{
			barra.MouseFilter = Control.MouseFilterEnum.Stop;
			barra.GuiInput += (InputEvent e) => alEventoBarra(e);
		}

		HBoxContainer fila = new HBoxContainer();
		fila.AddThemeConstantOverride("separation", 10);
		barra.AddChild(fila);

		// Cada app puede traer su propio ícono; la bandera roja es solo de Red Flag
		fila.AddChild(icono ?? new IconoVentana { Tipo = logo });
		if (mostrarMarca)
		{
			fila.AddChild(CrearLabel("RED FLAG", 15, Colors.White));
			fila.AddChild(CrearLabel("|", 15, Borde));
			fila.AddChild(CrearLabel(titulo, 15, TextoApagado));
		}
		else
		{
			fila.AddChild(CrearLabel(titulo, 15, Colors.White));
		}
		fila.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
		fila.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Minimizar, AlHacerClic = alMinimizar });
		fila.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Maximizar, AlHacerClic = alMaximizar });
		fila.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Cerrar, AlHacerClic = alCerrar });

		// Zona de contenido
		contenido = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		contenido.AddThemeStyleboxOverride("panel",
			Caja(fondoContenido ?? FondoVentana, new Color(0, 0, 0, 0), 0, 0));
		raiz.AddChild(contenido);

		return ventana;
	}
}
