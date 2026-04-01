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

  async addSyncLog(entry: { eventId: string; result: 'success' | 'error'; errorMessage?: string }): Promise<void> {
    const db = await this.dbPromise;
    await db.add('sync_log', { ...entry, attemptedAt: new Date().toISOString() });
  }
}
