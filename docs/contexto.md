# Contexto: cómo se llegó a este diseño

_Resumen de las conversaciones entre Esteban y Claude del 8 y 9 de octubre de 2026, para poder continuar fuera de ese chat._

## Origen

Esteban venía desarrollando la idea con Gemini y se envió dos correos con esas notas (8 oct 2026). Están copiadas en [referencia/](referencia/). Decidió **no tomarlas como base**, sino diseñar de nuevo con decisiones propias, usándolas solo como referencia. Diferencias importantes con esas notas: aquellas proponían PostgreSQL, microservicios y acceso totalmente anónimo; aquí se eligió SQL Server, una sola app y una identidad basada en el correo.

## La idea en palabras de Esteban

Un profesor ya tiene su PowerPoint. Lo pasa por la app y lo presenta de una forma más divertida e interactiva. Puede agregar "burbujas" con preguntas que los estudiantes contestan. Todas esas interacciones se recogen y se analizan para saber cómo va el curso: si están aprendiendo y si participan. Las notas que él escribe en PowerPoint para sí mismo deberían aparecerle como guion mientras presenta.

## Cómo se tomaron las decisiones clave

- **El "wow" es el aula en vivo**, no las sugerencias de IA. Se preguntó qué momento haría que un profesor diga "necesito esto" la primera vez, y Esteban eligió la interacción en vivo.
- **La IA no debe ser intrusiva.** Esteban insistió en confiar en el instinto del profesor: pocas sugerencias, en una lista que se revisa una vez, con el contenido ya redactado pero editable.
- **Notas del orador:** idea de Esteban. Luego agregó que el script se pueda ocultar y que haya un modo de notas fijas, no solo teleprompter.
- **Acceso de estudiantes:** Esteban distinguió dos contextos. En un webinar puede ser semi-anónimo (apodos opcionales). En un curso con las mismas personas cada semana se necesita trazabilidad con nombre real. Claude señaló que un correo sin verificar permite suplantar a un compañero; Esteban propuso resolverlo con grupos: cuenta + inscripción con código una sola vez, en vez de verificar en cada sesión.
- **Reutilización:** usar el mismo PPTX otra vez es otra sesión; para retomar una clase, se avanza a la diapositiva necesaria.
- **Animaciones:** Esteban casi no las usa, pero cree que otros profesores sí. Aceptó dejarlas fuera de la v1 **solo si agregarlas después no obliga a reconstruir**. De ahí salen la navegación por "diapositiva + paso" y el renderizador intercambiable.
- **Recorte del MVP:** Claude advirtió que el diseño completo tenía demasiados frentes. Esteban coincidió: Fase 1 = importar, notas, un widget y entrada mínima por código. Lo que más valor da, la IA y la experiencia del estudiante, va en la Fase 2.
- **Costo:** Esteban pidió mantenerlo simple y económico. De ahí la elección de Container Apps con escala a cero y la oferta gratuita de Azure SQL.
- **Frontend en React:** Esteban lo eligió porque lo está aprendiendo; el proyecto también le sirve para eso.

## Alternativas descartadas

| Alternativa | Por qué no |
|---|---|
| Parsear el PPTX a elementos editables en v1 | Mucho más complejo y rompe el formato del profesor |
| Que la IA coloque widgets automáticamente | Contradice el principio de que decide el profesor |
| Navegación libre del estudiante | Distrae; se compensa con el PDF al final |
| Verificar el correo en cada sesión | Fricción en cada clase; los grupos con cuenta lo resuelven mejor |
| Azure SignalR Service (plan gratuito) | Solo 20 conexiones simultáneas, menos que una clase |
| App Service | El plan gratuito es muy limitado y el básico cobra aunque no se use |
| Blazor Server para el frontend | La vista del estudiante en celulares con wifi inestable pierde el circuito |
| Microservicios (propuesta de las notas de Gemini) | Innecesario para un MVP de una persona |
