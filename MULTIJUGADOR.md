# Red Flag — actualización de multijugador y configuración

## Incluye en esta versión
- Navegador visual de salas en una tarjeta lateral sobre la escena existente de oficina.
- Crear sala abre una configuración inicial de IP de escucha, puerto, capacidad, duración, dificultad, modo y privacidad.
- IP automática usa `0.0.0.0` para escuchar en todas las interfaces IPv4. La IP personalizada se valida y debe pertenecer al equipo local.
- Búsqueda automática del siguiente puerto UDP libre si el puerto solicitado no se puede abrir.
- Descubrimiento UDP de salas públicas en la red local, actualización manual y filtrado por nombre de sala, IP, nombre/código del anfitrión y nombres/códigos anunciados de jugadores. Las salas privadas no se anuncian.
- Conexión rápida a una sala seleccionada con campo de contraseña, y pantalla separada para conexión manual por IP.
- Sala de espera con la oficina de fondo y botón de ajustes (⚙) para cambiar los valores de la sala; solo el anfitrión puede aplicar cambios o iniciar la partida.
- Nombre de jugador obligatorio para jugar online e identificador persistente de 8 caracteres. El anfitrión rechaza un identificador que ya esté registrado en la misma sala, incluso si llega desde otra instancia o dispositivo; los reintentos desde la misma conexión son idempotentes.
- Revelado del rol propio y posterior entrada a la escena de oficina existente, con etiquetas de nombre/código sobre los jugadores y sincronización básica de posiciones.
- Configuración de Pantalla, Sonido, Gráficos y Personalización. Resolución, ventana completa, VSync, volúmenes Music/SFX, accesibilidad de color y visibilidad del HUD se guardan localmente.
- Apartados de Peinados, Trajes, Ojos y Piel creados como espacios vacíos, tal como se solicitó.

## Prueba local recomendada
1. Abre el proyecto con Godot 4.7 .NET y espera a que compile los scripts C#.
2. En el menú principal, entra en **Configuración → Personalización**, establece un nombre y guarda.
3. Entra en **Multijugador → Crear sala**. Selecciona Automática para la prueba inicial. El juego escucha en todas las interfaces y busca un puerto libre empezando en `24567`.
4. En otra instancia del juego, abre Multijugador y pulsa **Actualizar salas**. En la misma red local debería aparecer la sala pública. También puedes entrar con IP `127.0.0.1` si ambas instancias corren en el mismo equipo.
5. Desde la sala, prueba el botón ⚙, cambia el modo/duración/capacidad y aplica.
6. Inicia la partida desde el anfitrión, comprueba el rol privado y pulsa **Entrar a la oficina**.

### Comprobar que un perfil no puede entrar dos veces

1. Abre dos instancias del juego usando el mismo perfil local (por ejemplo, dos ejecuciones en el mismo equipo y usuario de Windows).
2. Desde la primera instancia, crea una sala.
3. Desde la segunda, intenta entrar con el mismo perfil.
4. El servidor debe rechazar el segundo ingreso con el aviso de que el perfil ya está conectado. La lista y el contador de jugadores deben conservar una sola entrada para ese identificador.

La comprobación usa el identificador persistente del perfil, no el nombre visible. Dos perfiles diferentes pueden tener el mismo nombre y entrar normalmente; dos instalaciones distintas solo se consideran el mismo perfil si comparten el mismo identificador.

## Prueba entre dos redes diferentes (Internet)

El juego utiliza ENet sobre UDP. Para que un cliente que está fuera de la red del anfitrión pueda alcanzar la sala, configura el router del anfitrión:

1. En el anfitrión, crea la sala con **IP de escucha: Automática** y deja un puerto fijo, por ejemplo `24567`. No cambies el puerto mientras haces la prueba.
2. En Windows, ejecuta `ipconfig` y anota la dirección IPv4 local del equipo anfitrión (por ejemplo, `192.168.1.50`). Conviene reservar esa dirección en el router para que no cambie.
3. En el router, crea una regla de **reenvío de puerto UDP**: puerto externo `24567` → IP local del anfitrión `192.168.1.50`, puerto interno `24567`. No alcanza con abrir TCP: ENet usa UDP.
4. En el Firewall de Windows, permite el tráfico entrante UDP por el puerto seleccionado, o permite la aplicación del juego en redes privadas/públicas según corresponda.
5. Averigua la IP pública del anfitrión y compártela junto con el puerto. El otro jugador debe usar **Unirse a sala por IP**, introducir esa IP pública y el mismo puerto UDP. Si la sala es privada, también necesita la contraseña.
6. Haz la prueba desde una conexión realmente diferente (por ejemplo, el segundo jugador conectado a un hotspot móvil). Probar la IP pública desde la misma Wi-Fi puede fallar si el router no soporta NAT loopback.

**Importante:** si la IP WAN que muestra el router no coincide con la IP pública que ves en Internet, puede haber otra capa de NAT. Si la WAN está dentro de `100.64.0.0/10`, es un indicio de CGNAT. En esos casos el reenvío de puertos del router doméstico no será suficiente: el anfitrión tendrá que pedir al proveedor una IPv4 pública, usar una solución de red privada virtual compatible o desplegar un servidor/relay accesible desde Internet. El navegador de salas no anuncia salas a través de Internet: para esa función haría falta un directorio público.

## Limitaciones conocidas
- **El navegador automático solo descubre salas en la red local.** El descubrimiento global por Internet requiere un servicio de directorio público desplegado y accesible por todos los clientes; este proyecto no contiene backend, hosting ni credenciales para ese servicio. Las conexiones por IP directa pueden funcionar entre redes distintas si el anfitrión configura el reenvío del puerto UDP ENet y el firewall, y su proveedor no lo bloquea mediante CGNAT.
- El descubrimiento LAN usa UDP `24568`; el juego usa el puerto ENet seleccionado (por defecto `24567`, o el siguiente libre si el solicitado está ocupado). Para Internet, el puerto que se reenvía en el router debe coincidir con el puerto que muestra la sala. Los firewalls pueden necesitar permitir ambos.
- La sincronización de posiciones es una base funcional, no una implementación final de autoridad física/colisiones. La lógica de investigación, sabotaje, temporizador y victoria/derrota aún debe conectarse al flujo multijugador.
- Los ajustes de controles muestran los controles actuales, pero no incluyen todavía reasignación de teclas desde la interfaz. La personalización estética solo contiene los apartados vacíos pedidos.
- No se pudo compilar ni ejecutar Godot/.NET en este entorno; la validación final requiere abrir el proyecto en Godot 4.7 .NET y hacer la prueba con dos instancias.
