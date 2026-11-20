using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 180;
	[Export]
	public bool CanControl = true;

	private AnimatedSprite2D anim;

	// Las tiras de sprites tienen distinto tamaño de personaje según la dirección
	// (de espaldas se ve más alto, de costado más bajo). Se compensa con una
	// escala por animación para que no "salte" de tamaño al girar.
	private const float AlturaReferencia = 140f;
	private const float PiesEnCelda = 80f;   // distancia del centro de la celda a los pies
	private Vector2 escalaBase;
	private float piesY;
	private float baseX;

	public override void _Ready()
	{
		anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		escalaBase = anim.Scale;
		baseX = anim.Position.X;
		piesY = anim.Position.Y + PiesEnCelda * escalaBase.Y;
	}

	private static float FactorEscala(string nombre)
	{
		switch (nombre)
		{
			case "walkup": return AlturaReferencia / 148f;
			case "walkleft":
			case "walkright": return AlturaReferencia / 130f;
			default: return 1f;
		}
	}

	// Cambia de animación sin reiniciarla cada cuadro y mantiene los pies en el mismo lugar
	private void Reproducir(string nombre, float velocidad = 1f)
	{
		if (anim.Animation != nombre || !anim.IsPlaying())
		{
			anim.Play(nombre);
			float k = FactorEscala(nombre);
			anim.Scale = escalaBase * k;
			anim.Position = new Vector2(baseX, piesY - PiesEnCelda * escalaBase.Y * k);
		}
		anim.SpeedScale = velocidad;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Con un panel abierto (ej: configuración de partida) el jugador no se mueve
		if (!CanControl)
		{
			Reproducir("idle");
			return;
		}

		Vector2 direccion = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W)) direccion.Y--;
		if (Input.IsKeyPressed(Key.S)) direccion.Y++;
		if (Input.IsKeyPressed(Key.A)) direccion.X--;
		if (Input.IsKeyPressed(Key.D)) direccion.X++;

		bool corriendo = Input.IsKeyPressed(Key.Shift);
		Speed = corriendo ? 400 : 180;

		if (direccion == Vector2.Zero)
		{
			Reproducir("idle");
		}
		else
		{
			// En diagonal manda la animación de costado; así no se alterna entre direcciones
			string nombre;
			if (direccion.X != 0) nombre = direccion.X < 0 ? "walkleft" : "walkright";
			else nombre = direccion.Y < 0 ? "walkup" : "walkdown";

			// Al correr, los pasos van más rápido
			Reproducir(nombre, corriendo ? 1.8f : 1f);
		}

		Position += direccion.Normalized() * Speed * (float)delta;
		MoveAndSlide();
	}
}
