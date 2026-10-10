# Pendientes que dependen de Esteban

_Última actualización: 10 oct 2026. Marcados como (Nuevo) o (Cambió) los puntos que difieren de la versión anterior._

## Abiertos

- [ ] **Cargar el hash de la clave de acceso:** `dotnet user-secrets set "AdminAccess:KeySha256" "<hash>" --project src/Server` (el hash está en las notas de la entrada de KeePassXC). Sin esto, la zona del profesor rechaza toda clave.
- [ ] **Revisar las decisiones D-49 a D-53** que Claude tomó al implementar el M0 ([decisiones.md](decisiones.md)).
- [ ] **Revisar con una persona abogada el aviso de privacidad** ([legal/aviso-privacidad.md](legal/aviso-privacidad.md)) y completar los datos entre corchetes.
- [ ] **Leer la investigación de competencia** ([competencia.md](competencia.md)) y confirmar el posicionamiento: universidades de la región, clase virtual, notas del orador.
- [ ] **Sacar la carpeta `repos` de la sincronización de Google Drive.**

## Más adelante (sin acción por ahora)

- Presentaciones PPTX reales para probar la importación: Esteban las tiene; se piden al empezar el M1, que es el siguiente hito.
- Inicio de sesión del profesor (D-36).
- Nombre del producto (D-37).
- Documentación de uso en inglés, si hace falta (D-43).
- Ideas vistas en Genially: temporizador por pregunta y candado para cerrar la entrada a la sesión.
- Probar ClassPoint y Mentimeter un par de semanas antes de la Fase 2 ([competencia.md](competencia.md)).

## Resueltos

- [x] M0 terminado y fusionado en `main` (pull request #1), con el CI en verde: 24 pruebas del servidor, incluidas las de Testcontainers, frontend e imagen Docker. _(Nuevo)_
- [x] Rama `main` protegida con el ruleset `pr_main`: pull request obligatorio, los tres trabajos del CI en verde, solo fusión "squash", y las ramas se borran al fusionar. _(Nuevo)_
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
