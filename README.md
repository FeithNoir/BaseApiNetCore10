# BaseApiNetCore10

API base de autenticación en **.NET 10**, alineada con la serie [Crear base API](https://devchronicle-hugo.web.app/post/crear-base-api-dotnet) de DevChronicle.

Tiene capas DDD (Domain, Application, Infrastructure, Api), inyección de dependencias, JWT con refresh rotativo, hash de contraseñas, correos (bienvenida/verificación, restablecer, notificaciones) y los endpoints de auth del contrato común.

> **Salto de versión (26 nov 2026).** Esta no es la plantilla de febrero de 2026 (Identity, roles, SQLite). Esa quedó en la rama [`legacy/2026-02-plantilla-identity-sqlite`](https://github.com/FeithNoir/BaseApiNetCore10/tree/legacy/2026-02-plantilla-identity-sqlite). El detalle está en [CHANGELOG.md](CHANGELOG.md).

## Requisitos

- SDK de .NET 10 (se probó con 10.0.404)
- Docker, curl y jq
- Postgres y Mailpit, que levanta el `docker-compose.yml`

## Cómo correrla

```bash
git clone https://github.com/FeithNoir/BaseApiNetCore10.git
cd BaseApiNetCore10
docker compose up -d
cp .env.example .env   # cambia JWT_SECRET por uno de al menos 32 caracteres
dotnet tool restore    # instala dotnet-ef 10.0.15
dotnet test
dotnet run --project src/BaseApi.Api
```

Por defecto escucha en `http://localhost:8080` (variable `PORT`). Swagger UI queda en `/docs`. El recorrido de contrato:

```bash
./e2e.sh http://localhost:8080
```

## Estructura

```
src/BaseApi.Domain          # entidades, value objects y puertos
src/BaseApi.Application     # casos de uso y DTOs
src/BaseApi.Infrastructure  # EF Core, JWT, MailKit, plantillas
src/BaseApi.Api             # HTTP, Problem Details, OpenAPI
tests/BaseApi.Tests
```

La configuración sale de las variables de `.env.example`: `DATABASE_URL`, `JWT_SECRET`, `SMTP_*`, `APP_URL`, etc.

## Tutorial

El paso a paso, las trampas y el reto de bloqueo de cuenta están en el artículo de DevChronicle: [Crear base API: .NET 10 con EF Core, FluentValidation y MailKit](https://devchronicle-hugo.web.app/post/crear-base-api-dotnet).
