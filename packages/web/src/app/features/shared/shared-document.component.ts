import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { API_URL } from '../../core/http/api-url.token';

interface SharedDocumentInfo {
  id: string;
  fileName: string;
  fileSizeBytes: number;
  contentType: string;
  documentType: string;
  downloadUrl: string;
  batchNumber: string;
  createdAt: string;
}

@Component({
  selector: 'app-shared-document',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <div class="min-h-screen bg-slate-50 flex items-center justify-center p-6">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm max-w-lg w-full p-8">

        @if (loading()) {
          <div class="text-center py-8">
            <div class="inline-block w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mb-4"></div>
            <p class="text-slate-500 text-sm">Loading document...</p>
          </div>
        } @else if (error()) {
          <div class="text-center py-8">
            <div class="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg class="w-6 h-6 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h2 class="text-lg font-semibold text-slate-900 mb-2">Document Not Found</h2>
            <p class="text-sm text-slate-500">{{ error() }}</p>
          </div>
        } @else if (doc()) {
          <div>
            <div class="flex items-start gap-4 mb-6">
              <div class="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                </svg>
              </div>
              <div class="min-w-0">
                <h1 class="text-lg font-semibold text-slate-900 truncate">{{ doc()!.fileName }}</h1>
                <p class="text-sm text-slate-500 mt-0.5">{{ doc()!.documentType }}</p>
              </div>
            </div>

            <dl class="space-y-3 mb-6">
              <div class="flex justify-between text-sm">
                <dt class="text-slate-500">Batch</dt>
                <dd class="text-slate-900 font-medium">{{ doc()!.batchNumber }}</dd>
              </div>
              <div class="flex justify-between text-sm">
                <dt class="text-slate-500">File size</dt>
                <dd class="text-slate-900">{{ formatSize(doc()!.fileSizeBytes) }}</dd>
              </div>
              <div class="flex justify-between text-sm">
                <dt class="text-slate-500">Content type</dt>
                <dd class="text-slate-900">{{ doc()!.contentType }}</dd>
              </div>
              <div class="flex justify-between text-sm">
                <dt class="text-slate-500">Uploaded</dt>
                <dd class="text-slate-900">{{ formatDate(doc()!.createdAt) }}</dd>
              </div>
            </dl>

            <a
              [href]="doc()!.downloadUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="block w-full text-center bg-blue-600 text-white py-2.5 px-4 rounded-lg font-medium hover:bg-blue-700 transition-colors"
            >
              Download Document
            </a>
          </div>
        }

        <div class="mt-6 pt-4 border-t border-slate-100 text-center">
          <p class="text-xs text-slate-400">Tungsten Supply Chain Compliance Platform</p>
        </div>
      </div>
    </div>
  `,
})
export class SharedDocumentComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  loading = signal(true);
  error = signal<string | null>(null);
  doc = signal<SharedDocumentInfo | null>(null);

  ngOnInit() {
    const token = this.route.snapshot.paramMap.get('token');
    if (!token) {
      this.error.set('Invalid share link.');
      this.loading.set(false);
      return;
    }

    this.http.get<SharedDocumentInfo>(`${this.apiUrl}/api/shared/${token}`)
      .subscribe({
        next: (res) => {
          this.doc.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('This document could not be found or the link has expired.');
          this.loading.set(false);
        },
      });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' });
  }
}
