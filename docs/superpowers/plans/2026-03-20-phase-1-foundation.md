# Phase 1: Foundation — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold the monorepo with a working .NET API, Angular SPA, shared TypeScript package, PostgreSQL schema on Neon, Auth0 integration, and shell layout — all wired together and tested.

**Architecture:** Monorepo with four packages (shared, api, worker, web). API is ASP.NET Core 10 with Vertical Slice + MediatR. Frontend is Angular 21 with Tailwind CSS. Database is Neon PostgreSQL accessed via EF Core with connection pooling. Auth0 provides JWT bearer authentication. The shared package holds Zod schemas and TypeScript domain types used by both web and (future) Node tooling.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core, MediatR, FluentValidation, Angular 21, Tailwind CSS 4, Auth0, Neon PostgreSQL, Sentry, OpenTelemetry, xUnit, NSubstitute, Jasmine/Karma

**Spec:** `docs/superpowers/specs/2026-03-20-tungsten-pilot-mvp-design.md`

**Neon connection:** `docs/neon.secrets` (connection string with pooler endpoint)

### Version Note

The spec references .NET 9 / ASP.NET Core 9. This plan targets **.NET 10** because the installed SDK is 10.0.100 (current stable as of March 2026). .NET 10 is fully backwards compatible and the coding rules in `.rules/DOTNET.md` say "Target .NET 9 (or latest stable)." All package references and TFM use `net10.0`.

---

## Chunk 1: Monorepo Scaffold and Shared Package

### Task 1: Initialize Git and Monorepo Root

**Files:**
- Create: `.gitattributes`
- Modify: `.gitignore` (already exists)
- Modify: `CLAUDE.md` (already exists)

- [ ] **Step 1: Initialize git repo**

```bash
cd /c/__edMVP
git init
```

- [ ] **Step 2: Create .gitattributes**

```gitattributes
# Auto detect text files and normalise line endings
* text=auto

# Force LF for scripts and config
*.sh text eol=lf
*.ts text eol=lf
*.js text eol=lf
*.json text eol=lf
*.cs text eol=lf
*.csproj text eol=lf
*.sln text eol=lf
*.md text eol=lf
*.html text eol=lf
*.css text eol=lf
*.scss text eol=lf

# Binary files
*.png binary
*.jpg binary
*.jpeg binary
*.gif binary
*.ico binary
*.pdf binary
*.docx binary
```

- [ ] **Step 3: Create monorepo directory structure**

```bash
mkdir -p packages/shared/src
mkdir -p packages/api
mkdir -p packages/worker
mkdir -p packages/web
```

- [ ] **Step 4: Commit scaffold**

```bash
git add .gitattributes .gitignore .claudeignore CLAUDE.md .rules/ docs/
git commit -m "chore: initialize monorepo with docs and rules"
```

---

### Task 2: Shared TypeScript Package — Domain Types

**Files:**
- Create: `packages/shared/package.json`
- Create: `packages/shared/tsconfig.json`
- Create: `packages/shared/src/index.ts`
- Create: `packages/shared/src/enums.ts`
- Create: `packages/shared/src/models/batch.ts`
- Create: `packages/shared/src/models/custody-event.ts`
- Create: `packages/shared/src/models/user.ts`
- Create: `packages/shared/src/models/document.ts`
- Create: `packages/shared/src/models/compliance.ts`
- Create: `packages/shared/src/models/index.ts`
- Create: `packages/shared/src/schemas/batch.schema.ts`
- Create: `packages/shared/src/schemas/custody-event.schema.ts`
- Create: `packages/shared/src/schemas/user.schema.ts`
- Create: `packages/shared/src/schemas/index.ts`

- [ ] **Step 1: Create package.json**

```json
{
  "name": "@tungsten/shared",
  "version": "0.1.0",
  "private": true,
  "type": "module",
  "main": "dist/index.js",
  "types": "dist/index.d.ts",
  "scripts": {
    "build": "tsc",
    "watch": "tsc --watch"
  },
  "dependencies": {
    "zod": "^3.24.0"
  },
  "devDependencies": {
    "typescript": "~5.8.0"
  }
}
```

- [ ] **Step 2: Create tsconfig.json**

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "lib": ["ES2022"],
    "outDir": "dist",
    "rootDir": "src",
    "declaration": true,
    "declarationMap": true,
    "sourceMap": true,
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true
  },
  "include": ["src/**/*"]
}
```

- [ ] **Step 3: Create enums.ts**

All shared enums used across the platform:

```typescript
// packages/shared/src/enums.ts

export const UserRole = {
  SUPPLIER: 'SUPPLIER',
  BUYER: 'BUYER',
  PLATFORM_ADMIN: 'PLATFORM_ADMIN',
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const BatchStatus = {
  CREATED: 'CREATED',
  IN_TRANSIT: 'IN_TRANSIT',
  AT_PROCESSOR: 'AT_PROCESSOR',
  PROCESSING: 'PROCESSING',
  REFINED: 'REFINED',
  COMPLETED: 'COMPLETED',
} as const;
export type BatchStatus = (typeof BatchStatus)[keyof typeof BatchStatus];

export const ComplianceStatus = {
  PENDING: 'PENDING',
  COMPLIANT: 'COMPLIANT',
  FLAGGED: 'FLAGGED',
  INSUFFICIENT_DATA: 'INSUFFICIENT_DATA',
} as const;
export type ComplianceStatus = (typeof ComplianceStatus)[keyof typeof ComplianceStatus];

export const CustodyEventType = {
  MINE_EXTRACTION: 'MINE_EXTRACTION',
  CONCENTRATION: 'CONCENTRATION',
  TRADING_TRANSFER: 'TRADING_TRANSFER',
  LABORATORY_ASSAY: 'LABORATORY_ASSAY',
  PRIMARY_PROCESSING: 'PRIMARY_PROCESSING',
  EXPORT_SHIPMENT: 'EXPORT_SHIPMENT',
} as const;
export type CustodyEventType = (typeof CustodyEventType)[keyof typeof CustodyEventType];

export const ComplianceCheckStatus = {
  PASS: 'PASS',
  FAIL: 'FAIL',
  FLAG: 'FLAG',
  INSUFFICIENT_DATA: 'INSUFFICIENT_DATA',
  PENDING: 'PENDING',
} as const;
export type ComplianceCheckStatus = (typeof ComplianceCheckStatus)[keyof typeof ComplianceCheckStatus];

export const ComplianceFramework = {
  RMAP: 'RMAP',
  OECD_DDG: 'OECD_DDG',
} as const;
export type ComplianceFramework = (typeof ComplianceFramework)[keyof typeof ComplianceFramework];

export const DocumentType = {
  CERTIFICATE_OF_ORIGIN: 'CERTIFICATE_OF_ORIGIN',
  ASSAY_REPORT: 'ASSAY_REPORT',
  TRANSPORT_DOCUMENT: 'TRANSPORT_DOCUMENT',
  SMELTER_CERTIFICATE: 'SMELTER_CERTIFICATE',
  MINERALOGICAL_CERTIFICATE: 'MINERALOGICAL_CERTIFICATE',
  EXPORT_PERMIT: 'EXPORT_PERMIT',
  OTHER: 'OTHER',
} as const;
export type DocumentType = (typeof DocumentType)[keyof typeof DocumentType];

export const SmelterConformanceStatus = {
  CONFORMANT: 'CONFORMANT',
  ACTIVE_PARTICIPATING: 'ACTIVE_PARTICIPATING',
  NON_CONFORMANT: 'NON_CONFORMANT',
} as const;
export type SmelterConformanceStatus = (typeof SmelterConformanceStatus)[keyof typeof SmelterConformanceStatus];

export const RiskLevel = {
  HIGH: 'HIGH',
  MEDIUM: 'MEDIUM',
  LOW: 'LOW',
} as const;
export type RiskLevel = (typeof RiskLevel)[keyof typeof RiskLevel];

export const NotificationType = {
  COMPLIANCE_FLAG: 'COMPLIANCE_FLAG',
  DOCUMENT_AVAILABLE: 'DOCUMENT_AVAILABLE',
  PASSPORT_GENERATED: 'PASSPORT_GENERATED',
  USER_INVITED: 'USER_INVITED',
  COMPLIANCE_ESCALATION: 'COMPLIANCE_ESCALATION',
} as const;
export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType];

export const GeneratedDocumentType = {
  MATERIAL_PASSPORT: 'MATERIAL_PASSPORT',
  AUDIT_DOSSIER: 'AUDIT_DOSSIER',
} as const;
export type GeneratedDocumentType = (typeof GeneratedDocumentType)[keyof typeof GeneratedDocumentType];

export const TenantStatus = {
  ACTIVE: 'ACTIVE',
  SUSPENDED: 'SUSPENDED',
} as const;
export type TenantStatus = (typeof TenantStatus)[keyof typeof TenantStatus];

export const JobType = {
  COMPLIANCE_CHECK: 'COMPLIANCE_CHECK',
  PASSPORT_GENERATION: 'PASSPORT_GENERATION',
  DOSSIER_GENERATION: 'DOSSIER_GENERATION',
  EMAIL_SEND: 'EMAIL_SEND',
} as const;
export type JobType = (typeof JobType)[keyof typeof JobType];

export const JobStatus = {
  QUEUED: 'QUEUED',
  RUNNING: 'RUNNING',
  COMPLETED: 'COMPLETED',
  FAILED: 'FAILED',
} as const;
export type JobStatus = (typeof JobStatus)[keyof typeof JobStatus];
```

- [ ] **Step 4: Create domain model interfaces**

`packages/shared/src/models/user.ts`:
```typescript
import { UserRole } from '../enums.js';

export interface User {
  id: string;
  auth0Sub: string;
  email: string;
  displayName: string;
  role: UserRole;
  tenantId: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UserResponse {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  tenantId: string;
  isActive: boolean;
}

export interface MeResponse {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  tenantId: string;
  tenantName: string;
}
```

`packages/shared/src/models/batch.ts`:
```typescript
import { BatchStatus, ComplianceStatus } from '../enums.js';

export interface Batch {
  id: string;
  tenantId: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
  status: BatchStatus;
  complianceStatus: ComplianceStatus;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBatchRequest {
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
}

export interface BatchResponse {
  id: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
  status: BatchStatus;
  complianceStatus: ComplianceStatus;
  createdAt: string;
  eventCount: number;
}
```

`packages/shared/src/models/custody-event.ts`:
```typescript
import { CustodyEventType } from '../enums.js';

export interface MineExtractionMetadata {
  gpsCoordinates: string;
  mineOperatorIdentity: string;
  mineralogicalCertificateRef: string;
}

export interface ConcentrationMetadata {
  facilityName: string;
  processDescription: string;
  inputWeightKg: number;
  outputWeightKg: number;
  concentrationRatio: number;
}

export interface TradingTransferMetadata {
  sellerIdentity: string;
  buyerIdentity: string;
  transferDate: string;
  contractReference: string;
}

export interface LaboratoryAssayMetadata {
  laboratoryName: string;
  assayMethod: string;
  tungstenContentPct: number;
  assayCertificateRef: string;
}

export interface PrimaryProcessingMetadata {
  smelterId: string;
  processType: string;
  inputWeightKg: number;
  outputWeightKg: number;
}

export interface ExportShipmentMetadata {
  originCountry: string;
  destinationCountry: string;
  transportMode: string;
  exportPermitRef: string;
}

export type CustodyEventMetadata =
  | MineExtractionMetadata
  | ConcentrationMetadata
  | TradingTransferMetadata
  | LaboratoryAssayMetadata
  | PrimaryProcessingMetadata
  | ExportShipmentMetadata;

export interface CustodyEvent {
  id: string;
  batchId: string;
  tenantId: string;
  eventType: CustodyEventType;
  idempotencyKey: string;
  eventDate: string;
  location: string;
  gpsCoordinates: string | null;
  actorName: string;
  smelterId: string | null;
  description: string;
  metadata: CustodyEventMetadata;
  schemaVersion: number;
  isCorrection: boolean;
  correctsEventId: string | null;
  sha256Hash: string;
  previousEventHash: string | null;
  createdBy: string;
  createdAt: string;
}

export interface CreateCustodyEventRequest {
  eventType: CustodyEventType;
  eventDate: string;
  location: string;
  gpsCoordinates?: string;
  actorName: string;
  smelterId?: string;
  description: string;
  metadata: CustodyEventMetadata;
}
```

`packages/shared/src/models/document.ts`:
```typescript
import { DocumentType, GeneratedDocumentType } from '../enums.js';

export interface Document {
  id: string;
  tenantId: string;
  custodyEventId: string | null;
  batchId: string;
  fileName: string;
  storageKey: string;
  fileSizeBytes: number;
  contentType: string;
  sha256Hash: string;
  documentType: DocumentType;
  uploadedBy: string;
  createdAt: string;
}

export interface DocumentResponse {
  id: string;
  fileName: string;
  fileSizeBytes: number;
  contentType: string;
  documentType: DocumentType;
  uploadedBy: string;
  createdAt: string;
  downloadUrl: string;
}

export interface GeneratedDocument {
  id: string;
  batchId: string;
  tenantId: string;
  documentType: GeneratedDocumentType;
  storageKey: string;
  generatedBy: string;
  shareToken: string | null;
  shareExpiresAt: string | null;
  generatedAt: string;
}
```

`packages/shared/src/models/compliance.ts`:
```typescript
import { ComplianceCheckStatus, ComplianceFramework } from '../enums.js';

export interface ComplianceCheck {
  id: string;
  custodyEventId: string;
  batchId: string;
  tenantId: string;
  framework: ComplianceFramework;
  status: ComplianceCheckStatus;
  details: Record<string, unknown>;
  checkedAt: string;
}

export interface ComplianceCheckResponse {
  id: string;
  framework: ComplianceFramework;
  status: ComplianceCheckStatus;
  details: Record<string, unknown>;
  checkedAt: string;
}
```

`packages/shared/src/models/index.ts`:
```typescript
export * from './user.js';
export * from './batch.js';
export * from './custody-event.js';
export * from './document.js';
export * from './compliance.js';
```

- [ ] **Step 5: Create Zod validation schemas**

`packages/shared/src/schemas/batch.schema.ts`:
```typescript
import { z } from 'zod';
import { BatchStatus, ComplianceStatus } from '../enums.js';

const isoCountryRegex = /^[A-Z]{2}$/;

export const createBatchSchema = z.object({
  batchNumber: z.string().min(1).max(100),
  mineralType: z.string().min(1).max(50).default('tungsten'),
  originCountry: z.string().regex(isoCountryRegex, 'Must be ISO 3166-1 alpha-2 code'),
  originMine: z.string().min(1).max(200),
  weightKg: z.number().positive(),
});

export type CreateBatchInput = z.infer<typeof createBatchSchema>;
```

`packages/shared/src/schemas/custody-event.schema.ts`:
```typescript
import { z } from 'zod';
import { CustodyEventType } from '../enums.js';

const mineExtractionMetadataSchema = z.object({
  gpsCoordinates: z.string().min(1),
  mineOperatorIdentity: z.string().min(1),
  mineralogicalCertificateRef: z.string().min(1),
});

const concentrationMetadataSchema = z.object({
  facilityName: z.string().min(1),
  processDescription: z.string().min(1),
  inputWeightKg: z.number().positive(),
  outputWeightKg: z.number().positive(),
  concentrationRatio: z.number().positive(),
});

const tradingTransferMetadataSchema = z.object({
  sellerIdentity: z.string().min(1),
  buyerIdentity: z.string().min(1),
  transferDate: z.string().datetime(),
  contractReference: z.string().min(1),
});

const laboratoryAssayMetadataSchema = z.object({
  laboratoryName: z.string().min(1),
  assayMethod: z.string().min(1),
  tungstenContentPct: z.number().min(0).max(100),
  assayCertificateRef: z.string().min(1),
});

const primaryProcessingMetadataSchema = z.object({
  smelterId: z.string().min(1),
  processType: z.string().min(1),
  inputWeightKg: z.number().positive(),
  outputWeightKg: z.number().positive(),
});

const exportShipmentMetadataSchema = z.object({
  originCountry: z.string().length(2),
  destinationCountry: z.string().length(2),
  transportMode: z.string().min(1),
  exportPermitRef: z.string().min(1),
});

export const metadataSchemaByEventType = {
  [CustodyEventType.MINE_EXTRACTION]: mineExtractionMetadataSchema,
  [CustodyEventType.CONCENTRATION]: concentrationMetadataSchema,
  [CustodyEventType.TRADING_TRANSFER]: tradingTransferMetadataSchema,
  [CustodyEventType.LABORATORY_ASSAY]: laboratoryAssayMetadataSchema,
  [CustodyEventType.PRIMARY_PROCESSING]: primaryProcessingMetadataSchema,
  [CustodyEventType.EXPORT_SHIPMENT]: exportShipmentMetadataSchema,
} as const;

export const createCustodyEventSchema = z.object({
  eventType: z.nativeEnum(CustodyEventType),
  eventDate: z.string().datetime(),
  location: z.string().min(1).max(500),
  gpsCoordinates: z.string().optional(),
  actorName: z.string().min(1).max(300),
  smelterId: z.string().optional(),
  description: z.string().min(1).max(2000),
  metadata: z.record(z.unknown()),
});

export {
  mineExtractionMetadataSchema,
  concentrationMetadataSchema,
  tradingTransferMetadataSchema,
  laboratoryAssayMetadataSchema,
  primaryProcessingMetadataSchema,
  exportShipmentMetadataSchema,
};
```

`packages/shared/src/schemas/user.schema.ts`:
```typescript
import { z } from 'zod';
import { UserRole } from '../enums.js';

export const createUserSchema = z.object({
  email: z.string().email(),
  displayName: z.string().min(1).max(200),
  role: z.nativeEnum(UserRole),
});

export type CreateUserInput = z.infer<typeof createUserSchema>;
```

`packages/shared/src/schemas/index.ts`:
```typescript
export * from './batch.schema.js';
export * from './custody-event.schema.js';
export * from './user.schema.js';
```

- [ ] **Step 6: Create root index.ts**

`packages/shared/src/index.ts`:
```typescript
export * from './enums.js';
export * from './models/index.js';
export * from './schemas/index.js';
```

- [ ] **Step 7: Install dependencies and build**

```bash
cd packages/shared
npm install
npm run build
```

Run: `npm run build`
Expected: Clean compilation, `dist/` folder created with `.js` and `.d.ts` files.

- [ ] **Step 8: Commit shared package**

```bash
git add packages/shared/
git commit -m "feat: add shared TypeScript package with domain types, enums, and Zod schemas"
```

---

## Chunk 2: .NET API Project Scaffold

### Task 3: Create .NET Solution and API Project

**Files:**
- Create: `packages/api/Tungsten.sln`
- Create: `packages/api/src/Tungsten.Api/Tungsten.Api.csproj`
- Create: `packages/api/src/Tungsten.Api/Program.cs`
- Create: `packages/api/src/Tungsten.Api/appsettings.json`
- Create: `packages/api/src/Tungsten.Api/appsettings.Development.json`
- Create: `packages/api/tests/Tungsten.Api.Tests/Tungsten.Api.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

```bash
cd packages/api
dotnet new sln -n Tungsten
mkdir -p src/Tungsten.Api
cd src/Tungsten.Api
dotnet new webapi --no-https -n Tungsten.Api --use-program-main false
cd ../..
dotnet sln add src/Tungsten.Api/Tungsten.Api.csproj
mkdir -p tests/Tungsten.Api.Tests
cd tests/Tungsten.Api.Tests
dotnet new xunit -n Tungsten.Api.Tests
cd ../..
dotnet sln add tests/Tungsten.Api.Tests/Tungsten.Api.Tests.csproj
dotnet add tests/Tungsten.Api.Tests reference src/Tungsten.Api
```

- [ ] **Step 2: Add NuGet packages to API project**

```bash
cd packages/api/src/Tungsten.Api
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package MediatR
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Sentry.AspNetCore
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.Console
```

- [ ] **Step 3: Add NuGet packages to test project**

```bash
cd packages/api/tests/Tungsten.Api.Tests
dotnet add package NSubstitute
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers.PostgreSql
dotnet add package FluentAssertions
```

- [ ] **Step 4: Update API .csproj settings**

Edit `packages/api/src/Tungsten.Api/Tungsten.Api.csproj` to ensure:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <!-- ... package references below ... -->
</Project>
```

- [ ] **Step 5: Create appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Auth0": {
    "Domain": "",
    "Audience": ""
  },
  "Sentry": {
    "Dsn": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- [ ] **Step 6: Create appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "{CONNECTION_STRING_FROM_docs/neon.secrets}"
  },
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN",
    "Audience": "YOUR_AUTH0_AUDIENCE"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

**Important:** Add `appsettings.Development.json` to `.gitignore` since it contains the real Neon credentials:
```
# .NET dev settings with secrets
packages/api/src/Tungsten.Api/appsettings.Development.json
```

- [ ] **Step 7: Verify build**

```bash
cd packages/api
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 8: Commit**

```bash
git add packages/api/ .gitignore
git commit -m "chore: scaffold .NET solution with API and test projects"
```

---

### Task 4: EF Core DbContext and Entity Configuration

**Files:**
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/TenantEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/BatchEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/CustodyEventEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/DocumentEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/ComplianceCheckEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/GeneratedDocumentEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/NotificationEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/JobEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/RmapSmelterEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/RiskCountryEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/SanctionedEntityEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/` (one per entity)

- [ ] **Step 1: Create entity classes**

Each entity maps to a database table. Using file-scoped namespaces, records for simple entities.

`TenantEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class TenantEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string SchemaPrefix { get; set; }
    public required string Status { get; set; } // ACTIVE, SUSPENDED
    public DateTime CreatedAt { get; set; }

    public ICollection<UserEntity> Users { get; set; } = [];
    public ICollection<BatchEntity> Batches { get; set; } = [];
}
```

`UserEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public required string Auth0Sub { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string Role { get; set; } // SUPPLIER, BUYER, PLATFORM_ADMIN
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
}
```

`BatchEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class BatchEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string BatchNumber { get; set; }
    public required string MineralType { get; set; }
    public required string OriginCountry { get; set; }
    public required string OriginMine { get; set; }
    public decimal WeightKg { get; set; }
    public required string Status { get; set; } // BatchStatus enum
    public required string ComplianceStatus { get; set; } // ComplianceStatus enum
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity Creator { get; set; } = null!;
    public ICollection<CustodyEventEntity> CustodyEvents { get; set; } = [];
    public ICollection<DocumentEntity> Documents { get; set; } = [];
}
```

`CustodyEventEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class CustodyEventEntity
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public required string EventType { get; set; } // CustodyEventType enum
    public required string IdempotencyKey { get; set; }
    public DateTime EventDate { get; set; }
    public required string Location { get; set; }
    public string? GpsCoordinates { get; set; }
    public required string ActorName { get; set; }
    public string? SmelterId { get; set; }
    public required string Description { get; set; }
    public JsonElement? Metadata { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public bool IsCorrection { get; set; }
    public Guid? CorrectsEventId { get; set; }
    public required string Sha256Hash { get; set; }
    public string? PreviousEventHash { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public BatchEntity Batch { get; set; } = null!;
    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity Creator { get; set; } = null!;
    public CustodyEventEntity? CorrectsEvent { get; set; }
    public ICollection<DocumentEntity> Documents { get; set; } = [];
    public ICollection<ComplianceCheckEntity> ComplianceChecks { get; set; } = [];
}
```

`DocumentEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class DocumentEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CustodyEventId { get; set; }
    public Guid BatchId { get; set; }
    public required string FileName { get; set; }
    public required string StorageKey { get; set; }
    public long FileSizeBytes { get; set; }
    public required string ContentType { get; set; }
    public required string Sha256Hash { get; set; }
    public required string DocumentType { get; set; } // DocumentType enum
    public Guid UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
    public CustodyEventEntity? CustodyEvent { get; set; }
    public BatchEntity Batch { get; set; } = null!;
    public UserEntity Uploader { get; set; } = null!;
}
```

`ComplianceCheckEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class ComplianceCheckEntity
{
    public Guid Id { get; set; }
    public Guid CustodyEventId { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public required string Framework { get; set; } // RMAP, OECD_DDG
    public required string Status { get; set; } // ComplianceCheckStatus enum
    public JsonElement? Details { get; set; }
    public DateTime CheckedAt { get; set; }

    public CustodyEventEntity CustodyEvent { get; set; } = null!;
    public BatchEntity Batch { get; set; } = null!;
}
```

`GeneratedDocumentEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class GeneratedDocumentEntity
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public required string DocumentType { get; set; } // MATERIAL_PASSPORT, AUDIT_DOSSIER
    public required string StorageKey { get; set; }
    public Guid GeneratedBy { get; set; }
    public string? ShareToken { get; set; }
    public DateTime? ShareExpiresAt { get; set; }
    public DateTime GeneratedAt { get; set; }

    public BatchEntity Batch { get; set; } = null!;
    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity Generator { get; set; } = null!;
}
```

`NotificationEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class NotificationEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public required string Type { get; set; } // NotificationType enum
    public required string Title { get; set; }
    public required string Message { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public bool EmailSent { get; set; }
    public int EmailRetryCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity User { get; set; } = null!;
}
```

`JobEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class JobEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string JobType { get; set; } // JobType enum
    public required string Status { get; set; } // JobStatus enum
    public Guid ReferenceId { get; set; }
    public string? ErrorDetail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
}
```

`RmapSmelterEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class RmapSmelterEntity
{
    public required string SmelterId { get; set; }
    public required string SmelterName { get; set; }
    public required string Country { get; set; }
    public required string ConformanceStatus { get; set; } // CONFORMANT, ACTIVE_PARTICIPATING, NON_CONFORMANT
    public DateOnly? LastAuditDate { get; set; }
    public DateTime LoadedAt { get; set; }
}
```

`RiskCountryEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class RiskCountryEntity
{
    public required string CountryCode { get; set; }
    public required string CountryName { get; set; }
    public required string RiskLevel { get; set; } // HIGH, MEDIUM, LOW
    public required string Source { get; set; }
}
```

`SanctionedEntityEntity.cs`:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class SanctionedEntityEntity
{
    public Guid Id { get; set; }
    public required string EntityName { get; set; }
    public required string EntityType { get; set; } // INDIVIDUAL, ORGANIZATION, COUNTRY
    public required string Source { get; set; }
    public DateTime LoadedAt { get; set; }
}
```

- [ ] **Step 2: Create AppDbContext**

`packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<BatchEntity> Batches => Set<BatchEntity>();
    public DbSet<CustodyEventEntity> CustodyEvents => Set<CustodyEventEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<ComplianceCheckEntity> ComplianceChecks => Set<ComplianceCheckEntity>();
    public DbSet<GeneratedDocumentEntity> GeneratedDocuments => Set<GeneratedDocumentEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<JobEntity> Jobs => Set<JobEntity>();
    public DbSet<RmapSmelterEntity> RmapSmelters => Set<RmapSmelterEntity>();
    public DbSet<RiskCountryEntity> RiskCountries => Set<RiskCountryEntity>();
    public DbSet<SanctionedEntityEntity> SanctionedEntities => Set<SanctionedEntityEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

- [ ] **Step 3: Create EF Core entity configurations**

Create one configuration file per entity in `Infrastructure/Persistence/Configurations/`. Key examples:

`TenantConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.SchemaPrefix).HasColumnName("schema_prefix").HasMaxLength(50).IsRequired();
        builder.HasIndex(t => t.SchemaPrefix).IsUnique();
        builder.Property(t => t.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
    }
}
```

`UserConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(u => u.Auth0Sub).HasColumnName("auth0_sub").HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Auth0Sub).IsUnique();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(300).IsRequired();
        builder.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
        builder.Property(u => u.TenantId).HasColumnName("tenant_id");
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.HasOne(u => u.Tenant).WithMany(t => t.Users).HasForeignKey(u => u.TenantId);
    }
}
```

`BatchConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class BatchConfiguration : IEntityTypeConfiguration<BatchEntity>
{
    public void Configure(EntityTypeBuilder<BatchEntity> builder)
    {
        builder.ToTable("batches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(b => b.TenantId).HasColumnName("tenant_id");
        builder.Property(b => b.BatchNumber).HasColumnName("batch_number").HasMaxLength(100).IsRequired();
        builder.HasIndex(b => new { b.TenantId, b.BatchNumber }).IsUnique();
        builder.Property(b => b.MineralType).HasColumnName("mineral_type").HasMaxLength(50).IsRequired();
        builder.Property(b => b.OriginCountry).HasColumnName("origin_country").HasMaxLength(2).IsRequired();
        builder.Property(b => b.OriginMine).HasColumnName("origin_mine").HasMaxLength(200).IsRequired();
        builder.Property(b => b.WeightKg).HasColumnName("weight_kg").HasPrecision(18, 4);
        builder.Property(b => b.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(b => b.ComplianceStatus).HasColumnName("compliance_status").HasMaxLength(30).IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.HasOne(b => b.Tenant).WithMany(t => t.Batches).HasForeignKey(b => b.TenantId);
        builder.HasOne(b => b.Creator).WithMany().HasForeignKey(b => b.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
```

`CustodyEventConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class CustodyEventConfiguration : IEntityTypeConfiguration<CustodyEventEntity>
{
    public void Configure(EntityTypeBuilder<CustodyEventEntity> builder)
    {
        builder.ToTable("custody_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.BatchId).HasColumnName("batch_id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(30).IsRequired();
        builder.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(500).IsRequired();
        builder.HasIndex(e => new { e.BatchId, e.IdempotencyKey }).IsUnique();
        builder.Property(e => e.EventDate).HasColumnName("event_date");
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(500).IsRequired();
        builder.Property(e => e.GpsCoordinates).HasColumnName("gps_coordinates").HasMaxLength(100);
        builder.Property(e => e.ActorName).HasColumnName("actor_name").HasMaxLength(300).IsRequired();
        builder.Property(e => e.SmelterId).HasColumnName("smelter_id").HasMaxLength(100);
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(e => e.SchemaVersion).HasColumnName("schema_version").HasDefaultValue(1);
        builder.Property(e => e.IsCorrection).HasColumnName("is_correction").HasDefaultValue(false);
        builder.Property(e => e.CorrectsEventId).HasColumnName("corrects_event_id");
        builder.Property(e => e.Sha256Hash).HasColumnName("sha256_hash").HasMaxLength(64).IsRequired();
        builder.Property(e => e.PreviousEventHash).HasColumnName("previous_event_hash").HasMaxLength(64);
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasOne(e => e.Batch).WithMany(b => b.CustodyEvents).HasForeignKey(e => e.BatchId);
        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Creator).WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.CorrectsEvent).WithMany().HasForeignKey(e => e.CorrectsEventId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

`DocumentConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(d => d.TenantId).HasColumnName("tenant_id");
        builder.Property(d => d.CustodyEventId).HasColumnName("custody_event_id");
        builder.Property(d => d.BatchId).HasColumnName("batch_id");
        builder.Property(d => d.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
        builder.Property(d => d.StorageKey).HasColumnName("storage_key").HasMaxLength(1000).IsRequired();
        builder.Property(d => d.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(d => d.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(d => d.Sha256Hash).HasColumnName("sha256_hash").HasMaxLength(64).IsRequired();
        builder.Property(d => d.DocumentType).HasColumnName("document_type").HasMaxLength(30).IsRequired();
        builder.Property(d => d.UploadedBy).HasColumnName("uploaded_by");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasOne(d => d.Tenant).WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.CustodyEvent).WithMany(e => e.Documents).HasForeignKey(d => d.CustodyEventId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(d => d.Batch).WithMany(b => b.Documents).HasForeignKey(d => d.BatchId);
        builder.HasOne(d => d.Uploader).WithMany().HasForeignKey(d => d.UploadedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
```

`ComplianceCheckConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class ComplianceCheckConfiguration : IEntityTypeConfiguration<ComplianceCheckEntity>
{
    public void Configure(EntityTypeBuilder<ComplianceCheckEntity> builder)
    {
        builder.ToTable("compliance_checks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.CustodyEventId).HasColumnName("custody_event_id");
        builder.Property(c => c.BatchId).HasColumnName("batch_id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Framework).HasColumnName("framework").HasMaxLength(20).IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(c => c.Details).HasColumnName("details").HasColumnType("jsonb");
        builder.Property(c => c.CheckedAt).HasColumnName("checked_at").HasDefaultValueSql("now()");
        builder.HasOne(c => c.CustodyEvent).WithMany(e => e.ComplianceChecks).HasForeignKey(c => c.CustodyEventId);
        builder.HasOne(c => c.Batch).WithMany().HasForeignKey(c => c.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

`GeneratedDocumentConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class GeneratedDocumentConfiguration : IEntityTypeConfiguration<GeneratedDocumentEntity>
{
    public void Configure(EntityTypeBuilder<GeneratedDocumentEntity> builder)
    {
        builder.ToTable("generated_documents");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(g => g.BatchId).HasColumnName("batch_id");
        builder.Property(g => g.TenantId).HasColumnName("tenant_id");
        builder.Property(g => g.DocumentType).HasColumnName("document_type").HasMaxLength(30).IsRequired();
        builder.Property(g => g.StorageKey).HasColumnName("storage_key").HasMaxLength(1000).IsRequired();
        builder.Property(g => g.GeneratedBy).HasColumnName("generated_by");
        builder.Property(g => g.ShareToken).HasColumnName("share_token").HasMaxLength(200);
        builder.HasIndex(g => g.ShareToken).IsUnique().HasFilter("share_token IS NOT NULL");
        builder.Property(g => g.ShareExpiresAt).HasColumnName("share_expires_at");
        builder.Property(g => g.GeneratedAt).HasColumnName("generated_at").HasDefaultValueSql("now()");
        builder.HasOne(g => g.Batch).WithMany().HasForeignKey(g => g.BatchId);
        builder.HasOne(g => g.Tenant).WithMany().HasForeignKey(g => g.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(g => g.Generator).WithMany().HasForeignKey(g => g.GeneratedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
```

`NotificationConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(n => n.TenantId).HasColumnName("tenant_id");
        builder.Property(n => n.UserId).HasColumnName("user_id");
        builder.Property(n => n.Type).HasColumnName("type").HasMaxLength(30).IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(n => n.Message).HasColumnName("message").IsRequired();
        builder.Property(n => n.ReferenceId).HasColumnName("reference_id");
        builder.Property(n => n.IsRead).HasColumnName("is_read").HasDefaultValue(false);
        builder.Property(n => n.EmailSent).HasColumnName("email_sent").HasDefaultValue(false);
        builder.Property(n => n.EmailRetryCount).HasColumnName("email_retry_count").HasDefaultValue(0);
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasOne(n => n.Tenant).WithMany().HasForeignKey(n => n.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
```

`JobConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<JobEntity>
{
    public void Configure(EntityTypeBuilder<JobEntity> builder)
    {
        builder.ToTable("jobs");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(j => j.TenantId).HasColumnName("tenant_id");
        builder.Property(j => j.JobType).HasColumnName("job_type").HasMaxLength(30).IsRequired();
        builder.Property(j => j.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(j => j.ReferenceId).HasColumnName("reference_id");
        builder.Property(j => j.ErrorDetail).HasColumnName("error_detail");
        builder.Property(j => j.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at");
        builder.HasOne(j => j.Tenant).WithMany().HasForeignKey(j => j.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(j => j.Status);
    }
}
```

`RmapSmelterConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class RmapSmelterConfiguration : IEntityTypeConfiguration<RmapSmelterEntity>
{
    public void Configure(EntityTypeBuilder<RmapSmelterEntity> builder)
    {
        builder.ToTable("rmap_smelters");
        builder.HasKey(s => s.SmelterId);
        builder.Property(s => s.SmelterId).HasColumnName("smelter_id").HasMaxLength(50);
        builder.Property(s => s.SmelterName).HasColumnName("smelter_name").HasMaxLength(300).IsRequired();
        builder.Property(s => s.Country).HasColumnName("country").HasMaxLength(2).IsRequired();
        builder.Property(s => s.ConformanceStatus).HasColumnName("conformance_status").HasMaxLength(30).IsRequired();
        builder.Property(s => s.LastAuditDate).HasColumnName("last_audit_date");
        builder.Property(s => s.LoadedAt).HasColumnName("loaded_at").HasDefaultValueSql("now()");
    }
}
```

`RiskCountryConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class RiskCountryConfiguration : IEntityTypeConfiguration<RiskCountryEntity>
{
    public void Configure(EntityTypeBuilder<RiskCountryEntity> builder)
    {
        builder.ToTable("risk_countries");
        builder.HasKey(r => r.CountryCode);
        builder.Property(r => r.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        builder.Property(r => r.CountryName).HasColumnName("country_name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.RiskLevel).HasColumnName("risk_level").HasMaxLength(10).IsRequired();
        builder.Property(r => r.Source).HasColumnName("source").HasMaxLength(200).IsRequired();
    }
}
```

`SanctionedEntityConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class SanctionedEntityConfiguration : IEntityTypeConfiguration<SanctionedEntityEntity>
{
    public void Configure(EntityTypeBuilder<SanctionedEntityEntity> builder)
    {
        builder.ToTable("sanctioned_entities");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(s => s.EntityName).HasColumnName("entity_name").HasMaxLength(500).IsRequired();
        builder.Property(s => s.EntityType).HasColumnName("entity_type").HasMaxLength(20).IsRequired();
        builder.Property(s => s.Source).HasColumnName("source").HasMaxLength(200).IsRequired();
        builder.Property(s => s.LoadedAt).HasColumnName("loaded_at").HasDefaultValueSql("now()");
        builder.HasIndex(s => s.EntityName);
    }
}
```

- [ ] **Step 4: Verify build**

```bash
cd packages/api
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add packages/api/
git commit -m "feat: add EF Core entities, configurations, and DbContext for all data model tables"
```

---

### Task 5: Database Migration

**Files:**
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Migrations/` (auto-generated)

- [ ] **Step 1: Add EF Core Design package**

```bash
cd packages/api/src/Tungsten.Api
dotnet add package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Step 2: Wire up DbContext in Program.cs temporarily for migration**

Minimal `Program.cs` to enable migration generation:
```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

- [ ] **Step 3: Generate initial migration**

```bash
cd packages/api
dotnet ef migrations add InitialCreate --project src/Tungsten.Api --startup-project src/Tungsten.Api
```

Expected: Migration files created in `Migrations/` folder.

- [ ] **Step 4: Apply migration to Neon database**

```bash
cd packages/api
dotnet ef database update --project src/Tungsten.Api --startup-project src/Tungsten.Api
```

Expected: Tables created in Neon PostgreSQL.

- [ ] **Step 5: Verify by running the app**

```bash
cd packages/api/src/Tungsten.Api
dotnet run
```

Then test: `curl http://localhost:5000/health`
Expected: `{"status":"healthy"}`

- [ ] **Step 6: Commit**

```bash
git add packages/api/
git commit -m "feat: add initial EF Core migration and health check endpoint"
```

---

## Chunk 3: Auth0 Integration and Program.cs Setup

### Task 6: Auth0 JWT Authentication

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs`
- Create: `packages/api/src/Tungsten.Api/Common/Auth/AuthorizationPolicies.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Auth/GetMe.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Auth/GetMeTests.cs`

- [ ] **Step 1: Write failing test for GetMe handler**

`packages/api/tests/Tungsten.Api.Tests/Features/Auth/GetMeTests.cs`:
```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Auth;

public class GetMeTests
{
    [Fact]
    public async Task Handle_ValidAuth0Sub_ReturnsUserProfile()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Corp",
            SchemaPrefix = "tenant_test",
            Status = "ACTIVE"
        };
        db.Tenants.Add(tenant);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Auth0Sub = "auth0|123",
            Email = "test@example.com",
            DisplayName = "Test User",
            Role = "SUPPLIER",
            TenantId = tenant.Id,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|123");

        var handler = new GetMe.Handler(db, currentUser);

        // Act
        var result = await handler.Handle(new GetMe.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Role.Should().Be("SUPPLIER");
        result.Value.TenantName.Should().Be("Test Corp");
    }

    [Fact]
    public async Task Handle_UnknownAuth0Sub_ReturnsNotFound()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|unknown");

        var handler = new GetMe.Handler(db, currentUser);

        var result = await handler.Handle(new GetMe.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd packages/api
dotnet test --filter "GetMeTests"
```

Expected: FAIL — types don't exist yet.

- [ ] **Step 3: Create Result type**

`packages/api/src/Tungsten.Api/Common/Result.cs`:
```csharp
namespace Tungsten.Api.Common;

public class Result<T>
{
    public T Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Value = default!; Error = error; IsSuccess = false; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

public class Result
{
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result() { IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }

    public static Result Success() => new();
    public static Result Failure(string error) => new(error);
}
```

- [ ] **Step 4: Create ICurrentUserService**

`packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs`:
```csharp
using System.Security.Claims;

namespace Tungsten.Api.Common.Auth;

public interface ICurrentUserService
{
    string Auth0Sub { get; }
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string Auth0Sub =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("No authenticated user");
}
```

- [ ] **Step 5: Create authorization policies**

`packages/api/src/Tungsten.Api/Common/Auth/AuthorizationPolicies.cs`:
```csharp
namespace Tungsten.Api.Common.Auth;

public static class Roles
{
    public const string Supplier = "SUPPLIER";
    public const string Buyer = "BUYER";
    public const string Admin = "PLATFORM_ADMIN";
}
```

`packages/api/src/Tungsten.Api/Common/Auth/RoleAuthorizationHandler.cs`:

Since roles live in the platform database (not JWT claims), we use a custom authorization handler that looks up the user's role from the DB on each request.

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public class RoleRequirement(params string[] allowedRoles) : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; } = allowedRoles;
}

public class TenantAccessRequirement : IAuthorizationRequirement;

public class RoleAuthorizationHandler(AppDbContext db, ICurrentUserService currentUser)
    : AuthorizationHandler<RoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive);

        if (user is null) return;

        // PLATFORM_ADMIN has access to everything
        if (user.Role == Roles.Admin || requirement.AllowedRoles.Contains(user.Role))
            context.Succeed(requirement);
    }
}

public class TenantAccessHandler(AppDbContext db, ICurrentUserService currentUser, IHttpContextAccessor httpContext)
    : AuthorizationHandler<TenantAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TenantAccessRequirement requirement)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive);

        if (user is null) return;

        // For now, tenant access is validated by checking the user exists and is active.
        // In Phase 2+, endpoints that take tenantId will validate user.TenantId matches.
        context.Succeed(requirement);
    }
}
```

`packages/api/src/Tungsten.Api/Common/Auth/AuthorizationPolicies.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;

namespace Tungsten.Api.Common.Auth;

public static class AuthorizationPolicies
{
    public const string RequireSupplier = "RequireSupplier";
    public const string RequireBuyer = "RequireBuyer";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireTenantAccess = "RequireTenantAccess";

    public static void AddTungstenPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(RequireSupplier, policy =>
            policy.Requirements.Add(new RoleRequirement(Roles.Supplier)));

        options.AddPolicy(RequireBuyer, policy =>
            policy.Requirements.Add(new RoleRequirement(Roles.Buyer)));

        options.AddPolicy(RequireAdmin, policy =>
            policy.Requirements.Add(new RoleRequirement(Roles.Admin)));

        options.AddPolicy(RequireTenantAccess, policy =>
            policy.Requirements.Add(new TenantAccessRequirement()));
    }
}
```

- [ ] **Step 6: Create GetMe handler (Vertical Slice)**

`packages/api/src/Tungsten.Api/Features/Auth/GetMe.cs`:
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Auth;

public static class GetMe
{
    public record Query : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Email,
        string DisplayName,
        string Role,
        Guid TenantId,
        string TenantName);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var user = await db.Users
                .AsNoTracking()
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub, ct);

            if (user is null || !user.IsActive)
                return Result<Response>.Failure("User not found");

            return Result<Response>.Success(new Response(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Role,
                user.TenantId,
                user.Tenant.Name));
        }
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
cd packages/api
dotnet test --filter "GetMeTests"
```

Expected: 2 tests pass.

- [ ] **Step 8: Wire up full Program.cs**

`packages/api/src/Tungsten.Api/Program.cs`:
```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Middleware;
using Tungsten.Api.Features.Auth;
using Tungsten.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auth0
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

if (!string.IsNullOrEmpty(auth0Domain))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Domain}/";
            options.Audience = auth0Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://{auth0Domain}/",
                ValidateAudience = true,
                ValidAudience = auth0Audience,
                ValidateLifetime = true,
            };
        });
}

builder.Services.AddAuthorization(options => options.AddTungstenPolicies());
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TenantAccessHandler>();

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetMe>());

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<GetMe>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// Sentry
if (!string.IsNullOrEmpty(builder.Configuration["Sentry:Dsn"]))
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = builder.Configuration["Sentry:Dsn"];
        o.TracesSampleRate = 0.2;
    });
}

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("tungsten-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// JSON
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply migrations and seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedAsync(db);
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<AuditLoggingMiddleware>();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Auth endpoints
app.MapGet("/api/me", async (IMediator mediator) =>
{
    var result = await mediator.Send(new GetMe.Query());
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.NotFound(new { error = result.Error });
}).RequireAuthorization();

app.Run();

// Make Program accessible for WebApplicationFactory
public partial class Program { }
```

Add FluentValidation using at top of Program.cs:
```csharp
using FluentValidation;
```

- [ ] **Step 9: Verify build and run**

```bash
cd packages/api
dotnet build
dotnet test
```

Expected: Build succeeds. All tests pass.

- [ ] **Step 10: Commit**

```bash
git add packages/api/
git commit -m "feat: add Auth0 JWT authentication, GetMe endpoint, Result pattern, and Program.cs wiring"
```

---

### Task 7: Audit Logging Middleware

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Middleware/AuditLoggingMiddleware.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create audit logging middleware**

`packages/api/src/Tungsten.Api/Common/Middleware/AuditLoggingMiddleware.cs`:
```csharp
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Tungsten.Api.Common.Middleware;

public class AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
{
    private static readonly HashSet<string> WriteMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!WriteMethods.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        var bodyHash = await ComputeRequestBodyHash(context.Request);

        await next(context);

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        logger.LogInformation(
            "AUDIT: {Timestamp} | User: {UserId} | {Method} {Path} | BodyHash: {BodyHash} | Status: {StatusCode}",
            DateTime.UtcNow.ToString("O"),
            userId,
            context.Request.Method,
            context.Request.Path,
            bodyHash,
            context.Response.StatusCode);
    }

    private static async Task<string> ComputeRequestBodyHash(HttpRequest request)
    {
        request.Body.Position = 0;
        var body = await new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
        request.Body.Position = 0;

        if (string.IsNullOrEmpty(body))
            return "empty";

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexStringLower(hashBytes);
    }
}
```

- [ ] **Step 2: Register middleware in Program.cs**

Add after `app.UseRateLimiter();`:
```csharp
app.UseMiddleware<AuditLoggingMiddleware>();
```

Add using:
```csharp
using Tungsten.Api.Common.Middleware;
```

- [ ] **Step 3: Verify build**

```bash
cd packages/api
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add packages/api/
git commit -m "feat: add audit logging middleware for all write operations (NFR-P08)"
```

---

### Task 7b: Unit Tests for Auth Policies and CurrentUserService

**Files:**
- Create: `packages/api/tests/Tungsten.Api.Tests/Common/Auth/RoleAuthorizationHandlerTests.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Common/Auth/CurrentUserServiceTests.cs`

- [ ] **Step 1: Write role authorization handler tests**

`packages/api/tests/Tungsten.Api.Tests/Common/Auth/RoleAuthorizationHandlerTests.cs`:
```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Common.Auth;

public class RoleAuthorizationHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SupplierPolicy_SupplierUser_Succeeds()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|1", Email = "s@test.com",
            DisplayName = "Supplier", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|1");

        var handler = new RoleAuthorizationHandler(db, currentUser);
        var requirement = new RoleRequirement(Roles.Supplier);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task SupplierPolicy_BuyerUser_Fails()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|2", Email = "b@test.com",
            DisplayName = "Buyer", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|2");

        var handler = new RoleAuthorizationHandler(db, currentUser);
        var requirement = new RoleRequirement(Roles.Supplier);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task AnyPolicy_AdminUser_AlwaysSucceeds()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|admin", Email = "a@test.com",
            DisplayName = "Admin", Role = "PLATFORM_ADMIN", TenantId = tenant.Id, IsActive = true
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|admin");

        var handler = new RoleAuthorizationHandler(db, currentUser);
        var requirement = new RoleRequirement(Roles.Supplier); // Admin should pass ANY policy
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task AnyPolicy_InactiveUser_Fails()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|inactive", Email = "i@test.com",
            DisplayName = "Inactive", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = false
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|inactive");

        var handler = new RoleAuthorizationHandler(db, currentUser);
        var requirement = new RoleRequirement(Roles.Supplier);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
cd packages/api
dotnet test --filter "RoleAuthorizationHandlerTests"
```

Expected: 4 tests pass.

- [ ] **Step 3: Commit**

```bash
git add packages/api/tests/
git commit -m "test: add unit tests for role authorization handler (4 scenarios)"
```

---

## Chunk 4: Angular Scaffold with Tailwind and Auth0

### Task 8: Angular Project Setup

**Files:**
- Create: `packages/web/` (Angular CLI scaffolded)

- [ ] **Step 1: Scaffold Angular project**

```bash
cd packages
ng new web --routing --style=scss --ssr=false --skip-git
```

- [ ] **Step 2: Install Tailwind CSS 4**

```bash
cd packages/web
npm install tailwindcss @tailwindcss/postcss postcss --save-dev
```

Create `packages/web/postcss.config.js`:
```javascript
module.exports = {
  plugins: {
    "@tailwindcss/postcss": {},
  },
};
```

Rename `packages/web/src/styles.scss` to `packages/web/src/styles.css` (Tailwind v4 works better with plain CSS). Update `angular.json` to reference `styles.css` instead of `styles.scss`.

`packages/web/src/styles.css`:
```css
@import "tailwindcss";

/* Tungsten Design System */
:root {
  --color-primary: #2563eb;
  --color-success: #22c55e;
  --color-warning: #f59e0b;
  --color-danger: #ef4444;
  --color-bg: #f8fafc;
  --color-surface: #ffffff;
  --color-text: #0f172a;
  --color-text-muted: #64748b;
  --color-border: #e2e8f0;
}

body {
  background-color: var(--color-bg);
  color: var(--color-text);
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}
```

- [ ] **Step 3: Add Inter font**

In `packages/web/src/index.html`, add in `<head>`:
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
```

- [ ] **Step 4: Verify Angular builds**

```bash
cd packages/web
ng build
```

Expected: Build succeeds, Tailwind classes are processed.

- [ ] **Step 5: Commit**

```bash
git add packages/web/
git commit -m "feat: scaffold Angular 21 project with Tailwind CSS 4 and Inter font"
```

---

### Task 9: Angular Auth0 Integration and Core Module

**Files:**
- Create: `packages/web/src/app/core/auth/auth.service.ts`
- Create: `packages/web/src/app/core/auth/auth.guard.ts`
- Create: `packages/web/src/app/core/auth/auth.interceptor.ts`
- Create: `packages/web/src/app/core/auth/role.guard.ts`
- Create: `packages/web/src/app/core/http/error.interceptor.ts`
- Create: `packages/web/src/app/core/http/api-url.token.ts`
- Create: `packages/web/src/app/core/layout/shell.component.ts`
- Create: `packages/web/src/app/core/layout/sidebar.component.ts`
- Create: `packages/web/src/app/core/layout/topbar.component.ts`
- Modify: `packages/web/src/app/app.config.ts`
- Modify: `packages/web/src/app/app.routes.ts`
- Modify: `packages/web/src/app/app.component.ts`

- [ ] **Step 1: Install Auth0 Angular SDK**

```bash
cd packages/web
npm install @auth0/auth0-angular
```

- [ ] **Step 2: Create API URL token**

`packages/web/src/app/core/http/api-url.token.ts`:
```typescript
import { InjectionToken } from '@angular/core';

export const API_URL = new InjectionToken<string>('API_URL', {
  providedIn: 'root',
  factory: () => 'http://localhost:5000',
});
```

- [ ] **Step 3: Create auth service**

`packages/web/src/app/core/auth/auth.service.ts`:
```typescript
import { Injectable, inject, signal, computed } from '@angular/core';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, of, catchError } from 'rxjs';
import { API_URL } from '../http/api-url.token';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  role: 'SUPPLIER' | 'BUYER' | 'PLATFORM_ADMIN';
  tenantId: string;
  tenantName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private auth0 = inject(Auth0Service);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  readonly isAuthenticated = toSignal(this.auth0.isAuthenticated$, { initialValue: false });
  readonly isLoading = toSignal(this.auth0.isLoading$, { initialValue: true });

  private _profile = signal<UserProfile | null>(null);
  readonly profile = this._profile.asReadonly();
  readonly role = computed(() => this._profile()?.role ?? null);

  login() {
    this.auth0.loginWithRedirect();
  }

  logout() {
    this.auth0.logout({ logoutParams: { returnTo: window.location.origin } });
  }

  loadProfile() {
    this.http.get<UserProfile>(`${this.apiUrl}/api/me`).pipe(
      catchError(() => of(null))
    ).subscribe(profile => {
      this._profile.set(profile);
    });
  }
}
```

- [ ] **Step 4: Create auth guard and role guard**

`packages/web/src/app/core/auth/auth.guard.ts`:
```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;
  auth.login();
  return false;
};
```

`packages/web/src/app/core/auth/role.guard.ts`:
```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function roleGuard(...allowedRoles: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    const role = auth.role();
    if (!role) return router.parseUrl('/login');

    // PLATFORM_ADMIN has access to all routes
    if (role === 'PLATFORM_ADMIN' || allowedRoles.includes(role)) return true;

    return router.parseUrl('/unauthorized');
  };
}
```

- [ ] **Step 5: Create interceptors**

`packages/web/src/app/core/auth/auth.interceptor.ts`:
```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { switchMap, take } from 'rxjs';
import { API_URL } from '../http/api-url.token';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = inject(API_URL);

  // Only attach token to API requests — skip external URLs, Auth0, CDN, etc.
  if (!req.url.startsWith(apiUrl)) {
    return next(req);
  }

  const auth0 = inject(Auth0Service);

  return auth0.getAccessTokenSilently().pipe(
    take(1),
    switchMap(token => {
      const cloned = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      });
      return next(cloned);
    })
  );
};
```

`packages/web/src/app/core/http/error.interceptor.ts`:
```typescript
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        // Auth0 will handle token refresh or redirect
      }
      console.error(`[HTTP Error] ${err.status} ${req.method} ${req.url}`, err.error);
      return throwError(() => err);
    })
  );
```

- [ ] **Step 6: Create shell layout (sidebar + topbar)**

`packages/web/src/app/core/layout/sidebar.component.ts`:
```typescript
import { Component, input, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../auth/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <aside class="w-64 bg-slate-900 text-white min-h-screen flex flex-col">
      <div class="p-6 border-b border-slate-700">
        <h1 class="text-lg font-bold tracking-tight">Tungsten</h1>
        <p class="text-xs text-slate-400 mt-1">Supply Chain Compliance</p>
      </div>
      <nav class="flex-1 p-4 space-y-1">
        @for (item of navItems(); track item.route) {
          <a
            [routerLink]="item.route"
            routerLinkActive="bg-slate-700 text-white"
            class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-slate-300 hover:bg-slate-800 hover:text-white transition-colors"
          >
            <span class="text-base">{{ item.icon }}</span>
            {{ item.label }}
          </a>
        }
      </nav>
    </aside>
  `,
})
export class SidebarComponent {
  private auth = inject(AuthService);

  readonly navItems = computed<NavItem[]>(() => {
    const role = this.auth.role();
    switch (role) {
      case 'SUPPLIER':
        return [
          { label: 'Dashboard', route: '/supplier', icon: '\u{1F4CA}' },
          { label: 'Submit Event', route: '/supplier/submit', icon: '\u{2795}' },
        ];
      case 'BUYER':
        return [
          { label: 'Dashboard', route: '/buyer', icon: '\u{1F4CA}' },
        ];
      case 'PLATFORM_ADMIN':
        return [
          { label: 'Dashboard', route: '/admin', icon: '\u{1F4CA}' },
          { label: 'Users', route: '/admin/users', icon: '\u{1F465}' },
          { label: 'RMAP Data', route: '/admin/rmap', icon: '\u{1F4C4}' },
          { label: 'Compliance', route: '/admin/compliance', icon: '\u{2705}' },
        ];
      default:
        return [];
    }
  });
}
```

`packages/web/src/app/core/layout/topbar.component.ts`:
```typescript
import { Component, inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  template: `
    <header class="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6">
      <div></div>
      <div class="flex items-center gap-4">
        <span class="text-sm text-slate-600">{{ auth.profile()?.displayName }}</span>
        <button
          (click)="auth.logout()"
          class="text-sm text-slate-500 hover:text-slate-700 transition-colors"
        >
          Sign out
        </button>
      </div>
    </header>
  `,
})
export class TopbarComponent {
  protected auth = inject(AuthService);
}
```

`packages/web/src/app/core/layout/shell.component.ts`:
```typescript
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  template: `
    <div class="flex min-h-screen">
      <app-sidebar />
      <div class="flex-1 flex flex-col">
        <app-topbar />
        <main class="flex-1 p-6 bg-slate-50">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent {}
```

- [ ] **Step 7: Create environment files**

`packages/web/src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  auth0: {
    domain: 'YOUR_AUTH0_DOMAIN',
    clientId: 'YOUR_AUTH0_CLIENT_ID',
    audience: 'YOUR_AUTH0_AUDIENCE',
  },
};
```

`packages/web/src/environments/environment.production.ts`:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://YOUR_RENDER_API_URL',
  auth0: {
    domain: 'YOUR_AUTH0_DOMAIN',
    clientId: 'YOUR_AUTH0_CLIENT_ID',
    audience: 'YOUR_AUTH0_AUDIENCE',
  },
};
```

Update `packages/web/src/app/core/http/api-url.token.ts` to use environment:
```typescript
import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

export const API_URL = new InjectionToken<string>('API_URL', {
  providedIn: 'root',
  factory: () => environment.apiUrl,
});
```

- [ ] **Step 8: Update app.config.ts**

```typescript
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAuth0 } from '@auth0/auth0-angular';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideAuth0({
      domain: environment.auth0.domain,
      clientId: environment.auth0.clientId,
      useRefreshTokens: false, // FR-P064: no refresh tokens in pilot
      cacheLocation: 'localstorage',
      authorizationParams: {
        redirect_uri: typeof window !== 'undefined' ? window.location.origin : '',
        audience: environment.auth0.audience,
      },
    }),
  ],
};
```

- [ ] **Step 9: Update app.routes.ts**

```typescript
import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { ShellComponent } from './core/layout/shell.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'supplier',
        loadChildren: () => import('./features/supplier/supplier.routes').then(m => m.SUPPLIER_ROUTES),
        canActivate: [roleGuard('SUPPLIER')],
      },
      {
        path: 'buyer',
        loadChildren: () => import('./features/buyer/buyer.routes').then(m => m.BUYER_ROUTES),
        canActivate: [roleGuard('BUYER')],
      },
      {
        path: 'admin',
        loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES),
        canActivate: [roleGuard('PLATFORM_ADMIN')],
      },
    ],
  },
];
```

- [ ] **Step 10: Update app.component.ts**

```typescript
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class AppComponent {}
```

- [ ] **Step 11: Create placeholder feature routes and login page**

`packages/web/src/app/features/auth/login.component.ts`:
```typescript
import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50">
      <div class="bg-white p-8 rounded-xl shadow-sm border border-slate-200 max-w-md w-full text-center">
        <h1 class="text-2xl font-bold text-slate-900 mb-2">Tungsten</h1>
        <p class="text-slate-500 mb-6">Supply Chain Compliance Platform</p>
        <button
          (click)="auth.login()"
          class="w-full bg-blue-600 text-white py-2.5 px-4 rounded-lg font-medium hover:bg-blue-700 transition-colors"
        >
          Sign in
        </button>
      </div>
    </div>
  `,
})
export class LoginComponent {
  protected auth = inject(AuthService);
}
```

`packages/web/src/app/features/supplier/supplier.routes.ts`:
```typescript
import { Routes } from '@angular/router';

export const SUPPLIER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./supplier-dashboard.component').then(m => m.SupplierDashboardComponent),
  },
];
```

`packages/web/src/app/features/supplier/supplier-dashboard.component.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  template: `
    <div>
      <h2 class="text-xl font-semibold text-slate-900 mb-4">Supplier Dashboard</h2>
      <p class="text-slate-500">Dashboard content will be implemented in Phase 6.</p>
    </div>
  `,
})
export class SupplierDashboardComponent {}
```

`packages/web/src/app/features/buyer/buyer.routes.ts`:
```typescript
import { Routes } from '@angular/router';

export const BUYER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./buyer-dashboard.component').then(m => m.BuyerDashboardComponent),
  },
];
```

`packages/web/src/app/features/buyer/buyer-dashboard.component.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-buyer-dashboard',
  standalone: true,
  template: `
    <div>
      <h2 class="text-xl font-semibold text-slate-900 mb-4">Buyer Dashboard</h2>
      <p class="text-slate-500">Dashboard content will be implemented in Phase 7.</p>
    </div>
  `,
})
export class BuyerDashboardComponent {}
```

`packages/web/src/app/features/admin/admin.routes.ts`:
```typescript
import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./admin-dashboard.component').then(m => m.AdminDashboardComponent),
  },
];
```

`packages/web/src/app/features/admin/admin-dashboard.component.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  template: `
    <div>
      <h2 class="text-xl font-semibold text-slate-900 mb-4">Admin Dashboard</h2>
      <p class="text-slate-500">Dashboard content will be implemented in Phase 8.</p>
    </div>
  `,
})
export class AdminDashboardComponent {}
```

- [ ] **Step 12: Verify Angular build**

```bash
cd packages/web
ng build
```

Expected: Build succeeds.

- [ ] **Step 13: Commit**

```bash
git add packages/web/
git commit -m "feat: add Angular Auth0 integration, shell layout with sidebar/topbar, and placeholder feature routes"
```

---

## Chunk 5: .NET Worker Scaffold and Integration Test Setup

### Task 10: Worker Project Scaffold

**Files:**
- Create: `packages/worker/Tungsten.Worker.csproj`
- Create: `packages/worker/Program.cs`
- Create: `packages/worker/appsettings.json`

- [ ] **Step 1: Create worker project**

```bash
cd packages/worker
dotnet new worker -n Tungsten.Worker --force
```

- [ ] **Step 2: Add to solution**

```bash
cd packages/api
dotnet sln add ../worker/Tungsten.Worker.csproj
```

Note: In the current structure, the Worker references the API project to share the DbContext and entities. In a future refactoring (before Phase 3), the shared EF Core infrastructure should be extracted into a `Tungsten.Domain` class library to avoid the Worker pulling in ASP.NET Core dependencies. For now, this direct reference is acceptable for the pilot.

```bash
dotnet add ../worker/Tungsten.Worker.csproj reference src/Tungsten.Api/Tungsten.Api.csproj
```

- [ ] **Step 3: Add NuGet packages to worker**

```bash
cd packages/worker
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package MediatR
```

- [ ] **Step 4: Update worker Program.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<AppDbContext>());

var host = builder.Build();
host.Run();
```

- [ ] **Step 5: Verify build**

```bash
cd packages/api
dotnet build Tungsten.sln
```

Expected: Both API and Worker build successfully.

- [ ] **Step 6: Commit**

```bash
git add packages/worker/ packages/api/Tungsten.sln
git commit -m "feat: scaffold worker project with shared DbContext reference"
```

---

### Task 11: Integration Test Setup with Testcontainers

**Files:**
- Create: `packages/api/tests/Tungsten.Api.Tests/Integration/TestWebApplicationFactory.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Integration/HealthCheckTests.cs`

- [ ] **Step 1: Write failing integration test**

`packages/api/tests/Tungsten.Api.Tests/Integration/TestWebApplicationFactory.cs`:
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply migrations
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

`packages/api/tests/Tungsten.Api.Tests/Integration/HealthCheckTests.cs`:
```csharp
using System.Net;
using FluentAssertions;

namespace Tungsten.Api.Tests.Integration;

public class HealthCheckTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Run integration test**

```bash
cd packages/api
dotnet test --filter "HealthCheckTests"
```

Expected: PASS — health endpoint returns 200. (Requires Docker running for Testcontainers.)

- [ ] **Step 3: Commit**

```bash
git add packages/api/tests/
git commit -m "feat: add integration test infrastructure with Testcontainers PostgreSQL"
```

---

### Task 12: Seed Reference Data

**Files:**
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/SeedData.cs`

- [ ] **Step 1: Create seed data service**

`packages/api/src/Tungsten.Api/Infrastructure/Persistence/SeedData.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.RiskCountries.AnyAsync())
            return;

        // OECD Annex II high-risk countries (representative sample)
        db.RiskCountries.AddRange(
            new RiskCountryEntity { CountryCode = "CD", CountryName = "Democratic Republic of the Congo", RiskLevel = "HIGH", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "RW", CountryName = "Rwanda", RiskLevel = "HIGH", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "BI", CountryName = "Burundi", RiskLevel = "HIGH", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "UG", CountryName = "Uganda", RiskLevel = "HIGH", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "TZ", CountryName = "Tanzania", RiskLevel = "MEDIUM", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "KE", CountryName = "Kenya", RiskLevel = "LOW", Source = "OECD Annex II" }
        );

        // Sample RMAP smelters
        db.RmapSmelters.AddRange(
            new RmapSmelterEntity { SmelterId = "CID001100", SmelterName = "Wolfram Bergbau und Hütten AG", Country = "AT", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 6, 15), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID002158", SmelterName = "Global Tungsten & Powders Corp.", Country = "US", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 3, 10), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID002082", SmelterName = "Xiamen Tungsten Co., Ltd.", Country = "CN", ConformanceStatus = "ACTIVE_PARTICIPATING", LastAuditDate = new DateOnly(2025, 8, 22), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID000999", SmelterName = "Unaudited Smelter Example", Country = "XX", ConformanceStatus = "NON_CONFORMANT", LastAuditDate = null, LoadedAt = DateTime.UtcNow }
        );

        // Sample sanctioned entities
        db.SanctionedEntities.AddRange(
            new SanctionedEntityEntity { Id = Guid.NewGuid(), EntityName = "Sanctioned Mining Corp", EntityType = "ORGANIZATION", Source = "UN Security Council", LoadedAt = DateTime.UtcNow },
            new SanctionedEntityEntity { Id = Guid.NewGuid(), EntityName = "Restricted Trader LLC", EntityType = "ORGANIZATION", Source = "EU Sanctions List", LoadedAt = DateTime.UtcNow }
        );

        // Seed a pilot tenant
        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = "Pilot Tenant",
            SchemaPrefix = "tenant_pilot",
            Status = "ACTIVE"
        };
        db.Tenants.Add(tenant);

        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Call seed in Program.cs**

Add after `var app = builder.Build();`:
```csharp
// Apply migrations and seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedAsync(db);
}
```

- [ ] **Step 3: Verify**

```bash
cd packages/api
dotnet build
dotnet test
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add packages/api/
git commit -m "feat: add reference data seeding (risk countries, RMAP smelters, sanctions, pilot tenant)"
```

---

## Summary

**Phase 1 Foundation delivers:**
1. Git repo with monorepo structure
2. Shared TypeScript package with all domain types, enums, and Zod schemas
3. .NET API with EF Core, all 13 database tables, migration applied to Neon
4. Auth0 JWT authentication with `/api/me` endpoint
5. Result pattern, audit logging middleware, rate limiting, CORS
6. Angular 21 app with Tailwind CSS, Auth0 SDK, shell layout (sidebar + topbar)
7. Three lazy-loaded feature modules with placeholder dashboards
8. Worker project scaffold sharing DbContext with API
9. Integration test infrastructure (Testcontainers PostgreSQL)
10. Reference data seeding (risk countries, RMAP smelters, sanctions)

**Next plan:** Phase 2 — Custody Events (batch CRUD, event creation with SHA-256 hashing, idempotency, corrections, integrity verification)
