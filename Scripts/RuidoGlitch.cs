using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Fondo de "interferencia" (rectángulos que parpadean + líneas de escaneo).
/// Es puramente visual: no recibe clics ni modifica nada del juego.
/// </summary>
public partial class RuidoGlitch : Control
{
	public Color ColorBase = new Color(0.95f, 0.1f, 0.1f);
	public float Densidad = 1f; // 1 = rojo intenso, valores menores = más suave

	private struct Trozo
	{
		public Rect2 Rect;
		public float Alfa;
	}

	private readonly Random rnd = new Random();
	private readonly List<Trozo> trozos = new List<Trozo>();
	private readonly List<Trozo> bandas = new List<Trozo>();
	private double acumulado = 0;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Regenerar();
	}

	public override void _Process(double delta)
	{
		acumulado += delta;
		if (acumulado >= 0.09)
		{
			acumulado = 0;
			Regenerar();
			QueueRedraw();
		}
	}

	private void Regenerar()
	{
		trozos.Clear();
		bandas.Clear();

		Vector2 s = Size;
		if (s.X < 8 || s.Y < 8) return;

		int cantidad = (int)(s.X * s.Y / 900f * Densidad);
		for (int i = 0; i < cantidad; i++)
		{
			float ancho = 4 + (float)rnd.NextDouble() * (rnd.NextDouble() < 0.15 ? 90f : 28f);
			float alto = rnd.NextDouble() < 0.8 ? 2f : 4f;
			float x = (float)rnd.NextDouble() * s.X;
			float y = (float)rnd.NextDouble() * s.Y;
			float alfa = 0.08f + (float)rnd.NextDouble() * 0.5f;

			// Zona central más calma para que el texto se lea bien
			bool centro = x > s.X * 0.08f && x < s.X * 0.92f && y > s.Y * 0.22f && y < s.Y * 0.78f;
			if (centro) alfa *= 0.25f;

			trozos.Add(new Trozo { Rect = new Rect2(x, y, ancho, alto), Alfa = alfa });
		}

		int cantidadBandas = 1 + rnd.Next(3);
		for (int i = 0; i < cantidadBandas; i++)
		{
			float y = (float)rnd.NextDouble() * s.Y;
			float alto = 6 + rnd.Next(12);
			bandas.Add(new Trozo
			{
				Rect = new Rect2(0, y, s.X, alto),
				Alfa = (0.05f + (float)rnd.NextDouble() * 0.10f) * Densidad
			});
		}
	}

	public override void _Draw()
	{
		Vector2 s = Size;

		DrawRect(new Rect2(Vector2.Zero, s),
			new Color(ColorBase.R * 0.07f, ColorBase.G * 0.07f, ColorBase.B * 0.07f));

		foreach (Trozo t in trozos)
			DrawRect(t.Rect, new Color(ColorBase.R, ColorBase.G, ColorBase.B, t.Alfa));

		foreach (Trozo b in bandas)
			DrawRect(b.Rect, new Color(ColorBase.R, ColorBase.G, ColorBase.B, b.Alfa));

		// Líneas de escaneo tipo monitor
		for (float y = 0; y < s.Y; y += 3f)
			DrawRect(new Rect2(0, y, s.X, 1), new Color(0, 0, 0, 0.25f));
	}
}
