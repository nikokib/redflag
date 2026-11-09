using Godot;
using System;

/// <summary>Íconos de la ventana de configuración (engranaje, barras, reloj, info), dibujados por código.</summary>
public partial class IconoConfig : Control
{
	public enum Clase { Engranaje, Barras, Reloj, Info }
	public Clase Tipo = Clase.Engranaje;

	public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

	public override void _Draw()
	{
		Vector2 s = Size;
		Vector2 c = s / 2f;
		float r = Math.Min(s.X, s.Y) / 2f - 1;
		Color azul = RedFlagUI.BordeVivo;
		Color rojo = RedFlagUI.Rojo;

		switch (Tipo)
		{
			case Clase.Engranaje:
				for (int i = 0; i < 8; i++)
				{
					float ang = i * Mathf.Pi / 4f;
					Vector2 d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
					DrawLine(c + d * r * 0.65f, c + d * r, azul, 4f);
				}
				DrawArc(c, r * 0.68f, 0, Mathf.Tau, 24, azul, 4f);
				DrawCircle(c, r * 0.28f, azul);
				DrawCircle(c, r * 0.14f, RedFlagUI.FondoPanel);
				break;
			case Clase.Barras:
				float w = s.X / 5f;
				for (int i = 0; i < 4; i++)
				{
					float alto = s.Y * (0.25f + 0.2f * i);
					DrawRect(new Rect2(i * (w + 1.5f) + 1, s.Y - alto, w, alto), azul);
				}
				break;
			case Clase.Reloj:
				DrawArc(c, r, 0, Mathf.Tau, 28, azul, 2.5f);
				DrawLine(c, c + new Vector2(0, -r * 0.6f), azul, 2.5f);
				DrawLine(c, c + new Vector2(r * 0.45f, 0), azul, 2.5f);
				break;
			case Clase.Info:
				DrawCircle(c, r, rojo);
				DrawCircle(c + new Vector2(0, -r * 0.45f), r * 0.12f, Colors.Black);
				DrawLine(c + new Vector2(0, -r * 0.15f), c + new Vector2(0, r * 0.55f), Colors.Black, r * 0.24f);
				break;
		}
	}
}
