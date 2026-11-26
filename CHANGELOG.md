# Cambios

## 2026-11-26 — Alineación con la serie Crear base API

Este es un salto de versión, no un parche. El código de `main` ahora coincide con la entrega de .NET 10 de la serie [Crear base API](https://devchronicle-hugo.web.app/post/crear-base-api-dotnet) en DevChronicle.

La plantilla anterior (Identity, roles, SQLite, proyectos `BaseApiNet10.*`) quedó intacta en la rama `legacy/2026-02-plantilla-identity-sqlite`. Si un proyecto ya la usaba, no hace un `git pull` inocente: conviene tratarlo como una plantilla nueva o quedarse en esa rama.

### Qué cambió

- Las capas pasaron de `Api` / `Business` / `Core` / `Data` a `Domain` / `Application` / `Infrastructure` / `Api`, más un proyecto de pruebas.
- SQLite (y el `.db` que estaba en el repo) salieron. La base es Postgres, con `docker compose` (Postgres 18 + Mailpit).
- ASP.NET Identity y el `RoleController` salieron. El usuario es una entidad propia; el contrato de auth es el de la serie (registro, verificar correo, login, refresh rotativo, logout, cambiar y restablecer clave, me).
- User Secrets como único origen de config salieron en favor de variables de entorno (ver `.env.example`).
- AutoMapper salió. Los DTOs se mapean a mano.
- JWT con access de 15 minutos (HS256) y refresh opaco de 7 días, guardado solo como SHA-256.
- Contraseñas con el `PasswordHasher` de ASP.NET (no un paquete aparte).
- Correos con MailKit y plantillas incrustadas, pensados para Mailpit en local.
- Errores en formato Problem Details (RFC 9457).
- OpenAPI con Swashbuckle en `/docs`.
- Pruebas con xUnit v3 y `Microsoft.Testing.Platform`.
- Se agregó un `.gitignore` de verdad. El árbol anterior incluía `bin/`, `obj/`, `.vs/` y la base SQLite; eso ya no se versiona.
- El `Dockerfile` de Visual Studio apuntaba a `BaseApiNet10.Api` y SQLite, así que no se migró. El `docker-compose.yml` nuevo solo levanta Postgres y Mailpit.

### Qué se conservó en espíritu

- .NET 10, JWT, MailKit y Swagger/OpenAPI.
- La idea de una API base reutilizable, ahora con el contrato común de la serie.
