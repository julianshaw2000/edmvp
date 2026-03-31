import { Component, input, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { API_URL } from '../../../core/http/api-url.token';

@Component({
  selector: 'app-passport-share-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="bg-gradient-to-br from-indigo-50 to-white rounded-xl border border-indigo-200 shadow-sm overflow-hidden">
      <div class="p-5 sm:p-6">
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 rounded-xl bg-indigo-100 flex items-center justify-center">
            <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
            </svg>
          </div>
          <div>
            <h3 class="text-sm font-semibold text-slate-900">Material Passport Ready</h3>
            <p class="text-xs text-slate-500">Share with your customers to demonstrate compliance</p>
          </div>
        </div>

        <div class="flex flex-wrap gap-2 mb-4">
          <button (click)="generatePassport()"
            [disabled]="generating()"
            class="inline-flex items-center gap-1.5 px-3 py-2 bg-indigo-600 text-white rounded-lg text-xs font-semibold hover:bg-indigo-700 disabled:opacity-50 transition-colors">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
            </svg>
            {{ generating() ? 'Generating...' : 'Download PDF' }}
          </button>

          <button (click)="copyShareLink()"
            [disabled]="sharing()"
            class="inline-flex items-center gap-1.5 px-3 py-2 bg-white border border-slate-200 text-slate-700 rounded-lg text-xs font-semibold hover:bg-slate-50 disabled:opacity-50 transition-colors">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3"/>
            </svg>
            {{ copied() ? 'Copied!' : sharing() ? 'Creating...' : 'Copy Link' }}
          </button>

          <button (click)="showEmailForm.set(!showEmailForm())"
            class="inline-flex items-center gap-1.5 px-3 py-2 bg-white border border-slate-200 text-slate-700 rounded-lg text-xs font-semibold hover:bg-slate-50 transition-colors">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
            </svg>
            Email to Customer
          </button>
        </div>

        @if (showEmailForm()) {
          <div class="border-t border-indigo-100 pt-4 space-y-3">
            <div>
              <label class="block text-xs font-medium text-slate-600 mb-1">Recipient Email</label>
              <input type="email" [(ngModel)]="recipientEmail" name="recipientEmail"
                placeholder="customer@example.com"
                class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"/>
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-600 mb-1">Message (optional)</label>
              <textarea [(ngModel)]="emailMessage" name="emailMessage" rows="2"
                placeholder="Add a note for your customer..."
                class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 resize-none"></textarea>
            </div>
            <button (click)="sendEmail()"
              [disabled]="sending() || !recipientEmail"
              class="inline-flex items-center gap-1.5 px-4 py-2 bg-indigo-600 text-white rounded-lg text-xs font-semibold hover:bg-indigo-700 disabled:opacity-50 transition-colors">
              {{ sending() ? 'Sending...' : 'Send Passport' }}
            </button>
            @if (emailSent()) {
              <p class="text-xs text-emerald-600 font-medium">Passport sent successfully!</p>
            }
          </div>
        }

        @if (error()) {
          <p class="text-xs text-rose-600 mt-2">{{ error() }}</p>
        }
      </div>
    </div>
  `,
})
export class PassportShareCardComponent {
  batchId = input.required<string>();

  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  generating = signal(false);
  sharing = signal(false);
  copied = signal(false);
  showEmailForm = signal(false);
  sending = signal(false);
  emailSent = signal(false);
  error = signal<string | null>(null);

  recipientEmail = '';
  emailMessage = '';

  private passportDocId = signal<string | null>(null);

  generatePassport() {
    this.generating.set(true);
    this.error.set(null);
    this.http.post<{ id: string; downloadUrl: string }>(
      `${this.apiUrl}/api/batches/${this.batchId()}/passport`, {}
    ).subscribe({
      next: (res) => {
        this.passportDocId.set(res.id);
        this.generating.set(false);
        window.open(res.downloadUrl, '_blank');
      },
      error: (err) => {
        this.error.set(err.error?.error ?? 'Failed to generate passport');
        this.generating.set(false);
      },
    });
  }

  copyShareLink() {
    this.sharing.set(true);
    this.error.set(null);
    this.ensurePassportDoc().then(docId => {
      if (!docId) return;
      this.http.post<{ shareUrl: string }>(
        `${this.apiUrl}/api/generated-documents/${docId}/share`, {}
      ).subscribe({
        next: (res) => {
          navigator.clipboard.writeText(res.shareUrl);
          this.copied.set(true);
          this.sharing.set(false);
          setTimeout(() => this.copied.set(false), 2000);
        },
        error: (err) => {
          this.error.set(err.error?.error ?? 'Failed to create share link');
          this.sharing.set(false);
        },
      });
    });
  }

  sendEmail() {
    this.sending.set(true);
    this.error.set(null);
    this.emailSent.set(false);
    this.ensurePassportDoc().then(docId => {
      if (!docId) return;
      this.http.post(
        `${this.apiUrl}/api/generated-documents/${docId}/share-email`,
        { recipientEmail: this.recipientEmail, message: this.emailMessage || null }
      ).subscribe({
        next: () => {
          this.emailSent.set(true);
          this.sending.set(false);
          this.recipientEmail = '';
          this.emailMessage = '';
        },
        error: (err) => {
          this.error.set(err.error?.error ?? 'Failed to send email');
          this.sending.set(false);
        },
      });
    });
  }

  private async ensurePassportDoc(): Promise<string | null> {
    if (this.passportDocId()) return this.passportDocId()!;

    return new Promise(resolve => {
      this.http.get<{ items: { id: string; documentType: string }[] }>(
        `${this.apiUrl}/api/generated-documents?batchId=${this.batchId()}`
      ).subscribe({
        next: (res) => {
          const passport = res.items.find(d => d.documentType === 'MATERIAL_PASSPORT');
          if (passport) {
            this.passportDocId.set(passport.id);
            resolve(passport.id);
          } else {
            this.http.post<{ id: string }>(
              `${this.apiUrl}/api/batches/${this.batchId()}/passport`, {}
            ).subscribe({
              next: (r) => {
                this.passportDocId.set(r.id);
                resolve(r.id);
              },
              error: () => {
                this.error.set('Failed to generate passport');
                this.sharing.set(false);
                this.sending.set(false);
                resolve(null);
              },
            });
          }
        },
        error: () => {
          this.error.set('Failed to check for existing passport');
          this.sharing.set(false);
          this.sending.set(false);
          resolve(null);
        },
      });
    });
  }
}
