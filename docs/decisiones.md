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
