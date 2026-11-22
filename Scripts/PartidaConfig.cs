using Godot;

public enum ModoJuego
{
	TurnoDeGuardia,
	Infinito
}

public enum Dificultad
{
	Facil,
	Normal,
	Dificil
}

/// <summary>
/// Configuración de la partida singleplayer. Es estática: se elige en el panel
/// de la silla y la lee ComputerGameplay al arrancar (y al reintentar).
/// </summary>
public static class PartidaConfig
{
	public static ModoJuego Modo = ModoJuego.TurnoDeGuardia;
	public static Dificultad Nivel = Dificultad.Normal;
	public static int DuracionMinutos = 3; // solo se usa en Turno de guardia

	public static double DuracionSegundos => DuracionMinutos * 60.0;

	public static string NombreNivel => Nivel switch
	{
		Dificultad.Facil => "Fácil",
		Dificultad.Normal => "Normal",
		_ => "Difícil"
	};

	// Probabilidad de que una conexión sea un virus
	public static double ProbInfectada => Nivel switch
	{
		Dificultad.Facil => 0.15,
		Dificultad.Normal => 0.20,
		_ => 0.30
	};

	// Segundos antes de que una solicitud se autoacepte
	public static double SegundosAutoAceptar => Nivel switch
	{
		Dificultad.Facil => 20,
		Dificultad.Normal => 15,
		_ => 10
	};

	public static int MaxErroresAceptar => Nivel switch
	{
		Dificultad.Facil => 7,
		Dificultad.Normal => 5,
		_ => 3
	};

	public static int MaxErroresRechazar => Nivel switch
	{
		Dificultad.Facil => 7,
		Dificultad.Normal => 5,
		_ => 3
	};

	public static int MaxAutoAceptadas => Nivel switch
	{
		Dificultad.Facil => 5,
		Dificultad.Normal => 3,
		_ => 2
	};
}
