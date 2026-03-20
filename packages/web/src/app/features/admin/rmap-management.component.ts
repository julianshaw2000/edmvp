import { Component, inject, signal } from '@angular/core';
import { AdminFacade } from './admin.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'app-rmap-management',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <app-page-header
      title="RMAP Smelter List"
      subtitle="Upload and manage the Responsible Minerals Assurance Process smelter list"
    />

    <div class="bg-white rounded-xl shadow-sm border border-slate-200 p-6 mb-6">
      <h2 class="text-lg font-semibold text-slate-900 mb-4">Upload CSV</h2>

      <div
        class="border-2 border-dashed border-slate-300 rounded-lg p-8 text-center hover:border-blue-400 transition-colors"
      >
        <p class="text-slate-500 mb-4">Drag and drop a CSV file here, or click to browse</p>
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
          class="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
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
            class="bg-green-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
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
        <div class="mt-4 bg-green-50 border border-green-200 rounded-lg p-4">
          <p class="text-sm text-green-700">RMAP smelter list uploaded successfully.</p>
        </div>
      }

      @if (facade.rmapUploadError()) {
        <div class="mt-4 bg-red-50 border border-red-200 rounded-lg p-4">
          <p class="text-sm text-red-700">{{ facade.rmapUploadError() }}</p>
        </div>
      }
    </div>

    <div class="bg-slate-50 rounded-xl border border-slate-200 p-6">
      <p class="text-slate-500 text-sm">RMAP list management will display current smelters after upload.</p>
    </div>
  `,
})
export class RmapManagementComponent {
  protected facade = inject(AdminFacade);
  protected selectedFile = signal<File | null>(null);
  protected selectedFileName = signal<string | null>(null);

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
