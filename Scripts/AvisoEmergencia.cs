using Godot;
using System;

/// <summary>
/// Notificación flotante de correos urgentes y efectos del firewall.
/// Diseño neón: marco rojo/cian, "NUEVO CORREO DE EMERGENCIA", cuenta regresiva,
/// mensaje de urgencia y botón ABRIR CORREO. Al abrir el correo, el aviso desaparece.
/// </summary>
public partial class AvisoEmergencia : Control
{
    public Action AlAbrirCorreo;

    private MarcoNeon panel;
    private Label titulo;
    private Label segundos;
    private RichTextLabel urgente;
    private Control separador;
    private Button abrir;

    // Correo cuyo aviso ya se cerró porque el jugador abrió el correo
    private CorreoEmergencia correoAbierto;
    private CorreoEmergencia ultimoCorreo;

    private static readonly Color Rojo = new Color("ff2e3f");
    private static readonly Color Cian = new Color("39b7cc");
    private static readonly Color Claro = new Color("cfe7ee");

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        panel = new MarcoNeon
        {
            CustomMinimumSize = new Vector2(440, 0),
            Position = new Vector2(14, 55),
            Margen = 14,
            Chaflan = 12f,
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false
        };
        AddChild(panel);

        VBoxContainer caja = new VBoxContainer();
        caja.AddThemeConstantOverride("separation", 6);
        panel.AddChild(caja);

        HBoxContainer cabecera = new HBoxContainer();
        cabecera.AddThemeConstantOverride("separation", 12);
        cabecera.AddChild(IconoSistema.Crear(IconoSistema.Clase.Advertencia, 30, Rojo));
        titulo = RedFlagUI.CrearLabel("!! NUEVO CORREO DE EMERGENCIA !!", 19, Rojo);
        titulo.VerticalAlignment = VerticalAlignment.Center;
        cabecera.AddChild(titulo);
        caja.AddChild(cabecera);

        segundos = RedFlagUI.CrearLabel("", 18, Claro);
        caja.AddChild(segundos);

        urgente = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        urgente.AddThemeFontOverride("normal_font", RedFlagUI.Fuente);
        urgente.AddThemeFontOverride("bold_font", RedFlagUI.Fuente);
        urgente.AddThemeFontSizeOverride("normal_font_size", 17);
        urgente.AddThemeFontSizeOverride("bold_font_size", 17);
        urgente.AddThemeColorOverride("default_color", Claro);
        caja.AddChild(urgente);

        separador = new ColorRect { Color = new Color("5a7f8c"), CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore };
        caja.AddChild(separador);

        abrir = new Button
        {
            Text = "ABRIR CORREO",
            CustomMinimumSize = new Vector2(0, 50),
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        abrir.AddThemeStyleboxOverride("normal", Caja(new Color("030a10"), Cian));
        abrir.AddThemeStyleboxOverride("hover", Caja(new Color("082636"), Colors.White));
        abrir.AddThemeStyleboxOverride("pressed", Caja(new Color("0b3446"), Colors.White));
        abrir.AddThemeStyleboxOverride("focus", Caja(new Color(0, 0, 0, 0), Cian));
        abrir.AddThemeFontOverride("font", RedFlagUI.Fuente);
        abrir.AddThemeFontSizeOverride("font_size", 20);
        abrir.AddThemeColorOverride("font_color", new Color("9fdcea"));
        abrir.AddThemeColorOverride("font_hover_color", Colors.White);
        abrir.Pressed += () => AlAbrirCorreo?.Invoke();
        caja.AddChild(abrir);
    }

    private static StyleBoxFlat Caja(Color fondo, Color borde)
    {
        StyleBoxFlat sb = new StyleBoxFlat { BgColor = fondo, BorderColor = borde };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(8);
        sb.ContentMarginLeft = 10;
        sb.ContentMarginRight = 10;
        sb.ContentMarginTop = 4;
        sb.ContentMarginBottom = 4;
        return sb;
    }

    // Escribe "URGENTE:" y, resaltado, el mensaje de urgencia del correo.
    private void MostrarUrgente(string mensaje)
    {
        string limpio = (mensaje ?? "").Trim();
        if (limpio.StartsWith("URGENTE:", StringComparison.OrdinalIgnoreCase))
            limpio = limpio.Substring(8).Trim();
        urgente.Text = "URGENTE: [color=#ffd166][b]" + limpio.Replace("[", "(").Replace("]", ")") + "[/b][/color]";
    }

    private void MostrarTexto(string mensaje)
    {
        urgente.Text = (mensaje ?? "").Replace("[", "(").Replace("]", ")");
    }

    /// <summary>El jugador abrió el correo: el aviso del correo pendiente se retira.</summary>
    public void OcultarPendiente()
    {
        correoAbierto = ultimoCorreo;
        if (panel != null && ultimoCorreo != null && ultimoCorreo.Pendiente) panel.Visible = false;
    }

    public void Actualizar(CorreoEmergencia correo, int inversiones, int protecciones, double restoAviso, string mensaje)
    {
        ultimoCorreo = correo;
        bool pendiente = correo != null && correo.Pendiente;
        bool hayEstado = inversiones > 0 || protecciones > 0;

        // El correo ya se abrió: no se muestra más su aviso mientras siga pendiente
        if (pendiente && correo == correoAbierto && !hayEstado)
        {
            panel.Visible = false;
            return;
        }

        panel.Visible = pendiente || restoAviso > 0 || hayEstado;
        if (!panel.Visible) return;

        bool mostrarPendiente = pendiente && correo != correoAbierto;

        if (mostrarPendiente)
        {
            titulo.Text = "!! NUEVO CORREO DE EMERGENCIA !!";
            segundos.Text = "Quedan " + Math.Ceiling(correo.TiempoRestante) + " segundos";
            MostrarUrgente(correo.Asunto);
            separador.Visible = true;
            abrir.Visible = true;
        }
        else if (inversiones > 0)
        {
            titulo.Text = "SISTEMA INFECTADO";
            segundos.Text = "BOTONES INVERTIDOS: " + inversiones + " decisión(es)";
            MostrarTexto("Aceptar actúa como rechazar y viceversa.");
            separador.Visible = false;
            abrir.Visible = false;
        }
        else if (protecciones > 0)
        {
            titulo.Text = "FIREWALL PROTEGIDO";
            segundos.Text = "Bloqueos disponibles: " + protecciones;
            MostrarTexto(mensaje);
            separador.Visible = false;
            abrir.Visible = false;
        }
        else
        {
            titulo.Text = "CORREO PROCESADO";
            segundos.Text = mensaje;
            MostrarTexto("Podés continuar revisando conexiones.");
            separador.Visible = false;
            abrir.Visible = false;
        }
    }

    public void Ocultar() => panel.Visible = false;
}
