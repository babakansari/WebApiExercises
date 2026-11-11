#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Container we use for final publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build container
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Copy the code into the container
WORKDIR /src
COPY ["src/BookingSystem.Api.csproj", "BookingSystem.Api/"]

# NuGet restore
RUN dotnet restore "BookingSystem.Api/BookingSystem.Api.csproj"
COPY ["src", "BookingSystem.Api/"]

# Build the API
WORKDIR "BookingSystem.Api"
RUN dotnet build "BookingSystem.Api.csproj" -c Release -o /app/build

# Publish it
FROM build AS publish
RUN dotnet publish "BookingSystem.Api.csproj" -c Release -o /app/publish

# Make the final image for publishing
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingSystem.Api.dll"]
