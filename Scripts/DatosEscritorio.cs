using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Una decisión tomada sobre una solicitud de conexión.</summary>
public class RegistroConexion
{
	public DateTime Hora;
	public string Nombre;
	public string IP;
	public string MAC;
	public string Area;
	public string Rol;
	public string Decision; // "ACEPTADA", "RECHAZADA" o "AUTOACEPTADA"
	public bool Infectada;
}

/// <summary>Una entrada de la whitelist o de la blacklist.</summary>
public class EntradaLista
{
	public string Nombre;
	public string IP;
	public string MAC;
	public string Area;
}

/// <summary>La solicitud de conexión que está esperando respuesta.</summary>
public class SolicitudActual
{
	public bool Hay;
	public string Nombre = "";
	public string Area = "";
	public string Rol = "";
	public string IP = "";
	public string MAC = "";
}

/// <summary>Configuración de red que se edita en la sección "Red" (solo de interfaz).</summary>
public class ConfigRed
{
	public static readonly string[] Modos = { "Cableada", "Inalámbrica", "VPN" };
	public static readonly string[] Tipos = { "LAN", "WAN", "DMZ" };
	public static readonly string[] Seguridades = { "WPA3", "WPA2", "Abierta" };

	public int Modo = 0;
	public int Tipo = 0;
	public string Puerto = "443";
	public int Seguridad = 0;

	public void Restablecer()
	{
		Modo = 0;
		Tipo = 0;
		Puerto = "443";
		Seguridad = 0;
	}
}

/// <summary>
/// Información compartida entre el escritorio y sus apps (logs, incidentes, notas, chat,
/// whitelist/blacklist y estado en vivo de la partida).
/// No contiene reglas del juego: ComputerGameplay escribe acá lo que pasa y la interfaz lo lee.
/// </summary>
public class DatosEscritorio
{
	public readonly List<RegistroConexion> Historial = new List<RegistroConexion>();
	public readonly List<string> Chat = new List<string>();
	public string TextoNotas = "";

	// --- Listas de la app Red Flag ---
	public readonly List<EntradaLista> Whitelist = new List<EntradaLista>();
	public readonly List<EntradaLista> Blacklist = new List<EntradaLista>();
	public readonly ConfigRed Red = new ConfigRed();

	// --- Estado en vivo (lo actualiza ComputerGameplay, lo lee la ventana Red Flag) ---
	public readonly SolicitudActual Solicitud = new SolicitudActual();
	public double TiempoTurno;            // segundos que se muestran en "TIEMPO RESTANTE"
	public double AutoAceptarRestante;    // segundos para que la solicitud se autoacepte
	public int Puntos;
	public int ConexionesHoy;
	public int ErroresAceptar, MaxErroresAceptar;
	public int ErroresRechazar, MaxErroresRechazar;
	public int AutoAceptadas, MaxAutoAceptadas;

	/// <summary>Se dispara cada vez que se agrega un registro.</summary>
	public event Action Cambio;

	/// <summary>Se dispara cuando cambia la whitelist o la blacklist.</summary>
	public event Action ListasCambio;

	public void Registrar(RegistroConexion registro)
	{
		Historial.Add(registro);
		Cambio?.Invoke();
	}

	/// <summary>Agrega la solicitud actual a una lista. Devuelve false si ya estaba en alguna de las dos.</summary>
	public bool AgregarALista(bool blanca)
	{
		if (!Solicitud.Hay) return false;

		bool repetida = Whitelist.Any(e => e.IP == Solicitud.IP) || Blacklist.Any(e => e.IP == Solicitud.IP);
		if (repetida) return false;

		EntradaLista e = new EntradaLista
		{
			Nombre = Solicitud.Nombre,
			IP = Solicitud.IP,
			MAC = Solicitud.MAC,
			Area = Solicitud.Area
		};

		(blanca ? Whitelist : Blacklist).Add(e);
		ListasCambio?.Invoke();
		return true;
	}

	/// <summary>
	/// Clasifica una conexión ya resuelta de forma automática: las limpias van a la
	/// whitelist y los virus (infectadas) a la blacklist. Si ya estaba en alguna lista no se repite.
	/// </summary>
	public void AgregarAutomatico(RegistroConexion registro)
	{
		bool repetida = Whitelist.Concat(Blacklist).Any(x => x.IP == registro.IP && x.Nombre == registro.Nombre);
		if (repetida) return;

		EntradaLista e = new EntradaLista
		{
			Nombre = registro.Nombre,
			IP = registro.IP,
			MAC = registro.MAC,
			Area = registro.Area
		};

		(registro.Infectada ? Blacklist : Whitelist).Add(e);
		ListasCambio?.Invoke();
	}

	public void QuitarDeLista(bool blanca, EntradaLista entrada)
	{
		(blanca ? Whitelist : Blacklist).Remove(entrada);
		ListasCambio?.Invoke();
	}

	/// <summary>Infectadas que entraron a la red (aceptadas o autoaceptadas).</summary>
	public int VirusAceptados => Historial.Count(r => r.Infectada && r.Decision != "RECHAZADA");

	/// <summary>Infectadas que el jugador rechazó.</summary>
	public int AmenazasBloqueadas => Historial.Count(r => r.Infectada && r.Decision == "RECHAZADA");
}
