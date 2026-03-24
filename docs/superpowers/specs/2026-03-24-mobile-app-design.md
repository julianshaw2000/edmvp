# Mobile App — Design Spec (Future Phase)

**Date:** 2026-03-24
**Status:** Planned (not yet scheduled)
**Prerequisite:** PWA implemented, API keys available, core platform stable

---

## Overview

Native mobile app for field workers to log custody events at mine sites, processing facilities, and transport checkpoints. Designed for low-connectivity environments common in Central/East African mining regions.

---

## Target Users

- **Mine site operators** — log extraction events with GPS coordinates
- **Lab technicians** — record assay results on-site
- **Transport handlers** — log custody transfers at checkpoints
- **Processors** — record concentration and smelting events

---

## Core Features

### 1. Event Logging
- Select batch from list or scan QR code
- Log custody events with: event type, location (auto-GPS), actor name, description, metadata
- Camera capture for supporting photos (ore samples, documents, facility images)
- GPS auto-capture with manual override
- Offline queue — events stored locally when no connection, synced when online

### 2. Batch Viewer
- View assigned batches with status and compliance badges
- Event timeline per batch
- SHA-256 hash chain verification indicator

### 3. QR Scanner
- Scan Material Passport QR codes to view batch status
- Scan batch QR codes to quickly select batch for event logging

### 4. Authentication
- Auth0 native SDK (Google login)
- API key fallback for environments where OAuth redirect is problematic
- Biometric lock (fingerprint/face) for session persistence

---

## Technical Approach

### Recommended: React Native

| Option | Pros | Cons |
|--------|------|------|
| **React Native** | Cross-platform, large ecosystem, shared JS/TS with web | Bridge overhead, native module complexity |
| **Flutter** | Fast, beautiful UI, single codebase | Dart learning curve, less .NET ecosystem alignment |
| **.NET MAUI** | Same language as backend (.NET), native performance | Smaller ecosystem, fewer UI components |
| **Capacitor (Angular)** | Reuses existing Angular code | Limited native capability, hybrid performance |

React Native recommended — largest talent pool, best ecosystem for camera/GPS/offline, and TypeScript shared types with the Angular web app.

### Offline-First Architecture

```
┌─────────────────┐     ┌──────────────┐     ┌─────────────┐
│  Mobile App      │────▶│  Local SQLite │────▶│  Sync Engine │
│  (React Native)  │     │  (offline)    │     │  (on connect)│
└─────────────────┘     └──────────────┘     └──────┬──────┘
                                                     │
                                                     ▼
                                              ┌─────────────┐
                                              │  REST API    │
                                              │  (existing)  │
                                              └─────────────┘
```

- Events queued in local SQLite database
- Background sync when connectivity detected
- Conflict resolution: server wins (events are append-only, no conflicts)
- SHA-256 hash computed locally, verified on server

### API Integration

Uses the existing REST API with API key authentication (Phase 24):
- `GET /api/batches` — list assigned batches
- `POST /api/batches/{id}/events` — create custody event
- `GET /api/batches/{id}` — batch detail
- `GET /api/batches/{id}/events` — event timeline

No new backend endpoints needed — the existing API serves the mobile app.

---

## Screens

### 1. Login
- Google sign-in button
- API key entry (alternative)
- Biometric unlock (returning users)

### 2. Batch List
- Card-based list of assigned batches
- Search/filter by batch number, mineral, status
- Pull-to-refresh
- Offline indicator badge

### 3. Batch Detail
- Batch info (mineral, origin, weight, status)
- Event timeline with hash indicators
- "Log Event" floating action button
- Compliance status badges

### 4. Log Event
- Event type picker (extraction, assay, concentration, trading, smelting, export)
- Auto-populated GPS coordinates (with edit option)
- Actor name (auto-filled from profile, editable)
- Description field
- Photo capture button
- Metadata fields (dynamic based on event type)
- "Submit" button (queues locally if offline)

### 5. Sync Status
- List of pending events (not yet synced)
- Manual sync button
- Sync history with success/failure indicators

### 6. QR Scanner
- Full-screen camera view
- Auto-detect Material Passport QR codes
- Navigate to batch detail on scan

---

## Development Phases

### Phase A: Foundation (2-3 weeks)
- React Native project setup with TypeScript
- Auth0 native integration
- API client with API key support
- Navigation structure
- Batch list and detail screens

### Phase B: Event Logging (2-3 weeks)
- Event creation form with all event types
- GPS auto-capture
- Camera integration
- Local SQLite storage
- Offline queue

### Phase C: Sync Engine (1-2 weeks)
- Background sync service
- Conflict resolution
- Sync status UI
- Retry logic with exponential backoff

### Phase D: QR + Polish (1 week)
- QR code scanner integration
- Biometric authentication
- Push notification setup
- Performance optimization
- App store submission

---

## Infrastructure Requirements

- **Apple Developer Account** ($99/year) — required for iOS App Store
- **Google Play Developer Account** ($25 one-time) — required for Google Play
- **CI/CD** — EAS Build (Expo) or App Center for automated builds
- **Push Notifications** — Firebase Cloud Messaging (Android) + APNs (iOS)

---

## Out of Scope (v1)

- Material Passport PDF generation (use web app)
- Admin features (tenant management, user management)
- Compliance dashboard (use web app)
- Offline batch creation (batches must be created online)
- Real-time collaboration / live updates

---

## Success Criteria

1. Field worker can log a custody event at a mine site with GPS coordinates
2. Events queue locally when offline and sync automatically on reconnection
3. QR code scanning identifies batches instantly
4. SHA-256 hash chain maintained across mobile-logged events
5. App available on both iOS and Android via app stores
6. Login works via Google OAuth or API key
7. Photos can be attached to custody events
