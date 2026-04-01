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

    if (latLngs.length > 1) {
      L.polyline(latLngs, {
        color: '#94a3b8',
        weight: 2,
        dashArray: '6, 8',
        opacity: 0.7,
      }).addTo(this.markersLayer!);
    }

    if (latLngs.length === 1) {
      this.map.setView(latLngs[0], 8);
    } else {
      this.map.fitBounds(L.latLngBounds(latLngs), { padding: [40, 40] });
    }

    setTimeout(() => this.map?.invalidateSize(), 100);
  }
}
