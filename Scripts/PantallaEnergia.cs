using Godot;
using System;
using System.Threading.Tasks;

/// <summary>
/// Efecto de "apagar" y "reiniciar" la PC del escritorio.
///  - Apagar: la pantalla se cierra como un monitor viejo, queda negro y se vuelve al menú principal.
///  - Reiniciar: igual, pero aparece el arranque del sistema y se vuelve al escritorio.
/// Es solo presentación: mientras dura, la partida queda en pausa y no cambia ningún dato.
/// </summary>
public partial class PantallaEnergia : CanvasLayer
{
	private enum Tipo {Apagar, Reiniciar, ReiniciarPartida, ReiniciarEmergencia }

	private const string EscenaMenu = "res://Scenes/MenuPrincipal.tscn";

	private static bool enCurso = false;

	private Tipo modo = Tipo.Apagar;
	private Control raiz;
	private Vector2 pantalla;
	private Action alFinalizar;

	public static void Apagar(Node origen) => Iniciar(origen, Tipo.Apagar);

	public static void Reiniciar(Node origen) => Iniciar(origen, Tipo.Reiniciar);
	public static void ReiniciarPartida(Node origen) {
		Iniciar(origen, Tipo.ReiniciarPartida);
	}
	public static void ReiniciarEmergencia(Node origen, Action alFinalizar) {
		Iniciar(origen, Tipo.ReiniciarEmergencia, alFinalizar);
	}

	private static void Iniciar(Node origen, Tipo tipo, Action alFinalizar = null)
	{
		if (enCurso || origen == null) return;
		SceneTree arbol = origen.GetTree();
		if (arbol == null) return;

		enCurso = true;
		// Se cuelga de la raíz para que siga existiendo aunque cambie la escena
		arbol.Root.AddChild(new PantallaEnergia{
			modo = tipo,
			alFinalizar = alFinalizar
		});
	}

	public override void _Ready()
	{
		Layer = 200;
		ProcessMode = ProcessModeEnum.Always;

		raiz = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
		AddChild(raiz);
		raiz.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		pantalla = GetViewport().GetVisibleRect().Size;

		Secuencia();
	}

	public override void _ExitTree()
	{
		enCurso = false;
	}

	private async void Secuencia()
	{
		// La partida queda quieta mientras se apaga / reinicia
		if (modo != Tipo.ReiniciarEmergencia)
			GetTree().Paused = true;

		await CerrarComoMonitor();

		ColorRect negro = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		raiz.AddChild(negro);
		negro.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		if (modo == Tipo.Apagar)
		{
			await Esperar(1.1);

			GetTree().Paused = false;
			GetTree().ChangeSceneToFile(EscenaMenu);

			// El menú principal ya hace su propio fundido: dejamos el negro un instante y lo sacamos
			await Esperar(0.35);
			await Desvanecer(0.4);
			QueueFree();
			return;
		}
		
		await Esperar(0.7);
		await MostrarArranque();
		await Esperar(0.35);

		// Reinicio desde el menú Inicio: partida nueva
		if (modo == Tipo.ReiniciarPartida)
		{
			GetTree().Paused = false;
			GetTree().ReloadCurrentScene();
		}

		await Desvanecer(0.5);

		// Reinicio de emergencia: seguir la misma partida
		if (modo == Tipo.ReiniciarEmergencia)
		{
			alFinalizar?.Invoke();
		}

		GetTree().Paused = false;
		QueueFree();
	}

	// ------------------------------------------------------------------
	// Pasos
	// ------------------------------------------------------------------

	private async Task Esperar(double segundos)
	{
		await ToSignal(GetTree().CreateTimer(segundos, true), SceneTreeTimer.SignalName.Timeout);
	}

	private async Task Desvanecer(double segundos)
	{
		Tween t = CreateTween();
		t.SetPauseMode(Tween.TweenPauseMode.Process);
		t.TweenProperty(raiz, "modulate:a", 0f, segundos);
		await ToSignal(t, Tween.SignalName.Finished);
	}

	// Dos barras negras que se cierran desde arriba y abajo, y una línea brillante que se apaga.
	private async Task CerrarComoMonitor()
	{
		float mitad = pantalla.Y * 0.5f;

		ColorRect arriba = new ColorRect
		{
			Color = Colors.Black,
			Position = Vector2.Zero,
			Size = new Vector2(pantalla.X, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		ColorRect abajo = new ColorRect
		{
			Color = Colors.Black,
			Position = new Vector2(0f, pantalla.Y),
			Size = new Vector2(pantalla.X, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		raiz.AddChild(arriba);
		raiz.AddChild(abajo);

		Tween cierre = CreateTween();
		cierre.SetPauseMode(Tween.TweenPauseMode.Process);
		cierre.SetParallel(true);
		cierre.TweenProperty(arriba, "size:y", mitad, 0.38).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		cierre.TweenProperty(abajo, "position:y", mitad, 0.38).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		cierre.TweenProperty(abajo, "size:y", mitad, 0.38).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		await ToSignal(cierre, Tween.SignalName.Finished);

		// La línea del centro se achica hacia el medio y desaparece
		ColorRect linea = new ColorRect
		{
			Color = new Color("d8f4ff"),
			Position = new Vector2(0f, mitad - 1.5f),
			Size = new Vector2(pantalla.X, 3f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		raiz.AddChild(linea);

		Tween apagado = CreateTween();
		apagado.SetPauseMode(Tween.TweenPauseMode.Process);
		apagado.SetParallel(true);
		apagado.TweenProperty(linea, "position:x", pantalla.X * 0.5f, 0.28).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		apagado.TweenProperty(linea, "size:x", 0f, 0.28).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		await ToSignal(apagado, Tween.SignalName.Finished);

		linea.QueueFree();
	}

	// Texto de arranque estilo consola y una barra de carga.
	private async Task MostrarArranque()
	{
		string[] lineas =
		{
			"RED FLAG OS  v1.0",
			"",
			"Comprobando memoria ............ OK",
			"Cargando controladores ......... OK",
			"Iniciando servicios de red ..... OK",
			"Cargando escritorio ............ OK",
			"",
			"Bienvenido."
		};

		Label texto = RedFlagUI.CrearLabel("", 18, new Color("4dd0a0"));
		texto.Position = new Vector2(Mathf.Max(24f, pantalla.X * 0.07f), Mathf.Max(24f, pantalla.Y * 0.12f));
		texto.MouseFilter = Control.MouseFilterEnum.Ignore;
		raiz.AddChild(texto);

		float anchoBarra = pantalla.X * 0.5f;
		float xBarra = (pantalla.X - anchoBarra) * 0.5f;
		float yBarra = pantalla.Y * 0.80f;

		ColorRect marco = new ColorRect
		{
			Color = new Color("1b5f6e"),
			Position = new Vector2(xBarra - 2f, yBarra - 2f),
			Size = new Vector2(anchoBarra + 4f, 16f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		raiz.AddChild(marco);

		ColorRect fondoBarra = new ColorRect
		{
			Color = Colors.Black,
			Position = new Vector2(xBarra, yBarra),
			Size = new Vector2(anchoBarra, 12f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		raiz.AddChild(fondoBarra);

		ColorRect relleno = new ColorRect
		{
			Color = new Color("39b7cc"),
			Position = new Vector2(xBarra, yBarra),
			Size = new Vector2(0f, 12f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		raiz.AddChild(relleno);

		for (int i = 0; i < lineas.Length; i++)
		{
			texto.Text += (i == 0 ? "" : "\n") + lineas[i];
			relleno.Size = new Vector2(anchoBarra * (i + 1) / lineas.Length, 12f);
			await Esperar(0.3);
		}
	}
}
