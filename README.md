# Generating a .NET Core Web API from an OpenAPI (OAS) Spec

This repo has two ways to build an ASP.NET Core Web API:

1. **Script:** you supply an OAS YAML file and [`generate-webapi.sh`](./generate-webapi.sh) generates the server code with `openapi-generator-cli`.
2. **Agent:** you describe the API feature in a prompt and the [`api-webapi-generator`](./.github/agents/api-webapi-generator.agent.md) agent writes the OAS file, generates the code, implements it and tests it.

## Prerequisites

| Tool | Check | Install |
|------|-------|---------|
| OpenAPI Generator CLI | `openapi-generator-cli version` | `npm install -g @openapitools/openapi-generator-cli` |
| Java (used by the generator) | `java -version` | e.g. `brew install openjdk` |
| .NET SDK 10 | `dotnet --list-sdks` | https://dotnet.microsoft.com/download |

## Option 1: Generate from an existing OAS YAML file

### 1. Write or update the OAS file
Create an OAS 3.0.3 YAML file for the feature in [`features/`](./features), e.g. [`features/GroundEventOAS.yaml`](./features/GroundEventOAS.yaml).
In Copilot Chat, the [`/api-oas`](./.github/skills/api-oas/SKILL.md) skill can write it from a requirements document such as [`features/GroundEventReadme.md`](./features/GroundEventReadme.md). The skill follows [`API-Design-System.yaml`](./API-Design-System.yaml).

### 2. Run the script
```bash
./generate-webapi.sh features/GroundEventOAS.yaml
```
The script:
- validates the spec;
- works out the package name from the file name, ignoring the folder (`features/GroundEventOAS.yaml` → `GroundEvent.Api`);
- generates the project into `APIs/<PackageName>`.

You can override the package name and output folder:
```bash
./generate-webapi.sh <oas-file> [packageName] [outputDir]
./generate-webapi.sh features/GroundEventOAS.yaml MyCompany.GroundEvent.Api APIs/MyCompany.GroundEvent.Api
```

The script runs this command, which you can also run directly:
```bash
openapi-generator-cli generate \
  -i features/GroundEventOAS.yaml \
  -g aspnetcore \
  -o APIs/GroundEvent.Api \
  -t templates/aspnetcore \
  --model-name-mappings GroundEvent=GroundEventModel \
  --additional-properties=packageName=GroundEvent.Api,aspnetCoreVersion=10.0,buildTarget=program,operationResultTask=true,operationModifier=virtual,generateBody=true,nullableReferenceTypes=true,useSwashbuckle=true,apiVersionNumber=1.0,apiVersionGroup=v1,apiVersionMajor=1,apiVersionMinor=0,apiRoutePrefix=/api
```

| Option | Effect |
|--------|--------|
| `aspnetCoreVersion=10.0` | Targets ASP.NET Core 10 (match your installed SDK) |
| `buildTarget=program` | Creates a runnable app (`library` creates a class library instead) |
| `operationModifier=virtual`, `generateBody=true` | Creates concrete controllers with method bodies. Abstract controllers are not registered by ASP.NET Core, so Swagger would show *"No operations defined in spec!"* |
| `-t templates/aspnetcore` | Uses the custom [`controller.mustache`](./templates/aspnetcore/controller.mustache) and [`model.mustache`](./templates/aspnetcore/model.mustache) (see below) |
| `--model-name-mappings` | Added automatically by the script when a model name clashes with a package namespace segment (e.g. `GroundEvent` in `GroundEvent.Api`) |
| `operationResultTask=true` | Makes controller methods return `Task<IActionResult>` |
| `useSwashbuckle=true` | Adds Swagger UI |
| `apiVersion*`, `apiRoutePrefix` | Derived by the script from the OAS `servers` path (see API versioning below) |

The custom controller template makes every operation:
- return the **sample DTO from the OAS** with the operation's success status code, e.g. `201` for create;
- throw `NotImplementedException` when the success response has no body (e.g. `204` delete).

The sample is a plain C# object, not a JSON string parsed at runtime: the custom model template gives
every generated model a `Sample` property built from the OAS `example` values, and controllers return
`GroundEventType.Sample`, `new List<GroundEventType> { GroundEventType.Sample }`, and so on. Nested
models reuse each other's `Sample`, so the whole sample graph is compile-time checked.

It intentionally does **not** emit `[Authorize]` for the spec's security schemes, because the generated `Startup` does not configure authentication and every request would fail. Add authentication when you implement the API.

#### API versioning

The script reads the version segment of the OAS `servers` path (`https://.../api/v1` → prefix `/api`,
version `1.0`) and the templates turn it into real ASP.NET Core API versioning rather than a hard-coded
path, following the [Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning) conventions:

```csharp
[ApiController]
[ApiVersion("1.0")]
public class GroundEventTypesApiController : ControllerBase
{
    [HttpGet]
    [Route("/api/v{version:apiVersion}/groundeventtypes")]
    [MapToApiVersion("1.0")]
    ...
```

`Startup` registers `AddApiVersioning(...).AddMvc().AddApiExplorer(...)` with
`UrlSegmentApiVersionReader`, `ReportApiVersions` (responses carry an `api-supported-versions` header)
and `SubstituteApiVersionInUrl`, so the OpenAPI document published at `/openapi/v1/openapi.json`
shows the concrete `/api/v1/...` paths. To add a version later, annotate the controller or action with
the extra `[ApiVersion]`/`[MapToApiVersion]` values and add a matching `SwaggerDoc` entry.

### 3. Build and run
```bash
dotnet build APIs/GroundEvent.Api/src/GroundEvent.Api.csproj
dotnet run   --project APIs/GroundEvent.Api/src/GroundEvent.Api.csproj
```
Swagger UI is then available at `/openapi` on the URL printed in the console.

### 4. Implement the API
Replace the sample bodies in the generated controllers (`Controllers/*Api.cs`) with real logic, or subclass the controllers and override the `virtual` methods. Add hand-written or edited files to `APIs/<PackageName>/.openapi-generator-ignore` so that regenerating after a spec change doesn't overwrite them.

## Option 2: Generate the whole API from a prompt with the `api-webapi-generator` agent

The agent is the more convenient option: **you don't have to write the OAS YAML file yourself.** Describe the API feature, or point at a requirements file, and it works without stopping for confirmation:

1. **Checks the tools:** detects the installed generator, Java and .NET SDK versions.
2. **Writes the contract first:** creates the OAS YAML file in `features/` (e.g. `features/GroundEventOAS.yaml`) using the [`api-oas`](./.github/skills/api-oas/SKILL.md) skill and [`API-Design-System.yaml`](./API-Design-System.yaml), then validates it.
3. **Generates the server:** runs `./generate-webapi.sh features/<Feature>OAS.yaml` on that contract, so the code lands in `APIs/<Feature>.Api` with the custom templates and API versioning.
4. **Implements:** writes the controllers, services, validation and `ProblemDetail` error handling.
5. **Tests:** writes xUnit integration tests, then runs `dotnet build` and `dotnet test` and fixes failures.
6. **Reports:** lists the endpoints, the decisions it made on its own, and any departures from the design standards.

### How to use it
1. Open Copilot Chat in VS Code.
2. Select **api-webapi-generator** in the agent picker.
3. Send a prompt, for example:
   ```text
   Implement #features/GroundEventReadme.md as a .NET Web API
   ```
   or describe the feature directly:
   ```text
   Build a Customer Orders API: list, get by id, create, update and delete orders
   (id, customerId, items[], total, status: Pending|Shipped|Cancelled, createdUtc).
   ```

To run it without approval prompts, allow its terminal commands in VS Code settings, e.g. `"chat.tools.terminal.autoApprove": { "openapi-generator-cli": true, "dotnet": true, "java": true, "npx": true }`. Setting names vary between VS Code versions.

> **Note:** the agent is contract-first — it never generates code before the OAS file exists and validates, and it generates through `generate-webapi.sh` (same templates and `APIs/` layout as Option 1) rather than calling `openapi-generator-cli generate` itself.

## Which option to use

| You have... | Use |
|-------------|-----|
| A reviewed or approved OAS YAML file, and you only want generated code | Option 1: `generate-webapi.sh` |
| A feature description or requirements document, and you want a working API end to end | Option 2: `api-webapi-generator` agent |
| Requirements, and you want to review the contract before generating code | `/api-oas` skill, then Option 1 |

## Repository layout

| Path | Purpose |
|------|---------|
| [`API-Design-System.yaml`](./API-Design-System.yaml) | Organization API design standards |
| [`features/GroundEventReadme.md`](./features/GroundEventReadme.md) | Sample feature requirements |
| [`features/GroundEventOAS.yaml`](./features/GroundEventOAS.yaml) | Sample OAS contract created from those requirements |
| [`generate-webapi.sh`](./generate-webapi.sh) | Generates code from an OAS file into `APIs/` |
| [`templates/aspnetcore/`](./templates/aspnetcore/controller.mustache) | Custom generator templates: concrete controllers returning OAS sample DTOs as C# objects, with Asp.Versioning wiring |
| [`.github/skills/api-oas/`](./.github/skills/api-oas/SKILL.md) | Copilot skill for writing and editing OAS files |
| [`.github/agents/api-webapi-generator.agent.md`](./.github/agents/api-webapi-generator.agent.md) | Copilot agent that builds an API end to end from a prompt |
| `APIs/` | Generated Web API projects |
