using Godot;
using System;

/// <summary>Notificación flotante de correos urgentes y efectos del firewall.</summary>
public partial class AvisoEmergencia : Control
{
    public Action AlAbrirCorreo;

    private PanelContainer panel;
    private Label titulo;
    private Label segundos;
    private Label estado;
    private Button abrir;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(315, 0),
            Position = new Vector2(14, 55),
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false
        };
        panel.AddThemeStyleboxOverride("panel", RedFlagUI.Caja(new Color("180a12"), RedFlagUI.Rojo, 2, 4, 10));
        AddChild(panel);

        VBoxContainer caja = new VBoxContainer();
        caja.AddThemeConstantOverride("separation", 5);
        panel.AddChild(caja);
        titulo = RedFlagUI.CrearLabel("NUEVO CORREO URGENTE", 14, RedFlagUI.Rojo);
        segundos = RedFlagUI.CrearLabel("", 14, Colors.White);
        estado = RedFlagUI.CrearLabel("", 12, RedFlagUI.Texto);
        estado.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        caja.AddChild(titulo);
        caja.AddChild(segundos);
        caja.AddChild(estado);
        abrir = RedFlagUI.CrearBoton("ABRIR CORREO", new Vector2(0, 34), true);
        abrir.AddThemeFontSizeOverride("font_size", 13);
        abrir.Pressed += () => AlAbrirCorreo?.Invoke();
        caja.AddChild(abrir);
    }

    public void Actualizar(CorreoEmergencia correo, int inversiones, int protecciones, double restoAviso, string mensaje)
    {
        bool pendiente = correo != null && correo.Pendiente;
        bool hayEstado = inversiones > 0 || protecciones > 0;
        panel.Visible = pendiente || restoAviso > 0 || hayEstado;
        if (!panel.Visible) return;

        if (pendiente)
        {
            titulo.Text = "!! NUEVO CORREO DE EMERGENCIA !!";
            segundos.Text = "Quedan " + Math.Ceiling(correo.TiempoRestante) + " segundos";
            estado.Text = correo.Asunto;
            abrir.Visible = true;
        }
        else if (inversiones > 0)
        {
            titulo.Text = "SISTEMA INFECTADO";
            segundos.Text = "BOTONES INVERTIDOS: " + inversiones + " decisión(es)";
            estado.Text = "Aceptar actúa como rechazar y viceversa.";
            abrir.Visible = false;
        }
        else if (protecciones > 0)
        {
            titulo.Text = "FIREWALL PROTEGIDO";
            segundos.Text = "Bloqueos disponibles: " + protecciones;
            estado.Text = mensaje;
            abrir.Visible = false;
        }
        else
        {
            titulo.Text = "CORREO PROCESADO";
            segundos.Text = mensaje;
            estado.Text = "Podés continuar revisando conexiones.";
            abrir.Visible = false;
        }
    }

    public void Ocultar() => panel.Visible = false;
}
