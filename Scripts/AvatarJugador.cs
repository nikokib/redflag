using Godot;
using System;

/// <summary>
/// Avatar pixel-art del jugador, dibujado por código (ropa, cabello, piel y género).
/// Se usa en la ventana de Personalización y en la foto de las notificaciones de Red Flag.
/// Es solo estética: no modifica nada de las reglas del juego.
/// </summary>
public partial class AvatarJugador : Control
{
	public enum Modo { Cuerpo, Busto, Cabeza, Prenda }

	public struct PeinadoDef
	{
		public string Nombre;
		public int Estilo;   // 0 corto, 1 largo, 2 coleta, 3 moño, 4 rapado
		public Color Color;
	}

	public struct RopaDef
	{
		public string Nombre;
		public int Tipo;     // 0 traje, 1 camisa, 2 hoodie, 3 blazer, 4 campera, 5 sweater
		public Color Principal;
		public Color Secundario;
		public Color Pantalon;
	}

	public static readonly string[] NombresGenero = { "Masculino", "Femenino", "Neutro" };

	public static readonly string[] NombresPiel = { "Clara", "Dorada", "Trigueña", "Morena", "Oscura", "Pálida" };

	public static readonly Color[] Pieles =
	{
		new Color("f1c09a"), new Color("e0a97a"), new Color("c48a5a"),
		new Color("8d5a3a"), new Color("5e3a26"), new Color("fbe0cc")
	};

	public static readonly PeinadoDef[] Peinados =
	{
		new PeinadoDef { Nombre = "Corto negro",     Estilo = 0, Color = new Color("1d1b24") },
		new PeinadoDef { Nombre = "Corto castaño",   Estilo = 0, Color = new Color("5a3a24") },
		new PeinadoDef { Nombre = "Largo negro",     Estilo = 1, Color = new Color("1d1b24") },
		new PeinadoDef { Nombre = "Largo castaño",   Estilo = 1, Color = new Color("5a3a24") },
		new PeinadoDef { Nombre = "Coleta rubia",    Estilo = 2, Color = new Color("d9b24a") },
		new PeinadoDef { Nombre = "Moño pelirrojo",  Estilo = 3, Color = new Color("b8402a") },
		new PeinadoDef { Nombre = "Corto azul",      Estilo = 0, Color = new Color("2d6fb3") },
		new PeinadoDef { Nombre = "Rapado",          Estilo = 4, Color = new Color("2b2530") }
	};

	public static readonly RopaDef[] Ropas =
	{
		new RopaDef { Nombre = "Traje oscuro",    Tipo = 0, Principal = new Color("20242f"), Secundario = new Color("e9eef2"), Pantalon = new Color("1b1e27") },
		new RopaDef { Nombre = "Camisa celeste",  Tipo = 1, Principal = new Color("6fb1e6"), Secundario = new Color("f2f6fa"), Pantalon = new Color("2a2f3d") },
		new RopaDef { Nombre = "Hoodie violeta",  Tipo = 2, Principal = new Color("7a62c9"), Secundario = new Color("d9d2f5"), Pantalon = new Color("2a2d3a") },
		new RopaDef { Nombre = "Blazer gris",     Tipo = 3, Principal = new Color("7a8190"), Secundario = new Color("e8eef2"), Pantalon = new Color("343a46") },
		new RopaDef { Nombre = "Campera roja",    Tipo = 4, Principal = new Color("c4352f"), Secundario = new Color("f2f2f2"), Pantalon = new Color("1d2029") },
		new RopaDef { Nombre = "Sweater verde",   Tipo = 5, Principal = new Color("3f9a78"), Secundario = new Color("c9efe0"), Pantalon = new Color("2a2f3d") }
	};

	public int Ropa = 0;
	public int Cabello = 0;
	public int Piel = 0;
	public int Genero = 0;
	public Modo Vista = Modo.Cuerpo;

	/// <summary>Fondo rojo oscuro (como el cuadro de la foto en la notificación).</summary>
	public bool FondoRojo = false;

	/// <summary>Si es true, al iniciar lee la personalización guardada del jugador.</summary>
	public bool CargarDePreferencias = true;

	private float celda = 1f;
	private Vector2 origen = Vector2.Zero;
	private float vx = 0f;
	private float vy = 0f;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ClipContents = true;
		if (CargarDePreferencias)
		{
			Ropa = PlayerPreferences.AvatarRopa;
			Cabello = PlayerPreferences.AvatarCabello;
			Piel = PlayerPreferences.AvatarPiel;
			Genero = PlayerPreferences.AvatarGenero;
		}
		Normalizar();
		QueueRedraw();
	}

	/// <summary>Cambia la apariencia y vuelve a dibujar.</summary>
	public void Aplicar(int ropa, int cabello, int piel, int genero)
	{
		Ropa = ropa;
		Cabello = cabello;
		Piel = piel;
		Genero = genero;
		Normalizar();
		QueueRedraw();
	}

	public void CambiarVista(Modo vista)
	{
		Vista = vista;
		QueueRedraw();
	}

	private void Normalizar()
	{
		Ropa = Mod(Ropa, Ropas.Length);
		Cabello = Mod(Cabello, Peinados.Length);
		Piel = Mod(Piel, Pieles.Length);
		Genero = Mod(Genero, NombresGenero.Length);
	}

	public static int Mod(int valor, int n)
	{
		if (n <= 0) return 0;
		int r = valor % n;
		return r < 0 ? r + n : r;
	}

	/// <summary>Atajo para crear un avatar con la personalización guardada.</summary>
	public static AvatarJugador Crear(Modo vista, bool fondoRojo = false)
	{
		return new AvatarJugador
		{
			Vista = vista,
			FondoRojo = fondoRojo,
			MouseFilter = MouseFilterEnum.Ignore
		};
	}

	// Dibuja un rectángulo en coordenadas de la cuadrícula del personaje.
	private void Px(float x, float y, float w, float h, Color c)
	{
		float x0 = Mathf.Round(origen.X + (x - vx) * celda);
		float y0 = Mathf.Round(origen.Y + (y - vy) * celda);
		float x1 = Mathf.Round(origen.X + (x + w - vx) * celda);
		float y1 = Mathf.Round(origen.Y + (y + h - vy) * celda);
		DrawRect(new Rect2(x0, y0, Mathf.Max(1f, x1 - x0), Mathf.Max(1f, y1 - y0)), c);
	}

	public override void _Draw()
	{
		Vector2 s = Size;
		if (s.X < 4f || s.Y < 4f) return;

		if (FondoRojo)
		{
			Color arriba = new Color("0d0008");
			Color abajo = new Color("32020c");
			int bandas = 24;
			for (int i = 0; i < bandas; i++)
			{
				float t = i / (float)(bandas - 1);
				float y0 = Mathf.Round(s.Y * i / bandas);
				float y1 = Mathf.Round(s.Y * (i + 1) / bandas);
				DrawRect(new Rect2(0, y0, s.X, Mathf.Max(1f, y1 - y0)), arriba.Lerp(abajo, t));
			}
		}

		float vw;
		float vh;
		switch (Vista)
		{
			case Modo.Busto:
				vx = 2f; vy = -1f; vw = 20f; vh = 23f;
				break;
			case Modo.Cabeza:
				vx = 3.5f; vy = -1f; vw = 17f; vh = 18f;
				break;
			case Modo.Prenda:
				vx = 3f; vy = 15f; vw = 18f; vh = 14f;
				break;
			default:
				vx = 0f; vy = -1f; vw = 24f; vh = 38f;
				break;
		}

		float escala = Mathf.Min(s.X / vw, s.Y / vh);
		celda = escala >= 2f ? Mathf.Floor(escala) : escala;
		origen = new Vector2(Mathf.Round((s.X - vw * celda) * 0.5f), Mathf.Round((s.Y - vh * celda) * 0.5f));

		Normalizar();

		Color cont = new Color("17131f");
		Color piel = Pieles[Piel];
		Color pielSombra = piel.Darkened(0.16f);
		PeinadoDef pe = Peinados[Cabello];
		Color pelo = pe.Color;
		Color peloLuz = pelo.Lightened(0.22f);
		Color peloSombra = pelo.Darkened(0.2f);
		RopaDef ro = Ropas[Ropa];
		bool fem = Genero == 1;

		// --- Pelo que va detrás del cuerpo ---
		if (pe.Estilo == 1)
		{
			Px(3, 6, 18, 16, pelo);
			Px(3, 19, 18, 3, peloSombra);
		}
		else if (pe.Estilo == 2)
		{
			Px(19, 7, 3, 10, pelo);
			Px(19, 7, 3, 1, new Color("d2433c"));
			Px(19, 14, 3, 3, peloSombra);
		}

		// --- Piernas y zapatos ---
		Color pant = ro.Pantalon;
		Px(7, 27, 10, 8, cont);
		Px(8, 28, 4, 6, pant);
		Px(12, 28, 4, 6, pant);
		Px(11, 28, 1, 6, pant.Darkened(0.35f));
		Color zapato = new Color("0f0f14");
		Px(7, 34, 5, 2, zapato);
		Px(12, 34, 5, 2, zapato);
		Px(8, 34, 3, 1, new Color("2c2c38"));
		Px(13, 34, 3, 1, new Color("2c2c38"));

		// --- Torso y brazos ---
		int x0 = fem ? 8 : 7;
		int w = fem ? 8 : 10;
		Color prin = ro.Principal;
		Color sec = ro.Secundario;
		Color prinSombra = prin.Darkened(0.22f);

		// Brazos (manga + mano)
		Px(x0 - 3, 17, 3, 11, cont);
		Px(x0 + w, 17, 3, 11, cont);
		Px(x0 - 2, 18, 2, 9, prin);
		Px(x0 + w, 18, 2, 9, prin);
		Px(x0 - 2, 25, 2, 2, prinSombra);
		Px(x0 + w, 25, 2, 2, prinSombra);
		Px(x0 - 2, 27, 2, 2, piel);
		Px(x0 + w, 27, 2, 2, piel);

		// Torso
		Px(x0 - 1, 16, w + 2, 12, cont);
		Px(x0, 17, w, 10, prin);
		Px(x0, 26, w, 1, prinSombra);

		switch (ro.Tipo)
		{
			case 0: // traje con corbata
			{
				Px(10, 17, 4, 2, sec);
				Px(11, 19, 2, 4, sec);
				Px(x0, 17, 2, 6, prin.Lightened(0.08f));
				Px(x0 + w - 2, 17, 2, 6, prin.Lightened(0.08f));
				Color corbata = new Color("1f4e8c");
				Px(11, 18, 2, 1, corbata.Lightened(0.15f));
				Px(11, 19, 2, 6, corbata);
				Px(11, 25, 2, 1, corbata.Darkened(0.3f));
				break;
			}
			case 1: // camisa
			{
				Px(10, 17, 4, 1, sec);
				Px(11, 18, 2, 8, prin.Lightened(0.1f));
				Px(11, 19, 1, 1, sec);
				Px(11, 22, 1, 1, sec);
				Px(11, 25, 1, 1, sec);
				break;
			}
			case 2: // hoodie
			{
				Px(9, 17, 6, 2, prinSombra);
				Px(11, 19, 1, 4, sec);
				Px(13, 19, 1, 4, sec);
				Px(x0 + 1, 23, w - 2, 3, prinSombra);
				break;
			}
			case 3: // blazer sin corbata
			{
				Px(10, 17, 4, 3, sec);
				Px(x0, 17, 2, 7, prin.Lightened(0.1f));
				Px(x0 + w - 2, 17, 2, 7, prin.Lightened(0.1f));
				Px(11, 22, 2, 1, prinSombra);
				break;
			}
			case 4: // campera abierta
			{
				Px(10, 17, 4, 10, sec);
				Px(10, 17, 1, 10, prinSombra);
				Px(13, 17, 1, 10, prinSombra);
				Px(x0, 24, 2, 2, prinSombra);
				break;
			}
			default: // sweater
			{
				Px(9, 17, 6, 1, sec);
				Px(x0, 25, w, 2, prinSombra);
				Px(x0 + 2, 20, w - 4, 1, prin.Lightened(0.1f));
				break;
			}
		}

		// Maletín (solo con el traje, en la vista de cuerpo entero)
		if (ro.Tipo == 0 && Vista == Modo.Cuerpo)
		{
			Color cuero = new Color("7a4a2b");
			Px(16, 29, 8, 6, cont);
			Px(18, 28, 4, 1, cont);
			Px(17, 30, 6, 4, cuero);
			Px(17, 32, 6, 1, cuero.Darkened(0.25f));
			Px(19, 31, 2, 1, new Color("d9b24a"));
		}

		// --- Cuello ---
		Px(10, 15, 4, 2, pielSombra);

		// --- Cabeza ---
		Px(5, 5, 14, 10, cont);
		Px(6, 4, 12, 12, cont);
		Px(6, 5, 12, 10, piel);
		Px(6, 14, 12, 1, pielSombra);

		// --- Cara ---
		Color ojo = new Color("1b1823");
		Px(8, 9, 2, 3, ojo);
		Px(14, 9, 2, 3, ojo);
		Px(8, 9, 1, 1, Colors.White);
		Px(14, 9, 1, 1, Colors.White);
		Px(8, 8, 2, 1, peloSombra);
		Px(14, 8, 2, 1, peloSombra);
		if (fem)
		{
			Px(7, 9, 1, 1, cont);
			Px(16, 9, 1, 1, cont);
			Color rubor = piel.Lerp(new Color("ff6f7d"), 0.4f);
			Px(7, 12, 2, 1, rubor);
			Px(15, 12, 2, 1, rubor);
		}
		Px(11, 13, 2, 1, new Color("a24b45"));

		// --- Pelo del frente ---
		switch (pe.Estilo)
		{
			case 0: // corto con mechones
				Px(5, 2, 14, 4, pelo);
				Px(5, 6, 2, 4, pelo);
				Px(17, 6, 2, 4, pelo);
				Px(6, 6, 5, 1, pelo);
				Px(13, 6, 5, 1, pelo);
				Px(7, 7, 2, 1, pelo);
				Px(14, 7, 2, 1, pelo);
				Px(7, 1, 3, 1, pelo);
				Px(10, 0, 4, 2, pelo);
				Px(14, 1, 3, 1, pelo);
				Px(8, 3, 3, 1, peloLuz);
				Px(13, 2, 2, 1, peloLuz);
				break;
			case 1: // largo
				Px(5, 2, 14, 4, pelo);
				Px(6, 1, 12, 1, pelo);
				Px(5, 6, 2, 9, pelo);
				Px(17, 6, 2, 9, pelo);
				Px(6, 6, 12, 1, pelo);
				Px(8, 7, 3, 1, pelo);
				Px(13, 7, 3, 1, pelo);
				Px(8, 3, 3, 1, peloLuz);
				Px(13, 2, 2, 1, peloLuz);
				break;
			case 2: // coleta
				Px(5, 2, 14, 4, pelo);
				Px(6, 1, 12, 1, pelo);
				Px(5, 6, 2, 4, pelo);
				Px(17, 6, 2, 4, pelo);
				Px(6, 6, 12, 1, pelo);
				Px(8, 7, 3, 1, pelo);
				Px(13, 7, 3, 1, pelo);
				Px(8, 3, 3, 1, peloLuz);
				break;
			case 3: // moño
				Px(5, 2, 14, 4, pelo);
				Px(6, 1, 12, 1, pelo);
				Px(10, -1, 4, 3, pelo);
				Px(11, -1, 2, 1, peloLuz);
				Px(5, 6, 2, 4, pelo);
				Px(17, 6, 2, 4, pelo);
				Px(6, 6, 12, 1, pelo);
				Px(8, 7, 3, 1, pelo);
				Px(13, 7, 3, 1, pelo);
				Px(8, 3, 3, 1, peloLuz);
				break;
			default: // rapado
				Px(6, 3, 12, 3, pelo);
				Px(5, 5, 1, 3, pelo);
				Px(18, 5, 1, 3, pelo);
				Px(8, 4, 3, 1, peloLuz);
				break;
		}
	}
}
