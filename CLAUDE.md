# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Tungsten Supply Chain Compliance Platform — Pilot MVP** deployed on Render. A commercial (non-defense) custody-tracking system for tungsten supply chain participants, validating batch lifecycle tracking against RMAP and OECD DDG compliance frameworks. The pilot targets mine-to-refinery custody events, Material Passport generation, and audit dossiers for real suppliers and buyers.

Full spec: `docs/Tungsten_Pilot_MVP_Spec_Render_v0_1.docx`

## Architecture

**Three-tier web application on Render managed infrastructure:**

- **Frontend:** Single Angular 21+ app (Render Static Site) with three lazy-loaded feature modules — Supplier Portal, Buyer Portal, Admin Dashboard. Route guards enforce role client-side; every API endpoint enforces role server-side independently.
- **Backend:** ASP.NET Core (.NET 10+) Web API (Render Web Service) using Vertical Slice architecture with MediatR CQRS. Minimal APIs preferred over controllers.
- **Database:** PostgreSQL on Render. Single-tenant by schema prefix.
- **Auth:** ASP.NET Core Identity — self-issued JWT access tokens (15 min) + HttpOnly refresh token cookie (14 days). Password reset and email confirmation via Resend transactional email. Role resolved via `/me` endpoint after JWT validation, not from JWT claims alone.
- **Worker:** Background service for compliance checking and document generation (Render Background Worker).
- **Monorepo structure:** `/packages/api`, `/packages/worker`, `/packages/web`, `/packages/shared`. Shared package contains domain types, Zod validation schemas, and compliance rule interfaces.

## Build & Development Commands

### .NET Backend
```bash
dotnet build                              # Build
dotnet test                               # Run all tests
dotnet test --filter "FullyQualifiedName~TestName"  # Run single test
dotnet format --verify-no-changes         # Check formatting
dotnet format                             # Auto-format
```
CI gate: `dotnet build && dotnet test && dotnet format --verify-no-changes`

### Angular Frontend
```bash
ng serve                    # Dev server
ng build                    # Production build
ng test                     # Run tests
ng test --include=**/path/to/spec.ts  # Run single test file
```

## Coding Rules (mandatory — see `.rules/` for full details)

### .NET (`.rules/DOTNET.md`)
- .NET 10, nullable enabled, warnings as errors, file-scoped namespaces
- **Vertical Slice architecture** by default — organize by feature, not by layer
- MediatR CQRS: every use case is `IRequest<TResponse>` + handler. No business logic in endpoints.
- **Result pattern** for all expected failures — never throw for validation/not-found/business rules
- Minimal APIs with `TypedResults`, grouped by feature via `IEndpointRouteBuilder` extensions
- FluentValidation + MediatR pipeline behaviour (no throwing)
- EF Core: `AsNoTracking()` on reads, project to DTOs, never return entities from API
- All I/O async end-to-end, pass `CancellationToken` through entire call chain
- Primary constructors for DI, `record` for DTOs/commands/queries
- RFC 7807 Problem Details for all error responses

### Angular (`.rules/ANGULAR_*.md`)
- Angular 21+, standalone components only, no NgModules
- **`inject()` only** — no constructor DI
- **Signal-first:** `input()` / `output()` / `model()` — no `@Input`/`@Output` decorators
- `signal()` for state, `computed()` for derived, `effect()` only for side effects
- `@if` / `@for` / `@switch` control flow — no structural directives (`*ngIf`, `*ngFor`)
- `toSignal()` — no `async` pipe
- Smart/Dumb component pattern with Facade per feature
- Signal-based state stores per feature (no NgRx unless cross-feature complexity demands it)
- `httpResource()` for GET reads, `HttpClient` for mutations
- Adapter pattern: transform API DTOs to domain models at the data-access boundary
- Functional guards, resolvers, interceptors
- `ChangeDetectionStrategy.OnPush` on all presentational components
- **Signal Forms** (`@angular/forms/signals`): `form(model, rules)` + `[formField]` — no ReactiveFormsModule

### Cross-Cutting
- Features never import other features directly — shared logic goes to `core/` or `shared/` (Angular) or `Common/` (dotnet)
- No `Subject` for state — use `signal()`
- No `async` pipe — use `toSignal()`
- Hash-based tamper evidence: SHA-256 of each custody event at write time
