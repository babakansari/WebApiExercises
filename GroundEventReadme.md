# Ground Event API (OAS)

Create a complete **OpenAPI Specification (OAS 3.0.3)** document for the Sample Flight Ground Event feature. The OAS will be used by **Swagger Codegen for .NET Core** to generate API service interfaces and DTOs.


## 1. Required OAS Sections

Include:
- Info and Servers
- Security and Security Schemes
- Tags
- Paths and HTTP methods
- Unique, descriptive Operation IDs
- Components (reusable schemas, parameters, and responses)

Follow OAS 3.0.3 standards and existing project conventions.

## 2. Domain Entities

**GroundEventType**
- `id`: integer
- `name`: string

**GroundEvent**
- `id`: integer
- `type`: Reference to GroundEventType
- `airline`: string
- `aircraft`: string
- `station`: string
- `priority`: Priority value
- `startUTC`: date-time
- `endUTC`: date-time

## 3. API Endpoints

### GET `/groundeventtypes`
Operation ID: `getGroundEventTypes`

Fetch all Ground Event Types.

Responses: 200, 400, 403, 500.

### GET `/groundevents`
Operation ID: `getGroundEvents`

Fetch Ground Events.

Responses: 200, 400, 403, 500.

### POST `/groundevents`
Operation ID: `createGroundEvent`

Create a new Ground Event.

Request DTO:
```json
{
  "type": { "id": 1 },
  "airline": "string",
  "aircraft": "string",
  "station": "string",
  "priority": "string",
  "startUTC": "2026-11-08T10:00:00Z",
  "endUTC": "2026-11-08T12:00:00Z"
}
```

Success response:
```json
{
  "recordId": 123
}
```

Responses: 200/201, 400, 404, 500.

### PUT `/groundevents/{refID}`
Operation ID: `updateGroundEvent`

Update an existing Ground Event.

- `refID`: Required integer path parameter.
- Request DTO: Same as POST.
- Success response: `{ "recordId": 123 }`
- Responses: 200/201, 400, 404, 500.

### DELETE `/groundevents/{refID}`
Operation ID: `deleteGroundEvent`

Delete an existing Ground Event.

- `refID`: Required integer path parameter.
- Responses: 200/204, 400, 404, 500.
- HTTP 204 must not return a response body.

## 4. Error Response Structure

Use a reusable ProblemDetail-based error schema:

```json
{
  "detail": {
    "status": 400,
    "title": "Validation failed",
    "instance": "/groundevents"
  },
  "extensions": [
    {
      "reference": "type.id",
      "detail": {
        "status": 400,
        "title": "Invalid reference",
        "instance": "/groundevents"
      }
    }
  ]
}
```

- `detail`: General ProblemDetail object.
- `extensions`: Array of property-specific errors.
- `reference`: Property reference, supporting dot notation (e.g., `type.id`, `airline`).
- Extension `detail`: ProblemDetail object.

## 5. Reusable Components

Define the following under `components/schemas`:

- `GroundEventType`
- `GroundEventTypeReference`
- `GroundEvent`
- `GroundEventRequest`
- `GroundEventResponse`
- `ProblemDetail`
- `GroundEventErrorResponse`
- `GroundEventErrorExtension`

Reuse schemas through `$ref` instead of duplicating definitions.

## 6. Guidelines

- Use camelCase JSON property names.
- Use appropriate OpenAPI data types and formats.
- Define required and optional properties.
- Define request and response content types.
- Use reusable response definitions.
- Ensure all references resolve correctly.
- Assign unique operation IDs suitable for .NET method generation.
- Follow existing authentication conventions; do not invent security requirements.
- Ensure compatibility with Swagger Codegen for .NET Core.

## 7. Expected Deliverable

Produce one complete, valid, self-contained OAS 3.0.3 YAML document containing all five endpoints, their request/response definitions, security, tags, operation IDs, and reusable components.

Do not generate API implementation code.