# PWA Mobile Supplier — Requirements Document

**Date:** 2026-04-01
**Status:** Approved
**Scope:** Mobile-optimized PWA features for field suppliers within the existing Angular web app

---

## 1. Purpose

The auditraks platform has two types of supplier users with different needs:

| | Web Supplier (office) | Mobile Supplier (field) |
|---|---|---|
| **Who** | Office-based supplier staff | Field workers at mine sites, labs, transport checkpoints |
| **Where** | Desk with stable internet | Remote locations — Central/East Africa, often offline |
| **When** | After the fact — entering data from reports and paperwork | In real time — logging events as they happen on site |
| **How** | Full form with all metadata, smelter search, document upload | Simplified form — tap event type, auto-GPS, snap photo, submit |
| **Connectivity** | Always online | Offline-first with background sync |

Both create the same custody events on the same batches via the same API (`POST /api/batches/{id}/events`). The mobile supplier is a **field capture shortcut** — the web supplier is the **full-featured version** for detailed data entry. They complement each other within the same supplier organization.

**Example workflow:**
1. Jean-Baptiste is at the Nyungwe mine. He opens the PWA on his phone, taps "Extraction", GPS auto-captures his location, he types "Nyungwe Mine, Rwanda", snaps a photo of the ore, and hits submit. The event queues offline and syncs when he gets signal.
2. Back at the office, Marie logs into the web portal, reviews Jean-Baptiste's event, uploads the mineralogical certificate PDF, and submits the laboratory assay event with full metadata.

---

## 2. User Profile

**Primary user:** Field workers in the 3TG mineral supply chain

**Characteristics:**
- Works at mine sites, concentration facilities, trading posts, smelters, or export checkpoints
- Has a smartphone (Android or iOS) — may not have a laptop
- Unreliable or no internet connectivity at the work site
- May have limited technical literacy
- Needs large tap targets, minimal text input, clear visual feedback
- Works in varying light conditions (underground mines, outdoor sun)
- May share a device with colleagues

**Access method:** PWA installed via "Add to Home Screen" — no app store required

---

## 3. What Mobile Supplier Can Do

### 3.1 View Batches (read-only, cached offline)

- See all batches assigned to their organization
- View batch details: number, mineral type, origin, weight, compliance status
- View event timeline per batch
- Batches cached in IndexedDB for offline access
- Pull-to-refresh when online

**Mobile supplier CANNOT create batches.** Batch creation is an administrative action done by office staff via the web portal. It requires knowledge of the batch numbering scheme, origin verification, and is typically done with supporting paperwork.

### 3.2 Log Custody Events (offline-capable)

The core mobile function. A simplified, touch-optimized form for recording what happened to a batch at each stage of the supply chain.

**Form fields:**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Batch | Dropdown selector | Yes | Select from cached batch list |
| Event Type | Icon grid (6 options) | Yes | Large tap targets, visual icons |
| Location | Text field | Yes | Human-readable name (e.g., "Nyungwe Mine, Rwanda") |
| GPS | Auto-captured | No | Captured silently in background, shown as helper text, stored in metadata |
| Actor Name | Text field | Yes | Who is performing this action |
| Description | Text area | No | Optional notes |
| Photo | Camera capture | No | Uses device camera via `<input capture="environment">` |

**Event types** (matching the 6-stage custody chain):

| Icon | Type | Typical User |
|------|------|-------------|
| ⛏️ | Mine Extraction | Mine site operator |
| 🔬 | Laboratory Assay | Lab technician |
| 🏭 | Concentration | Processing facility worker |
| 🤝 | Trading/Transfer | Trading agent |
| 🔥 | Primary Processing (Smelting) | Smelter operator |
| 🚢 | Export/Shipment | Export handler |

**Offline behavior:**
- Event is stored in IndexedDB with status `pending`
- SHA-256 hash computed client-side before storage
- GPS coordinates stored as `metadata.gpsCoordinates`
- "Event queued for sync" confirmation shown immediately
- Event syncs automatically when connectivity returns

**What the mobile form does NOT include** (use web portal instead):
- Event-specific metadata fields (GPS coordinates, input/output weights, assay percentages, smelter RMAP search, contract references, export permits)
- Document/certificate upload (PDF, TIFF — not suitable for phone camera)
- Event corrections (requires reviewing the full event chain)
- Smelter typeahead search against RMAP database

### 3.3 Scan QR Codes

- Camera-based QR scanning to identify batches
- Scans Material Passport QR codes (from `/verify/{batchId}` URLs) or raw batch IDs
- On successful scan: navigates to batch detail or pre-fills batch in event logger
- Manual batch ID entry fallback when camera is unavailable
- Uses `html5-qrcode` library via browser camera API

### 3.4 Sync Management

A dedicated screen showing the state of all queued events.

**Sync status screen shows:**
- Pending event count (prominent, large number)
- "Sync Now" button (disabled when offline or already syncing)
- Last sync result (X synced, Y failed)
- Event list with status indicators:
  - 🟡 Pending — waiting to sync
  - 🔵 Syncing — currently being sent
  - 🟢 Synced — successfully sent to server
  - 🔴 Failed — server rejected (4xx error)

**Actions per event:**
- Failed events: **Retry** (resets to pending and re-syncs) or **Delete** (removes from queue)
- Synced events: automatically cleared after successful sync

**Auto-sync behavior:**
- Monitors connectivity every 5 seconds
- When transitioning from offline → online, triggers sync automatically
- No manual intervention needed for normal operation

### 3.5 Offline Indicator

- Persistent amber banner at top of screen when offline
- Text: "You are offline — events are queued locally"
- Shows pending event count badge
- Pulsing icon animation for visibility
- Disappears immediately when connectivity returns

---

## 4. What Mobile Supplier Cannot Do

These actions require the full web portal:

| Action | Why web-only |
|--------|-------------|
| Create batches | Administrative action requiring batch numbering, origin verification, paperwork |
| Upload documents (PDF, certificates) | Files are large, require metadata classification, phone camera not suitable for documents |
| Submit event corrections | Requires reviewing full event chain and compliance context |
| Fill detailed event metadata | Smelter RMAP search, assay percentages, concentration ratios, contract references |
| View compliance details | Detailed check results (RMAP, OECD DDG, Sanctions, Mass Balance, Sequence) |
| Generate Material Passport | PDF generation is a buyer/office action |
| Share Material Passport | Email and link sharing is an office workflow |
| Manage account settings | Profile, password, API keys |

---

## 5. Technical Requirements

### 5.1 Offline Storage

| Store | Technology | Contents |
|-------|-----------|----------|
| Pending events | IndexedDB (`idb` library) | Events waiting to sync, with status tracking |
| Cached batches | IndexedDB | Last-known batch list for offline viewing |
| Sync log | IndexedDB | History of sync attempts for debugging |
| App shell | Angular service worker (NGSW) | HTML, JS, CSS — already configured |

### 5.2 Connectivity

- **Detection:** `navigator.onLine` + `window.addEventListener('online'/'offline')`
- **Monitoring:** Signal-based reactive state (`ConnectivityService.isOnline`)
- **Auto-sync trigger:** 5-second polling detects offline → online transition

### 5.3 Event Hashing

- SHA-256 computed client-side via Web Crypto API (`crypto.subtle.digest`)
- Hash includes: eventType, batchId, actor, timestamp, latitude, longitude, description, metadata
- Hash stored with the event in IndexedDB and sent to the API
- Server verifies hash integrity on receipt

### 5.4 GPS Capture

- Browser Geolocation API (`navigator.geolocation.getCurrentPosition`)
- High accuracy mode enabled, 10-second timeout
- Captured silently on form open — not blocking
- Shown as helper text: `GPS: -1.9403, 29.8739`
- Stored in event metadata as `gpsCoordinates` string
- Graceful fallback if GPS unavailable (field left at `0, 0`, user can still submit)

### 5.5 Photo Capture

- HTML file input with `capture="environment"` attribute
- Opens device camera directly on mobile
- Accepts image files only (`accept="image/*"`)
- Photo attached to the pending event (stored as reference, uploaded on sync)
- No image processing or compression in v1

### 5.6 QR Scanning

- `html5-qrcode` library using browser `getUserMedia` API
- Rear camera preferred (`facingMode: 'environment'`)
- Parses QR containing `/verify/{batchId}` URL or raw UUID
- Manual text entry fallback when camera permission denied or unavailable

### 5.7 Installability

Already configured:
- `manifest.webmanifest` — app name, icons (72-512px, maskable), standalone display, indigo theme
- Angular service worker (NGSW) — app shell caching
- "Add to Home Screen" — works on Chrome Android (auto-prompt) and Safari iOS (manual)

### 5.8 Sync Protocol

```
Event created → stored in IndexedDB (status: pending)
                     │
                     ▼
              Is device online?
              ┌──yes──┴──no──┐
              ▼              ▼
        POST to API     Wait for connectivity
              │              │
         ┌────┴────┐    (auto-detect)
         ▼         ▼         │
      Success    Error       ▼
         │     ┌───┴───┐  POST to API
         ▼     ▼       ▼     ...
      synced  4xx    5xx/network
              │       │
              ▼       ▼
           failed   pending (retry later)
```

**Retry policy:**
- 5xx/network errors: revert to `pending`, retry on next sync cycle
- 4xx errors (except 401): mark as `failed` with error message, no auto-retry
- 401 errors: revert to `pending` (token may have expired, will refresh)
- Manual retry: user clicks "Retry" on failed events in sync screen
- No maximum retry limit for auto-retries (server errors are transient)

---

## 6. Navigation

### Mobile Supplier Routes

| Route | Component | Purpose |
|-------|-----------|---------|
| `/supplier` | SupplierDashboardComponent | Batch list with offline banner (existing, enhanced) |
| `/supplier/batch/:id` | BatchDetailComponent | Batch detail with event timeline (existing) |
| `/supplier/log-event` | MobileEventLoggerComponent | Touch-optimized event form (new) |
| `/supplier/scan` | QrScannerComponent | QR code scanner (new) |
| `/supplier/sync` | SyncStatusComponent | Sync dashboard (new) |
| `/supplier/submit` | SubmitEventComponent | Full web event form (existing, unchanged) |
| `/supplier/batches/new` | CreateBatchComponent | Batch creation (existing, web-only) |

### Sidebar Navigation (Supplier Role)

| Label | Route | Icon | Notes |
|-------|-------|------|-------|
| Dashboard | `/supplier` | Grid | Existing |
| Submit Event | `/supplier/submit` | Plus | Existing — full web form |
| Log Event | `/supplier/log-event` | Plus | New — mobile-optimized |
| Scan QR | `/supplier/scan` | QR code | New |
| Sync | `/supplier/sync` | Refresh arrows | New — shows pending badge |

---

## 7. Browser Compatibility

| Feature | Chrome Android | Safari iOS (16.4+) | Firefox Android |
|---------|:-------------:|:-----------------:|:--------------:|
| Service Worker | Yes | Yes | Yes |
| IndexedDB | Yes | Yes | Yes |
| Camera (getUserMedia) | Yes | Yes | Yes |
| Geolocation | Yes | Yes | Yes |
| Web Crypto (SHA-256) | Yes | Yes | Yes |
| Add to Home Screen | Auto-prompt | Manual | Yes |
| Background Sync API | Yes | No | No |

**Safari limitation:** No Background Sync API — sync only occurs while the app is open. This is acceptable for field workers who open the app, log events, and close it.

---

## 8. Out of Scope (v1)

- Dark mode / theme switching
- Biometric authentication (not reliably supported in browsers)
- Push notifications (requires FCM/APNs setup)
- Background sync when app is closed (Safari limitation)
- Image compression or processing
- Offline batch creation
- Offline document upload
- Multi-language / localization
- Offline compliance checks
- Map view of GPS locations

---

## 9. Success Criteria

1. Field worker can install the PWA on their phone via "Add to Home Screen"
2. Field worker can log a custody event with GPS coordinates while offline
3. Events queue locally and sync automatically when connectivity returns
4. QR scanning identifies batches via device camera
5. Sync screen shows clear status of all pending/synced/failed events
6. Failed events can be retried or deleted
7. Offline banner clearly indicates when the device is disconnected
8. Batches are viewable from cache when offline
9. SHA-256 hash chain maintained for events logged via mobile
10. No additional infrastructure cost — runs on existing Render deployment
