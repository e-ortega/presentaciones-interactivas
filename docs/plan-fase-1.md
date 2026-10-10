# Plan de implementación — Fase 1 (MVP)

_Aprobado por Esteban el 9 oct 2026 (D-40). Los cambios de fondo van a [decisiones.md](decisiones.md)._

**Objetivo:** que Esteban pueda dar una clase real con la app: importar su PPTX, presentar con su script y lanzar al menos un quiz que los estudiantes respondan desde el celular.

**Ventanas (D-39):** presentador, para compartir o proyectar, y estudiante. **Fuera de la Fase 1:** inicio de sesión del profesor (D-36), sugerencias de IA, editor desde cero, grupos y cuentas de estudiante, apodos, nube de palabras, Q&A, publicar PDF, analíticas, animaciones.

**Principio de orden:** primero lo más riesgoso. La importación del PPTX es la parte con más incertidumbre, así que va antes que cualquier pantalla bonita.

## Estructura propuesta

```
presentaciones-interactivas/
├── CLAUDE.md
├── README.md
├── docs/
├── src/
│   ├── Server/            ASP.NET Core: API, hub SignalR, sirve el build de React
│   │   ├── Domain/        Entidades y reglas
│   │   ├── Features/      Endpoints por funcionalidad (Import, Presentations, Sessions, Widgets, Join)
│   │   ├── Infrastructure/ EF Core, almacenamiento de archivos, renderizador LibreOffice
│   │   └── Realtime/      SessionHub (SignalR), estado en vivo y contratos
│   └── web/               React + Vite
├── tests/
│   └── Server.Tests/
├── Dockerfile
└── docker-compose.yml
```

Un solo proyecto de servidor con carpetas, no varios proyectos. Los detalles de implementación (contrato del hub, identidad, cola de importación, frontend, pruebas, CI) están en [arquitectura.md](arquitectura.md). La interfaz `ISlideRenderer` basta para mantener intercambiable la conversión del PPTX sin agregar capas.

## Hitos

### M0 — Esqueleto
_Estado: terminado el 10 oct 2026 (pull request #1)._

- Solución .NET, proyecto `Server`, proyecto de pruebas.
- React + Vite + TypeScript en `src/web`, con los textos en archivos de recursos (D-44) y los tokens de `src/web/src/styles/tokens.css`; en desarrollo, proxy de Vite hacia la API; en producción, el build se copia a `wwwroot`.
- Docker Compose: app + SQL Server.
- Zona del profesor protegida por la clave de acceso (D-45): middleware que compara el SHA-256 de la clave ingresada con `AdminAccess:KeySha256` en tiempo constante y deja una cookie. En local, Esteban carga el hash con `dotnet user-secrets`. La pantalla de React para ingresar la clave se hace en el M1, junto con la primera pantalla del profesor.
- EF Core con la primera migración (entidades de la Fase 1, ver diseño §10).
- Almacenamiento de archivos detrás de una interfaz: disco local en desarrollo, Blob Storage en Azure.
- GitHub Actions: compilar y probar backend y frontend en cada pull request.
- **Listo cuando:** `docker compose up` levanta todo, la página de React llama a un endpoint de salud y el CI pasa en verde.

### M1 — Importación (el riesgo principal)
- Endpoint para subir un PPTX.
- `LibreOfficeSlideRenderer : ISlideRenderer`:
  1. `soffice --headless --convert-to pdf` → PDF de estudiante.
  2. `pdftoppm -png` → una imagen por diapositiva (un `SlideStep` por diapositiva).
  3. Open XML SDK → notas del orador por diapositiva.
- Procesamiento en segundo plano con estado (`Pending`, `Processing`, `Ready`, `Failed`); la importación de un deck grande no debe bloquear la petición.
- **Trampas conocidas:**
  - **Diapositivas ocultas:** LibreOffice las omite del PDF, pero Open XML las sigue listando. Hay que saltar las ocultas (`show="0"`) al leer las notas, o las notas quedan desfasadas de las imágenes.
  - **Fuentes:** instalar en el contenedor fuentes métricamente compatibles con las de Office (Carlito para Calibri, Caladea para Cambria, Liberation para Arial/Times). Sin ellas, el texto se desacomoda.
  - LibreOffice no es seguro para conversiones simultáneas con el mismo perfil: una conversión a la vez, o un perfil temporal por conversión.
- Probar con decks reales de Esteban, incluyendo uno con diapositivas ocultas y uno con fuentes poco comunes.
- **Listo cuando:** un PPTX real queda con imágenes, notas alineadas y PDF.

### M2 — Vista del presentador (sin tiempo real)
- Diapositiva actual, columna de script y barra inferior (diseño §4).
- Script ocultable, modo teleprompter y modo notas estáticas.
- Navegación anterior/siguiente por **pasos**, también con teclado.
- **Listo cuando:** Esteban puede recorrer su presentación con sus notas en pantalla.

### M3 — Sesión en vivo
- Crear sesión: código tipo `8888-6666`, enlace y QR.
- `SessionHub` (SignalR): grupo por sesión; cada cliente se une con un rol (`Presenter`, `Display`, `Participant`); el profesor envía `(slideIndex, stepIndex)` y el hub lo emite al grupo de la sesión.
- Vista del estudiante adaptable, primero computadora (D-35; maqueta en el canvas de identidad visual).
- **Ventana para compartir o proyectar** (`Display`): sala de espera con código y QR, diapositiva a pantalla completa y resultados revelados; sin notas ni controles. El presentador la abre con un botón en una ventana aparte.
- Entrada del estudiante: código → nombre + correo + aviso de consentimiento (Ley 8968, texto en `docs/legal/aviso-privacidad.md`, casilla sin marcar por defecto) → lobby. Crea o reutiliza el `Participant` por correo y registra la `Attendance` con la fecha del consentimiento.
- Lobby del profesor: contador de conectados, admitir o expulsar, iniciar.
- Reconexión: si el celular pierde la conexión, al volver recibe el estado actual de la sesión.
- **Listo cuando:** varios celulares siguen la presentación sincronizados.

### M4 — Quiz de opción múltiple
- Crear y editar un quiz en una diapositiva (pregunta, opciones, opción correcta opcional).
- Lanzar: se crea un `WidgetLaunch` con el snapshot de la pregunta; los estudiantes ven los controles.
- Respuestas en vivo en el panel del profesor; una respuesta por participante y lanzamiento.
- Cerrar y decidir si se revelan los resultados.
- **Listo cuando:** una clase de prueba completa un quiz y el profesor ve el gráfico llenándose.

### M5 — Despliegue
- Dockerfile con .NET, LibreOffice, poppler-utils y las fuentes.
- Clave de acceso de la zona del profesor (D-45): cargar el hash como secreto de Container Apps (`AdminAccess__KeySha256`).
- Imagen en GitHub Container Registry; Azure Container Apps (Consumo, una réplica máxima); Azure SQL Database (oferta gratuita); Blob Storage.
- Alerta sobre la métrica "Free amount remaining" de la base.
- **Listo cuando:** la app funciona en Azure desde un celular fuera de la red local.

### M6 — Prueba en clase
- Usarla en una clase real de Esteban y anotar lo que falló o sobró. Esto define el inicio de la Fase 2.
