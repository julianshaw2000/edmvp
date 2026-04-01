import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
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
