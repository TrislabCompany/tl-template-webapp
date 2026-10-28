# Getting started

How to run the app on your machine, and how to run the tests.

## 1. What you need

| Tool | Version | For |
| --- | --- | --- |
| Docker, with Compose | Any current version | The whole app with `docker compose`, and the integration tests' Postgres |
| .NET SDK | The version in `global.json` | The API, the migrator and their tests |
| Node.js | 22 | The front end |
| pnpm | From `packageManager` in `web/package.json`: run `corepack enable` once | The front end |

Docker is enough to run the app. The SDK, Node and pnpm are needed to work on the code and to run the tests.

## 2. Run the whole app with Docker Compose

This is the everyday local run. It builds the three images from their Dockerfiles, as the pipeline does, and starts them with a Postgres 17 container:

```sh
cp .env.example .env
docker compose up --build
```

Open <http://localhost:8080>. You see the sample: a list of items you can add, rename, tick and delete.

The services are named like the apps in `platform.yaml` and the stamp's database:

| Service | What it is | Port on your machine |
| --- | --- | --- |
| `postgres` | Postgres 17, the same image as a dev stamp | `5432` |
| `migrator` | Applies the migrations and the seed data, then exits. `api` starts only once it has finished | None |
| `api` | The .NET API, at `http://api:8080` inside Compose | None: reach it through `web` |
| `web` | nginx with the front end, proxying `/api` to `api` | `8080` |

The API answers `GET /api/health/info` without sign-in. Call it through `web`, to see that the `/api` proxy reaches the API:

```sh
curl http://localhost:8080/api/health/info
# {"name":"api","version":"1.0.0","environment":"local"}
```

The API's OpenAPI document is at <http://localhost:8080/api/openapi/v1.json>.

Stop the app with `docker compose down`. Add `-v` to delete the database too; the migrator creates it again on the next start.

## 3. Run the API and the front end separately

To change code and see it at once, run the database in Compose and the rest on your machine:

```sh
docker compose up -d postgres migrator
dotnet run --project src/Api          # http://localhost:5080
pnpm --dir web install
pnpm --dir web dev                    # http://localhost:5173
```

`dotnet run` takes `DATABASE_URL` from `src/Api/Properties/launchSettings.json`, which points at the Compose Postgres. The Vite dev server reloads the page on every change, and sends `/api` to `http://localhost:5080`, as nginx sends it to `api` in the image; set `API_UPSTREAM` to use another address. In the dev server `/config.json` doesn't exist, so every value in it is unset.

## 4. Run the tests

```sh
dotnet test src/App.slnx
pnpm --dir web test
```

These are the commands `ci / ci` runs:

| Tests | Where | What they check |
| --- | --- | --- |
| Unit tests | `src/Tests/Api.Tests` | Code without a database, such as the mapping of the platform's settings |
| Integration tests | `src/Tests/Integration.Tests` | The API's endpoints against a real Postgres 17. The tests start a throw-away container themselves, so Docker must run |
| Front end | `web/src/**/*.test.tsx`, with `pnpm --dir web test` | The type check, the linter, and the components with Vitest |

`ci / ci` also builds the three images, scans them with Trivy and checks `platform.yaml`. To build one image yourself, as the pipeline does:

```sh
docker build -f src/Api/Dockerfile .
docker build -f web/Dockerfile web
```
