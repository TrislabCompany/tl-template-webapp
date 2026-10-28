# Configuration

The settings and secrets the app reads, and how to add one. The same image runs in every Environment; only these settings differ.

## 1. What the platform sets

The platform sets environment variables on each app. Locally, `.env` sets the same names for `docker compose` (copy it from `.env.example`).

| Setting | Apps | Set by | Locally |
| --- | --- | --- | --- |
| `PLATFORM_ENVIRONMENT` | `web`, `api`, `migrator` | The stamp: the Environment's name, such as `dev` | `local` |
| `PLATFORM_STAMP` | `web`, `api`, `migrator` | The stamp: its name, such as `shr-con-dev` | Not set |
| `DATABASE_URL` | `api`, `migrator` | The stamp's Postgres, as a URL such as `postgresql://user:password@host:5432/db?sslmode=require`, from the Key Vault | `postgresql://app:app@postgres:5432/app` |
| `PUBLIC_*` | `web` | `apps[].env` of `web` in `stamp.yaml` | `PUBLIC_EXAMPLE` |
| `API_UPSTREAM` | `web` | Never in a stamp | `http://api:8080`, in `docker-compose.yml` |

Secrets arrive as ordinary environment variables too: the platform reads them from the Key Vault. The app never signs in to the Key Vault, and no secret is ever in the repository, the image or `appsettings.json`.

## 2. How the API reads them

`src/Api/Platform/PlatformConfiguration.cs` is the one place that knows the platform's setting names. It turns them into configuration keys; everything else reads those keys, never an environment variable:

| Setting | Configuration key | When unset |
| --- | --- | --- |
| `DATABASE_URL` | `ConnectionStrings:Database`, read by the EF Core context in the API and the migrator | Neither starts: Postgres is required |
| `PLATFORM_ENVIRONMENT` | `Platform:Environment`, in the logs and in `GET /api/health/info` | `local` |

`DATABASE_URL` is a URL; the mapping turns it into an Npgsql connection string, with `sslmode` and `channel_binding` as Npgsql's `SSL Mode` and `Channel Binding`.

## 3. `/config.json`

The browser can't read environment variables, so when `web` starts, `web/docker-entrypoint.d/40-config.sh` writes `/config.json` from every `PLATFORM_*` setting and every setting whose name starts with `PUBLIC_`, and nothing else:

```json
{"PLATFORM_ENVIRONMENT":"dev","PLATFORM_STAMP":"shr-con-dev","PUBLIC_EXAMPLE":"hello"}
```

`web/src/main.tsx` loads it once, before the app renders; `web/src/lib/config.ts` reads it. Anything in it is public: never give a secret a `PUBLIC_` name.

The API's address isn't in `/config.json`. The app always calls the API at the relative path `/api`.

## 4. The `/api` upstream

nginx passes `/api` to the API. `40-config.sh` writes the address into the nginx configuration when the container starts:

| When | Upstream |
| --- | --- |
| `API_UPSTREAM` is set, as `docker-compose.yml` sets it | `API_UPSTREAM`, such as `http://api:8080` |
| Otherwise, in a stamp | `http://ca-${PLATFORM_STAMP}-api`, the `api` app of the same stamp |
| Neither is set | The container stops with an error that names both |

The path is passed on unchanged, so the API serves its routes under `/api`. nginx resolves the name once, when it starts. In `pnpm dev`, `web/vite.config.ts` proxies `/api` the same way, to `API_UPSTREAM` or `http://localhost:5080`.

## 5. Add a setting or a secret

**A setting for the API**, such as `FEATURE_X`:

1. Add it to `apps[].env` of `api` in the stamp's `stamp.yaml`, in `tl-cloud-infrastructure` ([deployment.md](deployment.md)).
2. Add a mapping to `PlatformConfiguration.cs`, from `FEATURE_X` to a configuration key, and read that key, or options bound to it, where it is used.
3. Add it with a local value to `.env.example`, and to the `api` service in `docker-compose.yml`.

**A secret for the API**, such as a key for an outside service:

1. Add its name to `apps[].secrets` of `api` in `stamp.yaml`, such as `mail-api-key`. The app gets it as `MAIL_API_KEY`. An engineer sets the value in the Key Vault through PIM; the platform team tells you when.
2. Map `MAIL_API_KEY` in `PlatformConfiguration.cs`, as above. If the app can't work without it, throw there when it is unset, as for `DATABASE_URL`.
3. Add it to `.env.example` with a harmless local value, never the real one.

**A backing service**, such as a cache: the stamp adds it under `data`, and the app gets its setting, such as `REDIS_URL`. Map it in `PlatformConfiguration.cs` as above.

**A value for the browser:** name it `PUBLIC_…`, add it to `apps[].env` of `web` in `stamp.yaml` and to `.env.example`, and read it from the config in `web/src/lib/config.ts`. No change to the image is needed.
