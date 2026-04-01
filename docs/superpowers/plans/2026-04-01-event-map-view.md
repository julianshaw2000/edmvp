# Event Location Map View — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Leaflet/OpenStreetMap map tab to the batch detail page showing custody event locations as numbered markers connected by a polyline.

**Architecture:** Single shared presentational component (`EventMapComponent`) using Leaflet directly. GPS extracted from event `metadata.gpsCoordinates`. Integrated as a new tab on both supplier and buyer batch detail pages.

**Tech Stack:** Leaflet, OpenStreetMap tiles, Angular 21+ standalone component

---

## File Structure

### New
- `packages/web/src/app/shared/ui/event-map.component.ts` — Leaflet map component

### Modified
- `packages/web/package.json` — add leaflet + @types/leaflet
- `packages/web/angular.json` — add Leaflet CSS to styles
- `packages/web/src/app/features/supplier/batch-detail.component.ts` — add Map tab
- `packages/web/src/app/features/buyer/batch-detail.component.ts` — add Map tab (if exists separately)

---

### Task 1: Install Leaflet and configure CSS

**Files:**
- Modify: `packages/web/package.json`
- Modify: `packages/web/angular.json`

- [ ] **Step 1: Install packages**

```bash
cd packages/web && npm install leaflet && npm install -D @types/leaflet
```

- [ ] **Step 2: Add Leaflet CSS to angular.json**

Read `packages/web/angular.json` and find the `styles` array in the build options (under `architect > build > options > styles`). Add the Leaflet CSS before the existing styles:

```json
"node_modules/leaflet/dist/leaflet.css"
```

So it looks like:
```json
"styles": [
  "node_modules/leaflet/dist/leaflet.css",
  "src/styles.css"
]
```

- [ ] **Step 3: Build to verify**

```bash
cd packages/web && npx ng build
```

- [ ] **Step 4: Commit**

```bash
git add packages/web/package.json packages/web/package-lock.json packages/web/angular.json
git commit -m "chore: add leaflet dependency and CSS for event map view

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Create EventMapComponent

**Files:**
- Create: `packages/web/src/app/shared/ui/event-map.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, input, OnChanges, AfterViewInit, ElementRef, viewChild, ChangeDetectionStrategy } from '@angular/core';
import * as L from 'leaflet';

export interface MapEvent {
  eventType: string;
  location: string;
  actorName: string;
  eventDate: string;
  gpsCoordinates?: string;
}

const EVENT_COLORS: Record<string, string> = {
  MINE_EXTRACTION: '#92400e',
  LABORATORY_ASSAY: '#1d4ed8',
  CONCENTRATION: '#475569',
  TRADING_TRANSFER: '#d97706',
  PRIMARY_PROCESSING: '#dc2626',
  EXPORT_SHIPMENT: '#4f46e5',
};

function parseGps(gps: string | undefined): [number, number] | null {
  if (!gps) return null;
  const parts = gps.split(',').map(s => parseFloat(s.trim()));
  if (parts.length !== 2 || isNaN(parts[0]) || isNaN(parts[1])) return null;
  return [parts[0], parts[1]];
}

@Component({
  selector: 'app-event-map',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rounded-xl border border-slate-200 overflow-hidden bg-white">
      @if (hasGpsEvents()) {
        <div #mapContainer style="height: 400px; width: 100%;"></div>
        <div class="px-4 py-2 bg-slate-50 border-t border-slate-200 flex flex-wrap gap-3 text-xs text-slate-500">
          @for (item of legend; track item.type) {
            <div class="flex items-center gap-1.5">
              <div class="w-2.5 h-2.5 rounded-full" [style.background]="item.color"></div>
              <span>{{ item.label }}</span>
            </div>
          }
        </div>
      } @else {
        <div class="px-6 py-12 text-center">
          <svg class="w-10 h-10 text-slate-300 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
          </svg>
          <p class="text-sm text-slate-400">No GPS data available for this batch</p>
          <p class="text-xs text-slate-300 mt-1">Events logged via the mobile app include GPS coordinates</p>
        </div>
      }
    </div>
  `,
})
export class EventMapComponent implements AfterViewInit, OnChanges {
  events = input.required<MapEvent[]>();

  mapContainer = viewChild<ElementRef>('mapContainer');

  private map: L.Map | null = null;
  private markersLayer: L.LayerGroup | null = null;

  legend = [
    { type: 'MINE_EXTRACTION', label: 'Extraction', color: '#92400e' },
    { type: 'LABORATORY_ASSAY', label: 'Assay', color: '#1d4ed8' },
    { type: 'CONCENTRATION', label: 'Concentration', color: '#475569' },
    { type: 'TRADING_TRANSFER', label: 'Trading', color: '#d97706' },
    { type: 'PRIMARY_PROCESSING', label: 'Smelting', color: '#dc2626' },
    { type: 'EXPORT_SHIPMENT', label: 'Export', color: '#4f46e5' },
  ];

  hasGpsEvents(): boolean {
    return this.events().some(e => parseGps(e.gpsCoordinates) !== null);
  }

  ngAfterViewInit() {
    this.renderMap();
  }

  ngOnChanges() {
    if (this.map) this.renderMap();
  }

  private renderMap() {
    const container = this.mapContainer()?.nativeElement;
    if (!container) return;

    const gpsEvents = this.events()
      .map((e, i) => ({ ...e, index: i + 1, coords: parseGps(e.gpsCoordinates) }))
      .filter(e => e.coords !== null) as Array<MapEvent & { index: number; coords: [number, number] }>;

    if (gpsEvents.length === 0) return;

    if (!this.map) {
      this.map = L.map(container, { scrollWheelZoom: true, attributionControl: true });
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 18,
      }).addTo(this.map);
      this.markersLayer = L.layerGroup().addTo(this.map);
    }

    this.markersLayer!.clearLayers();

    const latLngs: L.LatLngExpression[] = [];

    for (const event of gpsEvents) {
      const color = EVENT_COLORS[event.eventType] ?? '#6b7280';
      const latLng: L.LatLngExpression = event.coords;
      latLngs.push(latLng);

      const marker = L.circleMarker(latLng, {
        radius: 14,
        fillColor: color,
        color: '#ffffff',
        weight: 2,
        fillOpacity: 0.9,
      });

      marker.bindTooltip(String(event.index), {
        permanent: true,
        direction: 'center',
        className: 'map-number-tooltip',
      });

      const popupContent = `
        <div style="font-family: system-ui, sans-serif; font-size: 13px; line-height: 1.5;">
          <strong>${event.index}. ${event.eventType.replace(/_/g, ' ')}</strong><br/>
          <span style="color: #64748b;">${event.actorName}</span><br/>
          <span style="color: #94a3b8; font-size: 11px;">${event.location}</span><br/>
          <span style="color: #94a3b8; font-size: 11px;">${new Date(event.eventDate).toLocaleDateString()}</span>
        </div>
      `;
      marker.bindPopup(popupContent);

      marker.addTo(this.markersLayer!);
    }

    // Draw polyline connecting events in order
    if (latLngs.length > 1) {
      L.polyline(latLngs, {
        color: '#94a3b8',
        weight: 2,
        dashArray: '6, 8',
        opacity: 0.7,
      }).addTo(this.markersLayer!);
    }

    // Fit bounds
    if (latLngs.length === 1) {
      this.map.setView(latLngs[0], 8);
    } else {
      this.map.fitBounds(L.latLngBounds(latLngs), { padding: [40, 40] });
    }

    // Fix Leaflet rendering issue when map is in a hidden tab
    setTimeout(() => this.map?.invalidateSize(), 100);
  }
}
```

- [ ] **Step 2: Add global CSS for numbered tooltips**

In `packages/web/src/styles.css`, add at the end:

```css
/* Leaflet numbered marker tooltips */
.map-number-tooltip {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  color: white !important;
  font-weight: 700 !important;
  font-size: 11px !important;
  padding: 0 !important;
  margin: 0 !important;
}
.map-number-tooltip::before {
  display: none !important;
}
```

- [ ] **Step 3: Build**

```bash
cd packages/web && npx ng build
```

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/shared/ui/event-map.component.ts packages/web/src/styles.css
git commit -m "feat: add EventMapComponent with Leaflet/OpenStreetMap (map view)

Numbered circle markers colored by event type, connected by dashed polyline.
Auto-fits bounds. Popup on click shows event details. Legend bar below map.
Falls back to 'No GPS data' message when no coordinates available.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Integrate map tab into supplier batch detail

**Files:**
- Modify: `packages/web/src/app/features/supplier/batch-detail.component.ts`

- [ ] **Step 1: Add import and integrate**

Read `packages/web/src/app/features/supplier/batch-detail.component.ts`.

Add import:
```typescript
import { EventMapComponent, MapEvent } from '../../shared/ui/event-map.component';
```

Add `EventMapComponent` to the `imports` array.

Find the tabs array definition and add a new tab:
```typescript
{ id: 'map', label: 'Map' }
```

Find where tab content is rendered (the `@switch` or `@if` blocks for `activeTab()`). Add a new case for the map tab:

```html
        @if (activeTab() === 'map') {
          <app-event-map [events]="mapEvents()" />
        }
```

Add a computed property to the component class that transforms events into MapEvent format:

```typescript
mapEvents = computed<MapEvent[]>(() =>
  this.facade.events().map(e => ({
    eventType: e.eventType,
    location: e.location,
    actorName: e.actorName,
    eventDate: e.eventDate,
    gpsCoordinates: (e as any).metadata?.gpsCoordinates as string | undefined,
  }))
);
```

Add `computed` to the import from `@angular/core` if not already present.

- [ ] **Step 2: Build**

```bash
cd packages/web && npx ng build
```

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/features/supplier/batch-detail.component.ts
git commit -m "feat: add Map tab to supplier batch detail page

Shows custody event journey on Leaflet map with numbered markers and
polyline connecting locations in chronological order.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Integrate map tab into buyer batch detail

**Files:**
- Modify: `packages/web/src/app/features/buyer/batch-detail.component.ts`

- [ ] **Step 1: Add import and integrate**

Read `packages/web/src/app/features/buyer/batch-detail.component.ts`.

Apply the same changes as Task 3:
1. Import `EventMapComponent` and `MapEvent`
2. Add to `imports` array
3. Add `{ id: 'map', label: 'Map' }` to tabs
4. Add `@if (activeTab() === 'map')` block with `<app-event-map>`
5. Add `mapEvents` computed property

If the buyer batch detail file is significantly different from the supplier one, adapt accordingly — the key requirement is adding the map tab with the same component.

- [ ] **Step 2: Build**

```bash
cd packages/web && npx ng build
```

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/features/buyer/batch-detail.component.ts
git commit -m "feat: add Map tab to buyer batch detail page

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Full build, push, and verify

- [ ] **Step 1: Full build**

```bash
cd packages/web && npx ng build
```

- [ ] **Step 2: Push**

```bash
git push origin main
```
