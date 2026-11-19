using Godot;
using System;

/// <summary>
/// Marco de ventana con borde neón (degradé de rojo a cian) y esquinas cortadas,
/// como en los diseños de Configuración. Es solo estético.
/// </summary>
public partial class MarcoNeon : PanelContainer
{
	public Color ColorInicio = new Color("ff3b34");
	public Color ColorFin = new Color("39b7cc");
	public Color Fondo = new Color(0.008f, 0.035f, 0.07f, 0.97f);
	public float Chaflan = 12f;
	public float Grosor = 2f;
	public bool Brillo = true;
	public int Margen = 0;

	public override void _Ready()
	{
		StyleBoxEmpty vacio = new StyleBoxEmpty();
		if (Margen > 0) vacio.SetContentMarginAll(Margen);
		AddThemeStyleboxOverride("panel", vacio);
	}

	public override void _Draw()
	{
		Vector2 s = Size;
		if (s.X < 8f || s.Y < 8f) return;

		float c = Mathf.Min(Chaflan, Mathf.Min(s.X, s.Y) * 0.25f);
		float m = Grosor * 0.5f;
		Vector2[] pts =
		{
			new Vector2(c, m), new Vector2(s.X - c, m), new Vector2(s.X - m, c), new Vector2(s.X - m, s.Y - c),
			new Vector2(s.X - c, s.Y - m), new Vector2(c, s.Y - m), new Vector2(m, s.Y - c), new Vector2(m, c)
		};

		DrawColoredPolygon(pts, Fondo);

		if (Brillo)
		{
			for (int i = 0; i < pts.Length; i++)
				Lado(pts[i], pts[(i + 1) % pts.Length], Grosor * 3.5f, 0.10f);
		}

		for (int i = 0; i < pts.Length; i++)
			Lado(pts[i], pts[(i + 1) % pts.Length], Grosor, 1f);
	}

	private void Lado(Vector2 a, Vector2 b, float grosor, float alfa)
	{
		Vector2 s = Size;
		int n = Mathf.Max(1, Mathf.CeilToInt(a.DistanceTo(b) / 14f));
		for (int i = 0; i < n; i++)
		{
			Vector2 p0 = a.Lerp(b, i / (float)n);
			Vector2 p1 = a.Lerp(b, (i + 1) / (float)n);
			Vector2 medio = (p0 + p1) * 0.5f;
			float t = Mathf.Clamp((medio.X / Mathf.Max(1f, s.X) + medio.Y / Mathf.Max(1f, s.Y)) * 0.5f, 0f, 1f);
			Color col = ColorInicio.Lerp(ColorFin, t);
			col.A = col.A * alfa;
			DrawLine(p0, p1, col, grosor);
		}
	}
}
