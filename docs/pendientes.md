# Pendientes que dependen de Esteban

_Última actualización: 10 oct 2026. Marcados como (Nuevo) o (Cambió) los puntos que difieren de la versión anterior._

## Abiertos

- [ ] **Decidir quién revisa el código de Claude.** El ruleset pide revisión de Copilot, pero nunca se ejecutó en los pull requests #1 a #4 (parece que la cuenta no la tiene activada). Mientras tanto Claude revisa cada diff con una pasada independiente (D-61). Opciones: activar Copilot, que revise Esteban, o dejarlo así.
- [ ] **Confirmar el trato en los textos de la interfaz:** están en voseo ("Subí tu primer PowerPoint", "Probá de nuevo"), como en el documento de identidad visual. Si se prefiere "usted" o "tú", es cambiar un archivo: `src/web/src/i18n/es.json`.
- [ ] **Revisar las decisiones D-49 a D-67** que Claude tomó ([decisiones.md](decisiones.md)). _(Cambió: se agregan D-64 a D-67)_
- [ ] **Correr `npm install` en `src/web`** una vez, para que los hooks de pre-commit queden activos también en los commits de Esteban. _(Nuevo)_
- [ ] **Decidir qué hacer con los pull requests de Dependabot**, que empiezan a llegar los lunes: que Claude los revise y fusione al inicio de cada sesión, o que Esteban los atienda. _(Nuevo)_
- [ ] **Revisar con una persona abogada el aviso de privacidad** ([legal/aviso-privacidad.md](legal/aviso-privacidad.md)) y completar los datos entre corchetes.
- [ ] **Leer la investigación de competencia** ([competencia.md](competencia.md)) y confirmar el posicionamiento: universidades de la región, clase virtual, notas del orador.
- [ ] **Sacar la carpeta `repos` de la sincronización de Google Drive.**

## Más adelante (sin acción por ahora)

- Un deck real con **diapositivas ocultas** y otro con **fuentes poco comunes**, para probar esos dos casos con material real. Los seis decks usados no tienen ninguno de los dos; hoy solo están cubiertos con archivos sintéticos.
- **Borrar una presentación.** Hoy no se puede: las importaciones fallidas y las de prueba se quedan en la lista. Va con la gestión de presentaciones, en el M2 o después.
- **Calidad, fase 2** (cuando toque cada una): prueba de carga del hub de SignalR (M3); Playwright de punta a punta (cierre del M4); Azurite para probar `BlobFileStorage` y escaneo de la imagen Docker (M5); umbral de cobertura de pruebas; SonarCloud si se suma más gente. _(Nuevo)_
- Inicio de sesión del profesor (D-36).
- Nombre del producto (D-37).
- Documentación de uso en inglés, si hace falta (D-43).
- Ideas vistas en Genially: temporizador por pregunta y candado para cerrar la entrada a la sesión.
- Probar ClassPoint y Mentimeter un par de semanas antes de la Fase 2 ([competencia.md](competencia.md)).

## Resueltos

- [x] Calidad, fase 1: hooks de pre-commit, pruebas de arquitectura, contrato de la API, auditoría de dependencias y Dependabot (D-64 a D-67). _(Nuevo)_
- [x] Revisión hacia atrás del M0 y el M1: 9 hallazgos, 8 corregidos y 1 anotado para el M5 (D-63).
- [x] App probada en el navegador con la clave real: entra y la importación funciona.
- [x] Las preguntas de React son opcionales y no quedan como pendiente (D-62).
- [x] Verificaciones automáticas estrictas en el servidor y el frontend (D-60).
- [x] M1 (importación) terminado: servidor y pantallas de React.
- [x] Claude puede fusionar sus pull requests con el CI en verde (D-59).
- [x] Hash de la clave de acceso cargado en `dotnet user-secrets`.
- [x] Decks reales para probar la importación: seis presentaciones de un curso de Esteban. Se leen desde su carpeta; no se copian al repo.
- [x] M1, parte del servidor: importación probada con esos seis decks.
- [x] M0 terminado y fusionado en `main` (pull request #1), con el CI en verde: 24 pruebas del servidor, incluidas las de Testcontainers, frontend e imagen Docker.
- [x] Rama `main` protegida con el ruleset `pr_main`: pull request obligatorio, los tres trabajos del CI en verde, solo fusión "squash", y las ramas se borran al fusionar.
- [x] M0 (esqueleto) implementado en la rama `feat/m0-skeleton`.
- [x] `docker compose up` verificado con el Docker de WSL (Ubuntu): la app migra la base, sirve React y la cookie del profesor sobrevive a un reinicio. El servicio de Docker no arranca solo en WSL: `sudo service docker start`.
- [x] Documentos alineados con las decisiones aprobadas: diseño §12-13 e identidad visual reflejan D-39 y D-46; plan con `Realtime/` y D-44.
- [x] Arquitectura de implementación aprobada (D-48).
- [x] Datos de estudiantes nunca van a la IA (D-42).
- [x] Documentos de diseño en español (D-43).
- [x] Textos de interfaz en archivos de traducción desde el inicio (D-44).
- [x] Clave de acceso: generada y guardada en KeePassXC; mecanismo con hash (D-45).
- [x] Identidad visual aprobada (D-46). Infraestructura de Azure aprobada (D-47).
- [x] Repo en GitHub en la cuenta personal `e-ortega` (D-41); repo de la cuenta Ulatina borrado.
- [x] Un solo modo, tres ventanas (D-39). Plan de la Fase 1 aprobado (D-40).
- [x] Competencia investigada. Borrador del aviso Ley 8968.
- [x] TypeScript (D-33). Idiomas (D-34). Vista del estudiante adaptable (D-35).
- [x] Repo git (D-38). React + Vite (D-31). Contexto para Claude Code (D-32).
- [x] Animaciones fuera de la v1, preparada (D-25 a D-27). Entrada por código (D-23). Stack (D-28). Ley 8968 (D-29).
