# Scenario Studio — depura y edita escenarios con el juego en marcha

Scenario Studio es un panel acoplado junto al juego en marcha que muestra qué escenario se está reproduciendo y qué línea hay en pantalla. Puede pausar el diálogo, avanzar línea a línea, detenerse en puntos de ruptura, mover la siguiente línea, reiniciar un escenario con el estado del juego revertido y cambiar el escenario al vuelo — y enseguida muestra el cambio en el juego.

Tiene dos modos: **Principiante** muestra el momento de la historia en bloques sencillos; **Autor** muestra el texto del escenario con números de línea.

## Cómo abrirlo

Un juego que incluye el conjunto Developer Tools (la mecánica *Scenario Studio*) tiene un botón **Estudio** — la pluma — en su barra de herramientas. La opción de arranque `--studio` abre el panel nada más empezar. Las capturas de abajo son de la cocina de Cthangover; las ediciones se hicieron en una capa de prueba, nunca en los archivos del propio juego.

## Modo Principiante

![La cocina con el panel Estudio a la derecha en modo Principiante: escena home_kitchen, escenario home_kitchen_cooking, una insignia de capa; bajo «En pantalla ahora» la línea «Nueva línea» y la pregunta «Una habitación acondicionada como cocina»; las respuestas Salir (va a la escena home_vestibule), Cocinar (acción toggle_cooking_workbench) y una respuesta nueva, cada una con «Elige adónde» y Editar; los botones «+ Línea antes de la pregunta» y «+ Respuesta»; la caja «Este cambio afecta a» con la capa de destino, y Aplicar abajo.](assets/studio-novice.jpg)

La pestaña **Ahora** muestra el momento en pantalla: la línea, la pregunta y cada respuesta con adónde lleva. **Editar** cambia un texto, **Elige adónde** cambia a dónde conduce una respuesta (una etiqueta, otra escena, el final), **+ Línea** y **+ Respuesta** añaden al momento. Cada cambio muestra en **Este cambio afecta a** en qué mod o capa se escribirá.

## Modo Autor, pausa y puntos de ruptura

![El panel en modo Autor: Pausa, Paso, Continuar, Recursos y Reiniciar en la barra de transporte; el texto del escenario de home_kitchen con la línea 6 — el select «Una habitación acondicionada como cocina» — marcada con un punto rojo de ruptura y el marcador ▶ de la línea actual; debajo, «tras la línea 9» con + Línea, + Elección, + Ir a escena, y la capa de destino my_studio.](assets/dock-breakpoint.jpg)

- **Pausa** detiene el diálogo antes de su siguiente línea; **Paso** ejecuta una línea; **Continuar** sigue.
- Un clic en el margen pone un **punto de ruptura**: el diálogo se detiene antes de esa línea.
- Arrastrar el marcador **▶** a otra línea hace que esa línea sea la siguiente en ejecutarse.
- **+ Línea**, **+ Elección**, **+ Ir a escena** añaden tras la línea actual; **Editar texto** edita el escenario entero.

## Aplicar y reiniciar

**Aplicar** escribe los cambios y reinicia el escenario. Aquí se añadió una línea «Nueva línea» antes de la pregunta; tras aplicar, ya está en pantalla:

![El panel tras aplicar: en pantalla ahora «Nueva línea», el estado «Aplicado a «my_studio», escenario reiniciado con reversión».](assets/dock-applied.jpg)

**Reiniciar** ofrece: el escenario de nuevo sin reversión, el escenario de nuevo **revirtiendo el estado** (las marcas, los objetos, el reloj y el resto vuelven a como estaban cuando empezó el escenario), la escena de nuevo, o continuar desde la etiqueta actual.

## Inspeccionar el estado

![La pestaña Estado: «Los valores se pueden cambiar para probar otra rama», el reloj con día 24 y hora 23:00.](assets/dock-state.jpg)

La pestaña **Estado** muestra el estado del juego agrupado por dueño: el reloj, las variables del diálogo en curso, las misiones a las que remite el escenario, el inventario y qué escenarios ya se ejecutaron. Variables, cantidades de objetos y estado de misiones se pueden cambiar (Intro los escribe en el juego) para probar otra rama. **Escena** lista los escenarios de la escena actual, **Mapa** muestra las escenas y los caminos entre ellas (y dónde soltar fondos nuevos), y **Registro** apunta lo que ocurrió:

![La pestaña Registro: entrada en home_kitchen, home_kitchen_cooking empezado, la edición «+6 → my_studio», reinicios, «punto de ruptura, línea 6», «siguiente línea movida a 5», «punto de control en home_kitchen».](assets/dock-log.jpg)

## Dónde se escriben los cambios

- Un mod instalado como carpeta se puede cambiar en el sitio.
- Si no, los cambios van a la **capa de estudio** del juego (`user://studio/<game>`), que gana a todos los mods y los deja intactos. La capa se puede exportar como un mod nuevo, que luego se añade a la colección del juego.
- **Recursos** vuelve a leer los fondos soltados en un mod o en la carpeta `backgrounds/` de la capa mientras el juego corre; sobre ese fondo se puede crear después una escena nueva desde la pestaña Mapa.

## Si algo sale mal

1. **No hay botón de Estudio.** La colección del juego no incluye el estudio; añade el conjunto Developer Tools o arranca con `--studio`.
2. **Paso no hace nada.** El diálogo espera una respuesta: elige una primero.
3. **Otros jugadores no ven un cambio.** Los cambios de la capa de estudio se quedan en este juego en este ordenador; exporta la capa como mod para compartirlos.
