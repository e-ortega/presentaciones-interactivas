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
| `docs/identidad-visual.md` | Identidad visual y principios; tokens CSS en `src/web/src/styles/tokens.css` (usarlos, no inventar colores) |
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
5. **Enseñar React mientras se construye.** Esteban está aprendiendo React. Al escribir frontend: explicar cada concepto nuevo (hooks, estado, efectos, props, contexto) con el ejemplo concreto del código que se está escribiendo, a ritmo pausado, uno a uno. Claude puede cerrar con una o dos preguntas de comprobación, pero son opcionales: Esteban las contesta si quiere y nunca quedan como pendiente (D-62). Preferir código claro sobre código ingenioso.
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

M0, M1 y M2 terminados: la app importa un PPTX (imágenes, notas y PDF); la zona del profesor permite entrar con la clave, subir presentaciones y ver su estado; y la vista del presentador recorre una presentación con sus notas como guion, por botones o teclado. Siguiente hito: M3, sesión en vivo con SignalR (`docs/plan-fase-1.md`).

## Flujo de trabajo

`main` está protegida (ruleset `pr_main`): no se puede subir directo, ni documentación. Cada cambio va en una rama corta (`feat/...`, `fix/...`, `docs/...`) con pull request; se fusiona con "squash" cuando pasan los tres trabajos del CI (`Server (.NET)`, `Web (React)`, `Docker image`). Si se renombra un trabajo en `.github/workflows/ci.yml`, hay que actualizar el ruleset o los pull requests quedan bloqueados. Claude puede fusionar sus propios pull requests cuando el CI esté en verde (D-59), pero **antes debe hacer revisar el diff con `/code-review`** y corregir o anotar los hallazgos en el pull request (D-61). Esteban revisa después, a posteriori (D-73): por eso la descripción de cada pull request debe decir con claridad qué cambió, cómo se verificó y qué no se verificó.

Antes de subir cambios, correr localmente lo mismo que el CI: `dotnet build` (los avisos son errores), `dotnet format --verify-no-changes`, `dotnet test`; y en `src/web`: `npm run lint`, `npm test`, `npm run build` (D-60). No apagar una regla del linter o de los analizadores sin dejar el motivo escrito junto a ella.

## Comandos

- Servidor: `dotnet run --project src/Server` (usa LocalDB y aplica las migraciones al arrancar).
- Frontend: `npm run dev` en `src/web` (Vite reenvía `/api` y `/health` al servidor).
- Contenedores: Docker está en WSL (`Ubuntu-26.04`), no en Windows. Arrancar el servicio con `wsl -u root service docker start` y correr `docker compose` desde `/mnt/d/Documents/source/repos/presentaciones-interactivas`.
- Importar de verdad necesita LibreOffice, que solo está en la imagen Docker: con `dotnet run` en Windows la subida funciona pero la conversión falla. Para probar una importación, usar Docker Compose.
- Los decks reales de Esteban se leen desde su carpeta para pruebas manuales; **nunca se copian al repo**, que es público.
- Tipos de la API: al cambiar un endpoint o un DTO, correr `PI_UPDATE_OPENAPI=1 dotnet test` (actualiza `src/web/src/api/openapi.json`) y luego `npm run generate:api` en `src/web`; guardar los dos archivos (D-65). Si no, falla una prueba del servidor o el CI.
- Hooks: `git commit` corre lint-staged (D-64). Si un hook falla, corregir el problema; no saltarlo con `--no-verify`.
- Dependabot abre pull requests los lunes (D-67). Claude los revisa y fusiona los seguros; ante la duda, avisa a Esteban (D-68). Node y sus tipos (`@types/node`) se quedan en la 24 LTS (D-69, D-75).
- Las capturas de pantalla sirven para ver lo que las pruebas no ven (un botón sin texto pasó 46 pruebas): al cambiar estilos, mirar la pantalla en el navegador.
- Pruebas: `dotnet test` en la raíz; `npm test` y `npm run lint` en `src/web`.
- Nueva migración: `dotnet ef migrations add <Name> --project src/Server --output-dir Infrastructure/Data/Migrations`.
