import { Component, inject, signal, OnInit } from '@angular/core';
import { AdminFacade } from './admin.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';

@Component({
  selector: 'app-rmap-management',
  standalone: true,
  imports: [PageHeaderComponent, StatusBadgeComponent],
  template: `
    <app-page-header
      title="RMAP Smelter List"
      subtitle="Upload and manage the Responsible Minerals Assurance Process smelter list"
    />

    <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 mb-6">
      <div class="flex items-center gap-3 mb-5">
        <div class="w-8 h-8 rounded-lg bg-indigo-50 flex items-center justify-center">
          <svg class="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
          </svg>
        </div>
        <h2 class="text-lg font-semibold text-slate-900">Upload CSV</h2>
      </div>

      <div
        class="border-2 border-dashed border-slate-300 rounded-xl p-8 text-center hover:border-indigo-400 transition-colors duration-200"
      >
        <div class="w-12 h-12 rounded-xl bg-slate-100 flex items-center justify-center mx-auto mb-4">
          <svg class="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
          </svg>
        </div>
        <p class="text-slate-500 mb-4 text-sm">Drag and drop a CSV file here, or click to browse</p>
        <input
          #fileInput
          type="file"
          accept=".csv"
          class="hidden"
          (change)="onFileSelected($event)"
        />
        <button
          type="button"
          (click)="fileInput.click()"
          class="bg-indigo-600 text-white px-5 py-2.5 rounded-xl text-sm font-semibold hover:bg-indigo-700 shadow-sm shadow-indigo-600/20 transition-all duration-150"
        >
          Choose CSV File
        </button>

        @if (selectedFileName()) {
          <p class="mt-3 text-sm text-slate-700">Selected: <strong>{{ selectedFileName() }}</strong></p>
        }
      </div>

      @if (selectedFile()) {
        <div class="mt-4 flex items-center gap-3">
          <button
            type="button"
            (click)="onUpload()"
            [disabled]="facade.rmapUploading()"
            class="bg-emerald-600 text-white px-5 py-2.5 rounded-xl text-sm font-semibold hover:bg-emerald-700 disabled:opacity-50 shadow-sm shadow-emerald-600/20 transition-all duration-150"
          >
            @if (facade.rmapUploading()) {
              Uploading...
            } @else {
              Upload List
            }
          </button>
        </div>
      }

      @if (facade.rmapUploadSuccess()) {
        <div class="mt-4 bg-emerald-50 border border-emerald-200 rounded-xl p-4 flex items-start gap-3">
          <svg class="w-5 h-5 text-emerald-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
          </svg>
          <p class="text-sm text-emerald-700 font-medium">RMAP smelter list uploaded successfully.</p>
        </div>
      }

      @if (facade.rmapUploadError()) {
        <div class="mt-4 bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
          <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-sm text-rose-700">{{ facade.rmapUploadError() }}</p>
        </div>
      }
    </div>

    <!-- Smelter Table -->
    @if (facade.smeltersLoading()) {
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-12 text-center">
        <div class="w-8 h-8 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin mx-auto mb-3"></div>
        <p class="text-sm text-slate-500">Loading smelters...</p>
      </div>
    } @else if (facade.smelters().length > 0) {
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="px-5 py-4 border-b border-slate-200 flex items-center justify-between">
          <h2 class="text-base font-semibold text-slate-900">
            Smelters
            <span class="ml-2 text-xs font-semibold text-slate-400 bg-slate-100 px-2.5 py-1 rounded-full">{{ facade.smelters().length }}</span>
          </h2>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-sm table-zebra">
            <thead>
              <tr class="border-b border-slate-200 bg-slate-50/50">
                <th class="text-left px-5 py-3.5 text-xs font-semibold text-slate-500 uppercase tracking-wider">Smelter ID</th>
                <th class="text-left px-5 py-3.5 text-xs font-semibold text-slate-500 uppercase tracking-wider">Name</th>
                <th class="text-left px-5 py-3.5 text-xs font-semibold text-slate-500 uppercase tracking-wider">Country</th>
                <th class="text-left px-5 py-3.5 text-xs font-semibold text-slate-500 uppercase tracking-wider">Status</th>
                <th class="text-left px-5 py-3.5 text-xs font-semibold text-slate-500 uppercase tracking-wider">Last Audit</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              @for (smelter of facade.smelters(); track smelter.smelterId) {
                <tr class="hover:bg-indigo-50/50 transition-colors">
                  <td class="px-5 py-3.5 font-mono text-xs text-slate-600">{{ smelter.smelterId }}</td>
                  <td class="px-5 py-3.5 text-slate-900 font-medium">{{ smelter.smelterName }}</td>
                  <td class="px-5 py-3.5 text-slate-600">{{ smelter.country }}</td>
                  <td class="px-5 py-3.5">
                    <app-status-badge [status]="smelter.conformanceStatus" />
                  </td>
                  <td class="px-5 py-3.5 text-slate-400">
                    {{ smelter.lastAuditDate ?? '--' }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    } @else {
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-12 text-center">
        <div class="w-12 h-12 rounded-xl bg-slate-100 flex items-center justify-center mx-auto mb-3">
          <svg class="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
          </svg>
        </div>
        <p class="text-sm text-slate-500">No smelters loaded. Upload a CSV to populate the smelter list.</p>
      </div>
    }
  `,
})
export class RmapManagementComponent implements OnInit {
  protected facade = inject(AdminFacade);
  protected selectedFile = signal<File | null>(null);
  protected selectedFileName = signal<string | null>(null);

  ngOnInit() {
    this.facade.loadSmelters();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(file);
    this.selectedFileName.set(file?.name ?? null);
  }

  onUpload() {
    const file = this.selectedFile();
    if (file) {
      this.facade.uploadRmapList(file);
    }
  }
}
