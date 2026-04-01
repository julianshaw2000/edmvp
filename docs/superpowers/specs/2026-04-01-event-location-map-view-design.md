# Event Location Map View — Design Spec

**Date:** 2026-04-01
**Status:** Draft

---

## Overview

Add an interactive map view showing the geographic journey of custody events across the supply chain. Each event's GPS coordinates are plotted on a map, connected in chronological order, giving auditors and suppliers a visual verification that the custody chain makes geographic sense.

**Example:** Mine in Nyungwe, Rwanda → Lab in Kigali → Concentration in Gisenyi → Smelter in Austria → Port in Mombasa — plotted as connected points on a world map.

---

## Where It Appears

### 1. Batch Detail — Map Tab (all roles)

A new **"Map"** tab on the batch detail page (supplier, buyer, and admin views), alongside the existing Overview, Events, Documents, Compliance, and Activity tabs.

Shows all custody events for that batch plotted on a map with:
- Numbered markers at each event location
- Lines connecting events in chronological order
- Popup on marker click showing: event type, actor, date, location name

### 2. Supplier Dashboard — Mini Map Card (optional, v2)

A small map card on the supplier dashboard showing all recent event locations across all their batches. Deferred to v2 — the batch detail map is the priority.

---

## Data Source

Events already have location data in two forms:

1. **`location` field** — human-readable text (e.g., "Nyungwe Mine, Rwanda"). Always present.
2. **`metadata.gpsCoordinates`** — lat/lng string (e.g., "-1.9403,29.8739"). Present when logged via mobile PWA.

**Map plotting priority:**
- If `metadata.gpsCoordinates` exists → use those exact coordinates
- If only `location` text exists → geocode via a free service, or show the event in the timeline without a map pin

**For the pilot:** Only plot events that have GPS coordinates. Events without GPS show in the timeline list below the map but not on the map itself. This avoids a geocoding dependency.

---

## Technology

### Recommended: Leaflet + OpenStreetMap

| Option | Pros | Cons |
|--------|------|------|
| **Leaflet + OSM** (recommended) | Free, no API key, open source, lightweight (42KB), Angular-friendly | Tiles less polished than Google Maps |
| Google Maps | Best tiles, familiar UX | Requires API key, billing account, usage costs |
| Mapbox | Beautiful tiles, free tier (50K loads/month) | Requires API key, token management |

**Leaflet + OpenStreetMap** is the right choice for the pilot:
- Zero cost, no API key needed
- Tile URL: `https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png`
- Works offline with cached tiles (if user has viewed the area before)
- No billing risk

### NPM Package

`leaflet` (JS library) + `@types/leaflet` (TypeScript types)

No Angular-specific wrapper needed — Leaflet works directly with a DOM element. Using a wrapper like `ngx-leaflet` adds unnecessary abstraction.

---

## Map Component Design

### `EventMapComponent` (standalone, presentational)

**Inputs:**
- `events` — array of `{ eventType: string; location: string; actorName: string; eventDate: string; latitude?: number; longitude?: number }`

**Behavior:**
1. Filters events to only those with latitude/longitude
2. Renders a Leaflet map with tile layer
3. Adds numbered circle markers at each event location (chronological order)
4. Draws polyline connecting markers in order
5. Auto-fits map bounds to show all markers
6. Marker popup shows: event type, actor, date, location name
7. If no events have GPS coordinates, shows a message: "No GPS data available for this batch"

**Marker styling:**
- Numbered circles (1, 2, 3...) matching the event order
- Color matches event type:
  - ⛏️ Extraction → brown
  - 🔬 Assay → blue
  - 🏭 Concentration → slate
  - 🤝 Trading → amber
  - 🔥 Smelting → red
  - 🚢 Export → indigo
- Polyline: dashed, slate-400, connecting in order

**Map defaults:**
- Zoom: auto-fit to bounds with padding
- If single point: zoom level 8
- Tile layer: OpenStreetMap

---

## Integration Points

### Batch Detail (Supplier, Buyer, Admin)

Add "Map" as a new tab in the batch detail component. The tab only shows if at least one event has GPS coordinates.

**Supplier batch detail:** `packages/web/src/app/features/supplier/batch-detail.component.ts`
**Buyer batch detail:** `packages/web/src/app/features/buyer/batch-detail.component.ts`

Both use the same `EventMapComponent` — it's a shared presentational component.

### Data Flow

```
BatchDetailComponent
  │
  ├── facade.events()          ← existing custody events array
  │
  └── EventMapComponent
       ├── input: events with lat/lng extracted from metadata
       └── renders: Leaflet map with markers + polyline
```

The events are already loaded by the batch detail page. The map component just needs to extract GPS from `metadata.gpsCoordinates` and parse lat/lng.

---

## File Structure

### New Files
- `packages/web/src/app/shared/ui/event-map.component.ts` — Leaflet map component
- `packages/web/src/app/shared/ui/event-map.component.css` — Leaflet CSS import (required for proper rendering)

### Modified Files
- `packages/web/package.json` — add `leaflet` and `@types/leaflet`
- `packages/web/angular.json` — add Leaflet CSS to styles array
- `packages/web/src/app/features/supplier/batch-detail.component.ts` — add Map tab
- `packages/web/src/app/features/buyer/batch-detail.component.ts` — add Map tab (if separate from supplier)

---

## Event GPS Extraction

The `CustodyEventResponse` model currently has:

```typescript
interface CustodyEventResponse {
  id: string;
  batchId: string;
  eventType: string;
  eventDate: string;
  location: string;
  actorName: string;
  isCorrection: boolean;
  sha256Hash: string;
  metadata?: Record<string, unknown>;
}
```

GPS is stored in `metadata.gpsCoordinates` as a string: `"-1.9403,29.8739"`.

The map component parses this:

```typescript
function extractCoordinates(event: CustodyEventResponse): { lat: number; lng: number } | null {
  const gps = event.metadata?.['gpsCoordinates'] as string | undefined;
  if (!gps) return null;
  const [lat, lng] = gps.split(',').map(Number);
  if (isNaN(lat) || isNaN(lng)) return null;
  return { lat, lng };
}
```

---

## What We Do Not Build

- Geocoding of text-only locations (no Google Maps API dependency)
- Satellite/terrain tile layers (OSM street tiles only)
- Editable map pins (map is read-only)
- Real-time tracking / live location updates
- Route optimization or distance calculation
- Mini-map on the dashboard (v2)
- Offline map tile caching (relies on browser cache)

---

## Success Criteria

1. Batch detail page shows a "Map" tab when events have GPS data
2. Events are plotted as numbered markers in chronological order
3. Markers are connected with a polyline showing the custody journey
4. Clicking a marker shows event details in a popup
5. Map auto-fits to show all markers
6. Works on desktop and mobile (responsive)
7. No API key or external account required
8. No cost — uses free OpenStreetMap tiles
