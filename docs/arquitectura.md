# Arquitectura de implementación — Fase 1

_Propuesta de Claude, 9 oct 2026; aprobada por Esteban (D-48). Complementa el diseño ([diseno.md](diseno.md) §10 modelo de datos y §11 arquitectura de alto nivel) y el plan ([plan-fase-1.md](plan-fase-1.md))._

**Principio:** lo más simple que soporte la Fase 1 sin cerrar las puertas acordadas (pasos para animaciones, tres ventanas, varias réplicas a futuro). Cada punto dice qué se hace ahora y qué cambiaría al crecer.

---

## 1. Backend

| Tema | Propuesta | Por qué |
|---|---|---|
| Plataforma | **.NET 10 (LTS)**, ASP.NET Core | Versión con soporte largo |
| Proyectos | Un solo proyecto `Server` + `Server.Tests` | Una persona, un MVP: capas en carpetas, no en proyectos |
| Organización | **Por funcionalidad** (`Features/Import`, `Features/Presentations`, `Features/Sessions`, `Features/Widgets`, `Features/Join`), más `Domain/`, `Infrastructure/`, `Realtime/` | Todo lo de una funcionalidad está junto; fácil de leer y de explicar |
| Endpoints | **Minimal APIs** agrupadas por funcionalidad (`MapGroup`) | Menos ceremonia que controladores |
| Mediadores | **Sin MediatR** | Innecesario a este tamaño; además su licencia pasó a ser comercial |
| Datos | **EF Core** directo; el `DbContext` es la unidad de trabajo. **Sin repositorios genéricos** | Una capa de repositorios sobre EF solo agrega código |
| Dominio | Entidades con métodos que protegen sus reglas (`WidgetLaunch.Close()`, `Session.GoTo(slide, step)`); reglas de unicidad también como índices únicos en la base | Las reglas importantes no dependen de que cada endpoint se acuerde |
| Identificadores | `Guid` versión 7 (`Guid.CreateVersion7()`), ordenables por tiempo | Sin choques y buen rendimiento de índices |
| Fechas | `DateTimeOffset` en UTC; la interfaz convierte a hora local | Evita errores de zona horaria |
| Errores | Respuestas `ProblemDetails` estándar | El frontend maneja errores de una sola forma |
| Validación | Validación explícita en cada endpoint (sin librería al inicio) | Pocas entradas en la Fase 1 |
| Documentación de API | OpenAPI integrado de ASP.NET Core | Base para generar los tipos de TypeScript |

## 2. Tiempo real (SignalR)

**El servidor es la fuente de verdad** del estado de cada sesión: diapositiva y paso actuales, lanzamiento abierto, participantes conectados. Los clientes solo muestran lo que el servidor les dice.

### Estado en vivo

- Los hechos durables viven **en la base** y se escriben al momento: el estado de la sesión, la diapositiva y paso actuales, cada asistencia, y (M4) cada lanzamiento con su copia exacta de la pregunta y cada respuesta. Se leen de ahí cada vez (D-79).
- **En memoria** (`ILiveSessionStore`) queda solo lo que es cierto mientras hay gente conectada: quién tiene una conexión abierta. Con una sola réplica (D-47).
- Si el contenedor se reinicia no se pierde nada: los clientes se reconectan y reciben el estado desde la base.
- **Al crecer a varias réplicas:** Azure SignalR Service (Standard) y el estado en vivo en Redis o en la base. Por eso todo acceso al estado pasa por la interfaz `ILiveSessionStore`.

### Grupos

`session:{id}` (todos), `session:{id}:presenter`, `session:{id}:display`.

### Contrato del hub `SessionHub`

| Dirección | Mensaje | Quién | Nota |
|---|---|---|---|
| Conexión | `/hubs/session?sessionId=…&role=Presenter\|Display\|Participant` | Todos | El servidor comprueba que las cookies den ese rol; si no, cierra la conexión (D-82) |
| Cliente → servidor | `GoTo(slideIndex, stepIndex)` | Presentador | Solo con la sesión en vivo y hacia una posición que exista |
| | `LaunchWidget(widgetId)` / `CloseLaunch(launchId)` / `RevealResults(launchId)` | Presentador | |
| | `StartSession()` / `EndSession()` / `RemoveParticipant(attendanceId)` | Presentador | |
| | `SubmitResponse(launchId, payload)` | Estudiante | Una por estudiante y lanzamiento (índice único) |
| Servidor → cliente | `SessionSnapshot(estado completo)` | Todos | Al conectarse y al **reconectarse**: así nadie queda desfasado. Sin notas del orador para nadie; sin código ni asistencia para el estudiante (D-80) |
| | `SessionStarted(slideIndex, stepIndex)` | Todos | Del lobby a en vivo |
| | `SlideChanged(slideIndex, stepIndex)` | Todos | |
| | `LaunchOpened(launch)` / `LaunchClosed(launchId)` | Todos | `launch` lleva la copia exacta de la pregunta |
| | `ResponseCount(launchId, answered, connected)` | Presentador y ventana para compartir | **Agrupado**: como máximo cada ~300 ms, no un mensaje por respuesta |
| | `LiveTallies(launchId, tallies)` | Solo presentador | Antes de revelar |
| | `ResultsRevealed(launchId, tallies, correctOption)` | Todos | |
| | `AttendanceChanged(conectados, lista)` | Presentador y ventana para compartir | La ventana para compartir recibe solo el número; los nombres, solo el presentador (D-78) |
| | `SessionEnded()` / `RemovedFromSession()` | Todos / el estudiante expulsado | |

Los nombres de los métodos y los tipos de los mensajes viven en un solo lugar en cada lado: `Realtime/Contracts.cs` y `src/web/src/realtime/contracts.ts`. Hay que mantenerlos iguales a mano; una prueba de integración conecta un cliente real y verifica los mensajes.

## 3. Identidad y acceso

| Quién | Cómo se identifica | Detalle |
|---|---|---|
| **Profesor** | Clave de acceso (D-45) → cookie firmada, `HttpOnly`, `Secure` | `POST /api/access` cambia la clave por la cookie. Protege todo `/api/admin/*` y los métodos de presentador del hub |
| **Ventana para compartir** | Token de un solo uso que genera el presentador al abrirla (`POST /api/admin/sessions/{id}/display-token`), válido 2 minutos; la ventana lo cambia por su cookie en `POST /api/display/access` | Evita que alguien con el código abra la ventana del profesor. En el mismo navegador del profesor basta su cookie |
| **Estudiante** | `GET /api/join/{code}` le muestra a qué clase va a entrar; `POST /api/join/{code}` con nombre, correo y consentimiento → crea o reutiliza `Participant` (por correo) y `Attendance` → cookie firmada con el id de la asistencia | La misma cookie autentica la conexión de SignalR; si se recarga la página o se cae el wifi, vuelve como la misma persona. Queda admitido al entrar; el profesor puede sacarlo (D-77) |

- **Código de sesión:** 8 dígitos aleatorios (`8888-6666`), únicos entre sesiones activas; deja de servir cuando la sesión termina.
- **Llaves de las cookies:** se guardan en la base de datos (D-51), para que las cookies sigan valiendo cuando el contenedor se reinicia.
- **Límite de intentos** (rate limiting de ASP.NET Core) en `join` y en la clave de acceso, por IP, para impedir adivinar códigos o la clave. El de `join` es amplio, 300 por minuto, porque un aula comparte una sola IP (D-81). Detrás del proxy de Azure la IP real viene en un encabezado: hay que activar los encabezados reenviados en el M5 (ver el plan).
- **Sesión vencida:** cuando cualquier petición del profesor recibe un 401, la interfaz vuelve a pedir la clave.

## 4. Importación de presentaciones

1. `POST /api/admin/presentations` (multipart). Validaciones: extensión `.pptx` (se rechaza `.pptm`, con macros), firma de archivo ZIP, **tamaño máximo 100 MB**, que el Open XML SDK pueda abrirlo, al menos una diapositiva visible y máximo 300. Un rechazo responde 400 con un código (`NotPptx`, `TooLarge`, `Unreadable`, `NoVisibleSlides`, `TooManySlides`) que la interfaz traduce (D-54).
2. Se guarda el original y se crea la `Presentation` en estado `Pending`.
3. Una **cola en el mismo proceso** (`Channel<ImportJob>` + `BackgroundService`) procesa **una importación a la vez**, porque LibreOffice no tolera conversiones simultáneas con el mismo perfil.
4. LibreOffice corre con un perfil temporal propio, en una carpeta temporal, con **tiempo máximo de 2 minutos**; si se pasa, se termina el proceso y la importación queda en `Failed` con su motivo como código (D-54). Las imágenes salen en PNG de 1600 px de ancho (D-55).
5. El frontend consulta el estado cada pocos segundos (`GET /api/admin/presentations/{id}`). En la Fase 1 no hace falta SignalR para esto.
6. Al arrancar, las importaciones que quedaron en `Pending` o `Processing` se vuelven a encolar.
7. Si una importación no puede ni empezar o no puede guardar su resultado (típicamente, la base está en pausa), se reintenta hasta 3 veces con una espera entre intentos (D-63). Si falla, los archivos que ya había guardado se borran y queda solo el original.
- **Al crecer:** la cola pasa a Azure Storage Queues y el procesamiento a un Container Apps Job, sin cambiar `ISlideRenderer`.

## 5. Archivos

- Interfaz `IFileStorage`: disco local en desarrollo, Blob Storage **privado** en Azure.
- Las imágenes de los pasos se sirven **a través de la app** (`GET /api/sessions/{id}/slides/{n}/steps/{m}`), que verifica que quien pide sea el presentador, la ventana para compartir o un estudiante admitido de esa sesión; al estudiante, solo mientras la clase está en vivo (D-82). Con caché del navegador.
- El PDF de estudiante se sirve igual, solo si el profesor lo publicó.
- **Al crecer:** URLs firmadas de corta duración o una CDN, para no pasar las imágenes por el contenedor.

## 6. Frontend

| Tema | Propuesta | Por qué |
|---|---|---|
| Base | Vite + React + TypeScript (D-31, D-33) | |
| Rutas | React Router: `/admin` (presentaciones, importar, widgets) y `/admin/presentations/:id/present` (presentador sin sesión, D-70) ya existen; con las sesiones llegan `/present/:sessionId`, `/display/:sessionId`, `/join` y `/s/:code` | Una ruta por ventana |
| Datos del servidor | **TanStack Query** para las llamadas REST | Caché, reintentos y estados de carga resueltos |
| Tiempo real | Un hook `useLiveSession(sessionId, role)` que envuelve `@microsoft/signalr`, maneja la reconexión y expone el estado del último `SessionSnapshot` | Los componentes no tocan SignalR directamente; buen punto para aprender hooks |
| Estado global | **Sin Redux.** Estado local de React + el hook de sesión | No hace falta en la Fase 1 |
| Tipos de la API | Generados con `openapi-typescript` desde `src/api/openapi.json`, el contrato guardado en el repo y verificado contra el servidor por una prueba (D-65) | Los tipos de C# y TypeScript no se desincronizan |
| Textos | `react-i18next`, archivo `es.json` (D-44) | |
| Estilos | CSS Modules + `tokens.css` (D-46) | Sin librería de componentes: la identidad es propia y los elementos nativos son accesibles |
| Pruebas | Vitest + Testing Library; lint con oxlint (D-53) | |

## 7. Pruebas

- **Dominio:** pruebas unitarias con xUnit v3 (D-53).
- **Arquitectura:** las reglas de dependencias entre `Domain`, `Features` e `Infrastructure` son pruebas (NetArchTest.eNhancedEdition, D-66).
- **Contrato de la API:** una prueba compara el documento OpenAPI del servidor con `src/web/src/api/openapi.json` (D-65).
- **Hub:** las pruebas conectan clientes SignalR reales a la app en memoria (por *long polling*, porque ese servidor no tiene sockets); la prueba de humo del contenedor corre una sesión por WebSockets reales.
- **API y hub:** pruebas de integración con `WebApplicationFactory` y **SQL Server real en contenedor (Testcontainers)**, o el servidor de `PI_TEST_SQLSERVER` cuando no hay Docker (D-52).
- **Importación:** los PPTX de prueba son **sintéticos** y los genera el propio código de pruebas (`SyntheticDeck`), con diapositivas ocultas y sin notas: el repo es público y las presentaciones reales de Esteban no se suben. La conversión real con LibreOffice se prueba dentro del contenedor con una prueba de humo en el CI (D-56).
- **De punta a punta:** un recorrido con Playwright al cerrar el M4 (importar, presentar, unirse, responder, revelar).

## 8. Flujo de trabajo y CI/CD

- **Ramas:** `main` protegida con el ruleset `pr_main`: pull request obligatorio, los tres trabajos del CI en verde y solo fusión "squash". Cada hito o tarea va en una rama corta (`feat/m1-import`) con pull request. Commits en formato convencional (`feat:`, `fix:`, `docs:`).
- **GitHub Actions** en cada pull request: compilar y probar .NET, `npm ci`, lint, pruebas y build del frontend, y la prueba de humo de la imagen Docker.
- **Verificaciones estrictas (D-60):** en el servidor, todo aviso del compilador o de los analizadores detiene la compilación (`Directory.Build.props`) y el CI comprueba el formato con `dotnet format --verify-no-changes`. En el frontend, `oxlint --type-aware --deny-warnings`. Apagar una regla exige dejar el motivo escrito junto a ella.
- **Antes del commit (D-64):** hooks de Husky y lint-staged corren el lint y los tipos del frontend y corrigen el formato de los `.cs`. Se instalan solos con `npm install` en `src/web`.
- **Dependencias (D-67):** el flujo `Dependency audit` busca vulnerabilidades cada lunes y cuando cambian los paquetes, sin bloquear las fusiones; Dependabot propone las actualizaciones.
- **Revisión antes de fusionar (D-61):** el diff de cada pull request lo revisa alguien o algo distinto de quien lo escribió; los hallazgos se corrigen o se anotan en el pull request. La plantilla `.github/pull_request_template.md` lleva la lista.
- **Despliegue (M5):** al fusionar en `main`, se construye la imagen, se publica en GitHub Container Registry y se actualiza Container Apps. Autenticación a Azure con **OIDC** (credenciales federadas): sin secretos de Azure guardados en GitHub.
- **Secretos:** `dotnet user-secrets` en local; secretos de Container Apps en Azure. Nunca en el repo.

## 9. Observabilidad

- Logging integrado de ASP.NET Core en formato estructurado; en Azure lo recoge Log Analytics. Nunca se registran correos ni respuestas de estudiantes.
- `GET /health` para las sondas de Container Apps. No consulta la base (D-50).
- Métricas mínimas en logs: duración de cada importación, sesiones activas, conexiones por sesión.

## 10. Seguridad (resumen)

- HTTPS siempre; cookies `HttpOnly`, `Secure`, `SameSite=Lax`.
- Límite de intentos en el ingreso por código y en la clave.
- Validación estricta de archivos subidos; LibreOffice aislado en carpeta temporal y con tiempo máximo.
- Las notas del orador y los textos de los estudiantes se muestran siempre como texto, nunca como HTML.
- Los datos de estudiantes no salen hacia servicios de IA (D-42).
