using Godot;
using System;

/// <summary>
/// Íconos dibujados por código para las ventanas y la interfaz de Red Flag OS.
/// Cada app del escritorio tiene el suyo (la bandera roja es solo de la app Red Flag),
/// y también están los de la barra lateral (Dashboard, Solicitudes, Whitelist...).
/// </summary>
public partial class IconoSistema : Control
{
	public enum Clase
	{
		RedFlag, Pc, Navegador, Correo, Chat, Logs, Incidentes, Firewall, Antivirus, Camaras, Notas, Calculadora, Papelera,
		Dashboard, Solicitudes, Whitelist, Blacklist, Red, Ajustes, Personas, Prohibido, Antena, Mas, Check, Cruz,
		Guardar, Reiniciar
	}

	public Clase Tipo = Clase.RedFlag;
	public Color ColorBase = new Color("39b7cc");

	/// <summary>Ícono listo para poner en una barra de título o en un botón.</summary>
	public static IconoSistema Crear(Clase tipo, float lado, Color? color = null)
	{
		IconoSistema i = new IconoSistema
		{
			Tipo = tipo,
			CustomMinimumSize = new Vector2(lado, lado),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore
		};
		if (color.HasValue) i.ColorBase = color.Value;
		return i;
	}

	/// <summary>Ícono que corresponde a cada app del escritorio (según su id).</summary>
	public static Clase DeApp(string id)
	{
		switch (id)
		{
			case "mipc": return Clase.Pc;
			case "navegador": return Clase.Navegador;
			case "redflags": return Clase.RedFlag;
			case "incidentes": return Clase.Incidentes;
			case "logs": return Clase.Logs;
			case "correo": return Clase.Correo;
			case "chat": return Clase.Chat;
			case "firewall": return Clase.Firewall;
			case "antivirus": return Clase.Antivirus;
			case "camaras": return Clase.Camaras;
			case "notas": return Clase.Notas;
			case "calculadora": return Clase.Calculadora;
			case "papelera": return Clase.Papelera;
			default: return Clase.Dashboard;
		}
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	private Vector2 P(float x, float y) => new Vector2(Size.X * x, Size.Y * y);
	private Rect2 R(float x, float y, float w, float h) => new Rect2(Size.X * x, Size.Y * y, Size.X * w, Size.Y * h);

	public override void _Draw()
	{
		Vector2 s = Size;
		float g = Mathf.Max(1.5f, s.X * 0.09f); // grosor de línea
		Color c = ColorBase;

		switch (Tipo)
		{
			case Clase.RedFlag:
			{
				Color rojo = new Color("e0332e");
				Color oscuro = new Color("8f1a17");
				DrawRect(R(0.16f, 0.10f, 0.11f, 0.82f), rojo);
				DrawColoredPolygon(new[] { P(0.27f, 0.12f), P(0.60f, 0.20f), P(0.90f, 0.12f), P(0.90f, 0.56f), P(0.60f, 0.64f), P(0.27f, 0.56f) }, rojo);
				DrawColoredPolygon(new[] { P(0.27f, 0.40f), P(0.60f, 0.48f), P(0.90f, 0.40f), P(0.90f, 0.56f), P(0.60f, 0.64f), P(0.27f, 0.56f) }, oscuro);
				break;
			}
			case Clase.Pc:
			{
				Color azul = new Color("5aa9e6");
				DrawRect(R(0.08f, 0.12f, 0.84f, 0.58f), new Color("0b2a44"));
				DrawRect(R(0.08f, 0.12f, 0.84f, 0.58f), azul, false, g);
				DrawRect(R(0.16f, 0.20f, 0.68f, 0.42f), new Color("2b78b8"));
				DrawRect(R(0.43f, 0.70f, 0.14f, 0.12f), azul);
				DrawRect(R(0.26f, 0.84f, 0.48f, 0.08f), azul);
				break;
			}
			case Clase.Navegador:
			{
				Color cian = new Color("39b7cc");
				Vector2 centro = P(0.5f, 0.5f);
				float r = s.X * 0.40f;
				DrawCircle(centro, r, new Color("0b3a55"));
				DrawArc(centro, r, 0, Mathf.Tau, 28, cian, g);
				DrawLine(P(0.10f, 0.5f), P(0.90f, 0.5f), cian, g * 0.7f);
				DrawLine(P(0.5f, 0.10f), P(0.5f, 0.90f), cian, g * 0.7f);
				DrawArc(P(0.5f, 0.5f), r * 0.55f, Mathf.Pi * 0.5f, Mathf.Pi * 1.5f, 12, cian, g * 0.7f);
				DrawArc(P(0.5f, 0.5f), r * 0.55f, -Mathf.Pi * 0.5f, Mathf.Pi * 0.5f, 12, cian, g * 0.7f);
				break;
			}
			case Clase.Correo:
			{
				Color beige = new Color("d6c7a1");
				DrawRect(R(0.08f, 0.22f, 0.84f, 0.58f), new Color("2a2a22"));
				DrawRect(R(0.08f, 0.22f, 0.84f, 0.58f), beige, false, g);
				DrawPolyline(new[] { P(0.10f, 0.26f), P(0.50f, 0.56f), P(0.90f, 0.26f) }, beige, g);
				break;
			}
			case Clase.Chat:
			{
				Color verde = new Color("7ad7c1");
				DrawColoredPolygon(new[] { P(0.08f, 0.14f), P(0.92f, 0.14f), P(0.92f, 0.66f), P(0.46f, 0.66f), P(0.26f, 0.86f), P(0.26f, 0.66f), P(0.08f, 0.66f) }, new Color("0e3a3a"));
				DrawPolyline(new[] { P(0.08f, 0.14f), P(0.92f, 0.14f), P(0.92f, 0.66f), P(0.46f, 0.66f), P(0.26f, 0.86f), P(0.26f, 0.66f), P(0.08f, 0.66f), P(0.08f, 0.14f) }, verde, g);
				for (int i = 0; i < 3; i++) DrawCircle(P(0.30f + i * 0.20f, 0.40f), s.X * 0.055f, verde);
				break;
			}
			case Clase.Logs:
			{
				Color verde = new Color("8ec07c");
				DrawRect(R(0.20f, 0.08f, 0.60f, 0.84f), new Color("1b2a17"));
				DrawRect(R(0.20f, 0.08f, 0.60f, 0.84f), verde, false, g);
				for (int i = 0; i < 4; i++) DrawLine(P(0.30f, 0.26f + i * 0.17f), P(0.70f, 0.26f + i * 0.17f), verde, g * 0.8f);
				break;
			}
			case Clase.Incidentes:
			{
				Color oscuro = new Color("a8772f");
				Color claro = new Color("d9a24d");
				DrawRect(R(0.08f, 0.20f, 0.38f, 0.16f), oscuro);
				DrawRect(R(0.08f, 0.30f, 0.84f, 0.56f), claro);
				DrawRect(R(0.08f, 0.30f, 0.84f, 0.10f), oscuro);
				DrawCircle(P(0.70f, 0.64f), s.X * 0.12f, new Color("ff3b34"));
				break;
			}
			case Clase.Firewall:
			{
				Color ladrillo = new Color("ff7a45");
				Color junta = new Color("5a2210");
				DrawRect(R(0.06f, 0.14f, 0.88f, 0.72f), junta);
				for (int fila = 0; fila < 4; fila++)
				{
					float y = 0.16f + fila * 0.17f;
					float desfase = (fila % 2 == 0) ? 0f : 0.14f;
					for (int col = -1; col < 3; col++)
					{
						float x = 0.08f + desfase + col * 0.29f;
						float x0 = Mathf.Max(0.08f, x);
						float x1 = Mathf.Min(0.92f, x + 0.27f);
						if (x1 > x0) DrawRect(R(x0, y, x1 - x0, 0.14f), ladrillo);
					}
				}
				break;
			}
			case Clase.Antivirus:
			{
				Color verde = new Color("4dd0a0");
				Vector2[] escudo = { P(0.50f, 0.08f), P(0.88f, 0.22f), P(0.84f, 0.62f), P(0.50f, 0.92f), P(0.16f, 0.62f), P(0.12f, 0.22f) };
				DrawColoredPolygon(escudo, new Color("0f3a2c"));
				DrawPolyline(new[] { escudo[0], escudo[1], escudo[2], escudo[3], escudo[4], escudo[5], escudo[0] }, verde, g);
				DrawLine(P(0.50f, 0.30f), P(0.50f, 0.62f), verde, g);
				DrawLine(P(0.34f, 0.46f), P(0.66f, 0.46f), verde, g);
				break;
			}
			case Clase.Camaras:
			{
				Color gris = new Color("9aa5b1");
				DrawRect(R(0.08f, 0.30f, 0.58f, 0.40f), new Color("2a323a"));
				DrawRect(R(0.08f, 0.30f, 0.58f, 0.40f), gris, false, g);
				DrawColoredPolygon(new[] { P(0.66f, 0.42f), P(0.92f, 0.28f), P(0.92f, 0.72f), P(0.66f, 0.58f) }, gris);
				DrawCircle(P(0.22f, 0.44f), s.X * 0.05f, new Color("ff4a52"));
				break;
			}
			case Clase.Notas:
			{
				Color amarillo = new Color("f2d94e");
				DrawRect(R(0.16f, 0.10f, 0.68f, 0.80f), new Color("3a3410"));
				DrawRect(R(0.16f, 0.10f, 0.68f, 0.80f), amarillo, false, g);
				DrawRect(R(0.16f, 0.10f, 0.68f, 0.14f), amarillo);
				for (int i = 0; i < 3; i++) DrawLine(P(0.28f, 0.42f + i * 0.17f), P(0.72f, 0.42f + i * 0.17f), amarillo, g * 0.8f);
				break;
			}
			case Clase.Calculadora:
			{
				Color azul = new Color("7ab8ff");
				DrawRect(R(0.16f, 0.06f, 0.68f, 0.88f), new Color("14283f"));
				DrawRect(R(0.16f, 0.06f, 0.68f, 0.88f), azul, false, g);
				DrawRect(R(0.24f, 0.14f, 0.52f, 0.20f), azul);
				for (int f = 0; f < 3; f++)
					for (int col = 0; col < 3; col++)
						DrawRect(R(0.25f + col * 0.18f, 0.44f + f * 0.16f, 0.12f, 0.10f), azul);
				break;
			}
			case Clase.Papelera:
			{
				Color gris = new Color("86a1ab");
				DrawColoredPolygon(new[] { P(0.22f, 0.30f), P(0.78f, 0.30f), P(0.72f, 0.90f), P(0.28f, 0.90f) }, new Color("1e2e35"));
				DrawPolyline(new[] { P(0.22f, 0.30f), P(0.28f, 0.90f), P(0.72f, 0.90f), P(0.78f, 0.30f) }, gris, g);
				DrawRect(R(0.14f, 0.20f, 0.72f, 0.08f), gris);
				DrawRect(R(0.38f, 0.10f, 0.24f, 0.08f), gris);
				for (int i = 0; i < 3; i++) DrawLine(P(0.36f + i * 0.14f, 0.40f), P(0.37f + i * 0.13f, 0.80f), gris, g * 0.7f);
				break;
			}
			case Clase.Dashboard:
			{
				DrawColoredPolygon(new[] { P(0.50f, 0.10f), P(0.94f, 0.50f), P(0.80f, 0.50f), P(0.80f, 0.88f), P(0.20f, 0.88f), P(0.20f, 0.50f), P(0.06f, 0.50f) }, c);
				DrawRect(R(0.42f, 0.60f, 0.16f, 0.28f), new Color("010b16"));
				break;
			}
			case Clase.Solicitudes:
			{
				DrawRect(R(0.14f, 0.14f, 0.72f, 0.78f), c, false, g);
				DrawRect(R(0.34f, 0.06f, 0.32f, 0.16f), c);
				for (int i = 0; i < 3; i++)
				{
					float y = 0.38f + i * 0.17f;
					DrawRect(R(0.24f, y, 0.10f, 0.09f), c);
					DrawLine(P(0.42f, y + 0.045f), P(0.76f, y + 0.045f), c, g * 0.8f);
				}
				break;
			}
			case Clase.Whitelist:
			{
				Vector2[] escudo = { P(0.50f, 0.06f), P(0.90f, 0.20f), P(0.86f, 0.62f), P(0.50f, 0.94f), P(0.14f, 0.62f), P(0.10f, 0.20f) };
				DrawColoredPolygon(escudo, c);
				DrawPolyline(new[] { P(0.30f, 0.50f), P(0.45f, 0.66f), P(0.72f, 0.34f) }, new Color("010b16"), g * 1.2f);
				break;
			}
			case Clase.Blacklist:
			case Clase.Prohibido:
			{
				Vector2 centro = P(0.5f, 0.5f);
				float r = s.X * 0.38f;
				DrawArc(centro, r, 0, Mathf.Tau, 28, c, g * 1.3f);
				DrawLine(P(0.24f, 0.76f), P(0.76f, 0.24f), c, g * 1.3f);
				break;
			}
			case Clase.Red:
			{
				DrawLine(P(0.5f, 0.28f), P(0.5f, 0.52f), c, g);
				DrawLine(P(0.20f, 0.52f), P(0.80f, 0.52f), c, g);
				DrawLine(P(0.20f, 0.52f), P(0.20f, 0.68f), c, g);
				DrawLine(P(0.80f, 0.52f), P(0.80f, 0.68f), c, g);
				DrawRect(R(0.36f, 0.08f, 0.28f, 0.22f), c);
				DrawRect(R(0.08f, 0.66f, 0.24f, 0.22f), c);
				DrawRect(R(0.68f, 0.66f, 0.24f, 0.22f), c);
				break;
			}
			case Clase.Ajustes:
			{
				Vector2 centro = P(0.5f, 0.5f);
				for (int i = 0; i < 8; i++)
				{
					float a = i * Mathf.Tau / 8f;
					Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
					DrawLine(centro + d * s.X * 0.30f, centro + d * s.X * 0.46f, c, g * 1.6f);
				}
				DrawCircle(centro, s.X * 0.34f, c);
				DrawCircle(centro, s.X * 0.15f, new Color("010b16"));
				break;
			}
			case Clase.Personas:
			{
				DrawCircle(P(0.50f, 0.28f), s.X * 0.15f, c);
				DrawArc(P(0.50f, 0.94f), s.X * 0.30f, Mathf.Pi, Mathf.Tau, 16, c, g * 2f);
				DrawCircle(P(0.20f, 0.36f), s.X * 0.10f, c);
				DrawArc(P(0.20f, 0.94f), s.X * 0.18f, Mathf.Pi, Mathf.Tau, 12, c, g * 1.4f);
				DrawCircle(P(0.80f, 0.36f), s.X * 0.10f, c);
				DrawArc(P(0.80f, 0.94f), s.X * 0.18f, Mathf.Pi, Mathf.Tau, 12, c, g * 1.4f);
				break;
			}
			case Clase.Antena:
			{
				float gg = Mathf.Max(2f, s.X * 0.035f);
				DrawCircle(P(0.5f, 0.30f), s.X * 0.035f, c);
				DrawLine(P(0.5f, 0.34f), P(0.30f, 0.92f), c, gg);
				DrawLine(P(0.5f, 0.34f), P(0.70f, 0.92f), c, gg);
				DrawLine(P(0.40f, 0.62f), P(0.60f, 0.62f), c, gg);
				DrawLine(P(0.34f, 0.78f), P(0.66f, 0.78f), c, gg);
				DrawLine(P(0.30f, 0.92f), P(0.70f, 0.92f), c, gg);
				DrawLine(P(0.40f, 0.62f), P(0.66f, 0.78f), c, gg * 0.7f);
				DrawLine(P(0.60f, 0.62f), P(0.34f, 0.78f), c, gg * 0.7f);
				for (int i = 1; i <= 3; i++)
				{
					float r = s.X * (0.10f + i * 0.085f);
					DrawArc(P(0.5f, 0.30f), r, Mathf.Pi * 1.15f, Mathf.Pi * 1.85f, 14, c, gg);
					DrawArc(P(0.5f, 0.30f), r, -Mathf.Pi * 0.15f, Mathf.Pi * 0.15f, 8, c, gg);
					DrawArc(P(0.5f, 0.30f), r, Mathf.Pi * 0.85f, Mathf.Pi * 1.15f, 8, c, gg);
				}
				break;
			}
			case Clase.Mas:
			{
				DrawLine(P(0.5f, 0.18f), P(0.5f, 0.82f), c, g * 1.3f);
				DrawLine(P(0.18f, 0.5f), P(0.82f, 0.5f), c, g * 1.3f);
				break;
			}
			case Clase.Check:
			{
				DrawPolyline(new[] { P(0.20f, 0.52f), P(0.42f, 0.74f), P(0.82f, 0.28f) }, c, Mathf.Max(2f, s.X * 0.12f));
				break;
			}
			case Clase.Guardar:
			{
				DrawRect(R(0.12f, 0.12f, 0.76f, 0.76f), c);
				DrawRect(R(0.28f, 0.12f, 0.44f, 0.26f), new Color("010b16"));
				DrawRect(R(0.60f, 0.16f, 0.07f, 0.18f), c);
				DrawRect(R(0.24f, 0.54f, 0.52f, 0.34f), new Color("010b16"));
				break;
			}
			case Clase.Reiniciar:
			{
				Vector2 centro = P(0.5f, 0.5f);
				float r = s.X * 0.34f;
				DrawArc(centro, r, Mathf.Pi * 0.15f, Mathf.Pi * 1.75f, 24, c, g * 1.3f);
				DrawColoredPolygon(new[] { P(0.88f, 0.30f), P(0.88f, 0.62f), P(0.60f, 0.50f) }, c);
				break;
			}
			case Clase.Cruz:
			{
				float gr = Mathf.Max(2f, s.X * 0.12f);
				DrawLine(P(0.24f, 0.24f), P(0.76f, 0.76f), c, gr);
				DrawLine(P(0.76f, 0.24f), P(0.24f, 0.76f), c, gr);
				break;
			}
		}
	}
}
