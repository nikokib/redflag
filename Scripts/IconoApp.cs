using Godot;

/// <summary>
/// Ícono sencillo dibujado por código: monograma de color (apps del escritorio),
/// carpeta o PDF (explorador de archivos).
/// </summary>
public partial class IconoApp : Control
{
	public enum Forma { Monograma, Carpeta, Pdf }

	public Forma Tipo = Forma.Monograma;
	public string Texto = "";
	public Color ColorBase = new Color("39b7cc");

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		Vector2 s = Size;

		switch (Tipo)
		{
			case Forma.Monograma:
			{
				StyleBoxFlat caja = new StyleBoxFlat
				{
					BgColor = new Color(ColorBase.R * 0.22f, ColorBase.G * 0.22f, ColorBase.B * 0.22f),
					BorderColor = ColorBase
				};
				caja.SetBorderWidthAll(2);
				caja.SetCornerRadiusAll(8);
				DrawStyleBox(caja, new Rect2(Vector2.Zero, s));

				int tamano = (int)(s.Y * (Texto.Length > 2 ? 0.27f : 0.38f));
				Color claro = new Color(
					Mathf.Min(1f, ColorBase.R + 0.35f),
					Mathf.Min(1f, ColorBase.G + 0.35f),
					Mathf.Min(1f, ColorBase.B + 0.35f));
				DrawString(RedFlagUI.Fuente, new Vector2(0, s.Y * 0.5f + tamano * 0.35f), Texto,
					HorizontalAlignment.Center, s.X, tamano, claro);
				break;
			}
			case Forma.Carpeta:
			{
				Color oscuro = new Color("a8772f");
				Color claro = new Color("d9a24d");
				DrawRect(new Rect2(s.X * 0.08f, s.Y * 0.20f, s.X * 0.38f, s.Y * 0.16f), oscuro);
				DrawRect(new Rect2(s.X * 0.08f, s.Y * 0.30f, s.X * 0.84f, s.Y * 0.56f), claro);
				DrawRect(new Rect2(s.X * 0.08f, s.Y * 0.30f, s.X * 0.84f, s.Y * 0.10f), oscuro);
				break;
			}
			case Forma.Pdf:
			{
				DrawRect(new Rect2(s.X * 0.22f, s.Y * 0.10f, s.X * 0.56f, s.Y * 0.80f), new Color("e8eef0"));
				DrawRect(new Rect2(s.X * 0.22f, s.Y * 0.50f, s.X * 0.56f, s.Y * 0.22f), new Color("c0322d"));
				int tamano = (int)(s.Y * 0.16f);
				DrawString(RedFlagUI.Fuente, new Vector2(s.X * 0.22f, s.Y * 0.50f + s.Y * 0.17f), "PDF",
					HorizontalAlignment.Center, s.X * 0.56f, tamano, Colors.White);
				break;
			}
		}
	}
}
