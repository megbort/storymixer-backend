# StoryMixer Backend

ASP.NET Core Web API (Minimal APIs) targeting .NET 10.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Project layout

```
StoryMixer.slnx
src/
  StoryMixer.Api/   # Web API project
```

## Run locally

```bash
dotnet restore
dotnet run --project src/StoryMixer.Api
```

The API listens on `http://localhost:5263` by default. To use HTTPS (`https://localhost:7058`), trust the dev certificate once and run the `https` launch profile:

```bash
dotnet dev-certs https --trust
dotnet run --project src/StoryMixer.Api --launch-profile https
```

In the Development environment the OpenAPI document is served at `/openapi/v1.json`.

## Build

```bash
dotnet build
```
