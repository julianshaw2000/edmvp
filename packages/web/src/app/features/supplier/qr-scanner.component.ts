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
      <div class="bg-black/80 text-white px-4 py-4 flex items-center gap-3 relative z-10">
        <a routerLink="/supplier" class="p-1">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
          </svg>
        </a>
        <h1 class="text-lg font-semibold">Scan QR Code</h1>
      </div>

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
    const match = text.match(/verify\/([a-f0-9-]+)/i) || text.match(/^([a-f0-9-]{36})$/i);
    if (match) {
      this.navigateToBatch(match[1]);
    }
  }

  navigateToBatch(id: string) {
    if (id) this.router.navigate(['/supplier/batch', id]);
  }
}
