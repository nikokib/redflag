using Godot;
using System;

// Ventana de opciones sobre el menú principal. No cambia de escena ni corta la música.
// Diseño neón "RED FLAG | CONFIGURACIÓN": Pantalla, Sonido, Accesibilidad, Perfil y Personalización.
public partial class SettingsMenu : Control
{
    public event Action CerrarSolicitado;

    private Label tituloSeccion;
    private IconoSistema iconoSeccion;
    private VBoxContainer contenido;
    private Label mensajeEstado;
    private IconoSistema iconoInfo;
    private Control filaInfo;
    private Control lineaInfo;
    private Button botonRestaurar;
    private Button botonVolver;
    private Button botonGuardar;

    private const int CantidadPestanas = 5;
    private readonly Button[] botonesPestanas = new Button[CantidadPestanas];
    private readonly IconoSistema[] iconosPestanas = new IconoSistema[CantidadPestanas];
    private int pestanaActual = 0;

    private readonly string[] nombresPestanas = { "Pantalla", "Sonido", "Accesibilidad", "Perfil", "Personalización" };
    private readonly string[] titulosSeccion = { "PANTALLA", "SONIDO", "ACCESIBILIDAD", "PERFIL", "PERSONALIZACIÓN" };
    private readonly IconoSistema.Clase[] clasesIconos =
    {
        IconoSistema.Clase.Pc, IconoSistema.Clase.Altavoz, IconoSistema.Clase.Accesible,
        IconoSistema.Clase.Persona, IconoSistema.Clase.Camiseta
    };

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
    private int avatarRopa;
    private int avatarCabello;
    private int avatarPiel;
    private int avatarGenero;

    // Personalización: referencias para refrescar la vista previa
    private AvatarJugador avatarVista;
    private Label leyendaVista;
    private AvatarJugador.Modo modoVista = AvatarJugador.Modo.Cuerpo;
    private AvatarJugador miniRopa;
    private AvatarJugador miniCabello;
    private ColorRect muestraPiel;
    private IconoSistema iconoMiniGenero;
    private IconoSistema iconoFilaGenero;
    private readonly Label[] nombresOpcion = new Label[4];
    private readonly Control[] miniaturasCaja = new Control[4];

    private readonly int[] anchos = { 1280, 1600, 1920 };
    private readonly int[] altos = { 720, 900, 1080 };

    private static readonly Color Rojo = new Color("ff3b34");
    private static readonly Color RojoTexto = new Color("ff5a52");
    private static readonly Color Cian = new Color("39b7cc");
    private static readonly Color CianOscuro = new Color("1b5f6e");
    private static readonly Color Texto = new Color("d4e6ea");
    private static readonly Color TextoSuave = new Color("86a1ab");
    private static readonly Color FondoCaja = new Color("020c16");

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
        avatarRopa = PlayerPreferences.AvatarRopa;
        avatarCabello = PlayerPreferences.AvatarCabello;
        avatarPiel = PlayerPreferences.AvatarPiel;
        avatarGenero = PlayerPreferences.AvatarGenero;
    }

    // ------------------------------------------------------------------
    // Ayudas de estilo
    // ------------------------------------------------------------------

    private static Label Etiqueta(string texto, int tamano = 18, Color? color = null, bool ajustar = false)
    {
        Label l = RedFlagUI.CrearLabel(texto, tamano, color ?? Texto);
        if (ajustar)
        {
            l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        }
        return l;
    }

    private static StyleBoxFlat Caja(Color fondo, Color borde, int grosor, int radio, int margenH = 0, int margenV = 0)
    {
        StyleBoxFlat sb = new StyleBoxFlat { BgColor = fondo, BorderColor = borde };
        sb.SetBorderWidthAll(grosor);
        sb.SetCornerRadiusAll(radio);
        sb.ContentMarginLeft = margenH;
        sb.ContentMarginRight = margenH;
        sb.ContentMarginTop = margenV;
        sb.ContentMarginBottom = margenV;
        return sb;
    }

    private static MarginContainer Margen(Control hijo, int izq, int arriba, int der, int abajo)
    {
        MarginContainer mc = new MarginContainer();
        mc.AddThemeConstantOverride("margin_left", izq);
        mc.AddThemeConstantOverride("margin_top", arriba);
        mc.AddThemeConstantOverride("margin_right", der);
        mc.AddThemeConstantOverride("margin_bottom", abajo);
        mc.AddChild(hijo);
        return mc;
    }

    private static T Expandir<T>(T c) where T : Control
    {
        c.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        c.SizeFlagsVertical = SizeFlags.ExpandFill;
        return c;
    }

    private static ColorRect Linea(Color color, float grosor = 2f)
    {
        return new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(0, grosor),
            MouseFilter = MouseFilterEnum.Ignore
        };
    }

    private static Button BotonNeon(string texto, bool rojo, Vector2 minimo)
    {
        Button b = new Button
        {
            Text = texto,
            CustomMinimumSize = minimo,
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        Color borde = rojo ? Rojo : Cian;
        Color fuente = rojo ? RojoTexto : Texto;
        Color fondo = rojo ? new Color("1a0408") : FondoCaja;
        Color fondoHover = rojo ? new Color("33070e") : new Color("06202e");
        b.AddThemeStyleboxOverride("normal", Caja(fondo, borde, 2, 4, 14, 6));
        b.AddThemeStyleboxOverride("hover", Caja(fondoHover, borde, 2, 4, 14, 6));
        b.AddThemeStyleboxOverride("pressed", Caja(fondoHover, Colors.White, 2, 4, 14, 6));
        b.AddThemeStyleboxOverride("focus", Caja(new Color(0, 0, 0, 0), borde, 2, 4, 14, 6));
        b.AddThemeFontOverride("font", RedFlagUI.Fuente);
        b.AddThemeFontSizeOverride("font_size", 18);
        b.AddThemeColorOverride("font_color", fuente);
        b.AddThemeColorOverride("font_hover_color", Colors.White);
        b.AddThemeColorOverride("font_pressed_color", Colors.White);
        return b;
    }

    private static Button BotonFlecha(string simbolo)
    {
        Button b = BotonNeon(simbolo, false, new Vector2(40, 50));
        b.AddThemeFontSizeOverride("font_size", 22);
        b.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        return b;
    }

    private static Texture2D CrearAgarre(Color relleno, Color borde)
    {
        int ancho = 12;
        int alto = 22;
        Image img = Image.CreateEmpty(ancho, alto, false, Image.Format.Rgba8);
        img.Fill(relleno);
        for (int x = 0; x < ancho; x++)
        {
            img.SetPixel(x, 0, borde);
            img.SetPixel(x, alto - 1, borde);
        }
        for (int y = 0; y < alto; y++)
        {
            img.SetPixel(0, y, borde);
            img.SetPixel(ancho - 1, y, borde);
        }
        return ImageTexture.CreateFromImage(img);
    }

    private static void EstilizarSlider(HSlider barra)
    {
        StyleBoxFlat pista = Caja(new Color("04141f"), CianOscuro, 1, 3, 0, 3);
        StyleBoxFlat relleno = Caja(Cian, Cian, 1, 3, 0, 3);
        barra.AddThemeStyleboxOverride("slider", pista);
        barra.AddThemeStyleboxOverride("grabber_area", relleno);
        barra.AddThemeStyleboxOverride("grabber_area_highlight", relleno);
        Texture2D agarre = CrearAgarre(new Color("0b3446"), Cian);
        Texture2D agarreActivo = CrearAgarre(new Color("1b6f89"), Colors.White);
        barra.AddThemeIconOverride("grabber", agarre);
        barra.AddThemeIconOverride("grabber_highlight", agarreActivo);
        barra.AddThemeIconOverride("grabber_disabled", agarre);
    }

    private static void EstilizarCampo(LineEdit e)
    {
        e.AddThemeStyleboxOverride("normal", Caja(FondoCaja, Cian, 2, 4, 12, 8));
        e.AddThemeStyleboxOverride("focus", Caja(new Color("04141f"), Colors.White, 2, 4, 12, 8));
        e.AddThemeFontOverride("font", RedFlagUI.Fuente);
        e.AddThemeFontSizeOverride("font_size", 18);
        e.AddThemeColorOverride("font_color", Texto);
        e.AddThemeColorOverride("font_placeholder_color", TextoSuave);
        e.AddThemeColorOverride("caret_color", Cian);
    }

    // ------------------------------------------------------------------
    // Estructura de la ventana
    // ------------------------------------------------------------------

    private void ConstruirInterfaz()
    {
        // Capa oscura: tapa los botones de detrás y mantiene el fondo animado.
        ColorRect sombra = new ColorRect { Color = new Color(0f, 0.02f, 0.04f, 0.86f), MouseFilter = MouseFilterEnum.Stop };
        sombra.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(sombra);

        CenterContainer centrado = new CenterContainer();
        centrado.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(centrado);

        MarcoNeon ventana = new MarcoNeon { CustomMinimumSize = new Vector2(940, 540), Margen = 6 };
        centrado.AddChild(ventana);

        VBoxContainer principal = new VBoxContainer();
        principal.AddThemeConstantOverride("separation", 0);
        ventana.AddChild(principal);

        // --- Barra de título ---
        HBoxContainer barra = new HBoxContainer();
        barra.AddThemeConstantOverride("separation", 12);
        barra.CustomMinimumSize = new Vector2(0, 50);
        barra.AddChild(IconoSistema.Crear(IconoSistema.Clase.SobreNeon, 34, Rojo));
        barra.AddChild(Etiqueta("RED FLAG", 22, RojoTexto));
        barra.AddChild(Etiqueta("|", 22, RojoTexto));
        barra.AddChild(Etiqueta("CONFIGURACIÓN", 22, RojoTexto));
        barra.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        barra.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Minimizar, CustomMinimumSize = new Vector2(26, 26) });
        barra.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Maximizar, CustomMinimumSize = new Vector2(26, 26) });
        barra.AddChild(new IconoVentana { Tipo = IconoVentana.Clase.Cerrar, CustomMinimumSize = new Vector2(26, 26), AlHacerClic = Volver });
        principal.AddChild(Margen(barra, 18, 6, 18, 6));
        principal.AddChild(Linea(CianOscuro, 1));

        // --- Cuerpo: menú lateral + zona de contenido ---
        HBoxContainer cuerpo = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        cuerpo.AddThemeConstantOverride("separation", 0);
        principal.AddChild(cuerpo);

        VBoxContainer menu = new VBoxContainer { CustomMinimumSize = new Vector2(240, 0) };
        menu.AddThemeConstantOverride("separation", 8);
        for (int i = 0; i < CantidadPestanas; i++)
        {
            int numero = i;
            Button boton = CrearPestana(i);
            boton.Pressed += () => MostrarPestana(numero);
            menu.AddChild(boton);
        }
        cuerpo.AddChild(Margen(menu, 14, 16, 14, 14));

        ColorRect divisor = new ColorRect { Color = CianOscuro, CustomMinimumSize = new Vector2(1, 0), MouseFilter = MouseFilterEnum.Ignore };
        cuerpo.AddChild(divisor);

        VBoxContainer zona = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        zona.AddThemeConstantOverride("separation", 10);
        cuerpo.AddChild(Expandir(Margen(zona, 30, 16, 30, 10)));

        HBoxContainer cabecera = new HBoxContainer();
        cabecera.AddThemeConstantOverride("separation", 16);
        iconoSeccion = IconoSistema.Crear(IconoSistema.Clase.Pc, 40, Cian);
        cabecera.AddChild(iconoSeccion);
        tituloSeccion = Etiqueta("PANTALLA", 32, Texto);
        cabecera.AddChild(tituloSeccion);
        zona.AddChild(cabecera);
        zona.AddChild(Linea(Cian, 2));

        contenido = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        contenido.AddThemeConstantOverride("separation", 16);
        zona.AddChild(Expandir(Margen(contenido, 0, 8, 0, 0)));

        lineaInfo = Linea(CianOscuro, 1);
        zona.AddChild(lineaInfo);
        HBoxContainer info = new HBoxContainer();
        filaInfo = info;
        info.AddThemeConstantOverride("separation", 10);
        iconoInfo = IconoSistema.Crear(IconoSistema.Clase.Info, 22, Cian);
        info.AddChild(iconoInfo);
        mensajeEstado = Etiqueta("", 15, TextoSuave, true);
        info.AddChild(mensajeEstado);
        zona.AddChild(info);

        // --- Pie con los botones ---
        principal.AddChild(Linea(CianOscuro, 1));
        HBoxContainer pie = new HBoxContainer();
        pie.AddThemeConstantOverride("separation", 14);
        Button restaurar = BotonNeon("Restaurar valores", false, new Vector2(210, 48));
        botonRestaurar = restaurar;
        restaurar.Pressed += Restaurar;
        pie.AddChild(restaurar);
        pie.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        Button volver = BotonNeon("Volver", false, new Vector2(150, 48));
        botonVolver = volver;
        volver.Pressed += Volver;
        pie.AddChild(volver);
        Button guardar = BotonNeon("Guardar cambios", true, new Vector2(210, 48));
        botonGuardar = guardar;
        guardar.Pressed += Guardar;
        pie.AddChild(guardar);
        principal.AddChild(Margen(pie, 18, 12, 18, 12));

        MostrarPestana(0);
    }

    private Button CrearPestana(int indice)
    {
        Button b = new Button
        {
            Text = nombresPestanas[indice],
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(0, 54),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        b.AddThemeFontOverride("font", RedFlagUI.Fuente);
        b.AddThemeFontSizeOverride("font_size", 19);

        IconoSistema icono = IconoSistema.Crear(clasesIconos[indice], 28, Cian);
        icono.Position = new Vector2(16, 13);
        icono.Size = new Vector2(28, 28);
        b.AddChild(icono);

        botonesPestanas[indice] = b;
        iconosPestanas[indice] = icono;
        return b;
    }

    private void PintarPestanas()
    {
        for (int i = 0; i < CantidadPestanas; i++)
        {
            bool activa = i == pestanaActual;
            Button b = botonesPestanas[i];
            StyleBoxFlat normal = activa
                ? Caja(new Color("3d0a12"), Rojo, 2, 4, 0, 0)
                : Caja(new Color("04141f"), new Color("1f8aa6"), 2, 4, 0, 0);
            StyleBoxFlat hover = activa
                ? Caja(new Color("3d0a12"), Rojo, 2, 4, 0, 0)
                : Caja(new Color("082636"), Cian, 2, 4, 0, 0);
            normal.ContentMarginLeft = 62;
            hover.ContentMarginLeft = 62;
            b.AddThemeStyleboxOverride("normal", normal);
            b.AddThemeStyleboxOverride("hover", hover);
            b.AddThemeStyleboxOverride("pressed", hover);
            b.AddThemeStyleboxOverride("focus", normal);
            Color fuente = activa ? Colors.White : Texto;
            b.AddThemeColorOverride("font_color", fuente);
            b.AddThemeColorOverride("font_hover_color", Colors.White);
            b.AddThemeColorOverride("font_pressed_color", Colors.White);
            iconosPestanas[i].ColorBase = activa ? new Color("ff6b63") : Cian;
            iconosPestanas[i].QueueRedraw();
        }
    }

    private void MostrarInfo(string texto, bool correcto = false)
    {
        mensajeEstado.Text = texto;
        Color color = correcto ? new Color("8de8bd") : TextoSuave;
        mensajeEstado.AddThemeColorOverride("font_color", color);
        iconoInfo.ColorBase = correcto ? new Color("8de8bd") : Cian;
        iconoInfo.QueueRedraw();
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

        bool esPersonalizacion = numero == 4;
        tituloSeccion.Text = titulosSeccion[numero];
        iconoSeccion.Tipo = clasesIconos[numero];
        iconoSeccion.Visible = !esPersonalizacion;
        iconoSeccion.QueueRedraw();
        filaInfo.Visible = !esPersonalizacion;
        lineaInfo.Visible = !esPersonalizacion;
        botonVolver.Visible = !esPersonalizacion;
        botonRestaurar.Text = esPersonalizacion ? "RESTABLECER" : "Restaurar valores";
        botonGuardar.Text = esPersonalizacion ? "GUARDAR CAMBIOS" : "Guardar cambios";
        PintarPestanas();

        if (numero == 0) PestanaPantalla();
        if (numero == 1) PestanaSonido();
        if (numero == 2) PestanaAccesibilidad();
        if (numero == 3) PestanaPerfil();
        if (numero == 4) PestanaPersonalizacion();
    }

    // Fila "etiqueta ........ control"
    private HBoxContainer FilaAjuste(string etiqueta, Control derecha)
    {
        HBoxContainer fila = new HBoxContainer();
        fila.AddThemeConstantOverride("separation", 20);
        Label l = Etiqueta(etiqueta, 21, Texto);
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        l.VerticalAlignment = VerticalAlignment.Center;
        fila.AddChild(l);
        fila.AddChild(derecha);
        return fila;
    }

    // ------------------------------------------------------------------
    // Pestañas
    // ------------------------------------------------------------------

    private void PestanaPantalla()
    {
        OptionButton selector = new OptionButton { CustomMinimumSize = new Vector2(420, 50), FocusMode = FocusModeEnum.None };
        RedFlagUI.EstilizarOpcion(selector);
        selector.AddThemeStyleboxOverride("normal", Caja(FondoCaja, Cian, 2, 4, 14, 6));
        selector.AddThemeStyleboxOverride("hover", Caja(new Color("06202e"), Colors.White, 2, 4, 14, 6));
        selector.AddThemeFontSizeOverride("font_size", 20);
        selector.AddItem("1280 x 720");
        selector.AddItem("1600 x 900");
        selector.AddItem("1920 x 1080");
        selector.Select(resolucion);
        selector.ItemSelected += indice => resolucion = (int)indice;
        contenido.AddChild(FilaAjuste("Resolución de la ventana", selector));

        CasillaNeon completa = new CasillaNeon { Marcada = pantallaCompleta };
        completa.Cambiado += activo => pantallaCompleta = activo;
        contenido.AddChild(FilaAjuste("Pantalla completa", completa));

        CasillaNeon sync = new CasillaNeon { Marcada = vsync };
        sync.Cambiado += activo => vsync = activo;
        contenido.AddChild(FilaAjuste("Sincronización vertical (VSync)", sync));

        MostrarInfo("Los cambios de pantalla se aplican al guardar.");
    }

    private void PestanaSonido()
    {
        AgregarVolumen("Volumen general", volumenGeneral, v => volumenGeneral = v);
        AgregarVolumen("Música", volumenMusica, v => volumenMusica = v);
        AgregarVolumen("Efectos (SFX)", volumenEfectos, v => volumenEfectos = v);
        MostrarInfo("Los volúmenes se escuchan al moverlos. Guardá los cambios para conservarlos.");
    }

    private void AgregarVolumen(string nombre, float valor, Action<float> cambiar)
    {
        HBoxContainer fila = new HBoxContainer();
        fila.AddThemeConstantOverride("separation", 20);

        Label etiqueta = Etiqueta(nombre, 21, Texto);
        etiqueta.CustomMinimumSize = new Vector2(230, 0);
        etiqueta.VerticalAlignment = VerticalAlignment.Center;
        fila.AddChild(etiqueta);

        HSlider barra = new HSlider
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 1,
            Value = Mathf.RoundToInt(valor * 100),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(0, 30),
            FocusMode = FocusModeEnum.None
        };
        EstilizarSlider(barra);
        fila.AddChild(barra);

        Label numero = Etiqueta($"{Mathf.RoundToInt(valor * 100)}%", 20, Cian);
        numero.CustomMinimumSize = new Vector2(64, 0);
        numero.HorizontalAlignment = HorizontalAlignment.Right;
        numero.VerticalAlignment = VerticalAlignment.Center;
        fila.AddChild(numero);

        barra.ValueChanged += n =>
        {
            cambiar((float)n / 100f);
            numero.Text = $"{Mathf.RoundToInt((float)n)}%";
            PlayerPreferences.PreviewAudio(volumenGeneral, volumenMusica, volumenEfectos);
            MostrarInfo("Vista previa de sonido: todavía no se guardó.");
        };

        contenido.AddChild(fila);
    }

    private void PestanaAccesibilidad()
    {
        CasillaNeon paleta = new CasillaNeon { Marcada = daltonismo };
        paleta.Cambiado += valor => daltonismo = valor;
        contenido.AddChild(FilaAjuste("Paleta accesible", paleta));

        CasillaNeon hud = new CasillaNeon { Marcada = mostrarHud };
        hud.Cambiado += valor => mostrarHud = valor;
        contenido.AddChild(FilaAjuste("Mostrar HUD", hud));

        contenido.AddChild(Linea(CianOscuro, 1));
        contenido.AddChild(Etiqueta("CONTROLES", 20, Cian));
        contenido.AddChild(Etiqueta("Moverse: W A S D       Correr: Shift\nInteractuar: según la indicación de la escena", 17, TextoSuave, true));

        MostrarInfo("Estas dos opciones están integradas principalmente en las escenas multijugador actuales.");
    }

    private void PestanaPerfil()
    {
        contenido.AddChild(Etiqueta("Nombre de jugador", 21, Texto));
        LineEdit editar = new LineEdit
        {
            Text = nombreJugador,
            PlaceholderText = "Escribí tu nombre",
            MaxLength = 24,
            CustomMinimumSize = new Vector2(0, 50)
        };
        EstilizarCampo(editar);
        editar.TextChanged += valor => nombreJugador = valor;
        contenido.AddChild(editar);

        contenido.AddChild(Linea(CianOscuro, 1));
        contenido.AddChild(Etiqueta("ID de jugador: " + PlayerPreferences.PlayerId, 20, Cian));
        contenido.AddChild(Etiqueta("El identificador es único y se guarda en tu equipo.", 16, TextoSuave, true));

        MostrarInfo("Necesitás un nombre para crear o unirte a partidas online.");
    }

    private void PestanaPersonalizacion()
    {
        HBoxContainer cuerpo = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        cuerpo.AddThemeConstantOverride("separation", 40);
        contenido.AddChild(cuerpo);

        // --- Recuadro con el personaje y las flechas a los costados ---
        PanelContainer marcoVista = new PanelContainer { CustomMinimumSize = new Vector2(300, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        marcoVista.AddThemeStyleboxOverride("panel", Caja(new Color("03101a"), Cian, 2, 4, 10, 10));
        cuerpo.AddChild(marcoVista);

        HBoxContainer filaVista = new HBoxContainer();
        filaVista.AddThemeConstantOverride("separation", 6);
        marcoVista.AddChild(filaVista);

        Button anterior = BotonFlecha("<");
        anterior.Pressed += CambiarVista;
        filaVista.AddChild(anterior);

        avatarVista = new AvatarJugador
        {
            CargarDePreferencias = false,
            Vista = modoVista,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(150, 220)
        };
        filaVista.AddChild(avatarVista);

        Button siguiente = BotonFlecha(">");
        siguiente.Pressed += CambiarVista;
        filaVista.AddChild(siguiente);

        // --- Opciones: Ropa, Cabello, Piel, Género ---
        VBoxContainer opciones = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        opciones.AddThemeConstantOverride("separation", 10);
        cuerpo.AddChild(opciones);

        miniRopa = new AvatarJugador
        {
            CargarDePreferencias = false,
            Vista = AvatarJugador.Modo.Prenda,
            CustomMinimumSize = new Vector2(64, 52)
        };
        opciones.AddChild(FilaPersonalizacion("Ropa:", IconoSistema.Clase.Camiseta, new Color("6db8f0"), miniRopa, 0, d =>
        {
            avatarRopa = AvatarJugador.Mod(avatarRopa + d, AvatarJugador.Ropas.Length);
        }));

        miniCabello = new AvatarJugador
        {
            CargarDePreferencias = false,
            Vista = AvatarJugador.Modo.Cabeza,
            CustomMinimumSize = new Vector2(56, 52)
        };
        opciones.AddChild(FilaPersonalizacion("Cabello:", IconoSistema.Clase.Cabello, new Color("7fa6e8"), miniCabello, 1, d =>
        {
            avatarCabello = AvatarJugador.Mod(avatarCabello + d, AvatarJugador.Peinados.Length);
        }));

        muestraPiel = new ColorRect
        {
            CustomMinimumSize = new Vector2(44, 44),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore
        };
        opciones.AddChild(FilaPersonalizacion("Piel:", IconoSistema.Clase.Cara, new Color("e8c9a8"), muestraPiel, 2, d =>
        {
            avatarPiel = AvatarJugador.Mod(avatarPiel + d, AvatarJugador.Pieles.Length);
        }));

        iconoMiniGenero = IconoSistema.Crear(IconoSistema.Clase.GeneroM, 44, Cian);
        opciones.AddChild(FilaPersonalizacion("Género:", IconoSistema.Clase.GeneroM, new Color("a5b4ff"), iconoMiniGenero, 3, d =>
        {
            avatarGenero = AvatarJugador.Mod(avatarGenero + d, AvatarJugador.NombresGenero.Length);
        }));

        RefrescarPersonalizacion();
        MostrarInfo("Tu personaje aparece en la foto de las notificaciones de Red Flag. Se aplica al guardar.");
    }

    // Una fila como en el diseño: [ícono] Etiqueta: [<] [miniatura] [>]
    private Control FilaPersonalizacion(string titulo, IconoSistema.Clase iconoIzq, Color colorIcono, Control miniatura, int indice, Action<int> cambiar)
    {
        HBoxContainer fila = new HBoxContainer();
        fila.AddThemeConstantOverride("separation", 16);
        fila.SizeFlagsVertical = SizeFlags.ExpandFill;

        PanelContainer cajaIcono = new PanelContainer { CustomMinimumSize = new Vector2(70, 70), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        cajaIcono.AddThemeStyleboxOverride("panel", Caja(new Color("04141f"), CianOscuro, 1, 4, 10, 10));
        IconoSistema icono = IconoSistema.Crear(iconoIzq, 46, colorIcono);
        if (indice == 3) iconoFilaGenero = icono;
        cajaIcono.AddChild(icono);
        fila.AddChild(cajaIcono);

        Label etiqueta = Etiqueta(titulo, 21, Texto);
        etiqueta.CustomMinimumSize = new Vector2(110, 0);
        etiqueta.HorizontalAlignment = HorizontalAlignment.Center;
        etiqueta.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        fila.AddChild(etiqueta);

        fila.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        Button menos = BotonFlecha("<");
        menos.Pressed += () => { cambiar(-1); RefrescarPersonalizacion(); };
        fila.AddChild(menos);

        PanelContainer cajaMuestra = new PanelContainer { CustomMinimumSize = new Vector2(92, 72), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        cajaMuestra.AddThemeStyleboxOverride("panel", Caja(new Color("04141f"), Cian, 2, 4, 8, 8));
        miniatura.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        miniatura.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        cajaMuestra.AddChild(miniatura);
        nombresOpcion[indice] = null;
        miniaturasCaja[indice] = cajaMuestra;
        fila.AddChild(cajaMuestra);

        Button mas = BotonFlecha(">");
        mas.Pressed += () => { cambiar(1); RefrescarPersonalizacion(); };
        fila.AddChild(mas);

        return fila;
    }

    private void CambiarVista()
    {
        modoVista = modoVista == AvatarJugador.Modo.Cuerpo ? AvatarJugador.Modo.Busto : AvatarJugador.Modo.Cuerpo;
        RefrescarPersonalizacion();
    }

    private void RefrescarPersonalizacion()
    {
        if (avatarVista == null || !IsInstanceValid(avatarVista)) return;

        avatarVista.Vista = modoVista;
        avatarVista.Aplicar(avatarRopa, avatarCabello, avatarPiel, avatarGenero);

        miniRopa.Aplicar(avatarRopa, avatarCabello, avatarPiel, avatarGenero);
        miniCabello.Aplicar(avatarRopa, avatarCabello, avatarPiel, avatarGenero);
        muestraPiel.Color = AvatarJugador.Pieles[avatarPiel];

        IconoSistema.Clase simbolo = avatarGenero == 1 ? IconoSistema.Clase.GeneroF
            : avatarGenero == 2 ? IconoSistema.Clase.GeneroN : IconoSistema.Clase.GeneroM;
        iconoMiniGenero.Tipo = simbolo;
        iconoMiniGenero.QueueRedraw();
        if (iconoFilaGenero != null)
        {
            iconoFilaGenero.Tipo = simbolo;
            iconoFilaGenero.QueueRedraw();
        }

        // El nombre de cada opción se ve al dejar el mouse sobre la miniatura
        string[] nombres =
        {
            AvatarJugador.Ropas[avatarRopa].Nombre,
            AvatarJugador.Peinados[avatarCabello].Nombre,
            AvatarJugador.NombresPiel[avatarPiel],
            AvatarJugador.NombresGenero[avatarGenero]
        };
        for (int i = 0; i < miniaturasCaja.Length; i++)
        {
            if (miniaturasCaja[i] != null && IsInstanceValid(miniaturasCaja[i]))
                miniaturasCaja[i].TooltipText = nombres[i];
        }
    }

    // ------------------------------------------------------------------
    // Guardar / restaurar / volver
    // ------------------------------------------------------------------

    private void Guardar()
    {
        PlayerPreferences.SaveAudio(volumenGeneral, volumenMusica, volumenEfectos);
        PlayerPreferences.SaveDisplay(anchos[resolucion], altos[resolucion], pantallaCompleta);
        PlayerPreferences.SaveGraphics(vsync);
        PlayerPreferences.SaveAccessibility(daltonismo, mostrarHud);
        PlayerPreferences.SaveName(nombreJugador);
        PlayerPreferences.SaveAvatar(avatarRopa, avatarCabello, avatarPiel, avatarGenero);
        PlayerPreferences.ApplyDisplayAndGraphics();
        MostrarInfo("✓ Configuración guardada correctamente.", true);
        filaInfo.Visible = true;
        lineaInfo.Visible = true;
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
        avatarRopa = 0;
        avatarCabello = 0;
        avatarPiel = 0;
        avatarGenero = 0;
        PlayerPreferences.PreviewAudio(volumenGeneral, volumenMusica, volumenEfectos);
        MostrarPestana(pestanaActual);
        MostrarInfo("Valores restaurados. Presioná Guardar para confirmarlos.");
    }

    private void Volver()
    {
        // Si no guardó los volúmenes, restauramos los del disco.
        PlayerPreferences.ApplyAudio();
        CerrarSolicitado?.Invoke();
    }
}
