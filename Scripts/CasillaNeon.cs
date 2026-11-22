using Godot;
using System;

/// <summary>Casilla de verificación con el estilo neón de los diseños (cuadrado con tilde).</summary>
public partial class CasillaNeon : Control
{
	public event Action<bool> Cambiado;

	public Color ColorBorde = new Color("39b7cc");

	private bool marcada = false;
	private bool encima = false;

	public bool Marcada
	{
		get => marcada;
		set
		{
			if (marcada == value) return;
			marcada = value;
			QueueRedraw();
		}
	}

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(36, 36);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		MouseFilter = MouseFilterEnum.Stop;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		FocusMode = FocusModeEnum.None;
		MouseEntered += () => { encima = true; QueueRedraw(); };
		MouseExited += () => { encima = false; QueueRedraw(); };
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			AcceptEvent();
			marcada = !marcada;
			QueueRedraw();
			Cambiado?.Invoke(marcada);
		}
	}

	public override void _Draw()
	{
		Vector2 s = Size;
		StyleBoxFlat caja = new StyleBoxFlat
		{
			BgColor = encima ? new Color("06202e") : new Color("020c16"),
			BorderColor = encima ? Colors.White : ColorBorde
		};
		caja.SetBorderWidthAll(2);
		caja.SetCornerRadiusAll(6);
		DrawStyleBox(caja, new Rect2(Vector2.Zero, s));

		if (marcada)
		{
			float g = Mathf.Max(3f, s.X * 0.1f);
			DrawPolyline(new[]
			{
				new Vector2(s.X * 0.22f, s.Y * 0.52f),
				new Vector2(s.X * 0.43f, s.Y * 0.73f),
				new Vector2(s.X * 0.80f, s.Y * 0.28f)
			}, ColorBorde, g);
		}
	}
}
