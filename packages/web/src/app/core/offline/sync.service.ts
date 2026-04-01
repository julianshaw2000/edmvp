import { Injectable, inject, signal } from '@angular/core';
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
            const msg = err?.error?.error ?? err?.message ?? 'Unknown error';
            await this.offlineDb.updateEventStatus(event.id, 'failed', msg);
            await this.offlineDb.addSyncLog({ eventId: event.id, result: 'error', errorMessage: msg });
            failed++;
          } else {
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
