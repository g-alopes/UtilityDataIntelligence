# Utility Data Intelligence — Phase 1

Local-first .NET 8 / Blazor foundation for a utility data onboarding demo.

## Requirements
- Visual Studio 2022 with .NET 8 SDK
- Docker Desktop
- Optional: Ollama for Windows

## Start SQL Server
```powershell
docker compose up -d sqlserver
```

## Optional local AI (CPU-friendly starting point)
Install Ollama for Windows, then:
```powershell
ollama pull qwen2.5:1.5b
ollama serve
```
Ollama is optional; the application starts without it.

## Run
Open `UtilityDataIntelligence.sln` in Visual Studio 2022.
Set `UtilityData.Api` as startup project and run, then run `UtilityData.Web`.
- API: https://localhost:7041
- API Swagger: https://localhost:7041/swagger
- API health: https://localhost:7041/health
- Blazor UI: https://localhost:7241
- Ollama: http://localhost:11434

Or:
```powershell
dotnet restore
dotnet build
dotnet run --project src/UtilityData.Api
dotnet run --project src/UtilityData.Web
```

## Scope
This is Phase 1: solution foundation, local SQL Server container, API, Blazor shell, and optional local Ollama adapter. Tenant authentication, imports, deterministic validation, background processing, AI workflows, and tests are planned for subsequent phases.

Do not expose local services to an untrusted network or use real customer data in this demo without appropriate authorization.
