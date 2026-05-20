# Trainer.Api

ASP.NET Core Web API backing the cloud version of the boxing-studio app. Postgres on
[Neon](https://neon.tech) for persistence, deployed to [Fly.io](https://fly.io).

## Local development

```bash
# 1. Start Postgres (any local instance) and set the connection string.
export ConnectionStrings__Postgres="Host=localhost;Database=trainer_dev;Username=postgres;Password=postgres"

# 2. Generate a JWT signing key (development only — for prod use `fly secrets`).
export Auth__JwtSigningKey="$(openssl rand -base64 32)"

# 3. Apply migrations + start the API.
dotnet run --project src/Trainer.Api
```

API listens on `http://localhost:5xxx` (port set by `launchSettings.json`).

`appsettings.Development.json` ships with a placeholder connection string and a
non-production signing key — override via environment variables / `dotnet user-secrets`.

## Tests

```bash
dotnet test tests/Trainer.Api.Tests
```

Uses an in-process WebApplicationFactory backed by SQLite-in-memory. Covers auth flows,
refresh-token rotation, role scoping, and head-trainer guards. Postgres-specific
features (`citext`) are exercised against a real database on the CI/Neon stage only.

## Seed the first HeadTrainer

The API has no public sign-up — the first administrator must be created out of band:

```bash
dotnet run --project src/Trainer.Api -- seed-head-trainer admin@gym.test 'StrongPass!' 'Head Trainer'
```

On Fly.io:

```bash
fly ssh console -C "dotnet Trainer.Api.dll seed-head-trainer admin@gym.test 'StrongPass!' 'Head Trainer'"
```

The command is idempotent — re-running it for the same email updates the password.

## Deploying to Fly.io + Neon

1. Create a Neon project; copy the `postgres://...?sslmode=require` URL.
2. `fly launch --no-deploy --copy-config --name trainer-api`
3. Set secrets (these become environment variables on every VM):
   ```bash
   fly secrets set ConnectionStrings__Postgres="postgres://USER:PASS@HOST/DB?sslmode=require"
   fly secrets set Auth__JwtSigningKey="$(openssl rand -base64 32)"
   ```
4. `fly deploy`
5. Seed the HeadTrainer (see above). Migrations run automatically on every startup.

## Endpoints

| Verb | Path | Auth |
|------|------|------|
| `POST` | `/api/auth/login` | – |
| `POST` | `/api/auth/refresh` | – |
| `POST` | `/api/auth/logout` | – |
| `POST` | `/api/auth/accept-invite` | – |
| `POST` | `/api/auth/invite` | HeadTrainer |
| `GET` `POST` `PUT` `DELETE` | `/api/clients` | Trainer (own) / HeadTrainer (all) |
| `POST` | `/api/clients/{id}/reassign` | HeadTrainer |
| `GET` `POST` `PUT` `DELETE` | `/api/sessions` | Trainer (own) / HeadTrainer (all) |
| `GET` | `/api/training-types` | any |
| `POST` `PUT` `DELETE` | `/api/training-types` | HeadTrainer |
| `GET` `PUT` `DELETE` | `/api/users` | HeadTrainer |
| `GET` | `/api/backup/export` | HeadTrainer |
| `POST` | `/api/backup/import` | HeadTrainer |
| `GET` | `/health` | – |
