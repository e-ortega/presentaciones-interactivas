# Registro de decisiones

Cada decisión con su fecha y el motivo. Si una decisión cambia, no se borra: se marca como reemplazada y se agrega la nueva.

| # | Fecha | Decisión | Motivo |
|---|---|---|---|
| D-01 | 2026-10-08 | El producto parte de la presentación que el profesor ya tiene ("trae tu propio deck") | Quitar la fricción de reconstruir el material en otra herramienta |
| D-02 | 2026-10-08 | El momento "wow" a priorizar es la interacción en vivo en el aula | Es lo que engancha al profesor la primera vez; las analíticas vienen después |
| D-03 | 2026-10-08 | En la v1 las diapositivas se muestran como imágenes, sin parsear a elementos editables | Mucho menos esfuerzo y nunca rompe el formato del profesor |
| D-04 | 2026-10-08 | Habrá un editor mínimo para crear desde cero, sin competir con PowerPoint | Útil para quien no tiene deck, pero no es el núcleo |
| D-05 | 2026-10-08 | Las notas del orador del PPTX se muestran como script en la vista del presentador | El profesor ya las escribió; además son el mejor insumo para la IA |
| D-06 | 2026-10-08 | El script se puede ocultar y tiene dos modos: teleprompter y notas estáticas | No todos los profesores lo necesitan ni lo usan igual |
| D-07 | 2026-10-08 | Vista del presentador en tres zonas: diapositiva, script y barra de controles, con PIN/QR siempre visible | Durante la clase todo debe verse de un vistazo y estar a un toque |
| D-08 | 2026-10-08 | La IA sugiere solo un puñado de interacciones, en una lista corta que el profesor revisa una vez; nada se agrega automáticamente | Confiar en el criterio del profesor y no llenar el deck de ruido |
| D-09 | 2026-10-08 | Al aceptar una sugerencia, la IA redacta el contenido completo como borrador editable | Ahorra tiempo real; la decisión final sigue siendo del profesor |
| D-10 | 2026-10-08 | Al terminar de editar, el profesor genera una sesión con enlace y código y controla quién entra | Control del aula |
| D-11 | 2026-10-09 | Ciclo de sesión: lobby → en vivo → interacción activa → finalizada; el profesor decide si revela resultados | Mantiene el control en manos del profesor |
| D-12 | 2026-10-09 | La pantalla del estudiante va amarrada a la del profesor, sin navegación libre | Mantiene a todos enfocados |
| D-13 | 2026-10-09 | Al final, el profesor puede publicar un PDF de estudiante: el PPTX original convertido, sin notas ni widgets | Resuelve el caso de quien se quedó atrás tomando apuntes |
| D-14 | 2026-10-09 | El PDF se genera durante la importación | Publicarlo es solo habilitar la descarga; además sale del mismo paso de conversión |
| D-15 | 2026-10-09 | Dos vías de acceso: cuenta (cursos) o código + nombre + correo (eventos); se ofrece crear cuenta sin exigirla | Sirve tanto para un curso recurrente como para una charla puntual |
| D-16 | 2026-10-09 | El correo es la identidad del estudiante; el historial por código se une a la cuenta cuando la crea | Evita duplicados y conserva la trazabilidad |
| D-17 | 2026-10-09 | Los apodos son una opción del profesor por sesión | Semi-anónimo para webinars, nombre real para cursos |
| D-18 | 2026-10-09 | Sin verificación de correo por sesión; en cursos la confianza se establece al crear la cuenta y unirse al grupo con un código | Menos fricción en cada clase, misma trazabilidad |
| D-19 | 2026-10-09 | El "curso" se modela como **Grupo**, con código de inscripción propio; las presentaciones se agregan al grupo | Los estudiantes se inscriben una vez y ven todas las sesiones |
| D-20 | 2026-10-09 | Reutilizar una presentación es otra sesión; el profesor relanza la existente o crea una nueva desde el mismo PPTX | Modelo simple; una presentación tiene muchas sesiones |
| D-21 | 2026-10-09 | Para retomar una clase, el profesor avanza a la diapositiva necesaria; la app ofrece empezar donde terminó la sesión anterior | Ahorra clics sin quitar control |
| D-22 | 2026-10-09 | Separación Widget (diseño) / Lanzamiento (uso en una sesión); el lanzamiento guarda una copia exacta de la pregunta | Editar un quiz nunca corrompe respuestas ni analíticas pasadas |
| D-23 | 2026-10-09 | Alcance de la Fase 1: importar PPTX, notas, un widget (quiz de opción múltiple) y entrada mínima por código | Lo mínimo para probarlo en una clase real |
| D-24 | 2026-10-09 | Fase 2: sugerencias de la IA y experiencia del estudiante; editor, grupos y demás después | La IA es lo que más valor diferencial aporta |
| D-25 | 2026-10-09 | v1 sin animaciones, como limitación conocida | Esteban casi no las usa; se aceptan si no obligan a reconstruir después |
| D-26 | 2026-10-09 | Navegación y sincronización por "diapositiva + paso" desde la v1 (un paso por diapositiva) | Permite agregar animaciones después sin tocar presentador, estudiante ni sincronización |
| D-27 | 2026-10-09 | La conversión del PPTX vive detrás de una interfaz intercambiable (`IRenderizadorPresentacion`) | Cambiar LibreOffice por un renderizador con animaciones sin afectar el resto |
| D-28 | 2026-10-09 | Stack: ASP.NET Core + SignalR, SQL Server y Azure, con diseño simple y económico | Experiencia de Esteban y costo bajo mientras se valida |
| D-29 | 2026-10-09 | Aviso de consentimiento según la Ley 8968 en el formulario de nombre y correo | Obligación legal al recoger datos personales en Costa Rica |
| D-30 | 2026-10-09 | El proyecto vive en `D:\Documents\source\repos\presentaciones-interactivas` (sincronizado con Google Drive); el repo git se crea después | Pedido de Esteban |
| D-31 | 2026-10-09 | Frontend en **React + Vite**, compilado y servido como archivos estáticos por la app ASP.NET Core | Esteban está aprendiendo React y el proyecto le sirve para eso; servirlo desde la misma app mantiene un solo contenedor, mismo origen y sin CORS |
| D-32 | 2026-10-09 | El contexto del proyecto vive en el repo (`CLAUDE.md` y `docs/`) para poder trabajar desde Claude Code fuera del chat de diseño | Pedido de Esteban |
| D-33 | 2026-10-09 | React con **TypeScript** (antes P-05) | Tipos parecidos a C#; atrapan errores de props y de mensajes de SignalR |
| D-34 | 2026-10-09 | Código y documentación interna en **inglés**; documentación al usuario e interfaz en **español**; versión en inglés de la interfaz más adelante (antes P-06) | Pedido de Esteban |
| D-35 | 2026-10-09 | La vista del estudiante es adaptable a computadora y celular, diseñada primero para computadora | Esteban espera que la mayoría entre desde computadora |
| D-36 | 2026-10-09 | Sin inicio de sesión del profesor en la Fase 1; se resuelve después | Pedido de Esteban; no es necesario para probar el núcleo |
| D-37 | 2026-10-09 | El nombre del producto se decide más adelante; "Presentaciones Interactivas" sigue como provisional | Pedido de Esteban |
| D-38 | 2026-10-09 | Se crea el repositorio git local del proyecto (rama `main`) | Pedido de Esteban |
| D-39 | 2026-10-09 | **Un solo modo, tres ventanas** (antes P-09): presentador, ventana opcional para compartir o proyectar (sin notas ni controles) y estudiante. La ventana para compartir entra en la Fase 1 | Sirve igual para clase virtual con o sin pantalla compartida y para aula con proyector |
| D-40 | 2026-10-09 | Plan de la Fase 1 aprobado (antes P-07), con la ventana para compartir | Resueltos los modos de uso |
| D-41 | 2026-10-09 | Repositorio **público** en GitHub, en la cuenta personal `e-ortega` (no en la cuenta de profesor de la Ulatina); el repo local se queda en `D:\Documents\source\repos` y Esteban lo saca de la sincronización de Drive | Pedido de Esteban |
| D-42 | 2026-10-09 | Los datos personales de los estudiantes **nunca se envían a servicios de IA**; la IA solo analiza la presentación (antes P-12) | Aprobado por Esteban |
| D-43 | 2026-10-09 | Los documentos de diseño de `docs/` se quedan en español; la documentación de uso en inglés se genera después si hace falta | Aprobado por Esteban |
| D-44 | 2026-10-09 | Textos de la interfaz en archivos de traducción desde el inicio (antes P-10) | Agregar inglés después es traducir un archivo |
| D-45 | 2026-10-09 | Clave de acceso para la zona del profesor mientras no haya login (antes P-11). La app guarda solo el **hash SHA-256** de la clave (`AdminAccess:KeySha256`) y compara en tiempo constante. La clave en texto plano vive en el gestor de contraseñas de Esteban; el hash va en `dotnet user-secrets` en desarrollo y como secreto de Container Apps en Azure. **Nunca en el repositorio** | El repo es público; si la configuración se filtra, el hash de una clave aleatoria de 256 bits no permite recuperarla |
| D-46 | 2026-10-09 | Identidad visual "Hoja de respuestas" aprobada (antes P-08) | Aprobado por Esteban |
| D-47 | 2026-10-09 | Infraestructura de Azure aprobada (antes P-01 a P-04): Container Apps (Consumo), Azure SQL Database (oferta gratuita), Blob Storage, GitHub Container Registry, SignalR dentro de la app, un contenedor con LibreOffice, Docker Compose en local | Aprobado por Esteban |
| D-48 | 2026-10-09 | Arquitectura de implementación de la Fase 1 aprobada (antes P-13): ver [arquitectura.md](arquitectura.md) | Aprobado por Esteban |
| D-49 | 2026-10-09 | La primera migración incluye solo lo que la Fase 1 usa. Quedan fuera hasta su fase: la entidad `User` y el dueño de la presentación (llegan con el login, D-36), `Group`, el origen IA del widget, apodos y PDF publicado | No construir fases posteriores de paso (regla 4); agregar columnas después es una migración simple |
| D-50 | 2026-10-09 | `GET /health` solo indica que la app está viva y **no consulta la base** | Las sondas de Container Apps mantendrían despierta la base serverless y gastarían la cuota gratuita |
| D-51 | 2026-10-09 | Las llaves que firman las cookies (Data Protection) se guardan en la base de datos | Con escala a cero el contenedor se reinicia seguido; sin esto, el profesor y los estudiantes perderían la sesión en cada reinicio |
| D-52 | 2026-10-09 | En desarrollo, `dotnet run` usa **LocalDB** por defecto; Docker Compose sigue usando SQL Server en contenedor. Las pruebas de integración usan Testcontainers o el servidor indicado en `PI_TEST_SQLSERVER` | Permite trabajar en Windows sin Docker encendido; la cadena de LocalDB no lleva contraseña y puede ir en el repo |
| D-53 | 2026-10-09 | Herramientas del M0: xUnit v3 sobre Microsoft Testing Platform, oxlint (el linter de la plantilla actual de Vite) y los tokens de diseño en un solo lugar, `src/web/src/styles/tokens.css` (antes `design/tokens.css`) | Son los valores por defecto actuales de .NET 10 y Vite; una sola copia de los tokens evita que se desincronicen |
| D-54 | 2026-10-10 | Los motivos de fallo de una importación y de rechazo de un archivo viajan como **códigos** (`ConversionTimedOut`, `NotPptx`...), no como texto; la interfaz los traduce | Los textos viven en los archivos de traducción (D-44); el servidor no escribe mensajes para el usuario |
| D-55 | 2026-10-10 | Las imágenes de las diapositivas son PNG de **1600 px de ancho** (el alto sigue la proporción del deck) | Nítidas en una pantalla Full HD y al proyectar, con unos 100 a 150 KB por diapositiva en los decks de prueba |
| D-56 | 2026-10-10 | La conversión real con LibreOffice se prueba **dentro del contenedor**, con una prueba de humo que sube un deck sintético a la app en Docker Compose (trabajo `Docker image` del CI). Las demás pruebas usan un renderizador falso | Prueba la imagen que se despliega, con sus fuentes; y las pruebas normales no necesitan LibreOffice instalado |
| D-57 | 2026-10-10 | _(La forma de generar fue reemplazada por D-65.)_ Los tipos de TypeScript de la API se generan con `npm run generate:api`, que lee el documento OpenAPI del servidor de desarrollo y escribe `src/web/src/api/schema.d.ts` (se guarda en el repo). Se corre a mano cuando cambia un endpoint | Generarlos en cada compilación arrancaba la app entera y llenaba la salida de errores de conexión; y `openapi-typescript` aún no acepta TypeScript 6, así que se usa con `npx` en vez de instalarlo |
| D-58 | 2026-10-10 | React Router se agrega en el M2, cuando exista una segunda pantalla. Por ahora la app tiene una sola: la zona del profesor | No agregar piezas antes de necesitarlas; y deja un concepto menos por aprender en este hito |
| D-59 | 2026-10-10 | Claude puede fusionar sus propios pull requests cuando los tres trabajos del CI estén en verde | Aprobado por Esteban; con `main` protegida, esperar cada fusión frenaba los ciclos autónomos |
| D-60 | 2026-10-10 | **Verificaciones automáticas estrictas.** Servidor: los avisos del compilador y de los analizadores de .NET detienen la compilación, y el CI comprueba el formato con `dotnet format`. Frontend: el linter analiza tipos y cualquier aviso lo hace fallar (APIs obsoletas, promesas sin atender, accesibilidad). Las reglas apagadas a propósito están en `.editorconfig` y `.oxlintrc.json`, con su motivo | Un tipo obsoleto de React (`FormEvent`) llegó a `main` sin que el CI lo notara; lo vio Esteban en su editor |
| D-61 | 2026-10-10 | **Revisión antes de fusionar.** Ningún pull request se fusiona sin que su diff haya sido revisado por alguien o algo distinto de quien lo escribió, con los hallazgos corregidos o anotados en el pull request. Cuando el autor es Claude, la revisión la hace una pasada de revisión de código independiente (`/code-review`) hasta que Esteban defina otra | Los primeros cuatro pull requests entraron sin ninguna revisión: la de Copilot que pide el ruleset nunca se ejecutó |
| D-62 | 2026-10-10 | Las preguntas de comprobación al explicar React son **opcionales**: Claude puede hacerlas, Esteban las contesta si quiere y no quedan como pendiente | Pedido de Esteban |
| D-63 | 2026-10-10 | Revisión hacia atrás del M0 y el M1 hecha: 9 hallazgos, 8 corregidos y 1 (el límite de intentos detrás del proxy de Azure) anotado como tarea del M5. Una importación que no puede ni empezar, por ejemplo con la base en pausa, se reintenta hasta 3 veces; los archivos de una importación fallida se borran del almacenamiento | Ese código entró a `main` antes de existir la política de revisión (D-61) |
| D-64 | 2026-10-10 | **Hooks de pre-commit** con Husky y lint-staged (`.husky/`, `lint-staged.config.mjs`): lint y tipos del frontend si hay archivos `.ts`/`.tsx`; formato de los `.cs` que se guardan, con el modo liviano de `dotnet format` (2 s en vez de 11). El análisis completo sigue en el CI | Detectar en segundos lo que el CI detecta en minutos, también en los commits de Claude |
| D-65 | 2026-10-10 | **Contrato de la API en el repo:** `src/web/src/api/openapi.json`. Una prueba del servidor falla si la API ya no coincide con él, y el CI falla si `schema.d.ts` no coincide con lo que se genera de ese archivo. Para aceptar un cambio: `PI_UPDATE_OPENAPI=1 dotnet test` y luego `npm run generate:api`. Reemplaza la forma de generar de D-57: ya no hace falta el servidor encendido | Los tipos se regeneraban a mano; si se olvidaba, el frontend compilaba contra tipos viejos sin que nada avisara |
| D-66 | 2026-10-10 | **Pruebas de arquitectura** con NetArchTest.eNhancedEdition: `Domain` no depende de nada más, `Infrastructure` no depende de `Features`, solo `Infrastructure/Storage` conoce Azure Blob, y las entidades no tienen propiedades con `set` público | Convierte decisiones de [arquitectura.md](arquitectura.md) en pruebas. Se eligió la eNhancedEdition (última versión de 2025) sobre NetArchTest.Rules (sin versiones desde 2021), a propuesta de Esteban |
| D-67 | 2026-10-10 | **Vulnerabilidades en dependencias:** flujo `Dependency audit` (`npm audit` y la auditoría de NuGet) cada lunes y cuando cambian los paquetes, **sin ser requisito para fusionar**; y Dependabot con alertas, correcciones de seguridad y actualizaciones semanales agrupadas | El repo es público y nadie avisaba de una vulnerabilidad. No bloquea los pull requests porque una alerta aparece cuando se publica, no cuando cambia el código |
| D-68 | 2026-10-10 | Claude revisa los pull requests de Dependabot y fusiona los que son seguros; si tiene dudas, avisa a Esteban en vez de fusionar | Pedido de Esteban |
| D-69 | 2026-10-10 | Node se queda en la versión **24 (LTS)** en la imagen Docker y en el CI; las versiones impares, sin soporte largo, no se adoptan. Se rechazó la actualización de Dependabot a Node 25 | Una versión sin soporte largo deja de recibir correcciones a los pocos meses |
| D-70 | 2026-10-10 | Vista del presentador (M2) en `/admin/presentations/:id/present`, para recorrer y ensayar una presentación sin sesión en vivo. La ruta `/present/:sessionId` de la arquitectura llega con las sesiones (M3) y reutiliza el mismo componente | El M2 es sin tiempo real; todavía no existen las sesiones |
| D-71 | 2026-10-10 | Los dos modos del guion (D-06) se concretan así: **teleprompter** muestra las notas de la diapositiva actual en letra grande y, debajo y atenuadas, las de la siguiente; **notas fijas** muestra las notas de todo el deck en una lista que no se desplaza sola, con la diapositiva actual marcada. La elección de modo y de mostrar u ocultar no se guarda entre visitas todavía | Interpretación de Claude de "sigue la diapositiva" y "panel fijo que no se mueve" del diseño §4; pendiente de que Esteban la confirme al usarla |
| D-72 | 2026-10-10 | **Esteban aprobó las decisiones D-49 a D-71**, que Claude había tomado al implementar los hitos M0 a M2 | Revisadas por Esteban |
| D-73 | 2026-10-10 | **Quién revisa el código:** Claude pasa cada cambio por una revisión independiente antes de fusionar (D-61) y Esteban revisa después, a posteriori. No se usa la revisión de Copilot; la regla que la pide en el ruleset no tiene efecto y puede quitarse | Decisión de Esteban |
| D-74 | 2026-10-10 | Los textos de la interfaz se quedan en **voseo** ("Subí tu primer PowerPoint", "Probá de nuevo") | Confirmado por Esteban |
| D-75 | 2026-10-10 | Los tipos de Node (`@types/node`) vuelven a la versión 24, la misma de Node en la imagen y el CI (D-69). Dependabot deja de proponer versiones mayores de Node y de sus tipos | Con tipos más nuevos que el Node real, el editor acepta funciones que no existen al ejecutar |
| D-76 | 2026-10-10 | La vista del presentador del M2 se acepta como punto de partida. Su experiencia de uso y un guion que se desplace como teleprompter real se trabajan más adelante | Esteban la probó: se ve bien para empezar, y el guion estático funciona por ahora |
| D-77 | 2026-10-10 | **Quien entra a una sesión queda admitido de una vez**; el profesor puede sacarlo, y quien fue sacado no vuelve a entrar con el mismo correo. No hay aprobación uno por uno | El diseño dice "admitir o expulsar"; aprobar a 40 personas una por una frena el inicio de la clase. El candado para cerrar la entrada queda como idea para después |
| D-78 | 2026-10-10 | **La ventana para compartir muestra cuántos están conectados, no sus nombres.** Los nombres los ve solo el profesor. Cambia lo previsto en la identidad visual (lista de asistencia proyectada) | El texto de consentimiento dice "tu profesor o profesora los verá; nadie más". Proyectar los nombres a toda la clase lo contradice. Se puede revertir cambiando también el texto legal |
| D-79 | 2026-10-10 | **Qué vive en memoria y qué en la base.** El estado y la diapositiva actual de una sesión se leen de la base cada vez; en memoria (`ILiveSessionStore`) queda solo quién está conectado. Ajusta la arquitectura §2, que ponía todo el estado en vivo en memoria | Una sola fuente de verdad evita que memoria y base se contradigan; a esta escala la lectura cuesta poco. La interfaz sigue siendo el punto de cambio para varias réplicas |
| D-80 | 2026-10-10 | **Las notas del orador nunca viajan por el hub.** El presentador las pide por HTTP con su cookie de profesor; el estado que el hub envía a todos no las incluye. El estudiante tampoco recibe el código ni la asistencia | Un mensaje enviado al grupo equivocado no puede filtrar lo que no contiene |
| D-81 | 2026-10-10 | El límite de intentos al entrar con código es amplio: **300 por minuto por IP** | Un aula entera suele salir a internet con una sola IP y 40 estudiantes entran en el mismo minuto; aun así, adivinar un código de 8 dígitos a ese ritmo es inviable |
| D-82 | 2026-10-10 | Un cliente se conecta al hub con `?sessionId=…&role=…` y el servidor comprueba que sus cookies le den ese rol; si no, cierra la conexión. Las imágenes de una sesión las ve el profesor siempre, la ventana para compartir de esa sesión, y un estudiante admitido solo mientras la clase está en vivo | Concreta la arquitectura §3 y §5 |

## Propuestas en revisión

Decisiones planteadas por Claude que esperan confirmación de Esteban. Ver [pendientes.md](pendientes.md).

| # | Propuesta | Motivo |
|---|---|---|
| ~~P-01~~ | Aprobada → D-47 | |
| ~~P-02~~ | Aprobada → D-47 | |
| ~~P-03~~ | Aprobada → D-47 | |
| ~~P-04~~ | Aprobada → D-47 | |
| ~~P-05~~ | Aprobada → D-33 | |
| ~~P-06~~ | Aprobada → D-34 | |
| ~~P-07~~ | Aprobada → D-40 | |
| ~~P-08~~ | Aprobada → D-46 | |
| ~~P-09~~ | Aprobada → D-39. **Un solo modo, tres ventanas** (revisada el 9 oct, noche): presentador, ventana opcional para compartir o proyectar (sin notas) y estudiante. La ventana para compartir entra en la Fase 1. Roles de cliente `Presenter`, `Display`, `Participant` | En la referencia de Genially el profesor comparte pantalla en la videollamada; sin una ventana limpia, la clase vería sus notas. Ver [diseno.md](diseno.md) §13 |
| ~~P-10~~ | Aprobada → D-44 | |
| ~~P-12~~ | Aprobada → D-42 | |
| ~~P-13~~ | Aprobada → D-48 | |
| ~~P-11~~ | Aprobada → D-45 | |
