import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-document-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <div class="space-y-2">
      @for (doc of documents(); track doc.id) {
        <div class="flex items-center justify-between py-3 px-4 rounded-xl border border-slate-100 hover:bg-slate-50 transition-colors group">
          <div class="flex items-center gap-3">
            <div class="w-9 h-9 rounded-lg bg-slate-100 flex items-center justify-center shrink-0 group-hover:bg-indigo-50 transition-colors">
              <svg class="w-4 h-4 text-slate-500 group-hover:text-indigo-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <div>
              <p class="text-sm font-medium text-slate-900">{{ doc.fileName }}</p>
              <p class="text-xs text-slate-400">{{ doc.documentType }} &middot; {{ formatSize(doc.fileSizeBytes) }}</p>
            </div>
          </div>
          <a
            [href]="doc.downloadUrl"
            target="_blank"
            class="inline-flex items-center gap-1.5 text-sm text-indigo-600 hover:text-indigo-700 font-semibold opacity-0 group-hover:opacity-100 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
            </svg>
            Download
          </a>
        </div>
      } @empty {
        <div class="flex flex-col items-center py-8">
          <svg class="w-8 h-8 text-slate-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <p class="text-sm text-slate-400">No documents uploaded</p>
        </div>
      }
    </div>
  `,
})
export class DocumentListComponent {
  documents = input.required<{
    id: string; fileName: string; fileSizeBytes: number;
    documentType: string; downloadUrl: string; createdAt: string;
  }[]>();

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
