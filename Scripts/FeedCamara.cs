using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Imagen de una cámara de seguridad: muestra la foto de la oficina con efecto glitch
/// (franjas desplazadas, separación de color, ruido y líneas de escaneo).
/// Es solo visual: no recibe clics ni modifica nada del juego.
/// </summary>
public partial class FeedCamara : Control
{
	public Texture2D Imagen;
	public string Titulo = "";
	public bool SinSenal = false;

	private readonly Random rnd = new Random();
	private readonly List<Rect2> ruido = new List<Rect2>();
	private readonly List<Vector3> franjas = new List<Vector3>(); // x = y relativo, y = alto relativo, z = desplazamiento en px
	private double acumulado = 0;
	private double reloj = 0;
	private double proximoGlitch = 1.0;
	private double finGlitch = 0;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ClipContents = true;
		proximoGlitch = 0.5 + rnd.NextDouble() * 2.0;
	}

	public override void _Process(double delta)
	{
		reloj += delta;
		acumulado += delta;

		// Cada tanto aparece una ráfaga de glitch
		if (reloj >= proximoGlitch && finGlitch <= 0)
		{
			finGlitch = 0.12 + rnd.NextDouble() * 0.25;
			proximoGlitch = reloj + 1.2 + rnd.NextDouble() * 2.8;
			NuevasFranjas();
		}
		if (finGlitch > 0)
		{
			finGlitch -= delta;
			if (finGlitch <= 0) franjas.Clear();
		}

		if (acumulado >= 0.08)
		{
			acumulado = 0;
			NuevoRuido();
			if (finGlitch > 0) NuevasFranjas();
			QueueRedraw();
		}
	}

	private void NuevasFranjas()
	{
		franjas.Clear();
		int cantidad = SinSenal ? 5 : 2 + rnd.Next(3);
		for (int i = 0; i < cantidad; i++)
		{
			float y = (float)rnd.NextDouble();
			float alto = 0.02f + (float)rnd.NextDouble() * 0.08f;
			float desp = (float)(rnd.NextDouble() * 2.0 - 1.0) * (SinSenal ? 40f : 24f);
			franjas.Add(new Vector3(y, alto, desp));
		}
	}

	private void NuevoRuido()
	{
		ruido.Clear();
		Vector2 s = Size;
		if (s.X < 8 || s.Y < 8) return;

		float densidad = SinSenal ? 0.9f : 0.12f;
		int cantidad = (int)(s.X * s.Y / 1500f * densidad);
		for (int i = 0; i < cantidad; i++)
		{
			float ancho = 3 + (float)rnd.NextDouble() * (rnd.NextDouble() < 0.15 ? 70f : 20f);
			float alto = rnd.NextDouble() < 0.8 ? 2f : 3f;
			ruido.Add(new Rect2((float)rnd.NextDouble() * s.X, (float)rnd.NextDouble() * s.Y, ancho, alto));
		}
	}

	// Rectángulo de la textura que se ve (recorta para llenar el cuadro sin deformar)
	private Rect2 Origen()
	{
		Vector2 t = Imagen.GetSize();
		Vector2 s = Size;
		float escala = Mathf.Max(s.X / t.X, s.Y / t.Y);
		Vector2 visible = new Vector2(s.X / escala, s.Y / escala);
		return new Rect2((t.X - visible.X) * 0.5f, (t.Y - visible.Y) * 0.5f, visible.X, visible.Y);
	}

	public override void _Draw()
	{
		Vector2 s = Size;
		if (s.X < 8 || s.Y < 8) return;

		DrawRect(new Rect2(Vector2.Zero, s), new Color("02080c"));

		if (Imagen != null)
		{
			Rect2 src = Origen();
			Color brillo = SinSenal ? new Color(0.55f, 0.6f, 0.65f, 1f) : Colors.White;
			DrawTextureRectRegion(Imagen, new Rect2(Vector2.Zero, s), src, brillo);

			// Franjas desplazadas + separación de color durante el glitch
			if (finGlitch > 0)
			{
				foreach (Vector3 f in franjas)
				{
					float y0 = f.X * s.Y;
					float h = Mathf.Max(3f, f.Y * s.Y);
					if (y0 + h > s.Y) y0 = s.Y - h;
					Rect2 destino = new Rect2(f.Z, y0, s.X, h);
					Rect2 origenFranja = new Rect2(src.Position.X, src.Position.Y + (y0 / s.Y) * src.Size.Y, src.Size.X, (h / s.Y) * src.Size.Y);
					DrawTextureRectRegion(Imagen, destino, origenFranja, Colors.White);
				}

				DrawTextureRectRegion(Imagen, new Rect2(3f, 0, s.X, s.Y), src, new Color(1f, 0.15f, 0.2f, 0.22f));
				DrawTextureRectRegion(Imagen, new Rect2(-3f, 0, s.X, s.Y), src, new Color(0.1f, 0.9f, 1f, 0.22f));
			}
		}

		// Ruido
		Color colorRuido = SinSenal ? new Color(0.75f, 0.8f, 0.85f) : new Color(0.25f, 0.85f, 0.75f);
		foreach (Rect2 r in ruido)
		{
			float a = SinSenal ? 0.10f + (float)rnd.NextDouble() * 0.35f : 0.05f + (float)rnd.NextDouble() * 0.16f;
			DrawRect(r, new Color(colorRuido.R, colorRuido.G, colorRuido.B, a));
		}

		// Banda brillante que baja lentamente
		float yBanda = (float)((reloj * 40.0) % (s.Y + 60f)) - 30f;
		DrawRect(new Rect2(0, yBanda, s.X, 18f), new Color(0.6f, 0.9f, 1f, 0.045f));

		// Líneas de escaneo tipo monitor
		for (float y = 0; y < s.Y; y += 3f)
			DrawRect(new Rect2(0, y, s.X, 1), new Color(0, 0, 0, 0.22f));

		// Viñeta suave en los bordes
		DrawRect(new Rect2(0, 0, s.X, 8), new Color(0, 0, 0, 0.25f));
		DrawRect(new Rect2(0, s.Y - 8, s.X, 8), new Color(0, 0, 0, 0.25f));

		// Textos de la cámara
		Font fuente = RedFlagUI.Fuente;
		DrawString(fuente, new Vector2(10, 20), Titulo, HorizontalAlignment.Left, -1, 13, new Color("d4e6ea"));

		if (SinSenal)
		{
			bool visible = ((int)(reloj * 2.0)) % 2 == 0;
			if (visible)
				DrawString(fuente, new Vector2(0, s.Y * 0.55f), "SIN SEÑAL", HorizontalAlignment.Center, s.X, 20, new Color("ff6b63"));
		}
		else
		{
			bool encendido = ((int)(reloj * 1.5)) % 2 == 0;
			if (encendido)
				DrawCircle(new Vector2(16, s.Y - 16), 5f, new Color("ff4a52"));
			DrawString(fuente, new Vector2(28, s.Y - 11), "REC", HorizontalAlignment.Left, -1, 13, new Color("ff4a52"));
		}
	}
}
