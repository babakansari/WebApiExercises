---
name: api-oas
description: 'Create or modify an OpenAPI Specification (OAS 3.0.x) YAML contract for any API feature. Use when: designing a new API, adding or changing endpoints, paths, operations, operationIds, request/response DTOs, schemas, parameters, error responses (ProblemDetail), security schemes, or tags; contract-first API design; generating OAS for Swagger Codegen / NSwag / OpenAPI Generator (.NET, Java, TypeScript); converting a feature README or requirements into an OpenAPI YAML.'
argument-hint: 'Feature requirements (README path or description) and target OAS file to create or update'
---

# API OAS (OpenAPI YAML) Authoring

Produce a complete, valid, self-contained OpenAPI YAML contract from feature requirements, or safely modify an existing one. Output is the **contract only** — never generate implementation code (controllers, services, DTO classes) unless explicitly asked.

## When to Use

- A feature README / ticket describes entities and endpoints and an OAS file is needed.
- Adding, changing, or removing endpoints, parameters, schemas, or responses in an existing OAS file.
- Preparing a spec for code generators (Swagger Codegen, OpenAPI Generator, NSwag, Kiota).

## Inputs to Gather

Before writing YAML, extract (or ask the user for anything missing and non-inferable):

| Input | Source / Default |
|-------|------------------|
| Feature name, description | Requirements doc |
| OAS version | Requirements; default `3.0.3` (best generator support) |
| Target code generator & language | Requirements; affects naming and keyword choices |
| Domain entities, properties, types, required/optional | Requirements |
| Endpoints: method, path, operationId, params, request/response bodies, status codes | Requirements |
| Error response model | Requirements; default RFC 9457 Problem Details |
| Security | Existing spec / org standard. **Never invent** a scheme — if unknown, ask |
| Servers, base path, versioning | Existing spec / org standard |
| Organization API standards | Look for files like `API-Design-System.yaml`, `*.spectral.yaml`, `.github/copilot-instructions.md`, existing `*.oas.yaml` / `openapi*.yaml` |

**Precedence:** explicit feature requirements > existing spec conventions in the repo > organization API standards > the defaults in this skill. When requirements conflict with org standards (e.g. unversioned path vs required `/v{major}`), follow the requirements and flag the deviation in your summary.

## Procedure

### 1. Discover context
1. Search the workspace for existing OAS files and API standards (see table above).
2. If modifying, read the whole existing spec first and record its conventions: naming case, path style, shared components, security, error model, tag style, response reuse.
3. Determine whether the request is **create** (new file) or **modify** (edit in place).

### 2. Model the domain (components/schemas)
For each entity, define schemas under `components/schemas`:

- **Resource schema** (`<Entity>`): full read model including `id` (usually `readOnly: true`).
- **Reference schema** (`<Entity>Reference`): only the identifier, used when another entity points to it (e.g. `{ "id": 1 }`).
- **Request schema** (`<Entity>Request`): create/update payload — no server-generated fields; references use `<Entity>Reference`.
- **Response schema(s)**: e.g. `<Entity>Response` / `RecordIdResponse` when the operation returns something other than the resource.
- **Enums**: separate named schema (e.g. `Priority`) when values are known; if values are unspecified, use `string` and note the assumption.
- **List responses**: `type: array` of `$ref`, or a wrapper with pagination (`items`, `nextCursor`/`total`) if paging is required.

Type/format mapping:

| Domain | OAS |
|--------|-----|
| integer id | `type: integer`, `format: int32` (or `int64` for large ids) |
| decimal/money | `type: number`, `format: double` (or `string` with pattern for exact precision) |
| date-time (UTC) | `type: string`, `format: date-time` (ISO 8601, e.g. `2026-11-08T10:00:00Z`) |
| date | `type: string`, `format: date` |
| guid | `type: string`, `format: uuid` |
| boolean | `type: boolean` |

For every schema: set `type: object`, a `required` list, `description`s, `example`s, and constraints (`minLength`, `maxLength`, `minimum`, `pattern`, `enum`) where known. Use **camelCase** property names unless the requirements specify otherwise (preserve given names like `startUTC` exactly).

### 3. Model errors
Default to a reusable ProblemDetail-based model (adapt names to the feature/org):

```yaml
ProblemDetail:            # RFC 9457
  type: object
  properties:
    type:     { type: string, format: uri-reference }
    title:    { type: string }
    status:   { type: integer, format: int32 }
    detail:   { type: string }
    instance: { type: string, format: uri-reference }
ErrorExtension:           # property-specific error
  type: object
  required: [reference, detail]
  properties:
    reference: { type: string, description: 'Property path in dot notation, e.g. type.id' }
    detail:    { $ref: '#/components/schemas/ProblemDetail' }
ErrorResponse:
  type: object
  required: [detail]
  properties:
    detail:     { $ref: '#/components/schemas/ProblemDetail' }
    extensions: { type: array, items: { $ref: '#/components/schemas/ErrorExtension' } }
```

If the requirements define a different error shape or naming (e.g. `<Feature>ErrorResponse`), use theirs exactly. Use media type `application/problem+json` for errors when the org standard requires it, otherwise `application/json`; be consistent across the file.

### 4. Define reusable parameters and responses
- `components/parameters`: path ids (e.g. `RefId`, `required: true`, `in: path`), and shared query params (`limit`, `cursor`, `sort`, filters).
- `components/responses`: `BadRequest` (400), `Unauthorized` (401), `Forbidden` (403), `NotFound` (404), `Conflict` (409), `UnprocessableEntity` (422), `InternalServerError` (500) — each referencing the error schema. Only include those actually used.
- `components/securitySchemes`: the scheme(s) used by the project (e.g. `bearerAuth` HTTP bearer JWT, or OAuth2). Do not fabricate scopes/URLs; leave a clear placeholder and flag it if unknown.

### 5. Define paths and operations
For each endpoint:

- **Path**: as given in requirements; otherwise plural, lowercase nouns (kebab-case per org standard), one resource identified by a path parameter, no verbs.
- **operationId**: unique, camelCase, verb + noun, stable — it becomes the generated method name (e.g. `getOrders`, `createOrder`, `updateOrder`, `deleteOrder`). Use exactly the ids given in requirements.
- `summary`, `description`, `tags` (one tag per feature/resource group, declared in top-level `tags`).
- `parameters`: via `$ref` to components where reusable.
- `requestBody`: `required: true`, `content: application/json`, `$ref` to the request schema, with an `example`.
- `responses`: every status code listed in the requirements. Rules:
  - When requirements say "200/201" or "200/204", pick the one matching method semantics and org standard (POST create → `201` + optional `Location` header; PUT → `200` with body or `204`; DELETE → `204`) and mention the choice — or include both if the requirements clearly intend both.
  - `204` and `304` **must not** have `content`.
  - Error codes `$ref` to `components/responses`.
- Method semantics: GET safe/no body; POST create/action; PUT full replace (idempotent); PATCH partial; DELETE idempotent.
- Apply `security` globally at the root unless specific operations differ (use `security: []` only for explicitly public operations).

### 6. Assemble the document
Follow the section order and patterns in [the template](./assets/oas-template.yaml):

`openapi` → `info` (title, version, description) → `servers` → `security` → `tags` → `paths` → `components` (`securitySchemes`, `parameters`, `schemas`, `responses`).

### 7. Modifying an existing spec
- Make surgical edits; preserve ordering, formatting, comments, and unrelated content.
- Reuse existing components before adding new ones; never duplicate a schema under another name.
- Keep operationIds and schema names stable — renaming breaks generated clients.
- Classify the change: **compatible** (new optional field, new endpoint, new response field) vs **breaking** (removed/renamed field/path/operationId, new required request field, narrowed type/enum, changed status codes). Bump `info.version` accordingly (minor vs major) and call out breaking changes.
- Remove components that become unreferenced only if you removed their last usage.

### 8. Code-generator compatibility (Swagger Codegen / OpenAPI Generator / NSwag)
- Prefer OAS `3.0.3`; avoid 3.1-only features (`type: [string, "null"]`, `const`, `examples` arrays on schemas) unless 3.1 is requested. Use `nullable: true` in 3.0.
- Every operation has a unique `operationId`; every object schema used in a body is a **named** component (no inline object schemas) so generators emit proper DTO classes.
- Avoid `allOf`/`oneOf` polymorphism unless needed; when used, add a `discriminator`.
- Avoid schema names that collide with language keywords or generator/framework types (`Object`, `Task`, `Type`, `Event`, `File`, `Error`, `Response`). Prefix with the feature name when in doubt.
- Keep `$ref` siblings empty in 3.0 (siblings next to `$ref` are ignored); wrap with `allOf` if a description is needed.

### 9. Validate
1. Ensure every `$ref` resolves and every declared component is used (or intentionally shared).
2. Run an available linter/validator, in order of preference:
   - Org ruleset if present: `npx @stoplight/spectral-cli lint --ruleset <ruleset>.yaml <spec>.yaml`
   - Otherwise: `npx @stoplight/spectral-cli lint <spec>.yaml` (needs a `.spectral.yaml` with `extends: ["spectral:oas"]`), or `npx @redocly/cli lint <spec>.yaml`
3. Fix all errors; fix warnings that relate to your changes.

### 10. Deliver
- Write the YAML file (default name: `<feature-name-kebab>.oas.yaml` next to the requirements, unless a location/name is specified or an existing spec is being updated).
- Summarize: endpoints and operationIds, schemas added/changed, assumptions (enum values, chosen status codes, security placeholders), deviations from org standards, breaking changes, and validation result.

## Completion Checklist

- [ ] OAS version, `info`, `servers` present
- [ ] Security scheme follows existing conventions (nothing invented)
- [ ] Tags declared and used
- [ ] All required endpoints with methods, unique operationIds, parameters
- [ ] Request bodies and success responses reference named schemas, with examples
- [ ] All required status codes; 204/304 have no body
- [ ] Reusable error schema + `components/responses`
- [ ] Required/optional fields, types, formats, constraints defined
- [ ] No duplicated schemas; all `$ref`s resolve
- [ ] Generator-compatible (named DTOs, no 3.1-only syntax for 3.0 docs)
- [ ] Linted clean; no implementation code produced

## Worked Example (abbreviated)

Requirements (e.g. a feature README): entity `GroundEvent` referencing `GroundEventType`; endpoints `GET /groundeventtypes`, `GET|POST /groundevents`, `PUT|DELETE /groundevents/{refID}`; POST/PUT return `{ "recordId": 123 }`; ProblemDetail errors with property-level `extensions`.

Resulting contract:
- Schemas: `GroundEventType`, `GroundEventTypeReference`, `GroundEvent`, `GroundEventRequest`, `GroundEventResponse` (`recordId`), `ProblemDetail`, `GroundEventErrorExtension`, `GroundEventErrorResponse`
- Parameter: `RefId` (integer, path, required, named `refID` as given)
- Responses: `BadRequest`, `Forbidden`, `NotFound`, `InternalServerError`
- operationIds: `getGroundEventTypes`, `getGroundEvents`, `createGroundEvent`, `updateGroundEvent`, `deleteGroundEvent`
- DELETE returns `204` with no body; paths kept as specified (deviation from `/v1/kebab-case` standard flagged in summary)
