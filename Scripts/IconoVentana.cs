using Godot;
using System;

/// <summary>
/// Icono dibujado por código para las ventanas estilo Red Flag
/// (logo, botones de la barra de título, avatar, ✔, ✖, reloj).
/// Si se le asigna AlHacerClic, se comporta como un botón.
/// </summary>
public partial class IconoVentana : Control
{
	public enum Clase { Logo, Alerta, Minimizar, Maximizar, Cerrar, Avatar, Check, Cruz, Reloj }

	public Clase Tipo = Clase.Logo;
	public Action AlHacerClic;

	private bool encima = false;

	public override void _Ready()
	{
		if (CustomMinimumSize == Vector2.Zero)
		{
			switch (Tipo)
			{
				case Clase.Logo:
				case Clase.Alerta:
					CustomMinimumSize = new Vector2(22, 22);
					break;
				case Clase.Avatar:
					CustomMinimumSize = new Vector2(84, 84);
					break;
				case Clase.Check:
				case Clase.Cruz:
					CustomMinimumSize = new Vector2(30, 30);
					break;
				case Clase.Reloj:
					CustomMinimumSize = new Vector2(22, 22);
					break;
				default:
					CustomMinimumSize = new Vector2(18, 18);
					break;
			}
		}

		SizeFlagsVertical = SizeFlags.ShrinkCenter;

		if (AlHacerClic != null)
		{
			MouseFilter = MouseFilterEnum.Stop;
			MouseDefaultCursorShape = CursorShape.PointingHand;
			MouseEntered += () => { encima = true; QueueRedraw(); };
			MouseExited += () => { encima = false; QueueRedraw(); };
		}
		else
		{
			MouseFilter = MouseFilterEnum.Ignore;
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (AlHacerClic != null && @event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			AcceptEvent();
			AlHacerClic.Invoke();
		}
	}

	public override void _Draw()
	{
		Vector2 s = Size;
		Color gris = encima ? Colors.White : new Color("8fb3bd");

		switch (Tipo)
		{
			case Clase.Logo:
			{
				StyleBoxFlat fondo = new StyleBoxFlat { BgColor = new Color("e0332e") };
				fondo.SetCornerRadiusAll(4);
				DrawStyleBox(fondo, new Rect2(Vector2.Zero, s));

				Color claro = new Color("ffe1de");
				DrawColoredPolygon(new[]
				{
					new Vector2(s.X * 0.16f, s.Y * 0.80f),
					new Vector2(s.X * 0.40f, s.Y * 0.40f),
					new Vector2(s.X * 0.64f, s.Y * 0.80f)
				}, claro);
				DrawColoredPolygon(new[]
				{
					new Vector2(s.X * 0.50f, s.Y * 0.80f),
					new Vector2(s.X * 0.68f, s.Y * 0.55f),
					new Vector2(s.X * 0.86f, s.Y * 0.80f)
				}, claro);
				DrawCircle(new Vector2(s.X * 0.72f, s.Y * 0.28f), s.X * 0.08f, claro);
				break;
			}
			case Clase.Alerta:
			{
				StyleBoxFlat fondo = new StyleBoxFlat { BgColor = new Color("2a0508"), BorderColor = new Color("ff3b34") };
				fondo.SetBorderWidthAll(2);
				fondo.SetCornerRadiusAll(3);
				DrawStyleBox(fondo, new Rect2(Vector2.Zero, s));

				Color rojo = new Color("ff4a44");
				DrawPolyline(new[]
				{
					new Vector2(s.X * 0.50f, s.Y * 0.18f),
					new Vector2(s.X * 0.85f, s.Y * 0.80f),
					new Vector2(s.X * 0.15f, s.Y * 0.80f),
					new Vector2(s.X * 0.50f, s.Y * 0.18f)
				}, rojo, 2f);
				DrawLine(new Vector2(s.X * 0.5f, s.Y * 0.40f), new Vector2(s.X * 0.5f, s.Y * 0.60f), rojo, 2f);
				DrawCircle(new Vector2(s.X * 0.5f, s.Y * 0.70f), 1.5f, rojo);
				break;
			}
			case Clase.Minimizar:
				DrawLine(new Vector2(3, s.Y * 0.65f), new Vector2(s.X - 3, s.Y * 0.65f), gris, 1.5f);
				break;
			case Clase.Maximizar:
				DrawRect(new Rect2(3, 3, s.X - 6, s.Y - 6), gris, false, 1.5f);
				break;
			case Clase.Cerrar:
			{
				Color c = encima ? new Color("ff5a52") : gris;
				DrawLine(new Vector2(3, 3), new Vector2(s.X - 3, s.Y - 3), c, 1.5f);
				DrawLine(new Vector2(s.X - 3, 3), new Vector2(3, s.Y - 3), c, 1.5f);
				break;
			}
			case Clase.Avatar:
			{
				Color cian = new Color("39b7cc");
				DrawRect(new Rect2(Vector2.Zero, s), new Color("02080f"));
				DrawRect(new Rect2(Vector2.Zero, s), cian, false, 2f);
				DrawArc(new Vector2(s.X * 0.5f, s.Y * 0.36f), s.X * 0.16f, 0f, Mathf.Tau, 32, cian, 3f);
				DrawArc(new Vector2(s.X * 0.5f, s.Y * 0.98f), s.X * 0.32f, Mathf.Pi, Mathf.Tau, 32, cian, 3f);
				break;
			}
			case Clase.Check:
			{
				Color verde = new Color("2cf0b0");
				DrawPolyline(new[]
				{
					new Vector2(s.X * 0.22f, s.Y * 0.52f),
					new Vector2(s.X * 0.43f, s.Y * 0.74f),
					new Vector2(s.X * 0.80f, s.Y * 0.28f)
				}, verde, Math.Max(3f, s.X * 0.10f));
				break;
			}
			case Clase.Cruz:
			{
				Color rojo = new Color("ff4a52");
				float grosor = Math.Max(3f, s.X * 0.10f);
				DrawLine(new Vector2(s.X * 0.26f, s.Y * 0.26f), new Vector2(s.X * 0.74f, s.Y * 0.74f), rojo, grosor);
				DrawLine(new Vector2(s.X * 0.74f, s.Y * 0.26f), new Vector2(s.X * 0.26f, s.Y * 0.74f), rojo, grosor);
				break;
			}
			case Clase.Reloj:
			{
				Color cian = new Color("39b7cc");
				Vector2 centro = s * 0.5f;
				DrawArc(centro, s.X * 0.42f, 0f, Mathf.Tau, 32, cian, 2f);
				DrawLine(centro, centro + new Vector2(0, -s.Y * 0.26f), cian, 2f);
				DrawLine(centro, centro + new Vector2(s.X * 0.18f, 0), cian, 2f);
				break;
			}
		}
	}
}
