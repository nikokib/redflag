using System.Collections.Generic;

/// <summary>
/// Estado que comparte el anfitrión en Salva la red.
/// No contiene el dato secreto de si la solicitud actual está infectada:
/// esa decisión siempre se valida en la computadora del anfitrión.
/// </summary>
public class EstadoCooperativo
{
    public int NumeroSolicitud;
    public int NumeroCorreo;
    public int VersionListas;
    public SolicitudActual Solicitud = new SolicitudActual();
    public CorreoEmergencia Correo;
    public double Tiempo;
    public double AutoRestante;
    public double AutoMaximo;
    public int Puntos;
    public int Conexiones;
    public int ErroresAceptar;
    public int ErroresRechazar;
    public int AutoAceptadas;
    public int MaxErroresAceptar;
    public int MaxErroresRechazar;
    public int MaxAutoAceptadas;
    public int Inversiones;
    public int Protecciones;
    public int CorreosCorrectos;
    public int CorreosFallidos;
    public string EstadoAntivirus = "";
    public string Aviso = "";
    public double DuracionAviso;
    public List<RegistroConexion> Historial = new List<RegistroConexion>();
    public List<EntradaLista> Blancas = new List<EntradaLista>();
    public List<EntradaLista> Negras = new List<EntradaLista>();
    public List<string> HistorialEmergencias = new List<string>();
    public bool Terminada;
    public bool Victoria;
    public string MotivoFinal = "";
    public string ResumenFinal = "";
}
