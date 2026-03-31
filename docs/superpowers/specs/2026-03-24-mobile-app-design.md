# Mobile Field App — Design Spec (PWA)

**Date:** 2026-03-24 (updated 2026-03-31)
**Status:** Planned (not yet scheduled)
**Approach:** Progressive Web App — extends the existing Angular application
**Prerequisite:** Core platform stable, service worker configured

---

## Overview

Mobile-optimized PWA for field workers to log custody events at mine sites, processing facilities, and transport checkpoints. Designed for low-connectivity environments common in Central/East African mining regions.

**Why PWA over native app:**

| Factor | PWA (chosen) | Native (React Native) |
|--------|-------------|----------------------|
| Codebase | Existing Angular app — no new project | Entirely new codebase |
| Distribution | Share URL or "Add to Home Screen" | App store review (2-4 weeks) |
| Updates | Deploy to Render, instant for all users | Users must update via store |
| Cost | $0 additional infrastructure | Apple $99/yr + Google $25 + months of dev |
| Offline | Service worker + IndexedDB | SQLite — marginally better |
| Camera/GPS | Browser APIs — fully supported | Native APIs — marginally faster |
| QR scanning | Browser camera API | Native camera — marginally faster |

The marginal native advantages (background sync when app is closed, slightly faster camera) do not justify a separate codebase for the pilot.

---

## Target Users

- **Mine site operators** — log extraction events with GPS coordinates
- **Lab technicians** — record assay results on-site
- **Transport handlers** — log custody transfers at checkpoints
- **Processors** — record concentration and smelting events

---

## Core Features

### 1. Offline Event Logging
- Select batch from cached list or scan QR code
- Log custody events with: event type, location (auto-GPS), actor name, description, metadata
- Camera capture for supporting photos (`<input type="file" capture="environment">`)
- GPS auto-capture via browser Geolocation API with manual override
- **Offline queue** — events stored in IndexedDB when no connection, synced when online
- SHA-256 hash computed client-side before storage

### 2. Batch Viewer (Cached)
- View assigned batches with status and compliance badges
- Event timeline per batch
- Batches cached in IndexedDB on last successful sync
- Offline indicator when viewing cached data

### 3. QR Scanner
- Uses browser camera API (`navigator.mediaDevices.getUserMedia`)
- Scan Material Passport QR codes to view batch status
- Scan batch QR codes to quickly select batch for event logging
- Library: `jsQR` or `html5-qrcode` (lightweight, no native dependencies)

### 4. Authentication
- Email + password login (ASP.NET Core Identity — existing)
- JWT stored in memory, refresh token in HttpOnly cookie (existing)
- API key fallback for environments where cookie-based auth is problematic
- Session persists across app restarts via refresh token

### 5. Installability
- Web app manifest already configured (`manifest.webmanifest`)
- "Add to Home Screen" prompt — app appears as native icon
- Standalone display mode (no browser chrome)
- Splash screen with auditraks branding

---

## Technical Architecture

### Offline-First with Service Worker + IndexedDB

```
┌─────────────────────┐     ┌──────────────────┐
│  Angular PWA         │────▶│  Service Worker   │
│  (existing app +     │     │  (cache strategy)  │
│   mobile views)      │     └────────┬─────────┘
└──────────┬──────────┘              │
           │                          ▼
           ▼                   ┌──────────────┐
    ┌──────────────┐          │  Cache API    │
    │  IndexedDB    │          │  (app shell,  │
    │  (offline     │          │   static      │
    │   event queue │          │   assets)     │
    │   + batch     │          └──────────────┘
    │   cache)      │
    └──────┬───────┘
           │ (on reconnect)
           ▼
    ┌──────────────┐
    │  REST API     │
    │  (existing)   │
    └──────────────┘
```

### Service Worker Strategy

| Resource Type | Strategy | Rationale |
|--------------|----------|-----------|
| App shell (HTML, JS, CSS) | Cache-first, update in background | Fast load, always available |
| API data (batches, events) | Network-first, fallback to cache | Fresh when online, cached when offline |
| Static assets (icons, fonts) | Cache-first | Never changes between deploys |
| Event submissions | Queue in IndexedDB, replay on connect | Must not lose field data |

### IndexedDB Schema

```
auditraks_offline
├── pending_events     — events waiting to sync
│   ├── id (auto)
│   ├── batchId
│   ├── eventPayload (full request body)
│   ├── sha256Hash (computed client-side)
│   ├── createdAt
│   └── syncStatus ('pending' | 'syncing' | 'failed')
│
├── cached_batches     — last-known batch state
│   ├── id
│   ├── batchData (full BatchResponse)
│   └── cachedAt
│
└── sync_log           — sync attempt history
    ├── id (auto)
    ├── eventId
    ├── result ('success' | 'error')
    ├── errorMessage
    └── attemptedAt
```

### Sync Engine

- **Trigger:** `navigator.onLine` event + periodic check (every 30 seconds when app is open)
- **Process:** Read all `pending` events from IndexedDB → POST each to API → mark as `success` or `failed`
- **Retry:** Failed events retry on next sync cycle (up to 10 attempts, then flagged for manual review)
- **Conflict resolution:** Server wins — custody events are append-only, so no conflicts possible
- **Batch cache refresh:** After successful sync, re-fetch batch list to update cached state

### API Integration

Uses the existing REST API — no new backend endpoints needed:
- `GET /api/batches` — list assigned batches (cache in IndexedDB)
- `POST /api/batches/{id}/events` — create custody event (or queue offline)
- `GET /api/batches/{id}` — batch detail
- `GET /api/batches/{id}/events` — event timeline

---

## Mobile-Optimized Views

These are new Angular components added to the existing web app, shown when viewport is mobile-sized or when the app is installed as PWA.

### 1. Mobile Event Logger (`/supplier/submit-mobile`)
- Simplified single-screen event form optimized for touch
- Large tap targets, minimal scrolling
- Event type as large icon grid (not dropdown)
- GPS coordinates auto-filled with location name
- Camera button prominent at top
- "Submit" button fixed at bottom
- Offline indicator banner when disconnected
- Pending event count badge

### 2. Sync Status (`/supplier/sync`)
- List of pending events with status indicators
- Green = synced, amber = pending, red = failed
- Manual "Sync Now" button
- Pull-to-refresh
- Last sync timestamp

### 3. QR Scanner (`/supplier/scan`)
- Full-screen camera viewfinder
- Auto-detect QR codes
- On scan: navigate to batch detail or pre-fill batch in event logger

### Responsive Behavior

The existing supplier portal views (dashboard, batch detail, submit event) already work on mobile browsers. The mobile-specific views above are **additions** for field-optimized workflows, not replacements. Detection:

```typescript
// In a shared service
readonly isMobileInstalled = computed(() =>
  window.matchMedia('(display-mode: standalone)').matches
  || window.matchMedia('(display-mode: fullscreen)').matches
);
```

---

## Implementation Phases

### Phase A: Offline Queue + Service Worker (1-2 weeks)
- Configure Angular service worker with caching strategies
- Create `OfflineQueueService` using IndexedDB (via `idb` library)
- Queue event submissions when offline
- Sync engine that replays queued events on reconnect
- Offline indicator component (banner shown when disconnected)
- Pending event count badge on supplier dashboard

### Phase B: Mobile Event Logger (1 week)
- Mobile-optimized event submission component
- GPS auto-capture via Geolocation API
- Camera capture for photos
- Touch-optimized event type picker
- Fixed bottom submit button

### Phase C: QR Scanner + Batch Cache (1 week)
- QR scanner using `html5-qrcode` library
- Batch list caching in IndexedDB
- Scan-to-select batch flow
- Scan Material Passport QR to view batch

### Phase D: Install + Polish (3-5 days)
- "Install App" prompt for mobile users
- Splash screen and app icons
- Standalone display mode testing
- Performance optimization (lazy loading, image compression)
- Field testing documentation

---

## Infrastructure Requirements

- **None additional** — PWA runs on the existing Render infrastructure
- No Apple Developer Account needed
- No Google Play Developer Account needed
- No app store submissions or reviews
- No push notification infrastructure (use in-app notifications when online)

---

## Browser Compatibility

| Feature | Chrome Android | Safari iOS | Firefox Android |
|---------|---------------|------------|-----------------|
| Service Worker | Yes | Yes (iOS 16.4+) | Yes |
| IndexedDB | Yes | Yes | Yes |
| Camera (getUserMedia) | Yes | Yes | Yes |
| Geolocation | Yes | Yes | Yes |
| Add to Home Screen | Yes (auto-prompt) | Yes (manual) | Yes |
| Background Sync API | Yes | No | No |

**Note:** Safari iOS does not support the Background Sync API. The sync engine will use periodic polling when the app is open instead. This is acceptable — field workers open the app, log events, and close it. Sync happens while the app is open.

---

## Out of Scope (v1)

- Material Passport PDF generation (use desktop web app)
- Admin features (tenant management, user management)
- Compliance dashboard (use desktop web app)
- Offline batch creation (batches must be created online)
- Push notifications (use in-app notifications when online)
- Background sync when app is closed (Safari limitation)

---

## Success Criteria

1. Field worker can log a custody event at a mine site with GPS coordinates
2. Events queue locally when offline and sync automatically when connection returns
3. QR code scanning identifies batches instantly via browser camera
4. SHA-256 hash chain maintained across mobile-logged events
5. App installable on both Android and iOS via "Add to Home Screen"
6. Login works via existing email + password authentication
7. Photos can be attached to custody events via camera capture
8. No additional infrastructure cost — runs on existing Render deployment
