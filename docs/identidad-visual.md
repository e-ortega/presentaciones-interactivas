# Identidad visual: "Hoja de respuestas"

_Propuesta de Claude con la skill frontend-design, 9 oct 2026. Pendiente de aprobación de Esteban (P-08). Tokens en [`design/tokens.css`](../design/tokens.css). Maquetas en el canvas "Identidad visual — Presentaciones Interactivas" (artifact de Claude)._

## Brief

- **Qué es:** clases en vivo a partir del PowerPoint del profesor.
- **Para quién:** profesores universitarios en Costa Rica y sus estudiantes.
- **Trabajo principal:** que la clase se lea de un vistazo y que responder sea un solo toque.
- **Restricciones:** en el modo virtual (principal) el estudiante usa sobre todo una **computadora**, a veces un celular, normalmente junto a una videollamada; en el modo en aula (posterior) la proyección se ve desde la última fila de un salón iluminado; el profesor ya tiene la carga cognitiva al máximo.

## Concepto

El vocabulario del aula, no el del software: **la hoja de respuestas** que se rellena con burbujas, la **lista de asistencia** en renglones, el **bolígrafo azul** y el **resaltador** del profesor. Cada recurso visual significa algo:

- **Burbuja** (círculo con letra): una opción de respuesta. Vacía = sin elegir; rellena en tinta = elegida.
- **Renglón** (línea azul clara): separa filas de una lista (asistencia, opciones). No se usa como adorno.
- **Tinta azul**: lo que el usuario hace o respondió.
- **Resaltador**: la respuesta correcta, y nada más. Es el **único momento audaz** del producto: al revelar resultados, el resaltador barre la opción correcta.
- **Rojo**: solo el indicador "En vivo". No se usa para errores ni para respuestas incorrectas, para no evocar la corrección con lapicero rojo.

## Color

| Token | Hex | Uso |
|---|---|---|
| Papel | `#F5F7F6` | Fondo de todas las pantallas |
| Grafito | `#262A30` | Texto (secundario: `#59606B`) |
| Tinta azul | `#1F4FB8` | Acciones y respuestas |
| Renglón | `#C9D6EA` | Líneas de lista; claro `#E3EAF4` para fondo de barras |
| Resaltador | `#FFE45C` | Solo la respuesta correcta revelada |
| Rojo en vivo | `#D7263D` | Solo el indicador de sesión en vivo |

Contraste sobre papel: grafito ≈ 14:1, grafito suave ≈ 5.9:1, tinta ≈ 6.8:1. Texto blanco sobre tinta ≈ 7:1.

## Tipografía

- **Atkinson Hyperlegible Next** para toda la interfaz. Fue diseñada para máxima legibilidad (letras y números que no se confunden), lo que importa al leer desde el fondo de un aula o en un celular pequeño. Números tabulares para códigos y conteos.
- **Atkinson Hyperlegible Mono** solo cuando el contenido es código (por ejemplo, comandos de Git en un quiz). No se usa para etiquetas.
- Escala de cuarta justa sobre 18 px: 14, 18, 24, 32, 42, 56; el código de sesión proyectado escala con la ventana hasta ~132 px.
- Sin mayúsculas sostenidas en etiquetas; títulos en tipo oración.

## Layout por pantalla

| Pantalla | Dispositivo | Claves |
|---|---|---|
| **Proyección: sala de espera** (modo en aula) | Proyector | Código gigante en tinta, QR y la lista de asistencia llenándose en renglones |
| **Proyección: resultados** (modo en aula) | Proyector | Opciones como filas de hoja de respuestas con barra y conteo; la correcta con resaltador |
| **Presentador** | Laptop del profesor | Diapositiva + guion + barra inferior (diseño §4) |
| **Estudiante** | Computadora (principal) y celular | Adaptable: en pantalla ancha, diapositiva grande y pregunta al lado; en celular, diapositiva arriba y burbujas de al menos 60 px de alto debajo |

La diapositiva del profesor siempre va en un marco blanco: es **su** contenido, con **su** tipografía, y la interfaz no lo maquilla.

## Principios

1. **Legible de un vistazo**, también en una ventana a medio tamaño junto a la videollamada y, en el modo en aula, desde la última fila. Si dudás entre dos tamaños, el más grande.
2. **Una sola audacia:** el resaltador al revelar. Todo lo demás, sobrio: sin sombras, sin degradados, sin tarjetas decorativas.
3. **El color significa algo.** Tinta = acción/respuesta; resaltador = correcta; rojo = en vivo. Nunca al revés ni como adorno.
4. **Palabras del aula.** "Unirme a la clase", "Enviar respuesta", "Mostrar resultados", "Lista de asistencia". Sin jerga técnica.
5. **Movimiento solo como respuesta** a una acción (enviar, revelar), y siempre respetando `prefers-reduced-motion`.

## Qué se descartó al revisar el plan

- **Pizarra verde con tiza:** cliché del tema educativo y con mal contraste en proyectores.
- **Rojo para errores o respuestas incorrectas:** evoca el lapicero de corrección y castiga al estudiante en público; las opciones incorrectas quedan en gris.
- **Papel crema y serif display:** es el aspecto por defecto de los diseños generados; aquí el papel es frío, de hoja de examen.

## Modos de uso

Las pantallas de proyección pertenecen al **modo en aula**, que está en discusión (diseño §13). El modo virtual usa la vista del presentador y la vista del estudiante adaptable.
