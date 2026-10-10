# Presentaciones Interactivas (nombre provisional)

App web que convierte una presentación de PowerPoint que el profesor **ya tiene** en una clase en vivo e interactiva. El profesor importa su deck y la app añade, encima, la vista del presentador con sus notas, preguntas para los estudiantes y resultados en tiempo real.

## Documentación

| Archivo | Contenido |
|---|---|
| [docs/diseno.md](docs/diseno.md) | Documento de diseño: visión, experiencia, modelo de datos, arquitectura y fases |
| [docs/decisiones.md](docs/decisiones.md) | Registro de decisiones, con fecha y motivo |
| [docs/pendientes.md](docs/pendientes.md) | Acciones pendientes que dependen de Esteban |
| [docs/arquitectura.md](docs/arquitectura.md) | Arquitectura de implementación de la Fase 1 |
| [docs/plan-fase-1.md](docs/plan-fase-1.md) | Plan de implementación del MVP por hitos |
| [docs/contexto.md](docs/contexto.md) | Cómo se llegó al diseño y qué se descartó |
| [docs/identidad-visual.md](docs/identidad-visual.md) | Identidad visual propuesta; tokens en `src/web/src/styles/tokens.css` |
| [docs/competencia.md](docs/competencia.md) | Competencia, mercado y monetización |
| [docs/legal/aviso-privacidad.md](docs/legal/aviso-privacidad.md) | Aviso de privacidad y consentimiento (Ley 8968), borrador |
| [docs/referencia/](docs/referencia/) | Notas previas hechas con Gemini (solo referencia) |
| [CLAUDE.md](CLAUDE.md) | Contexto e instrucciones para trabajar con Claude Code |

## Estado

Fase 1 en construcción. Terminados los hitos M0 (esqueleto), M1 (importación), M2 (vista del presentador) y M3 (sesión en vivo): el profesor entra con su clave, sube un PowerPoint, la app lo convierte en diapositivas con sus notas y un PDF, puede recorrerlo con sus notas al lado como guion, e iniciar una clase en vivo: los estudiantes entran con un código y ven la misma diapositiva que el profesor, y hay una ventana aparte para compartir o proyectar. Sigue el M4, el quiz de opción múltiple. Ver [docs/plan-fase-1.md](docs/plan-fase-1.md).

## Cómo correrlo

Requisitos: .NET 10 SDK, Node 24 y SQL Server LocalDB (viene con Visual Studio) o Docker.

**Para desarrollar** (dos terminales):

```bash
dotnet run --project src/Server     # API en http://localhost:5049; crea la base en LocalDB
cd src/web && npm install && npm run dev   # página en http://localhost:5173
```

**Clave de acceso del profesor.** La app solo conoce el hash SHA-256 de la clave, nunca la clave. Se carga una vez por máquina y no va al repositorio:

```bash
dotnet user-secrets set "AdminAccess:KeySha256" "<hash>" --project src/Server
```

**Todo en contenedores** (app + SQL Server), como en producción:

```bash
cp .env.example .env    # y completar los valores
docker compose up --build   # app en http://localhost:8080
```

Con Docker dentro de WSL, correr esos comandos desde la terminal de Ubuntu, en `/mnt/d/.../presentaciones-interactivas`.

Docker Compose **no lee los user-secrets**: en este modo el hash de la clave va en el archivo `.env`, en `ADMIN_ACCESS_KEY_SHA256`. Si se deja vacío, la zona del profesor avisa que no está configurada.

**Antes de cada commit** corren solos unos chequeos rápidos (lint del frontend, formato de C#). Se instalan con el `npm install` de arriba.

**Pruebas:**

```bash
dotnet test                 # servidor; las de base de datos necesitan Docker
cd src/web && npm test      # frontend
```

Sin Docker, las pruebas de base de datos se saltan. Para correrlas contra LocalDB, definir antes `PI_TEST_SQLSERVER` con `Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True`.

Las notas previas trabajadas con Gemini (correos del 8 oct 2026) quedan como referencia aparte; este proyecto toma sus propias decisiones.
