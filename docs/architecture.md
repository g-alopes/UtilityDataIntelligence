# Architecture — Phase 1

Dependency direction: Domain <- Application <- Infrastructure. API and Web are separate hosts.
Ollama is optional and accessed behind `IAiAssistantService`; provider changes should not affect application use cases.

Next phases: tenant identity and persistence; CSV import and local storage; bounded-batch worker; validation UI; AI analysis/suggestions; automated tests and benchmarks.
