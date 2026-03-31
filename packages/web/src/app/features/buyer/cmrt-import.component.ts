import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { API_URL } from '../../core/http/api-url.token';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

interface SmelterPreviewItem {
  metalType: string;
  smelterName: string | null;
  smelterId: string | null;
  country: string | null;
  matchStatus: 'matched' | 'unmatched';
  matchedSmelterId: string | null;
  matchedSmelterName: string | null;
  conformanceStatus: string | null;
  rowNumber: number;
}

interface PreviewResponse {
  declarationCompany: string;
  reportingYear: number | null;
  declarationScope: string | null;
  totalSmelters: number;
  matched: number;
  unmatched: number;
  errorCount: number;
  smelters: SmelterPreviewItem[];
  errors: string[];
}

interface ConfirmResponse {
  importId: string;
  created: number;
  skipped: number;
}

interface ImportHistoryItem {
  id: string;
  fileName: string;
  declarationCompany: string;
  reportingYear: number | null;
  rowsParsed: number;
  rowsMatched: number;
  rowsUnmatched: number;
  importedBy: string;
  importedAt: string;
}

@Component({
  selector: 'app-cmrt-import',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, PageHeaderComponent],
  template: `
    <a routerLink="/buyer" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>
    <app-page-header title="CMRT Import" subtitle="Import smelter data from a Conflict Minerals Reporting Template" />

    <div class="max-w-4xl">
      @if (!preview()) {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-8 mb-6">
          <div class="border-2 border-dashed border-slate-300 rounded-xl p-10 text-center hover:border-indigo-400 transition-colors cursor-pointer"
            (click)="fileInput.click()"
            (dragover)="$event.preventDefault(); $event.stopPropagation()"
            (drop)="onFileDrop($event)">
            <svg class="w-12 h-12 text-slate-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
            </svg>
            <p class="text-sm font-medium text-slate-700 mb-1">Drop your CMRT file here or click to browse</p>
            <p class="text-xs text-slate-400">Accepts .xlsx files (CMRT v6.x format)</p>
          </div>
          <input #fileInput type="file" accept=".xlsx" class="hidden" (change)="onFileSelected($event)" />

          @if (uploading()) {
            <div class="mt-4 flex items-center gap-2 text-sm text-indigo-600">
              <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              Parsing CMRT file...
            </div>
          }
          @if (error()) {
            <p class="mt-4 text-sm text-rose-600">{{ error() }}</p>
          }
        </div>
      }

      @if (preview(); as data) {
        <div class="space-y-6 mb-6">
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
            <h3 class="text-sm font-semibold text-slate-900 mb-3">Declaration Summary</h3>
            <div class="grid grid-cols-3 gap-4 text-sm">
              <div>
                <span class="text-slate-500">Company:</span>
                <span class="ml-1 font-medium text-slate-900">{{ data.declarationCompany }}</span>
              </div>
              <div>
                <span class="text-slate-500">Reporting Year:</span>
                <span class="ml-1 font-medium text-slate-900">{{ data.reportingYear ?? 'N/A' }}</span>
              </div>
              <div>
                <span class="text-slate-500">Scope:</span>
                <span class="ml-1 font-medium text-slate-900">{{ data.declarationScope ?? 'N/A' }}</span>
              </div>
            </div>
          </div>

          <div class="grid grid-cols-3 gap-4">
            <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5 text-center">
              <p class="text-2xl font-bold text-slate-900">{{ data.totalSmelters }}</p>
              <p class="text-xs text-slate-500">Total Smelters</p>
            </div>
            <div class="bg-emerald-50 rounded-xl border border-emerald-200 shadow-sm p-5 text-center">
              <p class="text-2xl font-bold text-emerald-700">{{ data.matched }}</p>
              <p class="text-xs text-emerald-600">Matched in RMAP</p>
            </div>
            <div class="bg-amber-50 rounded-xl border border-amber-200 shadow-sm p-5 text-center">
              <p class="text-2xl font-bold text-amber-700">{{ data.unmatched }}</p>
              <p class="text-xs text-amber-600">Unmatched</p>
            </div>
          </div>

          @if (data.errors.length > 0) {
            <div class="bg-rose-50 border border-rose-200 rounded-xl p-4">
              <h4 class="text-sm font-semibold text-rose-700 mb-2">Parsing Errors</h4>
              @for (err of data.errors; track err) {
                <p class="text-xs text-rose-600">{{ err }}</p>
              }
            </div>
          }

          <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-slate-50 border-b border-slate-200">
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Row</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Metal</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Smelter</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">ID</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Country</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Match</th>
                </tr>
              </thead>
              <tbody>
                @for (s of data.smelters; track s.rowNumber) {
                  <tr class="border-b border-slate-100"
                    [class]="s.matchStatus === 'matched' ? '' : 'bg-amber-50/50'">
                    <td class="px-4 py-2.5 text-slate-400">{{ s.rowNumber }}</td>
                    <td class="px-4 py-2.5 text-slate-700">{{ s.metalType }}</td>
                    <td class="px-4 py-2.5 text-slate-900 font-medium">{{ s.smelterName ?? '—' }}</td>
                    <td class="px-4 py-2.5 text-slate-500 font-mono text-xs">{{ s.smelterId ?? '—' }}</td>
                    <td class="px-4 py-2.5 text-slate-500">{{ s.country ?? '—' }}</td>
                    <td class="px-4 py-2.5">
                      @if (s.matchStatus === 'matched') {
                        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700">
                          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                          </svg>
                          {{ s.conformanceStatus }}
                        </span>
                      } @else {
                        <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-amber-50 text-amber-700">
                          Unmatched
                        </span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <div class="flex items-center gap-3">
            <button (click)="confirmImport()"
              [disabled]="confirming()"
              class="bg-indigo-600 text-white py-2.5 px-6 rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 shadow-sm transition-all">
              {{ confirming() ? 'Importing...' : 'Confirm Import' }}
            </button>
            <button (click)="cancelPreview()"
              class="text-sm font-medium text-slate-500 hover:text-slate-700 px-4 py-2.5 rounded-xl hover:bg-slate-100 transition-all">
              Cancel
            </button>
          </div>

          @if (confirmResult()) {
            <div class="bg-emerald-50 border border-emerald-200 rounded-xl p-4 text-sm text-emerald-700">
              Import complete: {{ confirmResult()!.created }} associations created, {{ confirmResult()!.skipped }} skipped.
            </div>
          }
        </div>
      }

      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-200">
          <h3 class="text-sm font-semibold text-slate-900">Import History</h3>
        </div>
        @if (history().length > 0) {
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 border-b border-slate-200">
                <th class="text-left px-4 py-3 font-semibold text-slate-600">File</th>
                <th class="text-left px-4 py-3 font-semibold text-slate-600">Company</th>
                <th class="text-center px-4 py-3 font-semibold text-slate-600">Matched</th>
                <th class="text-center px-4 py-3 font-semibold text-slate-600">Unmatched</th>
                <th class="text-left px-4 py-3 font-semibold text-slate-600">Imported</th>
              </tr>
            </thead>
            <tbody>
              @for (h of history(); track h.id) {
                <tr class="border-b border-slate-100 last:border-0">
                  <td class="px-4 py-3 font-medium text-slate-900">{{ h.fileName }}</td>
                  <td class="px-4 py-3 text-slate-500">{{ h.declarationCompany }}</td>
                  <td class="px-4 py-3 text-center text-emerald-600 font-medium">{{ h.rowsMatched }}</td>
                  <td class="px-4 py-3 text-center text-amber-600 font-medium">{{ h.rowsUnmatched }}</td>
                  <td class="px-4 py-3 text-slate-500">{{ h.importedAt | date:'medium' }}</td>
                </tr>
              }
            </tbody>
          </table>
        } @else {
          <div class="px-6 py-8 text-center text-slate-400 text-sm">No imports yet</div>
        }
      </div>
    </div>
  `,
})
export class CmrtImportComponent {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  uploading = signal(false);
  error = signal<string | null>(null);
  preview = signal<PreviewResponse | null>(null);
  confirming = signal(false);
  confirmResult = signal<ConfirmResponse | null>(null);
  history = signal<ImportHistoryItem[]>([]);

  private selectedFileName = '';

  constructor() {
    this.loadHistory();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.uploadFile(input.files[0]);
  }

  onFileDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    const file = event.dataTransfer?.files[0];
    if (file) this.uploadFile(file);
  }

  private uploadFile(file: File) {
    if (!file.name.endsWith('.xlsx')) {
      this.error.set('Only .xlsx files are supported');
      return;
    }
    this.uploading.set(true);
    this.error.set(null);
    this.selectedFileName = file.name;

    const formData = new FormData();
    formData.append('file', file);

    this.http.post<PreviewResponse>(
      `${this.apiUrl}/api/buyer/cmrt-import/preview`, formData
    ).subscribe({
      next: (res) => {
        this.preview.set(res);
        this.uploading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.error ?? 'Failed to parse CMRT file');
        this.uploading.set(false);
      },
    });
  }

  confirmImport() {
    const data = this.preview();
    if (!data) return;
    this.confirming.set(true);
    this.confirmResult.set(null);

    this.http.post<ConfirmResponse>(
      `${this.apiUrl}/api/buyer/cmrt-import/confirm`,
      {
        fileName: this.selectedFileName,
        declarationCompany: data.declarationCompany,
        reportingYear: data.reportingYear,
        smelters: data.smelters.map(s => ({
          metalType: s.metalType,
          smelterName: s.smelterName,
          smelterId: s.smelterId,
          country: s.country,
          matchStatus: s.matchStatus,
          matchedSmelterId: s.matchedSmelterId,
        })),
      }
    ).subscribe({
      next: (res) => {
        this.confirmResult.set(res);
        this.confirming.set(false);
        this.loadHistory();
      },
      error: (err) => {
        this.error.set(err.error?.error ?? 'Failed to confirm import');
        this.confirming.set(false);
      },
    });
  }

  cancelPreview() {
    this.preview.set(null);
    this.confirmResult.set(null);
    this.error.set(null);
  }

  private loadHistory() {
    this.http.get<ImportHistoryItem[]>(
      `${this.apiUrl}/api/buyer/cmrt-imports`
    ).subscribe({
      next: (res) => this.history.set(res),
    });
  }
}
