# Documento de diseño — Presentaciones Interactivas

_Autores: Esteban Ortega y Claude · Iniciado: 8 oct 2026 · Última actualización: 9 oct 2026 (noche)_

Las decisiones individuales, con su fecha y motivo, están en [decisiones.md](decisiones.md). El plan de implementación del MVP está en [plan-fase-1.md](plan-fase-1.md). Este documento describe el sistema como un todo.

---

## 1. Visión

Una app web que convierte una presentación de PowerPoint que el profesor **ya tiene** en una experiencia en vivo e interactiva para sus estudiantes. El profesor no reconstruye nada: importa su deck y la app le agrega interactividad encima.

**Diferenciador:** "trae tu propio deck". Herramientas como Mentimeter obligan a construir la presentación dentro de su editor. Aquí el punto de partida es el archivo que el profesor ya usa.

**Momento "wow":** la interacción en vivo en el aula. Es el gancho del producto. Las analíticas de participación y comprensión son el valor que, a futuro, haría pagar a una institución.

**Filosofía:** la app ayuda, el profesor decide. La IA sugiere y redacta borradores, pero nunca agrega nada por su cuenta. Las opciones de privacidad y acceso las configura el profesor según el contexto.

## 2. Usuarios y contextos

| Contexto | Ejemplo | Acceso de estudiantes | Identidad |
|---|---|---|---|
| **Curso** | Una materia de un cuatrimestre, mismas personas cada semana | Cuenta + inscripción al grupo con código | Nombre real, trazabilidad completa |
| **Evento suelto** | Webinar, charla, taller de un día | Código de sesión + nombre y correo, sin cuenta | Nombre real o apodo, según el profesor |

## 3. Flujo del profesor

1. **Importar** el PPTX (o, en fase posterior, crear desde cero con un editor mínimo).
2. La app **procesa** el archivo: genera una imagen por diapositiva, extrae las notas del orador y genera el PDF de estudiante.
3. *(Fase 2)* La **IA sugiere** un puñado de interacciones, presentadas como una lista corta ("4 ideas para tu deck"). Cada sugerencia trae el contenido ya redactado (pregunta y opciones), editable. El profesor acepta, edita o descarta.
4. El profesor agrega o ajusta **widgets** a mano en las diapositivas que quiera.
5. **Genera una sesión**: obtiene enlace, código y QR.
6. **Presenta** desde la vista del presentador.
7. Al terminar, puede **publicar el PDF** para que los estudiantes lo descarguen.

Reutilizar la presentación con otro grupo o en otro cuatrimestre es simplemente **otra sesión**. El profesor puede relanzar la presentación tal como está (con sus widgets) o crear una nueva a partir del mismo PPTX. Si una clase continúa la anterior, el profesor avanza hasta la diapositiva necesaria; la app le ofrece empezar donde terminó la sesión previa.

## 4. Vista del presentador

El centro de mando durante la clase. **Principio rector:** la carga cognitiva del profesor ya está al máximo, así que todo debe verse de un vistazo y estar a un toque. Sin menús.

```
┌───────────────────────────────────────────┬─────────────────────┐
│                                           │  SCRIPT (notas)     │
│                                           │                     │
│         DIAPOSITIVA ACTUAL                │  Notas de esta      │
│   (exactamente lo que ven los             │  diapositiva, en    │
│    estudiantes)                           │  letra legible      │
│                                           │                     │
│                                           │  ┄ notas de la      │
│                                           │    siguiente, tenue │
├───────────────────────────────────────────┴─────────────────────┤
│  ◀  ▶   [ Lanzar quiz ]                     PIN 8888-6666  [QR] │
└─────────────────────────────────────────────────────────────────┘
```

- **Diapositiva actual** (zona principal): lo mismo que se proyecta.
- **Script** (columna derecha): notas del orador del PPTX. Se puede **ocultar** para dar más espacio a la diapositiva. Dos modos:
  - **Teleprompter:** sigue la diapositiva automáticamente.
  - **Notas estáticas:** un panel fijo de consulta que no se mueve.
- **Barra inferior:** anterior/siguiente y los botones de los widgets de esa diapositiva. Al lanzar uno, los resultados aparecen en un panel (sobre la columna del script) con el gráfico llenándose en tiempo real.
- **PIN y QR:** siempre visibles, pequeños, para que quien llegue tarde pueda unirse.

> Referencia existente: `repos/teleprompter` es un teleprompter HTML independiente (velocidad, tamaño de letra, resaltado de palabra). Puede servir de base para el modo teleprompter.

## 5. Vista del estudiante

- **Adaptable a computadora y celular** (D-35). Se espera que la mayoría de los estudiantes entre **desde computadora**, así que se diseña primero para pantalla ancha (diapositiva grande y la pregunta al lado) y se apila en el celular (diapositiva arriba, pregunta abajo).
- **Amarrada** a la del profesor: ve exactamente la diapositiva y el paso que se proyectan. No hay navegación libre.
- Cuando el profesor lanza un widget, aparecen los controles para responder sobre la diapositiva.
- Ve los resultados solo si el profesor decide revelarlos.
- Al final, si el profesor lo publica, puede descargar el **PDF de estudiante**: el PPTX original convertido a PDF, sin notas ni widgets.

## 6. Sesión en vivo

```
LOBBY ──▶ EN VIVO ──▶ FINALIZADA
            │  ▲
            ▼  │
     INTERACCIÓN ACTIVA
```

| Estado | Profesor | Estudiante |
|---|---|---|
| **Lobby** | Proyecta código/QR, ve conectados, puede admitir o expulsar | "Esperando a que empiece" |
| **En vivo** | Avanza diapositivas y pasos | Ve la diapositiva sincronizada |
| **Interacción activa** | Lanza widget, ve respuestas en vivo, cierra y decide si revela resultados | Responde |
| **Finalizada** | Puede publicar el PDF | Nadie más entra; puede descargar el PDF si se publicó |

## 7. Identidad y acceso de estudiantes

- **El correo es la identidad**, tenga o no cuenta el estudiante. Si alguien entra varias veces por código con el mismo correo y después crea cuenta, su historial previo se une a la cuenta.
- **Entrada por código** (eventos y Fase 1): sitio + código tipo `8888-6666`, luego un formulario de nombre y correo con el **aviso de consentimiento de la Ley 8968**. Ahí mismo se ofrece crear cuenta, sin que sea requisito.
- **Cursos:** el profesor crea el **grupo**, que tiene su propio código de inscripción. Los estudiantes crean cuenta (y con ello se verifica el correo) y se unen una sola vez. Después, las presentaciones se agregan al grupo y cada sesión les aparece sin códigos ni formularios.
- **Apodos:** configuración del profesor por sesión. Tienen sentido en un webinar; en un curso con trazabilidad se usa el nombre real.
- **Verificación de correo:** no se hace por sesión. En cursos, la confianza se establece al crear la cuenta.

### Ley 8968 (Protección de la Persona frente al Tratamiento de sus Datos Personales)

El formulario de nombre y correo debe incluir un aviso de consentimiento informado: qué datos se recogen, para qué, quién es el responsable y cómo ejercer los derechos de acceso, rectificación y eliminación. El texto final debe revisarse con criterio legal antes de usar la app con estudiantes reales.

## 8. Widgets

| Widget | Fase |
|---|---|
| Quiz de opción múltiple | **1** |
| Nube de palabras | 3 |
| Preguntas y respuestas (Q&A) | 3 |

Separación clave:

- **Widget** = el diseño (la pregunta y sus opciones). Vive en una diapositiva y se puede editar en cualquier momento.
- **Lanzamiento** = ese widget usado en una sesión concreta. Al lanzarse, **guarda una copia exacta** de la pregunta y las opciones tal como se mostraron.

Así el profesor puede editar un quiz sin miedo: las respuestas viejas siempre quedan ligadas a la pregunta que los estudiantes realmente vieron, y las analíticas nunca mezclan dos versiones.

## 9. Importación y animaciones

### Pipeline de importación (v1)

1. Subir PPTX → guardar el original en Blob Storage.
2. **LibreOffice** (sin interfaz, en contenedor) convierte PPTX → **PDF**. Ese PDF es el de estudiante.
3. El PDF se parte en **una imagen por diapositiva**.
4. El **Open XML SDK** (.NET) extrae las notas del orador de cada diapositiva.
5. Se crean los registros de Presentación, Diapositivas y Pasos.

Limitaciones conocidas de la v1: se pierden las animaciones, y algunas fuentes poco comunes pueden verse distintas si no están instaladas en el contenedor.

### Preparación para animaciones (sin reconstruir después)

1. **La navegación es por "diapositiva + paso".** El profesor avanza por pasos y la sincronización envía `(diapositiva, paso)`. En la v1 cada diapositiva tiene un solo paso.
2. **La conversión vive detrás de una interfaz intercambiable** (`IRenderizadorPresentacion`; en el código, `ISlideRenderer`), que recibe el PPTX y devuelve diapositivas con sus pasos (imágenes) y notas.

Implementaciones futuras posibles:

- **Imágenes por paso** (recomendada): leer la secuencia de animaciones con Open XML, generar variantes ocultando lo que aún no aparece y renderizar cada estado. Conserva la aparición progresiva de viñetas e imágenes, que es lo pedagógicamente importante; pierde el movimiento en sí.
- **Fidelidad total** con una librería comercial (por ejemplo, Aspose.Slides). Solo si los profesores lo reclaman, por el costo de licencia.

## 10. Modelo de datos

Fase indicada entre corchetes. Los campos son orientativos.

| Entidad | Campos principales | Fase |
|---|---|---|
| **Usuario** | Id, Correo, Nombre, Rol (Profesor/Estudiante), CreadoEn | 1 (solo profesor) |
| **Participante** | Id, Correo (único), Nombre, UsuarioId? | 1 |
| **Grupo** | Id, ProfesorId, Nombre, CodigoInscripcion | 3 |
| **MiembroGrupo** | GrupoId, ParticipanteId, UnidoEn | 3 |
| **Presentacion** | Id, ProfesorId, GrupoId?, Titulo, Origen (Importada/Desde cero), RutaPptx, RutaPdf, EstadoProcesamiento | 1 |
| **Diapositiva** | Id, PresentacionId, Orden, NotasOrador | 1 |
| **Paso** | Id, DiapositivaId, Orden, RutaImagen | 1 (uno por diapositiva) |
| **Widget** | Id, DiapositivaId, Tipo, ConfigJson, Origen (Manual/IA), EstadoSugerencia (Pendiente/Aceptada/Editada/Descartada) | 1 (Origen IA en fase 2) |
| **Sesion** | Id, PresentacionId, GrupoId?, Codigo, Estado (Lobby/EnVivo/Finalizada), DiapositivaActual, PasoActual, PermiteApodos, PdfPublicado, IniciadaEn, FinalizadaEn | 1 |
| **Asistencia** | Id, SesionId, ParticipanteId, NombreMostrado, ConsentimientoEn, Admitido, EntroEn | 1 |
| **Lanzamiento** | Id, SesionId, WidgetId, SnapshotJson, AbiertoEn, CerradoEn, ResultadosRevelados | 1 |
| **Respuesta** | Id, LanzamientoId, AsistenciaId, PayloadJson, EnviadaEn — única por (Lanzamiento, Asistencia) | 1 |

Relaciones principales: una Presentación tiene muchas Diapositivas, y cada una tiene uno o más Pasos y cero o más Widgets. Una Presentación tiene muchas Sesiones. Cada Sesión tiene Asistencias y Lanzamientos, y cada Lanzamiento tiene Respuestas.

## 11. Arquitectura técnica

Stack: **ASP.NET Core + SignalR, SQL Server, Azure, y React + Vite en el frontend.** Principio: simple y económico mientras se valida la idea.

```
 Navegador profesor ─┐                       ┌─▶ Azure SQL Database (free offer)
                     ├─ HTTPS / WebSocket ─▶ │
 Celulares alumnos ──┘     (SignalR)         │   App ASP.NET Core (1 contenedor)
                                             │   · API + build de React
                                             │   · Hub SignalR en memoria
                                             │   · LibreOffice para importar
                                             └─▶ Azure Blob Storage (PPTX, imágenes, PDF)
```

- **Frontend React + Vite** (D-31): en desarrollo corre con el servidor de Vite y un proxy hacia la API; en producción, el build se sirve como archivos estáticos desde la misma app. Mismo origen: sin CORS y la cookie de autenticación funciona sin configuración extra. Los clientes usan `@microsoft/signalr`.
- **Un solo contenedor** con la app, el hub de SignalR y LibreOffice. Una sola réplica, así que los grupos de SignalR en memoria funcionan sin backplane.
- **Desarrollo local:** Docker Compose con la app y un contenedor de SQL Server. Costo cero.

### Recursos de Azure

| Recurso | Para qué | Costo esperado en MVP |
|---|---|---|
| **Azure Container Apps** (plan Consumo) | La app completa. Escala a cero cuando no se usa | Dentro de la concesión gratuita mensual por suscripción: 180.000 vCPU-segundos, 360.000 GiB-segundos y 2 millones de solicitudes |
| **Azure SQL Database** (oferta gratuita, serverless) | Base de datos | Gratis: 100.000 vCore-segundos y 32 GB por base al mes, con pausa automática al agotarse |
| **Azure Blob Storage** | PPTX originales, imágenes de pasos, PDF de estudiante | Centavos al mes con este volumen |
| **Registro de contenedores** | Guardar la imagen Docker | GitHub Container Registry (gratis) en lugar de Azure Container Registry |

Con 1 vCPU y 2 GiB, la concesión gratuita equivale a unas 50 horas al mes de app activa, y el tiempo inactivo se cobra a tarifa reducida. Alcanza para probar con clases reales.

**Lo que no se usa en el MVP, y por qué:**

- **Azure SignalR Service:** el plan gratuito permite solo 20 conexiones simultáneas, menos que una clase. SignalR corre dentro de la app. Cuando haga falta más de una réplica, se agrega el plan Standard (1.000 conexiones por unidad) o un backplane de Redis, sin cambiar el código de los hubs.
- **App Service:** el plan gratuito es muy limitado y el básico cobra todo el mes, se use o no.

**Precauciones con el modo económico:**

- **Arranque en frío:** si la app escaló a cero y la base se pausó, el primer acceso puede tardar hasta un minuto. El profesor abre la app unos minutos antes de clase. Si molesta, se fija una réplica mínima durante el horario de clases.
- **La base serverless no se pausa con conexiones abiertas.** Cerrar SSMS y herramientas al terminar, y configurar una alerta sobre la métrica "Free amount remaining".

### Fases posteriores

- **Fase 2:** API de un LLM para sugerencias y borradores de widgets. El insumo es el texto de cada diapositiva y sus notas del orador.
- **Fase 3:** servicio de correo (por ejemplo, Azure Communication Services) para cuentas e invitaciones.

## 12. Fases

| Fase | Objetivo | Incluye |
|---|---|---|
| **1 — MVP** | Probarlo en una clase real | Importar PPTX (imágenes + notas + PDF), vista del presentador con script, quiz de opción múltiple, sesión con entrada por código + nombre + correo + consentimiento, sincronización por diapositiva + paso |
| **2 — El valor** | Lo que diferencia al producto | Sugerencias de la IA con borrador editable, experiencia del estudiante pulida |
| **3 — Crecer** | Uso continuo en cursos | Grupos con cuentas, apodos, nube de palabras, Q&A, publicar PDF, editor desde cero, analíticas |
| **Después** | | Animaciones (imágenes por paso), modo en aula con vista de proyección (§13), integración con LMS, gamificación, interfaz en inglés |

## 13. Modos de uso (en discusión)

El diseño original asume **un modo virtual**: el profesor usa una sola pantalla (la vista del presentador) y cada estudiante ve la misma diapositiva en su propia computadora o celular. Al diseñar la identidad visual apareció un segundo uso posible, **en aula**, con un proyector. Esteban no lo había contemplado y le parece buena idea; queda por decidir si se incluye y cuándo.

| | **Virtual** (uso principal) | **En aula** |
|---|---|---|
| Dónde están | Cada uno en su lugar | En el mismo salón |
| Pantallas del profesor | Una: vista del presentador | Laptop (presentador) + proyector |
| Estudiante | Computadora (mayoría) o celular; ve diapositiva + pregunta | Celular; la diapositiva la ve en el proyector, en el celular basta la pregunta |
| Voz del profesor | Videollamada aparte (Zoom, Teams, Meet) | En persona |
| Resultados revelados | En la pantalla de cada estudiante | En el proyector |
| Vista extra | Ninguna | **Vista de proyección**: sala de espera con código, diapositiva a pantalla completa, resultados |

**Qué comparten:** prácticamente todo el backend: sesión, código, sincronización `(diapositiva, paso)`, widgets, lanzamientos, respuestas y analíticas.

**Qué cambia:** solo vistas. El modo en aula agrega la vista de proyección y permite una vista de estudiante más compacta.

**Propuesta (P-09):**

1. **Fase 1 solo con el modo virtual**, que es el uso principal.
2. **Dejarlo preparado sin costo**, igual que con las animaciones: cada cliente se conecta a la sesión con un **rol** (`Presenter`, `Participant` y, a futuro, `Display`); el hub emite los eventos al grupo de la sesión y no a "los estudiantes"; revelar resultados es un evento de sesión que cada vista dibuja a su manera. Así, el modo en aula después es agregar una vista y una opción al crear la sesión, sin tocar el backend.
3. Las maquetas de sala de espera y resultados proyectados ya existen y se reutilizan cuando llegue ese modo.

**Por confirmar del modo virtual:** que la voz va por una videollamada en paralelo. Si los estudiantes siguen la diapositiva en la app, el profesor no necesita compartir pantalla en la llamada.
