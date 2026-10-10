# Aviso de privacidad y consentimiento (Ley 8968)

_Borrador preparado por Claude el 9 oct 2026 a partir del texto de la Ley 8968, artículos 5, 7 y 14. **No es asesoría legal**: debe revisarlo una persona abogada en Costa Rica antes de usarlo con estudiantes reales. Los textos entre corchetes son datos por completar._

## Notas para la implementación

- El artículo 5 exige informar, **antes** de pedir los datos y de forma claramente legible: (a) que existe una base de datos, (b) para qué se recogen, (c) quién los recibe y quién puede consultarlos, (d) si las respuestas son obligatorias u opcionales, (e) cómo se tratarán, (f) qué pasa si la persona no los da, (g) sus derechos y (h) la identidad y dirección del responsable.
- El consentimiento debe ser **expreso y constar por escrito**, en formato físico o electrónico. En la app: una casilla **sin marcar por defecto** y obligatoria para entrar, y se guarda la fecha y hora de aceptación junto con la versión del aviso (`Attendance.ConsentAt` y `ConsentVersion`).
- Se puede **revocar** por el mismo medio, sin efecto retroactivo.
- El artículo 7 obliga a responder las solicitudes de acceso, rectificación o supresión **gratis y en 5 días hábiles**.
- El artículo 14 exige autorización expresa para **transferir** datos. Como los datos se guardan en servidores de Microsoft Azure, posiblemente fuera de Costa Rica, el aviso lo dice explícitamente y el consentimiento lo cubre.
- **Menores de edad:** la ley no trae una regla específica en los artículos revisados. Si la app se usa en colegios, pedir criterio legal sobre el consentimiento de padres o encargados antes de hacerlo.
- **Inscripción ante la PRODHAB:** existe un registro de bases de datos con costo anual. Consultar con la persona abogada si aplica a esta plataforma y en qué etapa (por ejemplo, al vender licencias a instituciones).
- **Cuando una universidad compre la licencia,** probablemente la universidad sea la responsable de los datos de sus estudiantes y la plataforma, la encargada de tratarlos. El aviso y un contrato de tratamiento de datos tendrán que ajustarse a ese caso.
- **Compromiso de diseño:** los datos personales de los estudiantes (nombre, correo, respuestas) **no se envían a servicios de IA**. La IA de la Fase 2 solo procesa el contenido de la presentación del profesor.

---

## 1. Texto corto en el formulario de entrada

Va justo encima de los campos de nombre y correo, en letra legible (no más pequeña que el texto base):

> **Antes de unirte:** para registrar tu participación en esta clase guardamos tu nombre, tu correo y tus respuestas. Tu nombre lo verán tu profesor o profesora y las demás personas de la sesión, como en una videollamada; tu correo y tus respuestas, solo tu profesor o profesora. Se guardan en servidores de Microsoft Azure, que pueden estar fuera de Costa Rica. Sin estos datos no podés unirte a la sesión. Podés consultarlos, corregirlos o pedir que los borremos escribiendo a [CORREO DE CONTACTO]. Detalles en el [Aviso de privacidad](#2-aviso-de-privacidad-completo).

Casilla obligatoria, sin marcar por defecto:

> ☐ Acepto que [NOMBRE DEL RESPONSABLE] trate mis datos como se describe arriba y en el Aviso de privacidad, incluido su almacenamiento en servidores fuera de Costa Rica.

Botón: **Unirme a la clase** (deshabilitado hasta marcar la casilla).

Si la sesión permite apodos, se agrega debajo del campo de nombre: _"En esta sesión podés usar un apodo en lugar de tu nombre real."_

---

## 2. Aviso de privacidad completo

_Versión 1.0, [FECHA DE PUBLICACIÓN]_

### ¿Quién es responsable de tus datos?

[NOMBRE DEL RESPONSABLE], cédula [NÚMERO], con domicilio en [DIRECCIÓN], Costa Rica. Contacto: [CORREO DE CONTACTO].

### ¿Existe una base de datos?

Sí. Los datos que nos das se guardan en la base de datos de [NOMBRE DEL PRODUCTO], una plataforma para participar en clases y presentaciones en vivo.

### ¿Qué datos recogemos?

- **Nombre** (o apodo, si la sesión lo permite) y **correo electrónico**, que escribís al unirte.
- **Tus respuestas** a las preguntas de la sesión y la hora en que las enviaste.
- **Tu asistencia**: a qué sesiones te uniste y cuándo.
- La **fecha y hora** en que aceptaste este aviso.

No pedimos otros datos personales, no usamos tu ubicación y no te pedimos acceso a tu cámara ni a tu micrófono.

### ¿Para qué los usamos?

- Registrar tu participación y tus respuestas en la sesión.
- Mostrarle a tu profesor o profesora quién participó y cómo respondió el grupo, para que pueda ajustar la clase.
- Si tenés cuenta o volvés a entrar con el mismo correo, unir tu historial de participación.

**No** los usamos para publicidad, **no** los vendemos y **no** los usamos para entrenar modelos de inteligencia artificial.

### ¿Quién puede verlos?

- El profesor, profesora o persona que dirige la sesión a la que te uniste.
- [Si aplica: la institución educativa que contrató la plataforma, solo para los cursos de esa institución.]
- Microsoft Azure, como proveedor que aloja la plataforma, sin acceso para usar los datos con fines propios.

Las demás personas de la clase pueden ver tu **nombre** en la lista de quienes están en la sesión, como en una videollamada, y los **resultados agregados** de una pregunta (por ejemplo, cuántas personas eligieron cada opción). No ven tu correo ni tu respuesta individual.

### ¿Es obligatorio darlos?

El nombre (o apodo) y el correo son **obligatorios** para unirte a una sesión. Responder las preguntas es **voluntario**.

### ¿Qué pasa si no los das?

No podrás unirte a la sesión desde la plataforma. Podés seguir la clase por los otros medios que indique tu profesor o profesora.

### ¿Dónde se guardan y por cuánto tiempo?

En servidores de **Microsoft Azure** ubicados en [REGIÓN DE AZURE, por ejemplo "Estados Unidos"]. Al aceptar este aviso autorizás expresamente ese almacenamiento fuera de Costa Rica.

Los guardamos mientras el curso o la sesión estén activos y hasta [PLAZO, por ejemplo "12 meses"] después de la última sesión; luego se eliminan o se anonimizan.

### ¿Cómo los protegemos?

Conexiones cifradas (HTTPS), acceso restringido a la persona que dirige la sesión y a quienes administran la plataforma, y copias de seguridad del proveedor.

### Tus derechos

Según la Ley 8968, podés en cualquier momento y sin costo:

- **Acceder**: saber si tenemos datos tuyos, cuáles, para qué y cómo los tratamos.
- **Rectificar**: corregir o actualizar datos inexactos o incompletos.
- **Suprimir**: pedir que eliminemos tus datos.
- **Revocar tu consentimiento**: retirarlo por el mismo medio en que lo diste. La revocación no afecta lo hecho antes de ella.

Escribí a [CORREO DE CONTACTO] desde el correo con el que te uniste. Respondemos en un máximo de **5 días hábiles**.

Si considerás que no se respetaron tus derechos, podés acudir a la **Agencia de Protección de Datos de los Habitantes (PRODHAB)**.

### Cambios a este aviso

Si cambiamos este aviso, publicaremos la nueva versión con su fecha. Si el cambio afecta cómo usamos tus datos, te pediremos de nuevo tu consentimiento la próxima vez que te unas a una sesión.

---

## Fuentes

- Ley 8968, texto publicado por el MICITT: https://www.micitt.go.cr/sites/default/files/marco_juridico_legal/08.%20Ley%20n.%C2%B0%208968%20Ley%20de%20Protecci%C3%B3n%20de%20la%20Persona%20frente%20al%20tratamiento%20de%20sus%20datos%20personales..pdf
- Guía de protección de datos de Costa Rica (Multilaw), sobre registro ante la PRODHAB y transferencias: https://www.multilaw.com/Multilaw/Data_Protection_Laws_Guide/DataProtection_Guide_Costa_Rica.aspx
