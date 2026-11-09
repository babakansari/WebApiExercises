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
Create an OAS 3.0.3 YAML file for the feature, e.g. [`GroundEventOAS.yaml`](./GroundEventOAS.yaml).
In Copilot Chat, the [`/api-oas`](./.github/skills/api-oas/SKILL.md) skill can write it from a requirements document such as [`GroundEventReadme.md`](./GroundEventReadme.md). The skill follows [`API-Design-System.yaml`](./API-Design-System.yaml).

### 2. Run the script
```bash
./generate-webapi.sh GroundEventOAS.yaml
```
The script:
- validates the spec;
- works out the package name from the file name (`GroundEventOAS.yaml` → `GroundEvent.Api`);
- generates the project into `APIs/<PackageName>`.

You can override the package name and output folder:
```bash
./generate-webapi.sh <oas-file> [packageName] [outputDir]
./generate-webapi.sh GroundEventOAS.yaml MyCompany.GroundEvent.Api APIs/MyCompany.GroundEvent.Api
```

The script runs this command, which you can also run directly:
```bash
openapi-generator-cli generate \
  -i GroundEventOAS.yaml \
  -g aspnetcore \
  -o APIs/GroundEvent.Api \
  --additional-properties=packageName=GroundEvent.Api,aspnetCoreVersion=10.0,buildTarget=program,operationIsAsync=true,operationResultTask=true,classModifier=abstract,operationModifier=abstract,nullableReferenceTypes=true,useSwashbuckle=true
```

| Option | Effect |
|--------|--------|
| `aspnetCoreVersion=10.0` | Targets ASP.NET Core 10 (match your installed SDK) |
| `buildTarget=program` | Creates a runnable app (`library` creates a class library instead) |
| `classModifier=abstract`, `operationModifier=abstract` | Makes the generated controllers abstract, so your logic goes in subclasses and survives regeneration |
| `operationResultTask=true` | Makes controller methods return `Task<IActionResult>` |
| `useSwashbuckle=true` | Adds Swagger UI |

### 3. Build and run
```bash
dotnet build APIs/GroundEvent.Api/src/GroundEvent.Api/GroundEvent.Api.csproj
dotnet run   --project APIs/GroundEvent.Api/src/GroundEvent.Api/GroundEvent.Api.csproj
```
Swagger UI is then available at the URL printed in the console.

### 4. Implement the API
The generator creates the contract only: abstract controllers and DTO models. To implement it:
- Write concrete controllers that inherit the generated ones, and add your services.
- List your hand-written files in `APIs/<PackageName>/.openapi-generator-ignore` so that regenerating after a spec change doesn't overwrite them.

> **Known issues with the current options:** the generated code may not compile until you fix these:
> - A package name that starts with a model name (e.g. `GroundEvent.Api` with a `GroundEvent` model) causes `'GroundEvent' is a namespace but is used like a type`. Use a different package name, e.g. `GroundEventService.Api`.
> - `operationIsAsync=true` together with `operationModifier=abstract` causes `CS1994`. Remove `operationIsAsync=true`; methods still return `Task<IActionResult>`.

## Option 2: Generate the whole API from a prompt with the `api-webapi-generator` agent

The agent is the more convenient option: **you don't have to write the OAS YAML file yourself.** Describe the API feature, or point at a requirements file, and it works without stopping for confirmation:

1. **Checks the tools:** detects the installed generator, Java and .NET SDK versions.
2. **Writes the contract:** creates the OAS YAML file using the [`api-oas`](./.github/skills/api-oas/SKILL.md) skill and [`API-Design-System.yaml`](./API-Design-System.yaml), then validates it.
3. **Generates the server:** runs `openapi-generator-cli` with the `aspnetcore` generator.
4. **Implements:** writes the controllers, services, validation and `ProblemDetail` error handling.
5. **Tests:** writes xUnit integration tests, then runs `dotnet build` and `dotnet test` and fixes failures.
6. **Reports:** lists the endpoints, the decisions it made on its own, and any departures from the design standards.

### How to use it
1. Open Copilot Chat in VS Code.
2. Select **api-webapi-generator** in the agent picker.
3. Send a prompt, for example:
   ```text
   Implement #GroundEventReadme.md as a .NET Web API
   ```
   or describe the feature directly:
   ```text
   Build a Customer Orders API: list, get by id, create, update and delete orders
   (id, customerId, items[], total, status: Pending|Shipped|Cancelled, createdUtc).
   ```

To run it without approval prompts, allow its terminal commands in VS Code settings, e.g. `"chat.tools.terminal.autoApprove": { "openapi-generator-cli": true, "dotnet": true, "java": true, "npx": true }`. Setting names vary between VS Code versions.

> **Note:** the agent currently writes generated code to `src/<Package>`, not `APIs/`, and uses the same generator options listed under Known issues above.

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
| [`GroundEventReadme.md`](./GroundEventReadme.md) | Sample feature requirements |
| [`GroundEventOAS.yaml`](./GroundEventOAS.yaml) | Sample OAS contract created from those requirements |
| [`generate-webapi.sh`](./generate-webapi.sh) | Generates code from an OAS file into `APIs/` |
| [`.github/skills/api-oas/`](./.github/skills/api-oas/SKILL.md) | Copilot skill for writing and editing OAS files |
| [`.github/agents/api-webapi-generator.agent.md`](./.github/agents/api-webapi-generator.agent.md) | Copilot agent that builds an API end to end from a prompt |
| `APIs/` | Generated Web API projects |
