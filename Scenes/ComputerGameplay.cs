using Godot;
using System;

public partial class ComputerGameplay : Control
{
	private Label lblNombre;
	private Label lblDepartamento;
	private Label lblIP;
	private Label lblMAC;

	private Label lblTiempo;
	private Label lblHora;
	private Label lblPuntos;
	private Label lblAutoAceptar;
	private Label lblConexiones;

	private Random rnd = new Random();
	private Conexion conexionActual;

	private class Conexion
{
	public string Nombre { get; set; }
	public string Departamento { get; set; }
	public string IP { get; set; }
	public string MAC { get; set; }
	public bool Infectada { get; set; }
}

	// Tiempo de la partida (2:45 = 165 segundos)
	private double tiempoPartida = 165;

	// Cuenta regresiva para auto aceptar
	private double autoAceptar = 15;

	// Datos inferiores
	private int puntos = 0;
	private int conexionesHoy=0;

	// Listas de datos
	private string[] nombres =
	{
		"Ana Lopez",
		"Juan Perez",
		"Maria Gomez",
		"Lucas Diaz",
		"Sofia Torres",
		"Pedro Ruiz",
		"Lucia Fernandez",
		"Martin Gomez",
		"Camila Alvarez",
		"Agustin Romero"
	};

	private string[] departamentos =
	{
		"Finanzas",
		"RRHH",
		"Marketing",
		"IT",
		"Ventas",
		"Soporte",
		"Seguridad",
		"Administracion"
	};

	private string[] nombresInfectados =
	{
		"VOID",
		"null_user",
		"adm1n",
		"syst3m",
		"ERROR404"
	};

	private string[] ipsInfectadas =
	{
		"10.66.6.66",
		"192.168.0.131313",
		"192.168.1.666",
		"0.0.0.0",
		"255.255.255.255"
	};

	public override void _Ready()
	{
		lblNombre = GetNode<Label>("Nombre");
		lblDepartamento = GetNode<Label>("Departamento");
		lblIP = GetNode<Label>("IP");
		lblMAC = GetNode<Label>("MAC");

		lblTiempo = GetNode<Label>("TiempoRestante");
		lblHora = GetNode<Label>("Hora");
		lblPuntos = GetNode<Label>("Puntos");
		lblAutoAceptar = GetNode<Label>("AutoAceptar");
		lblConexiones = GetNode<Label>("Conexiones");

		TextureButton btnAceptar = GetNode<TextureButton>("BtnAceptar");
		TextureButton btnRechazar = GetNode<TextureButton>("BtnRechazar");

		btnAceptar.Pressed += OnAceptarPressed;
		btnRechazar.Pressed += OnRechazarPressed;

		InicializarPantalla();
	}

	public override void _Process(double delta)
	{
		ActualizarHora();

		ActualizarTiempoPartida(delta);

		ActualizarAutoAceptar(delta);
	}


	private void InicializarPantalla()
	{
		puntos = 0;
		conexionesHoy = 0;

		lblPuntos.Text = puntos.ToString();
		lblConexiones.Text = conexionesHoy.ToString();

		GenerarSolicitud();
	}

	private void ActualizarTiempoPartida(double delta)
	{
		if (tiempoPartida > 0)
		{
			tiempoPartida -= delta;
		}

		TimeSpan tiempo = TimeSpan.FromSeconds(Math.Max(0, tiempoPartida));

		lblTiempo.Text = tiempo.ToString(@"mm\:ss");
	}

	private void ActualizarHora()
	{
		lblHora.Text = DateTime.Now.ToString("HH:mm:ss");
	}

	private void ActualizarAutoAceptar(double delta)
	{
		autoAceptar -= delta;

		lblAutoAceptar.Text = Math.Ceiling(autoAceptar).ToString("00");

		if (autoAceptar <= 0)
		{
			GenerarSolicitud();

			autoAceptar = 15;
		}
	}

	private void GenerarSolicitud()
{
	conexionActual = new Conexion();

	conexionActual.Infectada = rnd.Next(100) < 20;

	if (conexionActual.Infectada)
	{
		conexionActual.Nombre = nombresInfectados[rnd.Next(nombresInfectados.Length)];
		conexionActual.IP = ipsInfectadas[rnd.Next(ipsInfectadas.Length)];
	}
	else
	{
		conexionActual.Nombre = nombres[rnd.Next(nombres.Length)];
		conexionActual.IP = GenerarIP();
	}

	conexionActual.Departamento = departamentos[rnd.Next(departamentos.Length)];
	conexionActual.MAC = GenerarMAC();

	MostrarConexion();
	AgregarConexion();
}

private void MostrarConexion()
{
	lblNombre.Text = conexionActual.Nombre;
	lblDepartamento.Text = conexionActual.Departamento;
	lblIP.Text = conexionActual.IP;
	lblMAC.Text = conexionActual.MAC;
}

public bool ConexionEsInfectada()
{
	return conexionActual.Infectada;
}
	private string GenerarIP()
	{
		return $"192.168.{rnd.Next(0, 256)}.{rnd.Next(1, 255)}";
	}

	private string GenerarMAC()
	{
		return $"{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}:{rnd.Next(0, 256):X2}";
	}

	private void ActualizarPuntos()
{
	lblPuntos.Text = puntos.ToString();
}

	public void SumarPuntos(int cantidad)
	{
		puntos += cantidad;

		if (puntos < 0)
			puntos = 0;

		ActualizarPuntos();
	}

	public void AgregarConexion()
	{
		conexionesHoy++;
		lblConexiones.Text = conexionesHoy.ToString();
	}

	public void Aceptar()
	{
		if (ConexionEsInfectada())
		{
			// Aceptó un virus
			SumarPuntos(-20);
		}
		else
		{
			SumarPuntos(10);
		}
	}

	public void Rechazar()
	{
		if (ConexionEsInfectada())
		{
			SumarPuntos(20);
		}
		else
		{
			SumarPuntos(-10);
		}
	}

	private void OnAceptarPressed()
	{
		Aceptar();
		GenerarSolicitud();
		autoAceptar = 15;
	}

	private void OnRechazarPressed()
	{
		Rechazar();
		GenerarSolicitud();
		autoAceptar = 15;
	}
}
