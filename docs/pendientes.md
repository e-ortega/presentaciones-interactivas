# Pendientes que dependen de Esteban

_Última actualización: 9 oct 2026 (noche). Marcados como (Nuevo) los agregados en esta versión; (Cambió) los que cambiaron._

## Abiertos

- [ ] **Decidir los modos de uso** (P-09, [diseno.md](diseno.md) §13): Fase 1 solo virtual y modo en aula después, con el backend preparado. De esto depende aprobar el plan de la Fase 1. _(Cambió: antes era "vista de proyección")_
- [ ] **Confirmar que en el modo virtual la voz va por una videollamada aparte** (Zoom, Teams o Meet). _(Nuevo)_
- [ ] **Aprobar el plan de la Fase 1** (P-07) una vez resuelto P-09. _(Cambió)_
- [ ] **Idioma de los documentos de diseño:** `docs/` está en español. ¿Se traduce al inglés por D-34, o se queda en español y solo la documentación técnica nueva va en inglés? _(Nuevo)_
- [ ] **Interfaz preparada para traducción desde el inicio** (P-10). _(Nuevo)_
- [ ] **Clave de acceso para la zona del profesor** antes de publicar en Azure (P-11). _(Nuevo)_
- [ ] **Aprobar la identidad visual** "Hoja de respuestas" (P-08). Se ajustó para el modo virtual y se agregó la vista del estudiante en computadora. _(Cambió)_
- [ ] **Aprobar la infraestructura de Azure** (P-01 a P-04).
- [ ] **Repositorio remoto:** crear el repo en GitHub (¿privado o público?) y conectarlo. _(Nuevo)_
- [ ] **Git dentro de una carpeta sincronizada con Google Drive:** Drive puede sincronizar a medias la carpeta `.git` y corromperla. Conviene mover el repo fuera de `D:\Documents` (por ejemplo, `D:\source\repos`) o excluirlo de la sincronización; GitHub ya sirve de respaldo. _(Nuevo)_
- [ ] **Redactar el aviso de consentimiento (Ley 8968)** y revisarlo con criterio legal antes de usarlo con estudiantes reales.

## Más adelante (sin acción por ahora)

- Presentaciones PPTX reales para probar la importación: Esteban las tiene; se piden al llegar al M1.
- Inicio de sesión del profesor (D-36).
- Nombre del producto (D-37).

## Resueltos

- [x] TypeScript → sí (D-33).
- [x] Idiomas → código y documentación interna en inglés; documentación al usuario e interfaz en español; inglés en la interfaz después (D-34).
- [x] Estudiante desde computadora o celular → vista adaptable, primero computadora (D-35).
- [x] Repositorio git local → creado (D-38).
- [x] Tecnología del frontend → React + Vite servido por ASP.NET Core (D-31).
- [x] Contexto para Claude Code → `CLAUDE.md` y `docs/` (D-32).
- [x] Animaciones → fuera de la v1, preparada para agregarlas (D-25 a D-27).
- [x] Entrada mínima por código en la Fase 1 (D-23).
- [x] Stack → ASP.NET Core + SignalR, SQL Server, Azure (D-28).
- [x] Consentimiento según Ley 8968 → se incluye (D-29).
