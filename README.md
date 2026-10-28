# tl-template-webapp

The template for interactive web apps on the Trislab platform. Read this first; the detail is in [`docs/`](docs/).

## What it is

A React front end built with Vite and served by unprivileged nginx, a .NET 10 API, and a migrator that builds the Postgres schema before the API starts. The sample app keeps a list of items: you add an item, rename it, tick it as done or delete it, and the API stores the list in Postgres.

## When to use it

Use it for an app where people work with data, such as forms, lists or a workflow, for one organisation, with its own API and database but no tenants.

| Your product… | Choose |
| --- | --- |
| Shows content, with no backend of its own | `tl-template-website` |
| Lets people work with data for one organisation, with its own API and database | `tl-template-webapp`, this template |
| Serves many customers from one deployment, each a tenant with its own users, plans and data | `tl-template-saas` |

## How to use it

You don't create a repository from this template yourself. The platform creates it when your product is onboarded: the onboarding PR in `tl-cloud-infrastructure` adds the repository to `catalog/repos.yaml`, named `<customer>-webapp-<product>` (for example `trislab-webapp-timesheets`), and the pipeline creates it from this template with its team, rules and `dev` Environment. Don't use GitHub's "Use this template" button.

The new repository gets a copy of these files and never syncs with the template again. From then on the code, this README and `docs/` are your team's.

## Features included

What works on the first deploy, with no change:

- The sample feature, a list of items with forms, ready to replace with your own features.
- `/api` on the app's own hostname: nginx in `web` passes it to the API, so the API needs no hostname and no CORS settings.
- The migrator: before each revision of the API starts, it takes a Postgres lock, applies the EF Core migrations and the seed data, and exits.
- `/healthz` and `/readyz` on both apps. The API is ready only while Postgres answers.
- `/config.json`, written when `web` starts from the `PLATFORM_*` and `PUBLIC_*` settings, so one image runs in every Environment.
- Security headers on every answer, including a strict Content-Security-Policy, and long caching of hashed assets.
- The check `ci / ci` on every PR and every push to `main`: it builds the three images, runs the .NET and front end tests, scans the images with Trivy, and checks `platform.yaml`.
- Images that run as a non-root user, with every base image pinned by digest.

It has **no sign-in**. Sandbox hostnames are behind the platform's Zero Trust sign-in, but production hostnames are public, so add the sign-in your product needs before its first production Environment.

## Technologies

| Technology | Version |
| --- | --- |
| React | 19.3.1, with React Router 8.5.1 |
| Vite | 8.3.5 |
| TypeScript | 5.9.3 |
| Node.js | 22.23.4 (`node:22.23.4-alpine`, build stage of `web` only) |
| pnpm | 10.34.9 |
| ESLint and Vitest | ESLint 10.13.0 with `typescript-eslint` 8.72.1; Vitest 5.0.5 |
| nginx | 1.30.6 (`nginxinc/nginx-unprivileged:1.30.6-alpine`) |
| .NET | SDK 10.0.403 (`global.json`), runtime 10.0.14 (`aspnet:10.0.14-noble-chiseled`) |
| ASP.NET Core | 10.0.14 |
| EF Core | 10.0.14, with Npgsql's provider 10.0.3 |
| Postgres | 17.11 (`postgres:17.11-alpine`, in `docker-compose.yml` and the integration tests) |
| xUnit and Testcontainers | xUnit v3 4.0.2; Testcontainers 4.16.0 |
| Trivy | 0.74.0, in `ci / ci` |

Every base image is pinned by digest. Renovate will keep the versions current; until it runs, the platform team bumps them.

## Repository layout

| Path | Holds |
| --- | --- |
| `web/` | The front end: `src/` (with `features/` and `lib/config.ts`), its Dockerfile, `nginx/` and `docker-entrypoint.d/` |
| `src/Api/` | The API: `Program.cs`, `Features/`, `Data/` with the migrations, and `Platform/PlatformConfiguration.cs` |
| `src/Migrator/` | The migrator, the init container of the API |
| `src/Tests/` | `Api.Tests` (unit tests) and `Integration.Tests` (API tests against Postgres) |
| `docker-compose.yml`, `.env.example` | The whole app locally |
| `platform.yaml` | The images and the sizing of each app, within the stamp's caps |
| `.github/` | The `ci` workflow, the PR template and the code owners |
| `docs/` | The detail behind this README |

## First steps

1. **Run it locally:** `cp .env.example .env`, then `docker compose up --build`, and open <http://localhost:8080>. Before you push, run `dotnet test src/App.slnx` and `pnpm --dir web test`. [getting-started.md](docs/getting-started.md) also runs the API and the front end separately.
2. **Open your first PR:** change something in `web/src/` or `src/Api/`, push a branch and open a PR. `ci / ci` must pass and a code owner must approve.
3. **Deploy, release and promote:** a merge to `main` deploys to `dev`, a tag `vX.Y.Z` releases to the `release` tier, and a promotion moves a release to production. The platform adds these workflows to the template; [deployment.md](docs/deployment.md) explains how they work.
4. **Add a migration:** change the model in `src/Api/Data/`, then `dotnet ef migrations add <Name>`. Keep it backward compatible ([deployment.md](docs/deployment.md#3-migrations)).
5. **Add a setting or a secret:** add it to the stamp, then map it in `src/Api/Platform/PlatformConfiguration.cs` ([configuration.md](docs/configuration.md#5-add-a-setting-or-a-secret)).
6. **Add sign-in** before your first production Environment: choose what your product needs, and protect the API's endpoints and the front end's routes with it.

## More

- [Getting started](docs/getting-started.md): run the app with Docker Compose or piece by piece, the services, and the tests.
- [Architecture](docs/architecture.md): the images, the apps, the migrator, where data lives and the request path.
- [Configuration](docs/configuration.md): the settings, `PlatformConfiguration.cs`, `/config.json`, the `/api` upstream, and how to add a setting or a secret.
- [Deployment](docs/deployment.md): Environments, deploy, release and promote, migrations, `platform.yaml` and the caps, and how to ask for more.
