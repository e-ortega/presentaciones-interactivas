# CLAUDE.md — Presentaciones Interactivas

Contexto para trabajar en este proyecto sin depender de la conversación donde se diseñó. Leer esto primero y luego los documentos de `docs/`.

## Qué es

App web que convierte una presentación de PowerPoint que el profesor **ya tiene** en una clase en vivo e interactiva: vista del presentador con sus notas, quizzes que los estudiantes responden desde el celular y resultados en tiempo real. Diferenciador: "trae tu propio deck". Filosofía: **la app ayuda, el profesor decide** (la IA sugiere, nunca agrega nada sola).

Dueño del proyecto: **Esteban Ortega**, ingeniero de software en Costa Rica y profesor de desarrollo web. Fuerte en .NET/C#, SQL Server, Azure y Docker; **está aprendiendo React**.

## Documentos (fuente de verdad)

| Archivo | Para qué |
|---|---|
| `docs/diseno.md` | Diseño completo: experiencia, modelo de datos, arquitectura, Azure, fases |
| `docs/decisiones.md` | Registro de decisiones (D-xx) y propuestas en revisión (P-xx) |
| `docs/pendientes.md` | Lo que depende de Esteban |
| `docs/arquitectura.md` | Arquitectura de implementación: backend, contrato SignalR, identidad, importación, frontend, pruebas, CI/CD |
| `docs/plan-fase-1.md` | Plan de implementación del MVP, por hitos |
| `docs/identidad-visual.md` | Identidad visual y principios; tokens CSS en `design/tokens.css` (usarlos, no inventar colores) |
| `docs/competencia.md` | Competencia, posicionamiento y monetización |
| `docs/legal/aviso-privacidad.md` | Aviso de consentimiento Ley 8968 (texto de la interfaz) |
| `docs/contexto.md` | Cómo se llegó a este diseño y qué se descartó |
| `docs/referencia/` | Notas previas hechas con Gemini. **Solo referencia, no son decisiones** |

Si algo del código contradice `docs/decisiones.md`, gana el documento hasta que Esteban decida otra cosa.

## Reglas de trabajo

1. **Ciclos autónomos.** En tareas largas, avanzar por ciclos hasta cumplir el objetivo en vez de detenerse a preguntar entre pasos. Detenerse solo ante decisiones irreversibles o que claramente le corresponden a Esteban.
2. **Toda decisión importante se documenta** en `docs/decisiones.md` (nuevo número D-xx, fecha, decisión, motivo), no solo en el chat. Si cambia una decisión previa, se marca como reemplazada; no se borra. Si el cambio afecta el diseño, actualizar también `docs/diseno.md`.
3. **Pendientes explícitos.** Al terminar un ciclo, actualizar `docs/pendientes.md` y mostrar a Esteban la lista de lo que depende de él, marcando qué es nuevo o qué cambió desde la última versión.
4. **Alcance:** respetar las fases de `docs/diseno.md` §12. No construir cosas de fases posteriores "de paso".
5. **Enseñar React mientras se construye.** Esteban está aprendiendo React. Al escribir frontend: explicar cada concepto nuevo (hooks, estado, efectos, props, contexto) con el ejemplo concreto del código que se está escribiendo, a ritmo pausado, uno a uno, y al final hacer una o dos preguntas para confirmar que lo entendió. Preferir código claro sobre código ingenioso.
6. **Idioma (D-34):** código, comentarios y documentación técnica en **inglés**; documentación para el usuario y textos de interfaz en **español**. Los textos de la interfaz van en archivos de recursos desde el inicio (D-44) para agregar inglés después. Los documentos de diseño de `docs/` se quedan en español (D-43).

## Decisiones que más afectan el código

- **Diapositivas como imágenes** en v1 (D-03). No se parsea el PPTX a elementos editables.
- **Navegación por "diapositiva + paso"** (D-26). La sincronización siempre envía `(slideIndex, stepIndex)`. En v1 cada diapositiva tiene un paso. No simplificar a "solo diapositiva": es lo que permite agregar animaciones después.
- **`ISlideRenderer`** (D-27, en el diseño: `IRenderizadorPresentacion`): interfaz intercambiable que recibe el PPTX y devuelve diapositivas con pasos (imágenes) y notas. La v1 la implementa con LibreOffice.
- **Widget vs. Lanzamiento** (D-22): el widget es el diseño editable; el lanzamiento (`WidgetLaunch`) guarda una **copia exacta** (snapshot) de la pregunta al momento de lanzarse. Las respuestas cuelgan del lanzamiento, nunca del widget.
- **Vista del estudiante amarrada** a la del profesor (D-12) y **adaptable**, diseñada primero para computadora (D-35).
- **Un solo modo, tres ventanas (D-39):** presentador, ventana para compartir o proyectar (`Display`, sin notas ni controles) y estudiante. Cada cliente se une con un rol (`Presenter`, `Display`, `Participant`) y el hub emite eventos al grupo de la sesión.
- **Los datos de estudiantes nunca van a servicios de IA** (D-42).
- **Sin login de profesor en la Fase 1** (D-36). La zona del profesor pide una clave de acceso (D-45): la app solo conoce su hash SHA-256 en `AdminAccess:KeySha256` (user-secrets en local, secreto de Container Apps en Azure) y compara en tiempo constante. **Nunca escribir la clave ni su hash en el repositorio**: es público.
- **El correo es la identidad** del participante (D-16).
- **Ley 8968:** el formulario de nombre y correo lleva aviso de consentimiento y se guarda la fecha del consentimiento (D-29).

## Stack

- Backend: **ASP.NET Core + SignalR** (hub en memoria, una réplica), **EF Core + SQL Server**.
- Frontend: **React + Vite + TypeScript** (D-31, D-33), compilado y servido como archivos estáticos por la misma app ASP.NET Core: un solo contenedor, mismo origen, sin CORS.
- Importación: LibreOffice headless (PPTX → PDF), `pdftoppm` (PDF → PNG por diapositiva), Open XML SDK (notas del orador).
- Local: Docker Compose con SQL Server en contenedor.
- Azure (D-47): Container Apps (Consumo), Azure SQL Database (oferta gratuita), Blob Storage, imagen en GitHub Container Registry.

## Glosario dominio → código (D-34)

| Documentos | Código |
|---|---|
| Usuario | `User` |
| Participante | `Participant` |
| Grupo / MiembroGrupo | `Group` / `GroupMember` |
| Presentación | `Presentation` |
| Diapositiva | `Slide` |
| Paso | `SlideStep` |
| Widget | `Widget` |
| Sesión | `Session` (o `LiveSession` si choca con nombres del framework) |
| Asistencia | `Attendance` |
| Lanzamiento | `WidgetLaunch` |
| Respuesta | `Response` |

## Estado actual

Diseño y plan de la Fase 1 aprobados (D-39, D-40). Repositorio creado, todavía sin código. Siguiente paso: revisar `docs/pendientes.md` con Esteban y empezar `docs/plan-fase-1.md` por el hito M0.
