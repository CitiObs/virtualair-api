# virtualair-api-net

A .NET 8 solution providing the backend for the VirtualAir air quality platform. It acts as a proxy and integration layer that federates environmental observation data from multiple independent citizen-science and institutional observatory networks into a single, unified API surface following the [OGC SensorThings API 1.1](https://www.ogc.org/standards/sensorthings) standard.

The solution contains two ASP.NET Core projects that are deployed as separate services:

| Service | Port | Purpose |
|---|---|---|
| **VirtualAirApi** | 6020 | Management API — observatory registry, user accounts, health probes |
| **VirtualAirDataApi** | 5020 | Data API — SensorThings proxy, fan-out, ID namespacing, hourly aggregation |

---

## Table of Contents

- [Architecture](#architecture)
- [API Reference — VirtualAirApi](#api-reference--virtualairapi-port-6020)
- [API Reference — VirtualAirDataApi](#api-reference--virtualairdataapi-port-5020)
- [How Observatories Work](#how-observatories-work)
- [ID Namespacing](#id-namespacing)
- [Pagination](#pagination)
- [MultiDatastreams Aggregation](#multidatastreams-aggregation)
- [Authentication](#authentication)
- [Database Schema](#database-schema)
- [Installation and Local Run](#installation-and-local-run)
- [Environment Variables](#environment-variables)
- [Deployment](#deployment)
- [Dependencies](#dependencies)
- [Known Limitations](#known-limitations)

---

## Architecture

```
Client
  │
  ├──► VirtualAirApi (port 6020)
  │       PostgreSQL ──► observatory table (registry of external endpoints)
  │       PostgreSQL ──► user table (accounts + JWT auth)
  │
  └──► VirtualAirDataApi (port 5020)
          │  fetches observatory list from VirtualAirApi on every request
          │
          ├──► External Observatory A (e.g. FROST/SensorThings endpoint)
          ├──► External Observatory B
          └──► External Observatory N
                  │
                  └── merges results, rewrites @iot.id with observatory code prefix
```

`VirtualAirDataApi` has no database of its own. On every request it:

1. Calls `GET http://virtualairapi:6020/Observatory` to get the list of registered observatories.
2. Fans out the incoming SensorThings request to each observatory's external endpoint in parallel.
3. Merges the results, prefixing every `@iot.id` and navigation link with a unique 4-letter observatory code to prevent ID collisions.
4. Returns the combined response to the client.

---

## API Reference — VirtualAirApi (port 6020)

Swagger UI is available at `http://localhost:6020/swagger`.

### Observatory

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/Observatory` | — | List all registered observatories |
| `GET` | `/Observatory/{id}` | — | Get a single observatory by its database ID |
| `POST` | `/Observatory` | ✅ JWT | Register a new observatory (auto-generates a 4-letter code) |

**POST /Observatory request body**
```json
{
  "baseurl": "https://api.example.org/v1.1",
  "version": "1.1",
  "extension": null,
  "formatnavlinks": null
}
```

**GET /Observatory response**
```json
[
  {
    "id": 1,
    "baseurl": "https://api.example.org/v1.1",
    "version": "1.1",
    "code": "ABCD",
    "extension": null,
    "formatnavlinks": null
  }
]
```

### Authentication

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/Authentication/action` | — | Login — returns a JWT token |

**POST /Authentication/action request body**
```json
{
  "email": "user@example.com",
  "password": "secret"
}
```

**Response**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "jwt": "<jwt-token>"
}
```

### User

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/User/Register` | — | Register a new user account |
| `POST` | `/User/Delete` | ✅ JWT | Delete the authenticated user's own account |

Registration validates that the email is well-formed and the password is at least 4 characters. Returns a JWT on success.

### Health checks

| Method | Path | Description |
|---|---|---|
| `GET` | `/healthchecks?url={url}` | Fetches the given URL and returns its `value` array |
| `GET` | `/healthchecks/responseTop100Things?url={url}` | Times a `GET {url}/Things?$top=100` request; returns elapsed seconds |

### Version

| Method | Path | Description |
|---|---|---|
| `GET` | `/Version` | Returns the API version string |

---

## API Reference — VirtualAirDataApi (port 5020)

Swagger UI is available at `http://localhost:5020/swagger`.

Implements OGC SensorThings API 1.1. All paths are prefixed with `/v1.1/`.

**No authentication is required on this API.**

### Root / Metadata

```
GET /v1.1/
```
Returns the list of entity sets and OGC conformance links.

```json
{
  "value": [
    { "name": "Things",              "url": "https://api-virtualair.nilu.no/v1.1/Things" },
    { "name": "Datastreams",         "url": "https://api-virtualair.nilu.no/v1.1/Datastreams" },
    { "name": "Locations",           "url": "https://api-virtualair.nilu.no/v1.1/Locations" },
    { "name": "HistoricalLocations", "url": "https://api-virtualair.nilu.no/v1.1/HistoricalLocations" },
    { "name": "ObservedProperties",  "url": "https://api-virtualair.nilu.no/v1.1/ObservedProperties" },
    { "name": "Sensors",             "url": "https://api-virtualair.nilu.no/v1.1/Sensors" },
    { "name": "Observations",        "url": "https://api-virtualair.nilu.no/v1.1/Observations" },
    { "name": "FeaturesOfInterest",  "url": "https://api-virtualair.nilu.no/v1.1/FeaturesOfInterest" }
  ],
  "serverSettings": {
    "conformance": [
      "http://www.opengis.net/spec/iot_sensing/1.1/req/datamodel",
      "http://www.opengis.net/spec/iot_sensing/1.1/req/request-data",
      "http://www.opengis.net/spec/iot_sensing/1.1/req/resource-path/resource-path-to-entities"
    ]
  }
}
```

### Entity collection requests

Any SensorThings path without a specific entity ID fans out to all observatories and returns merged results.

```
GET /v1.1/Things
GET /v1.1/Things?$filter=...&$top=50&$skip=0
GET /v1.1/Datastreams
GET /v1.1/Observations
GET /v1.1/Locations
...
```

Each entity in the merged response includes an additional `properties.origin` field showing which observatory it came from:

```json
{
  "value": [
    {
      "@iot.id": "ABCD_42",
      "@iot.selfLink": "https://api-virtualair.nilu.no/v1.1/Things('ABCD_42')",
      "name": "Station Alpha",
      "properties": {
        "origin": "https://api.example.org/v1.1"
      }
    }
  ],
  "@iot.count": 1042,
  "@iot.nextLink": "https://api-virtualair.nilu.no/v1.1/Things?$top=100&$skip=100"
}
```

### Single entity requests

When the URL contains a specific ID, the request is routed to the matching observatory.

```
GET /v1.1/Things('ABCD_42')
GET /v1.1/Things('ABCD_42')/Datastreams
GET /v1.1/Things('ABCD_42')/Datastreams('ABCD_99')/Observations
```

### Nested Observations

```
GET /v1.1/Things('{tid}')/Datastreams('{did}')/Observations
```

Forwards to the owning observatory based on the code prefix in `tid`.

### MultiDatastreams (hourly aggregation)

```
GET /v1.1/MultiDatastreams?$filter=properties/aggregateUnit eq 'Hours' and properties/aggregateFor eq '/Datastreams({id})'&$expand=Observations($filter=...)
```

See [MultiDatastreams Aggregation](#multidatastreams-aggregation) below.

---

## How Observatories Work

An observatory is any HTTP endpoint that implements SensorThings API. Once registered in the VirtualAir database it is included in every fan-out request.

The `observatory` table fields:

| Field | Description |
|---|---|
| `baseurl` | Root URL of the external SensorThings endpoint (no trailing slash) |
| `version` | SensorThings version string (e.g. `"1.1"`) |
| `code` | Auto-generated 4-letter uppercase identifier unique to this observatory |
| `extension` | Optional URL path extension appended to requests (nullable) |
| `formatnavlinks` | Optional flag for per-observatory navigation link formatting quirks (nullable) |

When `VirtualAirDataApi` makes an outbound request to an observatory it constructs the URL as:

```
{baseurl}/{entityPath}{querystring}
```

Per-observatory URL quirks (such as the SRID prefix required by `samenmeten.rivm.nl` for geography filters) are handled in `EntityHandlerBase.HandleUrlExceptions`.

---

## ID Namespacing

External observatories use their own integer or string IDs that may collide with each other. `VirtualAirDataApi` rewrites every `@iot.id` and every `@iot.selfLink` / `@iot.navigationLink` in responses by prepending the 4-letter observatory code:

```
original @iot.id:  42
rewritten:         ABCD_42

original selfLink: https://api.example.org/v1.1/Things(42)
rewritten:         https://api-virtualair.nilu.no/v1.1/Things('ABCD_42')
```

When a client sends a request for a namespaced entity (`Things('ABCD_42')`), `SingleEntityHandler` extracts the `ABCD` prefix, looks up the matching observatory, strips the prefix, and forwards the original numeric ID to the correct endpoint.

---

## Pagination

`VirtualAirDataApi` distributes `$top` and `$skip` evenly across all observatories.

- Maximum `$top` per observatory is capped at **300**.
- `$top` is divided by the number of observatories. For example, requesting `$top=100` with 4 observatories results in `$top=25` sent to each observatory.
- `$skip` is similarly divided.
- If any observatory returns an `@iot.nextLink`, a combined `@iot.nextLink` is generated for the merged response.
- `@iot.count` values from all observatories are summed.

---

## MultiDatastreams Aggregation

The `MultiDatastreams` endpoint computes **on-the-fly hourly averages** from raw observations. It does not read from a dedicated aggregation table.

The `$filter` parameter must match exactly this pattern:

```
properties/aggregateUnit eq 'Hours' and properties/aggregateFor eq '/Datastreams({id})'
```

The `$expand` parameter must start with `Observations`.

**Example request:**
```
GET /v1.1/MultiDatastreams
  ?$filter=properties/aggregateUnit eq 'Hours' and properties/aggregateFor eq '/Datastreams(ABCD_99)'
  &$expand=Observations($filter=phenomenonTime ge 2024-01-01T00:00:00Z and phenomenonTime le 2024-01-31T23:00:00Z)
```

The endpoint fetches all matching raw observations (following `@iot.nextLink` automatically to page through large sets), then groups them by hour and computes:

| Field | Description |
|---|---|
| `actual` | Arithmetic mean of all results in the hour |
| `min` | Minimum result in the hour |
| `max` | Maximum result in the hour |
| `dev` | Population standard deviation of results in the hour |
| `parameters.resultCount` | Number of raw observations in the hour |

**Example response (abbreviated):**
```json
{
  "value": [
    {
      "@iot.id": "ABCD_99",
      "name": "PM2.5 [ 1 Hours ]",
      "observationType": "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_ComplexObservation",
      "unitOfMeasurements": [
        { "name": "ug.m-3", "symbol": "ug.m-3", "definition": "http://dd.eionet.europa.eu/vocabulary/uom/concentration/ug.m-3" }
      ],
      "Observations": [
        {
          "phenomenonTime": "2024-01-01T12:00:00Z/2024-01-01T13:00:00Z",
          "actual": 14.3,
          "min": 11.2,
          "max": 18.7,
          "dev": 2.1,
          "parameters": { "resultCount": 12 }
        }
      ]
    }
  ]
}
```

---

## Authentication

VirtualAirApi uses a custom JWT pipeline — not ASP.NET Identity.

- Passwords are hashed as `SHA-256(SHA-256(salt) + SHA-256(password))` with an 8-character random salt.
- JWTs are signed with HMAC-SHA256.
- The `JwtMiddleware` validates the token on every request and attaches the resolved user to `HttpContext.Items["User"]`.
- Endpoints protected with `[Authorize]` check for that context item.
- Pass the token in the `Authorization` header: `Authorization: Bearer <token>`

> ⚠️ JWT tokens currently do not expire (the expiry is not set). `VirtualAirDataApi` has no authentication at all.

---

## Database Schema

`VirtualAirApi` uses PostgreSQL. Apply the following DDL to create the required tables:

```sql
CREATE TABLE public.observatory (
  id          INTEGER PRIMARY KEY NOT NULL DEFAULT nextval('observatory_id_seq'::regclass),
  baseurl     CHARACTER VARYING(255) NOT NULL,
  version     CHARACTER VARYING(255) NOT NULL,
  extension   CHARACTER VARYING(255),
  formatnavlinks CHARACTER VARYING(255),
  code        CHARACTER VARYING(255)
);

CREATE TABLE public."user" (
  id       UUID PRIMARY KEY NOT NULL,
  email    CHARACTER VARYING(255),
  password CHARACTER VARYING(255),
  salt     CHARACTER VARYING(255),
  username CHARACTER VARYING(255),
  admin    BOOLEAN DEFAULT FALSE
);

CREATE UNIQUE INDEX user_email_uindex    ON "user" USING btree (email);
CREATE UNIQUE INDEX user_username_uindex ON "user" USING btree (username);
```

The DDL script is also at `VirtualAirApi/Scripts/create_database.txt`.

---

## Installation and Local Run

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker](https://www.docker.com/) and Docker Compose (for the full stack)
- A PostgreSQL instance (for `VirtualAirApi`)

### Run with Docker Compose (recommended)

The `docker-compose.yml` at the repository root also includes the Vue frontend from the sibling `virtualair-web-vue` directory. Place both repositories side by side.

1. Create a `.env` file in `virtualair-api-net/`:
   ```
   VirtualAirConnection=Server=localhost;Port=5432;Database=virtualair;User Id=youruser;Password=yourpassword;CommandTimeout=5000
   ```

2. Start all services:
   ```bash
   docker compose up
   ```

   | Service | URL |
   |---|---|
   | VirtualAirApi | http://localhost:6020 |
   | VirtualAirDataApi | http://localhost:5020 |
   | Vue frontend | http://localhost:3000 |

   Port overrides are supported via `.env`:
   ```
   API_PORT=6020
   DATAAPI_PORT=5020
   WEB_PORT=3000
   ```

### Run individually with dotnet CLI

```bash
# VirtualAirApi
cd VirtualAirApi
VirtualAirConnection="Server=...;..." dotnet run

# VirtualAirDataApi (in a separate terminal)
cd VirtualAirDataApi
dotnet run
```

When running `VirtualAirDataApi` locally outside Docker, it uses `http://host.docker.internal:6020` to reach `VirtualAirApi`. If running both with plain `dotnet run` (not in Docker), set up a local tunnel or change the URL in `BaseController.cs`.

---

## Environment Variables

| Variable | Used by | Description |
|---|---|---|
| `VirtualAirConnection` | VirtualAirApi | Full Npgsql connection string for the PostgreSQL database |

`VirtualAirDataApi` has no environment variables; it discovers its configuration from `VirtualAirApi` at runtime.

---

## Deployment

CI/CD is handled by GitLab CI (`.gitlab-ci.yml`). Pipelines run only on the `test` and `prod` branches.

Each branch push triggers four stages:

1. **buildapi** — builds a Docker image for `VirtualAirApi` using Kaniko
2. **pushapi** — pushes the image to the GitLab Container Registry tagged `a_{version}-{branch}`
3. **builddataapi** — builds a Docker image for `VirtualAirDataApi`
4. **pushdataapi** — pushes the image tagged `da_{version}-{branch}`

Image versions are set by the `APP_VERSION` and `APPDATA_VERSION` variables at the top of `.gitlab-ci.yml`. Bump those variables to publish a new release.

The actual deployment to the target environment (e.g. Kubernetes via ArgoCD) is managed separately by the `virtualair-docker-compose` project.

---

## Dependencies

### VirtualAirApi
| Package | Purpose |
|---|---|
| Npgsql (via EF Core provider) | PostgreSQL driver |
| Dapper + Dapper.Contrib | Lightweight ORM / SQL mapper |
| Newtonsoft.Json | JSON serialization |
| System.IdentityModel.Tokens.Jwt | JWT generation and validation |
| Swashbuckle.AspNetCore | Swagger / OpenAPI documentation |

### VirtualAirDataApi
| Package | Purpose |
|---|---|
| Newtonsoft.Json | JSON parsing and manipulation for SensorThings responses |
| Swashbuckle.AspNetCore | Swagger / OpenAPI documentation |

### External services
- **PostgreSQL** — required by `VirtualAirApi` for the observatory registry and user accounts.
- **External SensorThings endpoints** — each registered observatory must expose a SensorThings API 1.1-compatible HTTP endpoint. `VirtualAirDataApi` makes outbound HTTP requests to these on every client request.

---

## Known Limitations

- **JWT tokens do not expire.** The expiry is not set in `Security.GetToken`. Issued tokens remain valid indefinitely.
- **JWT signing key is hardcoded** in `VirtualAirApi/Common/AppSettings.cs`. Rotate it by changing the constant and redeploying.
- **`$top` pagination is approximate.** The top value is divided evenly across observatories (integer division). Results may be fewer than requested if `$top` is not a multiple of the observatory count. Skips are distributed the same way.
- **Observatory errors are silently swallowed.** If an external observatory endpoint is unreachable or returns an error, that observatory is skipped and no error is surfaced in the response.
- **MultiDatastreams only supports hourly aggregation** with the exact filter pattern documented above. Other aggregate units or filter shapes return an error object.
- **`VirtualAirDataApi` has no authentication.** It relies entirely on network-level access control.
- **Swagger is always enabled**, including in production builds (the environment check is commented out).

