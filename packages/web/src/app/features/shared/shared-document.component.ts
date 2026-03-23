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
      <div class="max-w-lg w-full">
        <div class="text-center mb-8">
          <div class="inline-flex items-center gap-2 mb-2">
            <div class="w-8 h-8 bg-indigo-600 rounded-lg flex items-center justify-center">
              <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
              </svg>
            </div>
            <span class="text-lg font-bold text-slate-900">auditraks</span>
          </div>
          <p class="text-xs text-slate-500">Secure Document Sharing</p>
        </div>

      <div class="bg-white rounded-2xl border border-slate-200 shadow-sm w-full p-8">

        @if (loading()) {
          <div class="text-center py-8">
            <div class="inline-block w-8 h-8 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin mb-4"></div>
            <p class="text-slate-500 text-sm">Loading document...</p>
          </div>
        } @else if (error()) {
          <div class="text-center py-8">
            <div class="w-14 h-14 bg-rose-50 rounded-2xl flex items-center justify-center mx-auto mb-4">
              <svg class="w-7 h-7 text-rose-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h2 class="text-lg font-bold text-slate-900 mb-2">Document Not Found</h2>
            <p class="text-sm text-slate-500">{{ error() }}</p>
          </div>
        } @else if (doc()) {
          <div>
            <div class="flex items-start gap-4 mb-6">
              <div class="w-14 h-14 bg-indigo-50 rounded-2xl flex items-center justify-center shrink-0">
                <svg class="w-7 h-7 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                </svg>
              </div>
              <div class="min-w-0">
                <h1 class="text-lg font-bold text-slate-900 truncate">{{ doc()!.fileName }}</h1>
                <p class="text-sm text-slate-500 mt-0.5">{{ doc()!.documentType }}</p>
              </div>
            </div>

            <dl class="space-y-3 mb-6">
              <div class="flex justify-between text-sm py-2 border-b border-slate-100">
                <dt class="text-slate-500">Batch</dt>
                <dd class="text-slate-900 font-semibold">{{ doc()!.batchNumber }}</dd>
              </div>
              <div class="flex justify-between text-sm py-2 border-b border-slate-100">
                <dt class="text-slate-500">File size</dt>
                <dd class="text-slate-900">{{ formatSize(doc()!.fileSizeBytes) }}</dd>
              </div>
              <div class="flex justify-between text-sm py-2 border-b border-slate-100">
                <dt class="text-slate-500">Content type</dt>
                <dd class="text-slate-900">{{ doc()!.contentType }}</dd>
              </div>
              <div class="flex justify-between text-sm py-2">
                <dt class="text-slate-500">Uploaded</dt>
                <dd class="text-slate-900">{{ formatDate(doc()!.createdAt) }}</dd>
              </div>
            </dl>

            <a
              [href]="doc()!.downloadUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="flex items-center justify-center gap-2 w-full bg-indigo-600 text-white py-3 px-4 rounded-xl font-semibold hover:bg-indigo-700 shadow-sm shadow-indigo-600/20 transition-all duration-150"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
              </svg>
              Download Document
            </a>
          </div>
        }

        <div class="mt-6 pt-4 border-t border-slate-100 text-center">
          <div class="flex items-center justify-center gap-2">
            <div class="w-5 h-5 bg-indigo-600 rounded flex items-center justify-center">
              <svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <p class="text-xs text-slate-400 font-medium">auditraks Supply Chain Compliance</p>
          </div>
        </div>
      </div>

      <div class="text-center mt-8 text-xs text-slate-400">Shared via auditraks · auditraks.com</div>
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
