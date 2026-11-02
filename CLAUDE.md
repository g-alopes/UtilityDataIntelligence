# Repository instructions

- Target .NET 8 / C# 12.
- Keep Domain independent of infrastructure and UI.
- Keep business rules out of controllers and Razor components.
- Use async I/O and propagate CancellationToken.
- Never trust tenant IDs supplied by the browser; derive tenant scope from authenticated server identity.
- AI suggestions are advisory and must never silently mutate records.
- Process large imports with streaming and bounded batches.
- Add xUnit tests for business rules and integration tests for persistence and tenant isolation.
- Never commit secrets, customer data, database files, or model weights.
- Run dotnet build and relevant tests before considering a change complete.
