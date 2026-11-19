using Godot;
using System;

// Ventana de opciones sobre el menú principal. No cambia de escena ni corta la música.
public partial class SettingsMenu : Control
{
    public event Action CerrarSolicitado;

    private VBoxContainer contenido;
    private Label mensajeEstado;
    private Button[] botonesPestanas = new Button[4];
    private int pestanaActual = 0;

    // Se mantienen los valores mientras cambiamos de pestaña.
    private float volumenGeneral;
    private float volumenMusica;
    private float volumenEfectos;
    private int resolucion;
    private bool pantallaCompleta;
    private bool vsync;
    private bool daltonismo;
    private bool mostrarHud;
    private string nombreJugador;

    private readonly int[] anchos = { 1280, 1600, 1920 };
    private readonly int[] altos = { 720, 900, 1080 };
    private readonly Color fondo = new Color("101820");
    private readonly Color superficie = new Color("1a2938");
    private readonly Color cian = new Color("40d6dc");
    private readonly Color textoSuave = new Color("a7bdca");

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        CargarValores();
        ConstruirInterfaz();
    }

    public override void _UnhandledInput(InputEvent evento)
    {
        if (evento.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            Volver();
        }
    }

    private void CargarValores()
    {
        volumenGeneral = PlayerPreferences.MasterVolume;
        volumenMusica = PlayerPreferences.MusicVolume;
        volumenEfectos = PlayerPreferences.SfxVolume;
        pantallaCompleta = PlayerPreferences.Fullscreen;
        vsync = PlayerPreferences.Vsync;
        daltonismo = PlayerPreferences.ColorblindMode;
        mostrarHud = PlayerPreferences.HudEnabled;
        nombreJugador = PlayerPreferences.PlayerName;
        resolucion = PlayerPreferences.WindowWidth >= 1900 ? 2
            : PlayerPreferences.WindowWidth >= 1500 ? 1 : 0;
    }

    private StyleBoxFlat Caja(Color color, int borde = 0)
    {
        var caja = new StyleBoxFlat();
        caja.BgColor = color;
        caja.BorderColor = cian;
        caja.BorderWidthLeft = borde;
        caja.BorderWidthRight = borde;
        caja.BorderWidthTop = borde;
        caja.BorderWidthBottom = borde;
        caja.CornerRadiusTopLeft = 8;
        caja.CornerRadiusTopRight = 8;
        caja.CornerRadiusBottomLeft = 8;
        caja.CornerRadiusBottomRight = 8;
        return caja;
    }

    private Label Etiqueta(string texto, int tamano = 16, bool tenue = false)
    {
        var label = new Label { Text = texto, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", tamano);
        label.AddThemeColorOverride("font_color", tenue ? textoSuave : Colors.White);
        return label;
    }

    private Button Boton(string texto, bool principal = false)
    {
        var boton = new Button { Text = texto, CustomMinimumSize = new Vector2(145, 43) };
        boton.AddThemeFontSizeOverride("font_size", 16);
        boton.AddThemeColorOverride("font_color", principal ? fondo : Colors.White);
        boton.AddThemeColorOverride("font_hover_color", Colors.White);
        boton.AddThemeStyleboxOverride("normal", Caja(principal ? cian : superficie, principal ? 0 : 1));
        boton.AddThemeStyleboxOverride("hover", Caja(new Color("2c6072"), 1));
        boton.AddThemeStyleboxOverride("pressed", Caja(new Color("175267"), 1));
        return boton;
    }

    private void ConstruirInterfaz()
    {
        // Capa oscura: tapa los botones de detrás y mantiene el fondo animado.
        var sombra = new ColorRect { Color = new Color(0f, 0.04f, 0.07f, 0.89f), MouseFilter = MouseFilterEnum.Stop };
        sombra.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(sombra);

        var centrado = new CenterContainer();
        centrado.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(centrado);

        var ventana = new PanelContainer { CustomMinimumSize = new Vector2(860, 500) };
        ventana.AddThemeStyleboxOverride("panel", Caja(fondo, 1));
        centrado.AddChild(ventana);

        var margen = new MarginContainer();
        margen.AddThemeConstantOverride("margin_left", 24);
        margen.AddThemeConstantOverride("margin_right", 24);
        margen.AddThemeConstantOverride("margin_top", 20);
        margen.AddThemeConstantOverride("margin_bottom", 20);
        ventana.AddChild(margen);

        var principal = new VBoxContainer();
        principal.AddThemeConstantOverride("separation", 15);
        margen.AddChild(principal);

        var cabecera = new HBoxContainer();
        principal.AddChild(cabecera);
        var titulo = new VBoxContainer();
        titulo.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        cabecera.AddChild(titulo);
        titulo.AddChild(Etiqueta("RED FLAG / SISTEMA", 12, true));
        titulo.AddChild(Etiqueta("CONFIGURACIÓN", 28));
        var cerrar = Boton("✕", false);
        cerrar.CustomMinimumSize = new Vector2(45, 40);
        cerrar.Pressed += Volver;
        cabecera.AddChild(cerrar);

        principal.AddChild(new HSeparator());

        var centro = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        centro.AddThemeConstantOverride("separation", 18);
        principal.AddChild(centro);

        var menu = new VBoxContainer { CustomMinimumSize = new Vector2(185, 0) };
        menu.AddThemeConstantOverride("separation", 9);
        centro.AddChild(menu);
        string[] nombres = { "Pantalla", "Sonido", "Accesibilidad", "Perfil" };
        for (int i = 0; i < nombres.Length; i++)
        {
            int numero = i;
            var boton = Boton(nombres[i]);
            boton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            boton.Pressed += () => MostrarPestana(numero);
            botonesPestanas[i] = boton;
            menu.AddChild(boton);
        }

        var area = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        area.AddThemeStyleboxOverride("panel", Caja(superficie));
        centro.AddChild(area);
        var areaMargen = new MarginContainer();
        areaMargen.AddThemeConstantOverride("margin_left", 24);
        areaMargen.AddThemeConstantOverride("margin_right", 24);
        areaMargen.AddThemeConstantOverride("margin_top", 20);
        areaMargen.AddThemeConstantOverride("margin_bottom", 20);
        area.AddChild(areaMargen);
        contenido = new VBoxContainer();
        contenido.AddThemeConstantOverride("separation", 15);
        areaMargen.AddChild(contenido);

        mensajeEstado = Etiqueta("Podés modificar varias pestañas antes de guardar.", 13, true);
        principal.AddChild(mensajeEstado);

        var pie = new HBoxContainer();
        pie.AddThemeConstantOverride("separation", 12);
        principal.AddChild(pie);
        var restaurar = Boton("Restaurar valores");
        restaurar.Pressed += Restaurar;
        pie.AddChild(restaurar);
        pie.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var volver = Boton("Volver");
        volver.Pressed += Volver;
        pie.AddChild(volver);
        var guardar = Boton("Guardar cambios", true);
        guardar.Pressed += Guardar;
        pie.AddChild(guardar);

        MostrarPestana(0);
    }

    private void MostrarPestana(int numero)
    {
        pestanaActual = numero;
        // Quitamos los controles anteriores sin perder los valores (están en campos).
        foreach (Node hijo in contenido.GetChildren())
        {
            contenido.RemoveChild(hijo);
            hijo.QueueFree();
        }
        for (int i = 0; i < botonesPestanas.Length; i++)
            botonesPestanas[i].Modulate = i == pestanaActual ? Colors.White : new Color("a1adb5");

        if (numero == 0) PestañaPantalla();
        if (numero == 1) PestañaSonido();
        if (numero == 2) PestañaAccesibilidad();
        if (numero == 3) PestañaPerfil();
    }

    private void PestañaPantalla()
    {
        contenido.AddChild(Etiqueta("PANTALLA", 21));
        contenido.AddChild(Etiqueta("Resolución de la ventana", 14, true));
        var selector = new OptionButton { CustomMinimumSize = new Vector2(0, 42) };
        selector.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        selector.AddItem("1280 × 720");
        selector.AddItem("1600 × 900");
        selector.AddItem("1920 × 1080");
        selector.Select(resolucion);
        selector.ItemSelected += indice => resolucion = (int)indice;
        contenido.AddChild(selector);
        var completa = new CheckButton { Text = "Pantalla completa", ButtonPressed = pantallaCompleta };
        completa.Toggled += activo => pantallaCompleta = activo;
        contenido.AddChild(completa);
        var sync = new CheckButton { Text = "Sincronización vertical (VSync)", ButtonPressed = vsync };
        sync.Toggled += activo => vsync = activo;
        contenido.AddChild(sync);
        contenido.AddChild(Etiqueta("Los cambios de pantalla se aplican al guardar.", 13, true));
    }

    private void PestañaSonido()
    {
        contenido.AddChild(Etiqueta("SONIDO", 21));
        AgregarVolumen("Volumen general", volumenGeneral, v => volumenGeneral = v);
        AgregarVolumen("Música", volumenMusica, v => volumenMusica = v);
        AgregarVolumen("Efectos (SFX)", volumenEfectos, v => volumenEfectos = v);
        contenido.AddChild(Etiqueta("Los volúmenes se escuchan al moverlos.\nGuardá los cambios para conservarlos.", 13, true));
    }

    private void AgregarVolumen(string nombre, float valor, Action<float> cambiar)
    {
        var fila = new HBoxContainer();
        contenido.AddChild(fila);
        var etiqueta = Etiqueta(nombre, 15);
        etiqueta.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        fila.AddChild(etiqueta);
        var numero = Etiqueta($"{Mathf.RoundToInt(valor * 100)}%", 15, true);
        fila.AddChild(numero);
        var barra = new HSlider { MinValue = 0, MaxValue = 100, Step = 1, Value = Mathf.RoundToInt(valor * 100) };
        barra.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        barra.CustomMinimumSize = new Vector2(0, 24);
        barra.ValueChanged += n =>
        {
            cambiar((float)n / 100f);
            numero.Text = $"{Mathf.RoundToInt((float)n)}%";
            PlayerPreferences.PreviewAudio(volumenGeneral, volumenMusica, volumenEfectos);
            mensajeEstado.Text = "Vista previa de sonido: todavía no se guardó.";
        };
        contenido.AddChild(barra);
    }

    private void PestañaAccesibilidad()
    {
        contenido.AddChild(Etiqueta("ACCESIBILIDAD Y CONTROLES", 20));
        var paleta = new CheckButton { Text = "Paleta accesible", ButtonPressed = daltonismo };
        paleta.Toggled += valor => daltonismo = valor;
        contenido.AddChild(paleta);
        var hud = new CheckButton { Text = "Mostrar HUD", ButtonPressed = mostrarHud };
        hud.Toggled += valor => mostrarHud = valor;
        contenido.AddChild(hud);
        contenido.AddChild(Etiqueta("Estas dos opciones están integradas principalmente en las escenas multijugador actuales.", 13, true));
        contenido.AddChild(new HSeparator());
        contenido.AddChild(Etiqueta("CONTROLES", 16));
        contenido.AddChild(Etiqueta("Moverse: W A S D       Correr: Shift\nInteractuar: según la indicación de la escena", 14, true));
    }

    private void PestañaPerfil()
    {
        contenido.AddChild(Etiqueta("PERFIL", 21));
        contenido.AddChild(Etiqueta("Nombre de jugador", 15, true));
        var editar = new LineEdit { Text = nombreJugador, PlaceholderText = "Escribí tu nombre", MaxLength = 24 };
        editar.TextChanged += valor => nombreJugador = valor;
        contenido.AddChild(editar);
        contenido.AddChild(Etiqueta("Necesitás un nombre para crear o unirte a partidas online.", 13, true));
        contenido.AddChild(new HSeparator());
        contenido.AddChild(Etiqueta("ID de jugador: " + PlayerPreferences.PlayerId, 16));
        contenido.AddChild(Etiqueta("El identificador es único y se guarda en tu equipo.", 13, true));
    }

    private void Guardar()
    {
        PlayerPreferences.SaveAudio(volumenGeneral, volumenMusica, volumenEfectos);
        PlayerPreferences.SaveDisplay(anchos[resolucion], altos[resolucion], pantallaCompleta);
        PlayerPreferences.SaveGraphics(vsync);
        PlayerPreferences.SaveAccessibility(daltonismo, mostrarHud);
        PlayerPreferences.SaveName(nombreJugador);
        PlayerPreferences.ApplyDisplayAndGraphics();
        mensajeEstado.Text = "✓ Configuración guardada correctamente.";
        mensajeEstado.AddThemeColorOverride("font_color", new Color("8de8bd"));
    }

    private void Restaurar()
    {
        volumenGeneral = 1f;
        volumenMusica = 0.8f;
        volumenEfectos = 0.8f;
        resolucion = 0;
        pantallaCompleta = false;
        vsync = true;
        daltonismo = false;
        mostrarHud = true;
        PlayerPreferences.PreviewAudio(volumenGeneral, volumenMusica, volumenEfectos);
        MostrarPestana(pestanaActual);
        mensajeEstado.Text = "Valores restaurados. Presioná Guardar para confirmarlos.";
    }

    private void Volver()
    {
        // Si no guardó los volúmenes, restauramos los del disco.
        PlayerPreferences.ApplyAudio();
        CerrarSolicitado?.Invoke();
    }
}
