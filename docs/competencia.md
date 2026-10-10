# Competencia, mercado y monetización

_Investigación de Claude, 9 oct 2026. Precios en USD tomados de las páginas oficiales o de agregadores, según se indica; pueden haber cambiado. Verificar antes de usarlos en una propuesta comercial._

## Resumen

El mercado de presentaciones interactivas está **lleno y maduro**. Nuestras dos ideas centrales ya existen por separado: **importar el PowerPoint** (Mentimeter, Pear Deck, ClassPoint) y **generar preguntas con IA a partir de las diapositivas** (ClassPoint AI). Competir en funciones contra ellos no tiene sentido.

Donde sí hay espacio es en **para quién y cómo**: profesores **universitarios hispanohablantes** de Costa Rica y Centroamérica, en clases **virtuales o híbridas**, con un producto que se usa desde el navegador sin instalar nada, que aprovecha las **notas del orador** como guion y como insumo de la IA, y que se vende con **precio, soporte, facturación y cumplimiento legal locales**.

## Los competidores

| Producto | Qué es | Trae tu PowerPoint | IA sobre tus diapositivas | Precio individual | Enfoque |
|---|---|---|---|---|---|
| **ClassPoint** | Complemento que se instala **dentro de PowerPoint** | Sí, trabajás en tu propio PowerPoint | **Sí**: genera preguntas a partir del contenido de la diapositiva | Gratis hasta 25 participantes; Pro USD 8/mes (96/año) hasta 200 | Profesores que ya presentan con PowerPoint |
| **Mentimeter** | Presentaciones con encuestas y quizzes | Importa PowerPoint, Keynote o PDF desde el plan Basic | Funciones de IA propias | Gratis con 50 participantes/mes; Basic educación ≈ USD 11/mes, Pro educación ≈ USD 17/mes (anual) | Educación y empresas |
| **Pear Deck** | Complemento para Google Slides y PowerPoint Online | Sí, sobre tus diapositivas | Plantillas y ayudas | Premium USD 149/año con descuento educativo | Escuelas K-12 de EE. UU. (GoGuardian) |
| **Nearpod** | Lecciones interactivas | Complemento de Google Slides en planes de pago | Contenido listo | Gratis hasta 40 estudiantes; Gold USD 159/año; Platinum USD 397/año | Escuelas K-12 de EE. UU. |
| **Wooclap** | Preguntas interactivas para clases | Integración con PowerPoint y Google Slides; no importa el archivo | Funciones de IA | Gratis con 5 preguntas/mes y participantes ilimitados; planes académicos por cotización | **Universidades**, fuerte en Europa |
| **Genially** | Contenido interactivo con modo en vivo (play.genially.com) | Creás el contenido en Genially | Funciones de IA | No verificado | Educación en español; muy usado en Latinoamérica |
| **Slido** | Encuestas y preguntas para eventos y reuniones | Integración con PowerPoint y Google Slides | Funciones de IA | No verificado | Empresas y eventos (Cisco/Webex) |
| **Kahoot!** | Quizzes gamificados | Puede sincronizar diapositivas | Funciones de IA | No verificado | Juegos de repaso, escuelas |

**El competidor más directo es ClassPoint:** hace "tu PowerPoint + quizzes + IA que lee tus diapositivas" a USD 8/mes. Su límite es que vive dentro de PowerPoint instalado: el profesor necesita la aplicación de escritorio y presentar desde ella. Verificar su compatibilidad con Mac antes de usarlo como argumento.

## Dónde podemos destacar

Ordenado de más a menos defendible:

1. **Mercado local que los grandes atienden mal.** Universidades de Costa Rica y Centroamérica: interfaz y soporte en español de la región, precio en colones accesible para un profesor, **factura electrónica costarricense** (las instituciones la exigen para comprar), cumplimiento de la **Ley 8968** documentado y una persona a quien llamar. Ninguno de los grandes compite en eso.
2. **Las notas del orador como corazón del producto.** El guion para el profesor y, en la Fase 2, la IA que sugiere preguntas leyendo **lo que el profesor quiere enseñar** (sus notas), no solo el texto de la diapositiva. Nadie de los revisados lo presenta así.
3. **Clase virtual con videollamada como caso principal.** La ventana limpia para compartir, el guion privado y la entrada desde la computadora del estudiante. Varios competidores nacieron para el aula física o el K-12 de EE. UU.
4. **Sin instalar nada.** Funciona en el navegador con cualquier PPTX, en Windows, Mac o Chromebook, frente al complemento de escritorio de ClassPoint.
5. **Analítica por curso para la universidad** (Fase 3): comprensión por tema a lo largo del cuatrimestre, útil para el profesor y para la coordinación académica. Es lo que justifica una licencia institucional.

Lo que **no** es diferenciador por sí solo: importar PowerPoint, tener quizzes, nubes de palabras o Q&A, o "usar IA".

## Mercado

- Los informes de mercado de "audience response software" (por ejemplo, Transparency Market Research, horizonte 2025–2035) ubican la **educación** y la **evaluación en clase** entre los segmentos principales y la **gamificación** como tendencia, pero las cifras están detrás de un pago y su calidad es irregular. No conviene basar decisiones en esos números.
- Más útil es dimensionar de abajo hacia arriba el mercado inicial: cantidad de universidades en Costa Rica y Centroamérica, profesores por universidad y porcentaje de clases virtuales o híbridas. **Pendiente** si se quiere armar un plan de negocio.
- **Validación barata:** usar la Fase 1 en los cursos de Esteban en la Universidad Latina y mostrarla a 10 colegas. La pregunta clave: ¿la usarían cada semana y cuánto pagarían ellos o su universidad?

## Monetización

| Modelo | Cómo | Comentario |
|---|---|---|
| **Freemium por profesor** | Gratis con límites (participantes por sesión, sugerencias de IA al mes); Pro mensual | Referencia: ClassPoint USD 8/mes, Mentimeter educación USD 11/mes. Para Centroamérica, un precio menor en colones. La IA tiene costo por uso: **limitarla en el plan gratis** |
| **Licencia institucional** (principal) | Contrato anual por universidad o facultad: profesores ilimitados o por bloque, analítica por curso, integración con Moodle, soporte y factura local | Es donde está el ingreso estable. Wooclap y Mentimeter cotizan así sus planes de campus |
| **Capacitación y talleres** | Empresas y formadores que hacen webinars | Mercado de Slido y Mentimeter; dejar para después |
| **Publicidad** | | **Descartado**: ambiente educativo y datos de estudiantes |

**Costos que crecen con el uso:** la infraestructura de Azure es casi cero al principio (ver diseño §11). Lo que escala es la IA de la Fase 2: cada presentación analizada consume tokens del modelo de lenguaje. Medir el costo real por presentación en la Fase 2 antes de fijar precios.

## Recomendación

1. Seguir con la Fase 1 tal como está: es barata y sirve para validar con clases reales.
2. Enfocar el posicionamiento en **"tu PowerPoint y tus notas, en una clase virtual en vivo, hecho para universidades de la región"**.
3. Antes de la Fase 2, probar ClassPoint y Mentimeter un par de semanas para conocer de primera mano qué hacen bien y qué les falta.
4. Validar disposición de pago con 10 profesores y al menos una coordinación académica antes de construir la licencia institucional.

## Fuentes

- ClassPoint, precios: https://www.classpoint.io/pricing
- ClassPoint AI (genera preguntas desde las diapositivas): https://futuretools.io/tools/classpoint-ai
- Mentimeter, planes: https://www.mentimeter.com/plans ; precios de educación (PricingSaaS, visto el 19 jun 2026): https://pricingsaas.com/companies/mentimeter
- Pear Deck, precios: https://www.peardeck.com/pricing ; complemento para PowerPoint Online: https://help.peardeck.com/en/migrated/get-the-pear-deck-for-powerpoint-online-add-in
- Nearpod, precios: https://nearpod.com/pricing.md
- Wooclap, precios: https://www.wooclap.com/en/pricing/
- Informe de mercado (sin cifras públicas): https://www.transparencymarketresearch.com/audience-response-software-market.html
- Captura de Genially en vivo compartida por Esteban (9 oct 2026)
