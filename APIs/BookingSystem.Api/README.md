# BookingSystem.Api - ASP.NET Core 10.0 Server

API for booking a user to an event.

## Upgrade NuGet Packages

NuGet packages get frequently updated.

To upgrade this solution to the latest version of all NuGet packages, use the dotnet-outdated tool.


Install dotnet-outdated tool:

```
dotnet tool install --global dotnet-outdated-tool
```

Upgrade only to new minor versions of packages

```
dotnet outdated --upgrade --version-lock Major
```

Upgrade to all new versions of packages (more likely to include breaking API changes)

```
dotnet outdated --upgrade
```


## Run

Linux/OS X:

```
sh build.sh
```

Windows:

```
build.bat
```

## Data store

The API uses the JSON-seeded in-memory store by default. To use the optional EF Core InMemory
provider, set `DataStore:Provider` to `EfCoreInMemory` in `src/appsettings.json`, or set the
environment variable `DataStore__Provider=EfCoreInMemory`.
The active data source is shown in the Swagger API description.

The EF Core store uses the same sample data and API operations as the default store. EF Core's
InMemory provider keeps data only for the lifetime of the process; it is intended for local
development and testing, not durable production storage. Set the provider back to `InMemory`
to restore the default.

## Run in Docker

```
cd src
docker build -t bookingsystem.api .
docker run -p 5000:8080 bookingsystem.api
```
