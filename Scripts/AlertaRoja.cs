using Godot;
using System;

/// <summary>
/// Capa roja sobre la pantalla que avisa que el sistema se está infectando.
/// Cada impostor aceptado la vuelve más intensa (bordes rojos que laten).
/// Es solo visual: no cambia nada de la lógica del juego.
/// </summary>
public partial class AlertaRoja : CanvasLayer
{
	private ColorRect destello;
	private TextureRect vineta;
	private float nivel = 0f;        // 0 = sin alerta, 1 = al borde del colapso
	private float destelloAlfa = 0f;
	private double tiempo = 0;

	public override void _Ready()
	{
		Layer = 90; // por debajo de la pantalla de Game Over (100)

		Gradient degradado = new Gradient();
		degradado.SetColor(0, new Color(0.8f, 0f, 0f, 0f));
		degradado.SetColor(1, new Color(0.8f, 0f, 0f, 0.85f));
		degradado.SetOffset(0, 0.35f);

		GradientTexture2D textura = new GradientTexture2D
		{
			Gradient = degradado,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(1f, 0.5f),
			Width = 256,
			Height = 256
		};

		vineta = new TextureRect
		{
			Texture = textura,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(1, 1, 1, 0)
		};
		vineta.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(vineta);

		destello = new ColorRect
		{
			Color = new Color(0.9f, 0.05f, 0.05f, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		destello.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(destello);
	}

	/// <summary>Llamar al aceptar un impostor. nuevoNivel va de 0 a 1.</summary>
	public void Pulso(float nuevoNivel)
	{
		nivel = Math.Clamp(nuevoNivel, 0f, 1f);
		destelloAlfa = 0.25f + nivel * 0.35f;
	}

	public override void _Process(double delta)
	{
		tiempo += delta;

		// Los bordes rojos "laten": más rápido y más fuerte cuanto más alto el nivel
		float latido = 0.8f + 0.2f * (float)Math.Sin(tiempo * (3.0 + nivel * 5.0));
		vineta.Modulate = new Color(1, 1, 1, Math.Min(1f, nivel * 0.9f * latido));

		if (destelloAlfa > 0f)
		{
			destelloAlfa = Math.Max(0f, destelloAlfa - (float)(1.5 * delta));
			destello.Color = new Color(0.9f, 0.05f, 0.05f, destelloAlfa);
		}
	}
}
