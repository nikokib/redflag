# RED FLAG – Tres eventos de emergencia (single player)

## Cómo abrirlo

1. Descomprimí este ZIP.
2. Usá **Godot 4 .NET** compatible con el proyecto (el `.csproj` declara Godot.NET.Sdk **4.7.0**), con SDK de **.NET 8** instalado.
3. En el Administrador de proyectos de Godot, elegí **Importar** y seleccioná `project.godot`.
4. Esperá la importación, compilá el proyecto C# y ejecutalo con F5.
5. Entrá a **Singleplayer**, acercate a la silla y empezá un turno.

## Qué hace el código

- El primer correo aparece a los **18 segundos**; los siguientes entre **20 y 30 segundos** después del anterior.
- Cada ronda contiene los 3 tipos en **orden aleatorio, sin repetir** hasta completar la ronda.
- El correo se abre **automáticamente** y aparece un aviso flotante arriba a la izquierda.
- No detiene el reloj de las solicitudes: el objetivo es crear presión.

### 1. Autorización masiva (10 segundos)
- Resolver `(a × b) − c` evita el desastre y suma **25 puntos**.
- Respuesta errónea o tiempo agotado: hasta **5 solicitudes autoaceptadas**, con registros reales y límites normales de derrota. En Normal se pierde al llegar a **3**, en Difícil **2**, en Fácil **5**.

### 2. Código de antivirus (10 segundos)
- Resolver `a + b` dispara el análisis y resolución correcta de **5 solicitudes** (incluida la actual); los resultados se registran y se ven en la app Antivirus.
- Respuesta errónea o tiempo agotado: **−15 puntos** y acorta el tiempo de la solicitud actual.

### 3. Phishing (8 segundos)
- Hay mensajes falsos y alertas legítimas. Se muestra el dominio del remitente:
  - Oficial `securenet.local` → **VERIFICAR EN IT**.
  - Sospechoso `securenet-soporte.com` → **REPORTAR PHISHING**.
- Acertar: **+20 puntos**, y el firewall bloquea automáticamente las **siguientes 3 conexiones infectadas** (no solicitudes limpias).
- Responder incorrectamente: se invierten las próximas **3 decisiones** de Aceptar/Rechazar. Se muestra aviso rojo permanente mientras dure el efecto.
- Si vence el tiempo: **−10 puntos**, sin inversión.

## Archivos que se tocaron

- `Scenes/ComputerGameplay.cs`: programación de eventos, reglas, premios, castigos, timers y puntuación.
- `Scripts/DatosEscritorio.cs`: modelos de correos, estado y eventos de actualización de UI.
- `Scripts/AppsEscritorio.cs`: bandeja de correo con respuestas reales, código y decisiones; resultado del antivirus, estado del firewall e historial de emergencias en Logs.
- `Scripts/EscritorioUI.cs`: deja de usar una captura fija para la app Correo Interno y pasa a usar su ventana interactiva.
- `Scripts/AvisoEmergencia.cs` (**nuevo**): aviso flotante, temporizador y alarma de botones invertidos.

## Visuales para personalizar luego

Ya funcionan sin imágenes adicionales; sus paneles se dibujan con nodos Control / Label / Button / PanelContainer y la paleta `RedFlagUI`.

Si querés mejorarlos en Godot, podés crear y asignar:

1. **Sobre rojo intermitente** para la bandeja de correo o para la barra de tareas.
2. **Icono de escudo verde** para el antivirus y el firewall protegido.
3. **Triángulo de advertencia** y una **textura glitch** para el modo infectado.
4. **Sonidos** breves de correo, éxito, error y alarma (mantener volúmenes moderados).

La imagen antigua `Interfaz/ventana_correo.png` se conserva, pero ya no se usa como ventana porque impedía responder.

## Pruebas rápidas

- Entrá en Singleplayer → 18 s → debe abrirse Correo con evento urgente.
- Para un correo matemático, escribí la operación y presioná RESPONDER o Enter.
- Para phishing, probá REPORTAR / VERIFICAR EN IT, y luego elegí mal para ver la inversión.
- Para antivirus, resolvé la suma y consultá los registros de Red Flag y la app Antivirus.
- Dejá vencer un correo matemático en dificultad Normal y verificá que se termina la partida por autoaceptaciones.

**Importante:** la computadora de generación no incluye Godot ni `dotnet`, por lo que el proyecto fue editado y verificado estáticamente, pero **no se pudo compilar ni ejecutar aquí**. Es necesario compilarlo en Godot .NET en tu equipo para detectar posibles diferencias de API o configuración.
