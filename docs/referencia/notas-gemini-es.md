> **Solo referencia.** Notas generadas con Gemini antes de este proyecto (correo de Esteban a sí mismo, 8 oct 2026, "Documentación Final del Proyecto: Aplicación de Presentaciones Interactivas"). No son decisiones: las decisiones vigentes están en `docs/decisiones.md`.

# Documentación Final del Proyecto: Aplicación de Presentaciones Interactivas

## 1. Propuesta de Valor
Transformar presentaciones tradicionales y estáticas en experiencias interactivas y dinámicas en tiempo real. Permite a profesores u oradores enganchar a la audiencia mediante participación activa, obteniendo métricas instantáneas sobre el nivel de comprensión sin interrumpir el flujo de la clase.

## 2. Características del MVP (Producto Mínimo Viable)
1. **Gestión e Importación de Presentaciones:**
   * Carga de archivos PPTX/PDF limpios o creados previamente.
   * Editor web minimalista integrado para crear o modificar diapositivas desde cero (texto, imágenes, colores, fuentes básicas y elementos de formato) manteniendo una baja carga de desarrollo.

2. **Control e Interacción del Presentador:**
   * Control manual de navegación entre diapositivas sincronizado en tiempo real.
   * Posibilidad de modular la frecuencia de las interacciones para evitar sobrecargar las diapositivas.

3. **Widgets Interactivos Iniciales:**
   * **Opción múltiple / Quizzes rápidos:** Para evaluar conocimientos al instante.
   * **Nubes de palabras (Word Clouds):** Para lluvias de ideas y recopilación de opiniones.
   * **Chat de Preguntas y Respuestas (Q&A):** Para canalizar las dudas de la audiencia.

4. **Analíticas en Tiempo Real:**
   * Panel con resultados agregados y métricas visuales de participación al instante.

## 3. Arquitectura Técnica e Integración con IA
* **Integración con IA:** Uso de APIs de modelos de lenguaje existentes para tareas como la generación automática de preguntas, sugerencias de quizzes o resumen de respuestas.
* **Comunicación en Tiempo Real:** Uso de WebSockets para sincronizar el estado de la diapositiva del profesor con todos los dispositivos de la audiencia de forma bidireccional y con baja latencia.
* **Estructura Backend:** Servicios/Microservicios ligeros encargados de la autenticación, la gestión de documentos/diapositivas y el control de la sesión en vivo.

## 4. Esquema de Base de Datos y Privacidad
* **Modelos Principales de Base de Datos:**
  * `Usuarios` (Profesores/Presentadores).
  * `Presentaciones` y `Diapositivas`.
  * `Widgets` (Tipo de widget, configuración, opciones).
  * `Sesiones` y `Respuestas`.

* **Privacidad y Anonimización de Datos:**
  * **Acceso Anónimo para Estudiantes:** La audiencia no requiere registro previo para unirse a una sesión (acceso mediante PIN/código de sesión).
  * **Métricas Agregadas:** Las respuestas a los quizzes y encuestas se procesan de forma anónima para proteger la identidad de los estudiantes y fomentar la participación sincera.

## 5. Decisiones de Diseño para Orador y Audiencia

### Para el Orador / Profesor:
* **Enfoque Pragmático y Minimalista:** Herramientas de edición sencillas orientadas a integrar interacción en lugar de competir con suites complejas como PowerPoint.
* **Control Total de la Sesión:** Capacidad de decidir cuándo activar o pausar una pregunta/widget durante la exposición.

### Para la Audiencia / Estudiante:
* **Interfaz Ligera y Enfocada:** Vista sincronizada exclusivamente con la diapositiva activa del orador.
* **Participación Directa:** Renderizado dinámico de los widgets de interacción.

## 6. Plan de Escalabilidad e Ideas Futuras
*(Registradas para etapas posteriores al MVP)*
1. **Personalización en Tiempo Real mediante IA:** Adaptación dinámica de los quizzes según el nivel de comprensión mostrado por los alumnos durante la sesión.
2. **Integración LMS:** Conexión con plataformas educativas como Canvas o Moodle para exportar calificaciones y métricas.
3. **Gamificación:** Tablas de clasificación (leaderboards) y mecánicas de puntos para aumentar el engagement.
