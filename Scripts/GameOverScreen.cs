using Godot;
using System;

/// <summary>
/// Pantalla final con animación de glitch. Se arma por código.
/// Derrota: ocupa toda la pantalla, aparece de golpe y con efectos de terror.
/// Victoria: ventana centrada y más calma.
/// Uso: AddChild(new GameOverScreen { Victoria = false, Motivo = "...", Resumen = "..." });
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
	public bool Victoria = false;
	public string Motivo = "";
	public string Resumen = "";

	private Control contenedor;
	private Label lblTitulo;
	private Label ecoCian;
	private Label ecoRojo;
	private ColorRect parpadeo;
	private Color parpadeoColor = Colors.Black;
	private float parpadeoAlfa = 0f;
	private double proximoParpadeo = 1.2;

	private readonly Random rnd = new Random();
	private string tituloFinal;
	private double t = 0;
	private float intensidad = 0;
	private bool tituloListo = false;
	private const string CaracteresGlitch = "#@%&$?!/<>01";

	public override void _Ready()
	{
		Layer = 100;

		// Victoria: se muestra la imagen del diseño completa
		if (Victoria && ResourceLoader.Exists("res://Interfaz/pantalla_victoria.png"))
		{
			ConstruirVictoria(GD.Load<Texture2D>("res://Interfaz/pantalla_victoria.png"));
			return;
		}

		tituloFinal = Victoria ? "TURNO COMPLETADO" : "EL SISTEMA HA COLAPSADO";
		Color colorTitulo = Victoria ? new Color("7fd6c2") : RedFlagUI.Rojo;
		intensidad = Victoria ? 0f : 40f;

		Vector2 pantalla = GetViewport().GetVisibleRect().Size;
		int tamTitulo = Victoria ? 40 : (int)Math.Clamp(pantalla.X * 0.055f, 36f, 84f);
		int tamMotivo = Victoria ? 18 : 24;
		int tamResumen = Victoria ? 14 : 16;
		Vector2 tamBoton = Victoria ? new Vector2(170, 40) : new Vector2(210, 48);

		// Derrota: negro-rojizo total desde el primer cuadro. Victoria: se oscurece de a poco.
		ColorRect fondo = new ColorRect
		{
			Color = Victoria ? new Color(0.03f, 0.03f, 0.05f, 0f) : new Color(0.02f, 0f, 0.01f, 1f)
		};
		fondo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(fondo);

		contenedor = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		contenedor.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(contenedor);

		// Ventana estilo Red Flag (barra de título + interferencia de fondo)
		// Barra de título: solo la banderita y "RED FLAG"
		PanelContainer ventana = RedFlagUI.CrearVentana(
			"RED FLAG",
			Victoria ? new Vector2(760, 0) : Vector2.Zero,
			out PanelContainer contenido,
			new Color("020409"),
			IconoVentana.Clase.Logo,
			null, null, null,
			IconoSistema.Crear(IconoSistema.Clase.RedFlag, 20),
			false);

		if (Victoria)
		{
			CenterContainer centro = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			centro.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			contenedor.AddChild(centro);
			centro.AddChild(ventana);
		}
		else
		{
			// La ventana ocupa TODA la pantalla
			ventana.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			contenedor.AddChild(ventana);
		}

		contenido.AddChild(new RuidoGlitch
		{
			ColorBase = Victoria ? new Color(0.25f, 0.85f, 0.75f) : new Color(0.95f, 0.1f, 0.1f),
			Densidad = Victoria ? 0.3f : 1.6f
		});

		// Viñeta oscura en los bordes (solo derrota)
		if (!Victoria)
		{
			Gradient degradado = new Gradient();
			degradado.SetColor(0, new Color(0, 0, 0, 0));
			degradado.SetColor(1, new Color(0, 0, 0, 0.92f));
			degradado.SetOffset(0, 0.40f);

			GradientTexture2D texturaVineta = new GradientTexture2D
			{
				Gradient = degradado,
				Fill = GradientTexture2D.FillEnum.Radial,
				FillFrom = new Vector2(0.5f, 0.5f),
				FillTo = new Vector2(1f, 0.5f),
				Width = 256,
				Height = 256
			};

			TextureRect vineta = new TextureRect
			{
				Texture = texturaVineta,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			contenido.AddChild(vineta);
		}

		CenterContainer centroContenido = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		contenido.AddChild(centroContenido);

		MarginContainer margen = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		margen.AddThemeConstantOverride("margin_top", 32);
		margen.AddThemeConstantOverride("margin_bottom", 32);
		margen.AddThemeConstantOverride("margin_left", 24);
		margen.AddThemeConstantOverride("margin_right", 24);
		centroContenido.AddChild(margen);

		VBoxContainer caja = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		caja.AddThemeConstantOverride("separation", Victoria ? 14 : 20);
		margen.AddChild(caja);

		// Título (en derrota lleva dos "ecos" de color desfasados, efecto de señal rota)
		Control cajaTitulo = new Control
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(Victoria ? 640f : pantalla.X * 0.9f, tamTitulo * 1.4f)
		};
		caja.AddChild(cajaTitulo);

		if (!Victoria)
		{
			ecoCian = CrearLabelTitulo(tamTitulo, new Color(0.1f, 0.9f, 1f, 0.5f));
			cajaTitulo.AddChild(ecoCian);
			ecoRojo = CrearLabelTitulo(tamTitulo, new Color(1f, 0f, 0.2f, 0.5f));
			cajaTitulo.AddChild(ecoRojo);
		}

		lblTitulo = CrearLabelTitulo(tamTitulo, colorTitulo);
		// Resplandor alrededor del título
		lblTitulo.AddThemeConstantOverride("outline_size", Victoria ? 6 : 10);
		lblTitulo.AddThemeColorOverride("font_outline_color",
			Victoria ? new Color(0.1f, 0.35f, 0.3f, 0.6f) : new Color(0.45f, 0.02f, 0.02f, 0.7f));
		cajaTitulo.AddChild(lblTitulo);

		Label lblMotivo = RedFlagUI.CrearLabel(Motivo, tamMotivo,
			Victoria ? RedFlagUI.Texto : new Color("ff7a73"), HorizontalAlignment.Center);
		caja.AddChild(lblMotivo);

		Label lblResumen = RedFlagUI.CrearLabel(Resumen, tamResumen,
			Victoria ? RedFlagUI.TextoApagado : new Color("c9a3a0"), HorizontalAlignment.Center);
		caja.AddChild(lblResumen);

		HBoxContainer botones = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			Visible = false
		};
		botones.AddThemeConstantOverride("separation", 16);
		caja.AddChild(botones);

		Button btnReintentar = RedFlagUI.CrearBoton("REINTENTAR", tamBoton, true);
		btnReintentar.Pressed += () => GetTree().ReloadCurrentScene();
		botones.AddChild(btnReintentar);

		Button btnMenu = RedFlagUI.CrearBoton("MENÚ PRINCIPAL", tamBoton);
		btnMenu.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MenuPrincipal.tscn");
		botones.AddChild(btnMenu);

		if (!Victoria)
		{
			// Destello rojo de golpe al aparecer
			ColorRect destello = new ColorRect
			{
				Color = new Color(0.9f, 0.05f, 0.05f, 0.9f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			destello.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			AddChild(destello);
			CreateTween().TweenProperty(destello, "color:a", 0f, 1.0f);

			// Capa para los parpadeos aleatorios (siempre arriba de todo)
			parpadeo = new ColorRect
			{
				Color = new Color(0, 0, 0, 0),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			parpadeo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			AddChild(parpadeo);
		}
		else
		{
			// La pantalla se oscurece de a poco
			CreateTween().TweenProperty(fondo, "color:a", 0.88f, 0.9f);
		}

		// Los botones aparecen al final de la animación
		Tween secuencia = CreateTween();
		secuencia.TweenInterval(1.8);
		secuencia.TweenCallback(Callable.From(() =>
		{
			botones.Visible = true;
			btnReintentar.GrabFocus();
		}));
	}

	// Pantalla de victoria: la imagen del diseño ocupa toda la pantalla, con zonas de clic
	// sobre sus botones (REINTENTAR y MENÚ PRINCIPAL) y el resumen en el espacio libre.
	private const float VictAncho = 1672f;
	private const float VictAlto = 941f;

	private static void PonerVict(Control c, float x0, float y0, float x1, float y1)
	{
		c.AnchorLeft = x0 / VictAncho;
		c.AnchorRight = x1 / VictAncho;
		c.AnchorTop = y0 / VictAlto;
		c.AnchorBottom = y1 / VictAlto;
		c.OffsetLeft = c.OffsetRight = c.OffsetTop = c.OffsetBottom = 0;
	}

	private bool victoriaImagen = false;
	private ShaderMaterial glitchVictoria;
	private double glitchTiempo = 0;      // tiempo dentro del glitch actual
	private double glitchProximo = 1.6;   // segundos hasta el próximo glitch
	private const double GlitchDuracion = 0.7;
	private double glitchPaso = 0;

	private const string ShaderGlitchSuave = @"
shader_type canvas_item;
uniform float fuerza : hint_range(0.0, 1.0) = 0.0;
uniform float semilla = 0.0;

float azar(float n)
{
	return fract(sin(n * 127.1 + semilla * 311.7) * 43758.5453);
}

void fragment()
{
	vec2 uv = UV;

	// Algunas franjas horizontales se corren apenas hacia un costado
	float fila = floor(uv.y * 26.0);
	float activa = step(0.80, azar(fila)) * fuerza;
	uv.x += (azar(fila + 17.0) - 0.5) * 0.010 * activa;

	// Separación de color muy leve (más marcada en las franjas)
	float sep = 0.0018 * fuerza + 0.0035 * activa;
	vec4 c = texture(TEXTURE, uv);
	float r = texture(TEXTURE, uv + vec2(sep, 0.0)).r;
	float b = texture(TEXTURE, uv - vec2(sep, 0.0)).b;
	COLOR = vec4(r, c.g, b, c.a);
}
";

	// Zona de clic invisible sobre un botón de la imagen (se ilumina apenas al pasar el mouse)
	private static Button ZonaBoton(float x0, float y0, float x1, float y1)
	{
		StyleBoxFlat Caja(float alfa)
		{
			StyleBoxFlat sb = new StyleBoxFlat { BgColor = new Color(0.6f, 0.9f, 1f, alfa) };
			sb.SetCornerRadiusAll(10);
			return sb;
		}

		Button b = new Button { Text = "" };
		b.AddThemeStyleboxOverride("normal", Caja(0f));
		b.AddThemeStyleboxOverride("hover", Caja(0.10f));
		b.AddThemeStyleboxOverride("pressed", Caja(0.20f));
		b.AddThemeStyleboxOverride("focus", Caja(0.05f));
		b.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
		PonerVict(b, x0, y0, x1, y1);
		return b;
	}

	// Pantalla de victoria: es la imagen del diseño, tal cual, con un glitch suave encima.
	private void ConstruirVictoria(Texture2D tex)
	{
		victoriaImagen = true;
		intensidad = 0f;
		proximoParpadeo = 2.5;

		// Fondo fijo (no tiembla)
		ColorRect fondo = new ColorRect { Color = new Color(0.02f, 0.03f, 0.05f), MouseFilter = Control.MouseFilterEnum.Stop };
		fondo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(fondo);

		contenedor = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0) };
		contenedor.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(contenedor);

		TextureRect CrearImagen(Color modulado, bool aditivo)
		{
			TextureRect r = new TextureRect
			{
				Texture = tex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Modulate = modulado
			};
			r.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			if (aditivo) r.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
			return r;
		}

		TextureRect imagen = CrearImagen(Colors.White, false);
		glitchVictoria = new ShaderMaterial { Shader = new Shader { Code = ShaderGlitchSuave } };
		glitchVictoria.SetShaderParameter("fuerza", 0f);
		imagen.Material = glitchVictoria;
		contenedor.AddChild(imagen);

		ConstruirDatosVictoria();

		// Botones: zonas de clic sobre los de la imagen
		Button btnReintentar = ZonaBoton(432, 795, 814, 875);
		btnReintentar.Pressed += () => GetTree().ReloadCurrentScene();
		contenedor.AddChild(btnReintentar);

		Button btnMenu = ZonaBoton(858, 795, 1240, 875);
		btnMenu.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MenuPrincipal.tscn");
		contenedor.AddChild(btnMenu);

		// La imagen aparece de a poco y el glitch entra recién después
		CreateTween().TweenProperty(contenedor, "modulate:a", 1f, 1.2f);
		btnReintentar.GrabFocus();
	}

	// Datos de la partida (se leen del Resumen) dibujados en el espacio libre de la imagen
	private void ConstruirDatosVictoria()
	{
		string[] orden = { "Virus rechazados", "Virus aceptados", "Conexiones revisadas",
			"Autoaceptadas", "Puntos", "Tiempo" };
		Color[] colores = { new Color("5fd6a8"), new Color("ff6b63"), RedFlagUI.Texto,
			RedFlagUI.Texto, new Color("7fb8ff"), new Color("7fb8ff") };

		System.Collections.Generic.Dictionary<string, string> datos = new System.Collections.Generic.Dictionary<string, string>();
		foreach (string linea in Resumen.Split('\n'))
		{
			int i = linea.IndexOf(": ", StringComparison.Ordinal);
			if (i > 0) datos[linea.Substring(0, i).Trim()] = linea.Substring(i + 2).Trim();
		}

		float altoPantalla = GetViewport().GetVisibleRect().Size.Y;
		int tamEtiqueta = (int)Math.Clamp(altoPantalla * 0.0125f, 9f, 16f);
		int tamValor = (int)Math.Clamp(altoPantalla * 0.026f, 16f, 32f);

		const int columnas = 3;
		float x0 = 508f, x1 = 1164f, y0 = 570f, filaAlto = 62f, hueco = 12f;
		float ancho = (x1 - x0 - hueco * (columnas - 1)) / columnas;

		int n = 0;
		for (int k = 0; k < orden.Length; k++)
		{
			if (!datos.TryGetValue(orden[k], out string valor)) continue;

			int col = n % columnas, fila = n / columnas;
			n++;

			PanelContainer tarjeta = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			StyleBoxFlat sb = RedFlagUI.Caja(new Color(0.01f, 0.05f, 0.1f, 0.78f), new Color(0.22f, 0.5f, 0.75f, 0.8f), 1, 6);
			sb.ContentMarginLeft = sb.ContentMarginRight = 8;
			sb.ContentMarginTop = sb.ContentMarginBottom = 2;
			tarjeta.AddThemeStyleboxOverride("panel", sb);

			float x = x0 + col * (ancho + hueco);
			float y = y0 + fila * (filaAlto + hueco);
			PonerVict(tarjeta, x, y, x + ancho, y + filaAlto);

			VBoxContainer caja = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
			caja.AddThemeConstantOverride("separation", 0);
			tarjeta.AddChild(caja);

			Label lblValor = RedFlagUI.CrearLabel(valor, tamValor, colores[k], HorizontalAlignment.Center);
			lblValor.MouseFilter = Control.MouseFilterEnum.Ignore;
			Label lblEtiqueta = RedFlagUI.CrearLabel(orden[k].ToUpperInvariant(), tamEtiqueta, RedFlagUI.TextoApagado, HorizontalAlignment.Center);
			lblEtiqueta.MouseFilter = Control.MouseFilterEnum.Ignore;
			caja.AddChild(lblValor);
			caja.AddChild(lblEtiqueta);

			contenedor.AddChild(tarjeta);
		}
	}

	private void ProcesarVictoria(double delta)
	{
		t += delta;
		if (t < 1.2) return; // primero se ve el diseño limpio

		// De a ratos entra un glitch suave: sube y baja con una curva, sin cortes bruscos
		if (glitchTiempo > 0)
		{
			glitchTiempo += delta;
			double p = glitchTiempo / GlitchDuracion;
			if (p >= 1.0)
			{
				glitchTiempo = 0;
				glitchProximo = 2.2 + rnd.NextDouble() * 2.8;
				glitchVictoria.SetShaderParameter("fuerza", 0f);
				return;
			}

			float curva = (float)Math.Sin(Math.PI * p);
			glitchVictoria.SetShaderParameter("fuerza", curva * curva);

			// Las franjas cambian unas pocas veces por segundo
			glitchPaso -= delta;
			if (glitchPaso <= 0)
			{
				glitchPaso = 0.09;
				glitchVictoria.SetShaderParameter("semilla", (float)rnd.NextDouble() * 50f);
			}
		}
		else
		{
			glitchProximo -= delta;
			if (glitchProximo <= 0) glitchTiempo = 0.0001;
		}
	}

	private static Label CrearLabelTitulo(int tamano, Color color)
	{
		Label l = RedFlagUI.CrearLabel("", tamano, color, HorizontalAlignment.Center);
		l.VerticalAlignment = VerticalAlignment.Center;
		l.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		return l;
	}

	public override void _Process(double delta)
	{
		if (victoriaImagen)
		{
			ProcesarVictoria(delta);
			return;
		}

		if (lblTitulo == null) return;

		t += delta;

		double inicio = Victoria ? 0.4 : 0.15;
		double paso = Victoria ? 0.05 : 0.04;

		// Efecto "descifrado": cada letra se revela de a una
		if (!tituloListo)
		{
			char[] letras = new char[tituloFinal.Length];
			bool completo = true;
			for (int i = 0; i < tituloFinal.Length; i++)
			{
				if (tituloFinal[i] == ' ' || t >= inicio + i * paso)
				{
					letras[i] = tituloFinal[i];
				}
				else
				{
					letras[i] = CaracteresGlitch[rnd.Next(CaracteresGlitch.Length)];
					completo = false;
				}
			}
			lblTitulo.Text = new string(letras);
			tituloListo = completo;
		}
		else if (!Victoria && rnd.NextDouble() < 0.03)
		{
			intensidad = Math.Max(intensidad, 8f); // micro-glitch ocasional
		}

		if (!Victoria)
		{
			ActualizarEfectosDeMiedo(delta);
		}

		// Temblor de pantalla que se va apagando
		if (intensidad > 0)
		{
			contenedor.Position = new Vector2(
				(float)(rnd.NextDouble() * 2 - 1) * intensidad,
				(float)(rnd.NextDouble() * 2 - 1) * intensidad);
			intensidad = Math.Max(0f, intensidad - (float)(30 * delta));
			if (intensidad == 0f) contenedor.Position = Vector2.Zero;
		}
	}

	private void ActualizarEfectosDeMiedo(double delta)
	{
		// Ecos de color del título, siempre temblando un poco
		string texto = lblTitulo.Text;
		ecoCian.Text = texto;
		ecoRojo.Text = texto;
		float desfase = tituloListo ? 3f + intensidad * 0.3f : 8f;
		ecoCian.Position = new Vector2(
			-desfase + (float)(rnd.NextDouble() * 2 - 1) * 1.5f,
			(float)(rnd.NextDouble() * 2 - 1) * 1.5f);
		ecoRojo.Position = new Vector2(
			desfase + (float)(rnd.NextDouble() * 2 - 1) * 1.5f,
			(float)(rnd.NextDouble() * 2 - 1) * 1.5f);

		// Parpadeos bruscos de pantalla (negro o rojo) cada tanto
		proximoParpadeo -= delta;
		if (proximoParpadeo <= 0)
		{
			proximoParpadeo = 0.6 + rnd.NextDouble() * 2.4;
			parpadeoAlfa = 0.25f + (float)rnd.NextDouble() * 0.4f;
			parpadeoColor = rnd.NextDouble() < 0.5 ? Colors.Black : new Color(0.9f, 0.05f, 0.05f);
			intensidad = Math.Max(intensidad, 12f);
		}

		if (parpadeoAlfa > 0f)
		{
			parpadeoAlfa = Math.Max(0f, parpadeoAlfa - (float)(5 * delta));
			parpadeo.Color = new Color(parpadeoColor.R, parpadeoColor.G, parpadeoColor.B, parpadeoAlfa);
		}
	}
}
