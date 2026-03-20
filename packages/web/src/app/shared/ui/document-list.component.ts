import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-document-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <div class="space-y-2">
      @for (doc of documents(); track doc.id) {
        <div class="flex items-center justify-between py-2 px-3 bg-slate-50 rounded-lg">
          <div>
            <p class="text-sm font-medium text-slate-900">{{ doc.fileName }}</p>
            <p class="text-xs text-slate-500">{{ doc.documentType }} &middot; {{ formatSize(doc.fileSizeBytes) }}</p>
          </div>
          <a
            [href]="doc.downloadUrl"
            target="_blank"
            class="text-sm text-blue-600 hover:text-blue-700"
          >Download</a>
        </div>
      } @empty {
        <p class="text-slate-400 text-sm text-center py-4">No documents uploaded</p>
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
