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
