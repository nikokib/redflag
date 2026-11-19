# Red Flag — actualización de multijugador y configuración

## Incluye en esta versión
- Navegador visual de salas en una tarjeta lateral sobre la escena existente de oficina.
- Crear sala abre una configuración inicial de IP de escucha, puerto, capacidad, duración, dificultad, modo y privacidad.
- IP automática usa `0.0.0.0` para escuchar en todas las interfaces IPv4. La IP personalizada se valida y debe pertenecer al equipo local.
- Búsqueda automática del siguiente puerto UDP libre si el puerto solicitado no se puede abrir.
- Descubrimiento UDP de salas públicas en la red local, actualización manual y filtrado por nombre de sala, IP, nombre/código del anfitrión y nombres/códigos anunciados de jugadores. Las salas privadas no se anuncian.
- Conexión rápida a una sala seleccionada con campo de contraseña, y pantalla separada para conexión manual por IP.
- Sala de espera con la oficina de fondo y botón de ajustes (⚙) para cambiar los valores de la sala; solo el anfitrión puede aplicar cambios o iniciar la partida.
- Nombre de jugador obligatorio para jugar online e identificador persistente de 8 caracteres.
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

## Limitaciones conocidas
- **El navegador automático solo descubre salas en la red local.** El descubrimiento global por Internet requiere un servicio de directorio público desplegado y accesible por todos los clientes; este proyecto no contiene backend, hosting ni credenciales para ese servicio. Las conexiones por IP directa siguen disponibles para Internet si el anfitrión configura el reenvío de puerto UDP y el firewall.
- El descubrimiento LAN usa UDP `24568`; el juego usa el puerto ENet seleccionado (por defecto `24567`, o el siguiente libre). Los firewalls pueden necesitar permitir ambos.
- La sincronización de posiciones es una base funcional, no una implementación final de autoridad física/colisiones. La lógica de investigación, sabotaje, temporizador y victoria/derrota aún debe conectarse al flujo multijugador.
- Los ajustes de controles muestran los controles actuales, pero no incluyen todavía reasignación de teclas desde la interfaz. La personalización estética solo contiene los apartados vacíos pedidos.
- No se pudo compilar ni ejecutar Godot/.NET en este entorno; la validación final requiere abrir el proyecto en Godot 4.7 .NET y hacer la prueba con dos instancias.
