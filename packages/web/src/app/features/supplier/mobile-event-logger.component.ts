import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { OfflineDbService, PendingEvent } from '../../core/offline/offline-db.service';
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

        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-2">Actor Name</label>
          <input type="text" [(ngModel)]="actor" name="actor"
            placeholder="Who is logging this event?"
            class="w-full px-4 py-3 border border-slate-300 rounded-xl text-base focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500" />
        </div>

        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-2">Description</label>
          <textarea [(ngModel)]="description" name="description" rows="3"
            placeholder="Optional notes..."
            class="w-full px-4 py-3 border border-slate-300 rounded-xl text-base focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 resize-none"></textarea>
        </div>

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

        @if (error()) {
          <p class="text-sm text-rose-600">{{ error() }}</p>
        }

        @if (submitted()) {
          <div class="bg-emerald-50 border border-emerald-200 rounded-xl p-3 text-sm text-emerald-700 flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
            </svg>
            Event queued for sync
          </div>
        }
      </div>

      <div class="fixed bottom-0 left-0 right-0 bg-white border-t border-slate-200 px-4 py-3">
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

      this.eventType = '';
      this.actor = '';
      this.location = '';
      this.description = '';

      if (navigator.onLine) {
        this.sync.syncNow();
      }
    } catch (err) {
      this.error.set('Failed to queue event');
      this.submitting.set(false);
    }
  }
}
