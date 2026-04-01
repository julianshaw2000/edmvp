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
