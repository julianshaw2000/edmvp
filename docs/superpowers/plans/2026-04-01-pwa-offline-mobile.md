# PWA Offline & Mobile Features — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add offline event queuing, sync engine, mobile-optimized event logger, QR scanner, and batch caching to the existing Angular PWA — matching the functionality of the Flutter mobile app.

**Architecture:** IndexedDB (via `idb` library) stores pending events and cached batches. A connectivity service monitors online/offline state. A sync service replays queued events to the API when connectivity returns. Mobile-optimized Angular components provide touch-friendly event logging and QR scanning. The existing Angular service worker handles app shell caching.

**Tech Stack:** Angular 21, `idb` (IndexedDB wrapper), `html5-qrcode` (QR scanning), Web Crypto API (SHA-256), Geolocation API, signals, standalone components

---

## File Structure

### Core Services (new)
- `packages/web/src/app/core/offline/offline-db.service.ts` — IndexedDB wrapper (pending events + cached batches)
- `packages/web/src/app/core/offline/connectivity.service.ts` — Online/offline monitoring
- `packages/web/src/app/core/offline/sync.service.ts` — Sync engine (replay queued events)
- `packages/web/src/app/core/offline/hash.service.ts` — SHA-256 via Web Crypto API

### Shared UI (new)
- `packages/web/src/app/shared/ui/offline-banner.component.ts` — Offline indicator banner

### Mobile Feature Components (new)
- `packages/web/src/app/features/supplier/mobile-event-logger.component.ts` — Touch-optimized event form
- `packages/web/src/app/features/supplier/sync-status.component.ts` — Sync dashboard
- `packages/web/src/app/features/supplier/qr-scanner.component.ts` — QR code scanner

### Modified Files
- `packages/web/package.json` — Add `idb` and `html5-qrcode` dependencies
- `packages/web/src/app/features/supplier/supplier.routes.ts` — Add mobile routes
- `packages/web/src/app/core/layout/sidebar.component.ts` — Add mobile nav items for supplier
- `packages/web/src/app/features/supplier/supplier-dashboard.component.ts` — Add offline banner + pending badge
- `packages/web/src/app/shared/state/batch.store.ts` — Add batch caching hooks

---

## Chunk 1: Dependencies + Core Offline Services

### Task 1: Install dependencies

**Files:**
- Modify: `packages/web/package.json`

- [ ] **Step 1: Install idb and html5-qrcode**

```bash
cd packages/web && npm install idb html5-qrcode
```

- [ ] **Step 2: Verify installation**

```bash
cd packages/web && npx ng build
```
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add packages/web/package.json packages/web/package-lock.json
git commit -m "chore: add idb and html5-qrcode dependencies for PWA offline features

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Create IndexedDB offline database service

**Files:**
- Create: `packages/web/src/app/core/offline/offline-db.service.ts`

- [ ] **Step 1: Create the service**

```typescript
import { Injectable } from '@angular/core';
import { openDB, DBSchema, IDBPDatabase } from 'idb';

export interface PendingEvent {
  id: string;
  batchId: string;
  eventType: string;
  actor: string;
  location: string;
  latitude: number;
  longitude: number;
  timestamp: string;
  description?: string;
  photoPath?: string;
  metadata?: Record<string, unknown>;
  hashValue?: string;
  syncStatus: 'pending' | 'syncing' | 'synced' | 'failed';
  syncError?: string;
  createdAt: string;
}

export interface CachedBatch {
  id: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
  status: string;
  complianceStatus: string;
  eventCount: number;
  cachedAt: string;
}

interface AuditraksOfflineDB extends DBSchema {
  pending_events: {
    key: string;
    value: PendingEvent;
    indexes: { 'by-batch': string; 'by-status': string };
  };
  cached_batches: {
    key: string;
    value: CachedBatch;
  };
  sync_log: {
    key: number;
    value: { eventId: string; result: 'success' | 'error'; errorMessage?: string; attemptedAt: string };
  };
}

@Injectable({ providedIn: 'root' })
export class OfflineDbService {
  private dbPromise: Promise<IDBPDatabase<AuditraksOfflineDB>>;

  constructor() {
    this.dbPromise = openDB<AuditraksOfflineDB>('auditraks_offline', 1, {
      upgrade(db) {
        const eventStore = db.createObjectStore('pending_events', { keyPath: 'id' });
        eventStore.createIndex('by-batch', 'batchId');
        eventStore.createIndex('by-status', 'syncStatus');
        db.createObjectStore('cached_batches', { keyPath: 'id' });
        db.createObjectStore('sync_log', { keyPath: undefined, autoIncrement: true });
      },
    });
  }

  // --- Pending Events ---
  async addEvent(event: PendingEvent): Promise<void> {
    const db = await this.dbPromise;
    await db.put('pending_events', event);
  }

  async getEventsByBatch(batchId: string): Promise<PendingEvent[]> {
    const db = await this.dbPromise;
    return db.getAllFromIndex('pending_events', 'by-batch', batchId);
  }

  async getPendingEvents(): Promise<PendingEvent[]> {
    const db = await this.dbPromise;
    const all = await db.getAll('pending_events');
    return all.filter(e => e.syncStatus === 'pending' || e.syncStatus === 'failed');
  }

  async getAllEvents(): Promise<PendingEvent[]> {
    const db = await this.dbPromise;
    return db.getAll('pending_events');
  }

  async getPendingCount(): Promise<number> {
    const events = await this.getPendingEvents();
    return events.length;
  }

  async updateEventStatus(id: string, status: PendingEvent['syncStatus'], error?: string): Promise<void> {
    const db = await this.dbPromise;
    const event = await db.get('pending_events', id);
    if (event) {
      event.syncStatus = status;
      event.syncError = error;
      await db.put('pending_events', event);
    }
  }

  async deleteEvent(id: string): Promise<void> {
    const db = await this.dbPromise;
    await db.delete('pending_events', id);
  }

  async clearSyncedEvents(): Promise<void> {
    const db = await this.dbPromise;
    const all = await db.getAll('pending_events');
    const tx = db.transaction('pending_events', 'readwrite');
    for (const event of all) {
      if (event.syncStatus === 'synced') {
        await tx.store.delete(event.id);
      }
    }
    await tx.done;
  }

  // --- Cached Batches ---
  async cacheBatches(batches: CachedBatch[]): Promise<void> {
    const db = await this.dbPromise;
    const tx = db.transaction('cached_batches', 'readwrite');
    for (const batch of batches) {
      await tx.store.put(batch);
    }
    await tx.done;
  }

  async getCachedBatches(): Promise<CachedBatch[]> {
    const db = await this.dbPromise;
    return db.getAll('cached_batches');
  }

  async getCachedBatch(id: string): Promise<CachedBatch | undefined> {
    const db = await this.dbPromise;
    return db.get('cached_batches', id);
  }

  // --- Sync Log ---
  async addSyncLog(entry: { eventId: string; result: 'success' | 'error'; errorMessage?: string }): Promise<void> {
    const db = await this.dbPromise;
    await db.add('sync_log', { ...entry, attemptedAt: new Date().toISOString() });
  }
}
```

- [ ] **Step 2: Build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/core/offline/offline-db.service.ts
git commit -m "feat: add IndexedDB offline database service for PWA

Stores pending events, cached batches, and sync log in IndexedDB via idb.
Supports CRUD for events with status tracking (pending/syncing/synced/failed).

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Create connectivity and hash services

**Files:**
- Create: `packages/web/src/app/core/offline/connectivity.service.ts`
- Create: `packages/web/src/app/core/offline/hash.service.ts`

- [ ] **Step 1: Create connectivity service**

```typescript
import { Injectable, signal, effect, OnDestroy } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ConnectivityService implements OnDestroy {
  readonly isOnline = signal(navigator.onLine);

  private onlineHandler = () => this.isOnline.set(true);
  private offlineHandler = () => this.isOnline.set(false);

  constructor() {
    window.addEventListener('online', this.onlineHandler);
    window.addEventListener('offline', this.offlineHandler);
  }

  ngOnDestroy() {
    window.removeEventListener('online', this.onlineHandler);
    window.removeEventListener('offline', this.offlineHandler);
  }
}
```

- [ ] **Step 2: Create hash service**

```typescript
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class HashService {
  async computeEventHash(params: {
    eventType: string;
    batchId: string;
    actor: string;
    timestamp: string;
    latitude: number;
    longitude: number;
    description?: string;
    metadata?: Record<string, unknown>;
  }): Promise<string> {
    const payload = JSON.stringify({
      eventType: params.eventType,
      batchId: params.batchId,
      actor: params.actor,
      timestamp: params.timestamp,
      latitude: params.latitude,
      longitude: params.longitude,
      description: params.description ?? null,
      metadata: params.metadata ?? null,
    });
    const encoded = new TextEncoder().encode(payload);
    const hashBuffer = await crypto.subtle.digest('SHA-256', encoded);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
  }
}
```

- [ ] **Step 3: Build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/core/offline/connectivity.service.ts packages/web/src/app/core/offline/hash.service.ts
git commit -m "feat: add connectivity monitor and SHA-256 hash service for PWA

ConnectivityService tracks online/offline via window events and exposes
a signal. HashService computes SHA-256 via Web Crypto API.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Create sync service

**Files:**
- Create: `packages/web/src/app/core/offline/sync.service.ts`

- [ ] **Step 1: Create the service**

```typescript
import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from '../http/api-url.token';
import { OfflineDbService, PendingEvent } from './offline-db.service';
import { ConnectivityService } from './connectivity.service';
import { firstValueFrom } from 'rxjs';

export interface SyncResult {
  synced: number;
  failed: number;
  lastSyncAt: string;
}

@Injectable({ providedIn: 'root' })
export class SyncService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);
  private offlineDb = inject(OfflineDbService);
  private connectivity = inject(ConnectivityService);

  readonly isSyncing = signal(false);
  readonly pendingCount = signal(0);
  readonly lastResult = signal<SyncResult | null>(null);
  readonly allEvents = signal<PendingEvent[]>([]);

  constructor() {
    this.refreshState();

    // Auto-sync when coming back online
    let wasOffline = !navigator.onLine;
    setInterval(() => {
      const online = this.connectivity.isOnline();
      if (online && wasOffline && !this.isSyncing()) {
        this.syncNow();
      }
      wasOffline = !online;
    }, 5000);
  }

  async refreshState(): Promise<void> {
    this.pendingCount.set(await this.offlineDb.getPendingCount());
    this.allEvents.set(await this.offlineDb.getAllEvents());
  }

  async syncNow(): Promise<SyncResult> {
    if (this.isSyncing() || !this.connectivity.isOnline()) {
      return { synced: 0, failed: 0, lastSyncAt: new Date().toISOString() };
    }

    this.isSyncing.set(true);
    let synced = 0;
    let failed = 0;

    try {
      const pending = await this.offlineDb.getPendingEvents();

      for (const event of pending) {
        try {
          await this.offlineDb.updateEventStatus(event.id, 'syncing');

          const metadata = { ...(event.metadata ?? {}), gpsCoordinates: `${event.latitude},${event.longitude}` };
          await firstValueFrom(this.http.post(
            `${this.apiUrl}/api/batches/${event.batchId}/events`,
            {
              eventType: event.eventType,
              eventDate: event.timestamp,
              location: event.location,
              actorName: event.actor,
              description: event.description ?? '',
              metadata,
            }
          ));

          await this.offlineDb.updateEventStatus(event.id, 'synced');
          await this.offlineDb.addSyncLog({ eventId: event.id, result: 'success' });
          synced++;
        } catch (err: any) {
          const status = err?.status;
          if (status && status >= 400 && status < 500 && status !== 401) {
            // Client error (except auth) — mark as failed, don't retry
            const msg = err?.error?.error ?? err?.message ?? 'Unknown error';
            await this.offlineDb.updateEventStatus(event.id, 'failed', msg);
            await this.offlineDb.addSyncLog({ eventId: event.id, result: 'error', errorMessage: msg });
            failed++;
          } else {
            // Server error or network — revert to pending for retry
            await this.offlineDb.updateEventStatus(event.id, 'pending');
          }
        }
      }
    } finally {
      this.isSyncing.set(false);
      const result: SyncResult = { synced, failed, lastSyncAt: new Date().toISOString() };
      this.lastResult.set(result);
      await this.refreshState();
    }

    return this.lastResult()!;
  }

  async retryEvent(id: string): Promise<void> {
    await this.offlineDb.updateEventStatus(id, 'pending');
    await this.refreshState();
    await this.syncNow();
  }

  async deleteEvent(id: string): Promise<void> {
    await this.offlineDb.deleteEvent(id);
    await this.refreshState();
  }
}
```

- [ ] **Step 2: Build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/core/offline/sync.service.ts
git commit -m "feat: add sync service for offline event replay (PWA)

Replays pending events from IndexedDB to the API. Auto-syncs on reconnect.
Handles client errors (fail permanently) vs server errors (retry later).
Tracks sync state via signals.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 2: Offline Banner + Batch Caching

### Task 5: Create offline banner component

**Files:**
- Create: `packages/web/src/app/shared/ui/offline-banner.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { ConnectivityService } from '../../core/offline/connectivity.service';
import { SyncService } from '../../core/offline/sync.service';

@Component({
  selector: 'app-offline-banner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!connectivity.isOnline()) {
      <div class="bg-amber-700 text-white px-4 py-2.5 flex items-center justify-center gap-2 text-sm font-medium">
        <svg class="w-4 h-4 animate-pulse" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 5.636a9 9 0 010 12.728m-2.829-2.829a5 5 0 000-7.07m-4.243 4.243a1 1 0 010-1.414"/>
        </svg>
        You are offline — events are queued locally
        @if (sync.pendingCount() > 0) {
          <span class="bg-amber-900 px-2 py-0.5 rounded-full text-xs">{{ sync.pendingCount() }} pending</span>
        }
      </div>
    }
  `,
})
export class OfflineBannerComponent {
  protected connectivity = inject(ConnectivityService);
  protected sync = inject(SyncService);
}
```

- [ ] **Step 2: Add to supplier dashboard**

In `packages/web/src/app/features/supplier/supplier-dashboard.component.ts`:
- Add import: `import { OfflineBannerComponent } from '../../shared/ui/offline-banner.component';`
- Add `OfflineBannerComponent` to imports array
- Add `<app-offline-banner />` at the very top of the template (before `<app-page-header>`)

- [ ] **Step 3: Build and commit**

```bash
cd packages/web && npx ng build
git add packages/web/src/app/shared/ui/offline-banner.component.ts packages/web/src/app/features/supplier/supplier-dashboard.component.ts
git commit -m "feat: add offline banner component with pending event count (PWA)

Shows amber banner when offline with pulse animation and pending event count.
Integrated at top of supplier dashboard.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Add batch caching to BatchStore

**Files:**
- Modify: `packages/web/src/app/shared/state/batch.store.ts`

- [ ] **Step 1: Add caching hooks**

In `packages/web/src/app/shared/state/batch.store.ts`:

Add import:
```typescript
import { OfflineDbService, CachedBatch } from '../../core/offline/offline-db.service';
import { ConnectivityService } from '../../core/offline/connectivity.service';
```

Add injections to the class:
```typescript
private offlineDb = inject(OfflineDbService);
private connectivity = inject(ConnectivityService);
```

In the `loadBatches` method, after the `next:` handler sets `_batches`, add caching:
```typescript
// Cache batches for offline access
this.offlineDb.cacheBatches(res.items.map(b => ({
  id: b.id,
  batchNumber: b.batchNumber,
  mineralType: b.mineralType,
  originCountry: b.originCountry,
  originMine: b.originMine,
  weightKg: b.weightKg,
  status: b.status,
  complianceStatus: b.complianceStatus,
  eventCount: b.eventCount,
  cachedAt: new Date().toISOString(),
})));
```

In the `error:` handler of `loadBatches`, add offline fallback:
```typescript
// Fallback to cached batches when offline
if (!this.connectivity.isOnline()) {
  this.offlineDb.getCachedBatches().then(cached => {
    if (cached.length > 0) {
      this._batches.set(cached.map(c => ({
        id: c.id,
        batchNumber: c.batchNumber,
        mineralType: c.mineralType,
        originCountry: c.originCountry,
        originMine: c.originMine,
        weightKg: c.weightKg,
        status: c.status,
        complianceStatus: c.complianceStatus,
        createdAt: c.cachedAt,
        eventCount: c.eventCount,
      })));
      this._batchesLoading.set(false);
    }
  });
}
```

- [ ] **Step 2: Build and commit**

```bash
cd packages/web && npx ng build
git add packages/web/src/app/shared/state/batch.store.ts
git commit -m "feat: add offline batch caching to BatchStore (PWA)

Caches batches in IndexedDB after successful API load. Falls back to
cached data when offline.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 3: Mobile Views — Event Logger + Sync Screen

### Task 7: Create mobile event logger

**Files:**
- Create: `packages/web/src/app/features/supplier/mobile-event-logger.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { OfflineDbService, PendingEvent } from '../../core/offline/offline-db.service';
import { ConnectivityService } from '../../core/offline/connectivity.service';
import { SyncService } from '../../core/offline/sync.service';
import { HashService } from '../../core/offline/hash.service';
import { OfflineBannerComponent } from '../../shared/ui/offline-banner.component';
import { BatchFacade } from '../../shared/state/batch.facade';

const EVENT_TYPES = [
  { value: 'MINE_EXTRACTION', label: 'Extraction', icon: '⛏️' },
  { value: 'LABORATORY_ASSAY', label: 'Assay', icon: '🔬' },
  { value: 'CONCENTRATION', label: 'Concentration', icon: '🏭' },
  { value: 'TRADING_TRANSFER', label: 'Trading', icon: '🤝' },
  { value: 'PRIMARY_PROCESSING', label: 'Smelting', icon: '🔥' },
  { value: 'EXPORT_SHIPMENT', label: 'Export', icon: '🚢' },
];

@Component({
  selector: 'app-mobile-event-logger',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, OfflineBannerComponent],
  template: `
    <app-offline-banner />

    <div class="min-h-screen bg-slate-50 pb-24">
      <!-- Header -->
      <div class="bg-indigo-600 text-white px-4 py-4">
        <div class="flex items-center gap-3">
          <a routerLink="/supplier" class="p-1">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
            </svg>
          </a>
          <h1 class="text-lg font-semibold">Log Custody Event</h1>
        </div>
      </div>

      <div class="px-4 py-5 space-y-5">
        <!-- Batch Selection -->
        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-2">Batch</label>
          <select [(ngModel)]="batchId" name="batchId"
            class="w-full px-4 py-3 border border-slate-300 rounded-xl text-base focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500">
            <option value="">Select batch...</option>
            @for (b of facade.batches(); track b.id) {
              <option [value]="b.id">{{ b.batchNumber }} — {{ b.mineralType }}</option>
            }
          </select>
        </div>

        <!-- Event Type Grid -->
        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-2">Event Type</label>
          <div class="grid grid-cols-3 gap-2">
            @for (type of eventTypes; track type.value) {
              <button type="button" (click)="eventType = type.value"
                [class]="'flex flex-col items-center gap-1 p-3 rounded-xl border-2 text-center transition-all ' +
                  (eventType === type.value ? 'border-indigo-500 bg-indigo-50' : 'border-slate-200 bg-white')">
                <span class="text-2xl">{{ type.icon }}</span>
                <span class="text-xs font-medium" [class]="eventType === type.value ? 'text-indigo-700' : 'text-slate-600'">{{ type.label }}</span>
              </button>
            }
          </div>
        </div>

        <!-- Location -->
        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-2">Location</label>
          <input type="text" [(ngModel)]="location" name="location"
            placeholder="e.g. Nyungwe Mine, Rwanda"
            class="w-full px-4 py-3 border border-slate-300 rounded-xl text-base focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500" />
          <div class="mt-1.5 flex items-center gap-2 text-xs text-slate-400">
            <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
            </svg>
            @if (capturingGps()) {
              <span>Capturing GPS...</span>
            } @else if (latitude !== 0 || longitude !== 0) {
              <span>GPS: {{ latitude.toFixed(4) }}, {{ longitude.toFixed(4) }}</span>
              <button (click)="captureGps()" class="text-indigo-500 hover:text-indigo-600">Refresh</button>
            } @else {
              <span>GPS unavailable</span>
              <button (click)="captureGps()" class="text-indigo-500 hover:text-indigo-600">Retry</button>
            }
          </div>
        </div>

        <!-- Actor -->
        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-2">Actor Name</label>
          <input type="text" [(ngModel)]="actor" name="actor"
            placeholder="Who is logging this event?"
            class="w-full px-4 py-3 border border-slate-300 rounded-xl text-base focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500" />
        </div>

        <!-- Description -->
        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-2">Description</label>
          <textarea [(ngModel)]="description" name="description" rows="3"
            placeholder="Optional notes..."
            class="w-full px-4 py-3 border border-slate-300 rounded-xl text-base focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 resize-none"></textarea>
        </div>

        <!-- Photo -->
        <div>
          <button type="button" (click)="photoInput.click()"
            class="w-full flex items-center justify-center gap-2 px-4 py-3 border-2 border-dashed border-slate-300 rounded-xl text-sm font-medium text-slate-600 hover:border-indigo-400 hover:text-indigo-600 transition-colors">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z"/>
              <circle cx="12" cy="13" r="3"/>
            </svg>
            {{ photoAttached() ? 'Photo attached' : 'Take Photo' }}
          </button>
          <input #photoInput type="file" accept="image/*" capture="environment" class="hidden"
            (change)="onPhotoSelected($event)" />
        </div>

        <!-- Error -->
        @if (error()) {
          <p class="text-sm text-rose-600">{{ error() }}</p>
        }

        <!-- Success -->
        @if (submitted()) {
          <div class="bg-emerald-50 border border-emerald-200 rounded-xl p-3 text-sm text-emerald-700 flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
            </svg>
            Event queued for sync
          </div>
        }
      </div>

      <!-- Fixed Submit Button -->
      <div class="fixed bottom-0 left-0 right-0 bg-white border-t border-slate-200 px-4 py-3 safe-area-bottom">
        <button (click)="submit()" [disabled]="submitting() || !batchId || !eventType || !actor || !location"
          class="w-full bg-indigo-600 text-white py-3.5 rounded-xl text-base font-semibold hover:bg-indigo-700 disabled:opacity-50 shadow-lg transition-all">
          {{ submitting() ? 'Queuing...' : 'Submit Event' }}
        </button>
      </div>
    </div>
  `,
})
export class MobileEventLoggerComponent {
  protected facade = inject(BatchFacade);
  private offlineDb = inject(OfflineDbService);
  private sync = inject(SyncService);
  private hashService = inject(HashService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  eventTypes = EVENT_TYPES;

  batchId = '';
  eventType = '';
  actor = '';
  location = '';
  description = '';
  latitude = 0;
  longitude = 0;

  capturingGps = signal(false);
  photoAttached = signal(false);
  submitting = signal(false);
  submitted = signal(false);
  error = signal<string | null>(null);

  constructor() {
    this.captureGps();
    this.facade.loadBatches();
    const qp = this.route.snapshot.queryParams;
    if (qp['batchId']) this.batchId = qp['batchId'];
  }

  captureGps() {
    if (!navigator.geolocation) return;
    this.capturingGps.set(true);
    navigator.geolocation.getCurrentPosition(
      pos => {
        this.latitude = pos.coords.latitude;
        this.longitude = pos.coords.longitude;
        this.capturingGps.set(false);
      },
      () => this.capturingGps.set(false),
      { enableHighAccuracy: true, timeout: 10000 }
    );
  }

  onPhotoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.photoAttached.set(!!input.files?.length);
  }

  async submit() {
    if (!this.batchId || !this.eventType || !this.actor || !this.location) return;
    this.submitting.set(true);
    this.error.set(null);
    this.submitted.set(false);

    try {
      const id = crypto.randomUUID();
      const timestamp = new Date().toISOString();

      const hashValue = await this.hashService.computeEventHash({
        eventType: this.eventType,
        batchId: this.batchId,
        actor: this.actor,
        timestamp,
        latitude: this.latitude,
        longitude: this.longitude,
        description: this.description || undefined,
      });

      const pendingEvent: PendingEvent = {
        id,
        batchId: this.batchId,
        eventType: this.eventType,
        actor: this.actor,
        location: this.location,
        latitude: this.latitude,
        longitude: this.longitude,
        timestamp,
        description: this.description || undefined,
        hashValue,
        syncStatus: 'pending',
        createdAt: timestamp,
      };

      await this.offlineDb.addEvent(pendingEvent);
      await this.sync.refreshState();

      this.submitted.set(true);
      this.submitting.set(false);

      // Reset form
      this.eventType = '';
      this.actor = '';
      this.location = '';
      this.description = '';

      // Try to sync immediately if online
      if (navigator.onLine) {
        this.sync.syncNow();
      }
    } catch (err) {
      this.error.set('Failed to queue event');
      this.submitting.set(false);
    }
  }
}
```

- [ ] **Step 2: Build and commit**

```bash
cd packages/web && npx ng build
git add packages/web/src/app/features/supplier/mobile-event-logger.component.ts
git commit -m "feat: add mobile-optimized event logger with offline queue (PWA)

Touch-friendly form with event type grid, GPS auto-capture, camera input,
and IndexedDB offline queue. SHA-256 hash computed client-side.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: Create sync status screen

**Files:**
- Create: `packages/web/src/app/features/supplier/sync-status.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { SyncService } from '../../core/offline/sync.service';
import { ConnectivityService } from '../../core/offline/connectivity.service';
import { OfflineBannerComponent } from '../../shared/ui/offline-banner.component';

@Component({
  selector: 'app-sync-status',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, OfflineBannerComponent],
  template: `
    <app-offline-banner />

    <div class="min-h-screen bg-slate-50">
      <!-- Header -->
      <div [class]="'text-white px-4 py-6 ' + (sync.pendingCount() > 0 ? 'bg-amber-600' : 'bg-emerald-600')">
        <div class="flex items-center gap-3 mb-4">
          <a routerLink="/supplier" class="p-1">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
            </svg>
          </a>
          <h1 class="text-lg font-semibold">Sync Status</h1>
        </div>
        <p class="text-4xl font-bold mb-1">{{ sync.pendingCount() }}</p>
        <p class="text-sm opacity-80">Pending Events</p>
        <button (click)="onSync()" [disabled]="sync.isSyncing() || !connectivity.isOnline()"
          class="mt-4 w-full bg-white/20 hover:bg-white/30 disabled:opacity-50 text-white py-2.5 rounded-xl text-sm font-semibold transition-colors">
          {{ sync.isSyncing() ? 'Syncing...' : !connectivity.isOnline() ? 'Offline' : 'Sync Now' }}
        </button>
        @if (sync.lastResult(); as result) {
          <p class="mt-2 text-xs opacity-70 text-center">
            Last sync: {{ result.synced }} synced, {{ result.failed }} failed
          </p>
        }
      </div>

      <!-- Event List -->
      <div class="px-4 py-4">
        @if (sync.allEvents().length === 0) {
          <div class="text-center py-12">
            <svg class="w-12 h-12 text-emerald-400 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
            </svg>
            <p class="text-sm text-slate-500">All events synced</p>
          </div>
        } @else {
          <div class="space-y-3">
            @for (event of sync.allEvents(); track event.id) {
              <div class="bg-white rounded-xl border border-slate-200 p-4">
                <div class="flex items-center justify-between mb-2">
                  <div class="flex items-center gap-2">
                    @switch (event.syncStatus) {
                      @case ('pending') {
                        <div class="w-2.5 h-2.5 rounded-full bg-amber-400"></div>
                      }
                      @case ('syncing') {
                        <div class="w-2.5 h-2.5 rounded-full bg-blue-400 animate-pulse"></div>
                      }
                      @case ('synced') {
                        <div class="w-2.5 h-2.5 rounded-full bg-emerald-400"></div>
                      }
                      @case ('failed') {
                        <div class="w-2.5 h-2.5 rounded-full bg-rose-400"></div>
                      }
                    }
                    <span class="text-sm font-medium text-slate-900">{{ event.eventType }}</span>
                    <span class="text-xs text-slate-400">{{ event.actor }}</span>
                  </div>
                  <span class="text-xs px-2 py-0.5 rounded-full font-medium"
                    [class]="event.syncStatus === 'synced' ? 'bg-emerald-50 text-emerald-700' :
                             event.syncStatus === 'failed' ? 'bg-rose-50 text-rose-700' :
                             event.syncStatus === 'syncing' ? 'bg-blue-50 text-blue-700' :
                             'bg-amber-50 text-amber-700'">
                    {{ event.syncStatus }}
                  </span>
                </div>
                <p class="text-xs text-slate-400 mb-1">{{ event.timestamp | date:'medium' }}</p>
                @if (event.syncError) {
                  <p class="text-xs text-rose-500 mb-2">{{ event.syncError }}</p>
                }
                @if (event.syncStatus === 'failed') {
                  <div class="flex gap-2 mt-2">
                    <button (click)="retry(event.id)" class="text-xs font-medium text-indigo-600 hover:text-indigo-700">Retry</button>
                    <button (click)="remove(event.id)" class="text-xs font-medium text-rose-500 hover:text-rose-600">Delete</button>
                  </div>
                }
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class SyncStatusComponent {
  protected sync = inject(SyncService);
  protected connectivity = inject(ConnectivityService);

  async onSync() {
    await this.sync.syncNow();
  }

  async retry(id: string) {
    await this.sync.retryEvent(id);
  }

  async remove(id: string) {
    await this.sync.deleteEvent(id);
  }
}
```

- [ ] **Step 2: Build and commit**

```bash
cd packages/web && npx ng build
git add packages/web/src/app/features/supplier/sync-status.component.ts
git commit -m "feat: add sync status screen with event list and retry (PWA)

Shows pending count, sync now button, event list with status badges.
Failed events can be retried or deleted. Auto-disables when offline.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 4: QR Scanner + Routes + Integration

### Task 9: Create QR scanner component

**Files:**
- Create: `packages/web/src/app/features/supplier/qr-scanner.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, inject, signal, OnDestroy, AfterViewInit, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-qr-scanner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="min-h-screen bg-black">
      <!-- Header -->
      <div class="bg-black/80 text-white px-4 py-4 flex items-center gap-3 relative z-10">
        <a routerLink="/supplier" class="p-1">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
          </svg>
        </a>
        <h1 class="text-lg font-semibold">Scan QR Code</h1>
      </div>

      <!-- Camera Area -->
      <div class="flex-1 flex items-center justify-center" style="min-height: 50vh;">
        <div id="qr-reader" class="w-full max-w-sm"></div>
        @if (!scannerActive()) {
          <div class="text-center text-white/60">
            <svg class="w-16 h-16 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h2M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z"/>
            </svg>
            <p class="text-sm">Starting camera...</p>
          </div>
        }
      </div>

      @if (error()) {
        <div class="px-4 py-2 bg-rose-900/80 text-rose-200 text-sm text-center">{{ error() }}</div>
      }

      <!-- Manual Entry -->
      <div class="bg-slate-900 px-4 py-5">
        <button (click)="showManual.set(!showManual())"
          class="w-full text-sm text-white/70 hover:text-white font-medium mb-3">
          {{ showManual() ? 'Hide manual entry' : 'Enter Batch ID manually' }}
        </button>
        @if (showManual()) {
          <div class="flex gap-2">
            <input type="text" [(ngModel)]="manualBatchId" name="manualBatchId"
              placeholder="Paste Batch ID..."
              class="flex-1 px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-xl text-white text-sm placeholder:text-slate-500 focus:ring-2 focus:ring-indigo-500" />
            <button (click)="navigateToBatch(manualBatchId)" [disabled]="!manualBatchId"
              class="px-4 py-2.5 bg-indigo-600 text-white rounded-xl text-sm font-semibold disabled:opacity-50">
              Go
            </button>
          </div>
        }
      </div>
    </div>
  `,
})
export class QrScannerComponent implements AfterViewInit, OnDestroy {
  private router = inject(Router);
  private scanner: any = null;

  scannerActive = signal(false);
  error = signal<string | null>(null);
  showManual = signal(false);
  manualBatchId = '';

  async ngAfterViewInit() {
    try {
      const { Html5Qrcode } = await import('html5-qrcode');
      this.scanner = new Html5Qrcode('qr-reader');
      await this.scanner.start(
        { facingMode: 'environment' },
        { fps: 10, qrbox: { width: 250, height: 250 } },
        (text: string) => this.onScan(text),
        () => {}
      );
      this.scannerActive.set(true);
    } catch (err) {
      this.error.set('Camera not available. Use manual entry below.');
      this.showManual.set(true);
    }
  }

  ngOnDestroy() {
    this.scanner?.stop()?.catch(() => {});
  }

  private onScan(text: string) {
    // QR could be a full URL (/verify/{id}) or just an ID
    const match = text.match(/verify\/([a-f0-9-]+)/i) || text.match(/^([a-f0-9-]{36})$/i);
    if (match) {
      this.navigateToBatch(match[1]);
    }
  }

  navigateToBatch(id: string) {
    if (id) this.router.navigate(['/supplier/batch', id]);
  }
}
```

- [ ] **Step 2: Build and commit**

```bash
cd packages/web && npx ng build
git add packages/web/src/app/features/supplier/qr-scanner.component.ts
git commit -m "feat: add QR scanner component with camera and manual entry (PWA)

Uses html5-qrcode for camera-based scanning. Falls back to manual batch
ID entry if camera unavailable. Navigates to batch detail on scan.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 10: Register routes and sidebar links

**Files:**
- Modify: `packages/web/src/app/features/supplier/supplier.routes.ts`
- Modify: `packages/web/src/app/core/layout/sidebar.component.ts`

- [ ] **Step 1: Add routes**

In `packages/web/src/app/features/supplier/supplier.routes.ts`, add these routes:

```typescript
{
  path: 'log-event',
  loadComponent: () => import('./mobile-event-logger.component').then(m => m.MobileEventLoggerComponent),
},
{
  path: 'sync',
  loadComponent: () => import('./sync-status.component').then(m => m.SyncStatusComponent),
},
{
  path: 'scan',
  loadComponent: () => import('./qr-scanner.component').then(m => m.QrScannerComponent),
},
```

- [ ] **Step 2: Add sidebar links**

In `packages/web/src/app/core/layout/sidebar.component.ts`, find the SUPPLIER case and add new items:

```typescript
{ label: 'Log Event', route: '/supplier/log-event', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 4v16m8-8H4"/></svg>' },
{ label: 'Scan QR', route: '/supplier/scan', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h2M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z"/></svg>' },
{ label: 'Sync', route: '/supplier/sync', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/></svg>' },
```

- [ ] **Step 3: Build and commit**

```bash
cd packages/web && npx ng build
git add packages/web/src/app/features/supplier/supplier.routes.ts packages/web/src/app/core/layout/sidebar.component.ts
git commit -m "feat: register PWA mobile routes and sidebar links

Add /supplier/log-event, /supplier/scan, /supplier/sync routes.
Add Log Event, Scan QR, Sync sidebar items for supplier role.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 11: Full build, push, and verify

- [ ] **Step 1: Full build**

```bash
cd packages/web && npx ng build
```
Expected: Build succeeds

- [ ] **Step 2: Push**

```bash
git push origin main
```

- [ ] **Step 3: Verify on mobile**

After deploy, test on a mobile device:
1. Open https://auditraks.com on mobile browser
2. Login as supplier
3. "Add to Home Screen" → app installs as standalone
4. Navigate to Log Event → submit an event
5. Toggle airplane mode → verify offline banner appears
6. Submit another event offline → verify it queues
7. Reconnect → verify auto-sync
8. Check Sync screen → see event statuses
9. Try QR scanner → camera opens (or manual fallback)
