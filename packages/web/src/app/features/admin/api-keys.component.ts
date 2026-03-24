import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from './data/admin-api.service';
import { ApiKeyResponse, CreateApiKeyResponse } from './data/admin.models';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';

@Component({
  selector: 'app-api-keys',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, FormsModule, PageHeaderComponent, LoadingSpinnerComponent],
  template: `
    <a routerLink="/admin" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>

    <div class="flex items-start justify-between mb-6">
      <app-page-header
        title="API Keys"
        subtitle="Manage programmatic access keys for third-party integrations"
      />
      <button
        (click)="showCreateForm.set(true)"
        class="flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 transition-colors"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
        </svg>
        Create API Key
      </button>
    </div>

    <!-- Create form -->
    @if (showCreateForm()) {
      <div class="bg-white rounded-xl border border-indigo-200 shadow-sm p-6 mb-6">
        <h3 class="font-semibold text-slate-900 mb-4">New API Key</h3>
        <div class="flex items-end gap-3">
          <div class="flex-1">
            <label class="block text-sm font-medium text-slate-700 mb-1.5">Key name</label>
            <input
              type="text"
              [(ngModel)]="newKeyName"
              placeholder="e.g. CI Pipeline, ERP Integration"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none"
              (keydown.enter)="createKey()"
            />
          </div>
          <button
            (click)="createKey()"
            [disabled]="creating() || !newKeyName().trim()"
            class="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            @if (creating()) { Creating... } @else { Create }
          </button>
          <button
            (click)="cancelCreate()"
            class="px-4 py-2 text-slate-600 text-sm font-medium rounded-lg border border-slate-300 hover:bg-slate-50 transition-colors"
          >
            Cancel
          </button>
        </div>
        @if (createError()) {
          <p class="mt-2 text-sm text-red-600">{{ createError() }}</p>
        }
      </div>
    }

    <!-- New key revealed once -->
    @if (newlyCreatedKey()) {
      <div class="bg-amber-50 border border-amber-300 rounded-xl p-5 mb-6">
        <div class="flex items-start gap-3 mb-3">
          <svg class="w-5 h-5 text-amber-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z"/>
          </svg>
          <div>
            <p class="font-semibold text-amber-900">Copy your API key now</p>
            <p class="text-sm text-amber-700 mt-0.5">This key will not be shown again. Store it securely.</p>
          </div>
        </div>
        <div class="flex items-center gap-2 mt-3">
          <code class="flex-1 bg-white border border-amber-200 rounded-lg px-3 py-2 text-sm font-mono text-slate-800 break-all">
            {{ newlyCreatedKey()!.key }}
          </code>
          <button
            (click)="copyKey(newlyCreatedKey()!.key)"
            class="shrink-0 flex items-center gap-1.5 px-3 py-2 bg-amber-600 text-white text-sm font-medium rounded-lg hover:bg-amber-700 transition-colors"
          >
            @if (copied()) {
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
              </svg>
              Copied!
            } @else {
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3"/>
              </svg>
              Copy
            }
          </button>
        </div>
        <button
          (click)="newlyCreatedKey.set(null)"
          class="mt-3 text-xs text-amber-700 hover:underline"
        >
          I've saved my key — dismiss
        </button>
      </div>
    }

    <!-- Keys list -->
    @if (loading()) {
      <div class="flex justify-center py-12">
        <app-loading-spinner />
      </div>
    } @else if (keys().length === 0) {
      <div class="text-center py-16 bg-white rounded-xl border border-slate-200">
        <svg class="w-10 h-10 text-slate-300 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"/>
        </svg>
        <p class="text-slate-500 text-sm">No API keys yet. Create one to enable programmatic access.</p>
      </div>
    } @else {
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-200 bg-slate-50 text-left">
              <th class="px-5 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Name</th>
              <th class="px-5 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Prefix</th>
              <th class="px-5 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Status</th>
              <th class="px-5 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Last Used</th>
              <th class="px-5 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Created</th>
              <th class="px-5 py-3"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            @for (key of keys(); track key.id) {
              <tr class="hover:bg-slate-50 transition-colors">
                <td class="px-5 py-4 font-medium text-slate-900">{{ key.name }}</td>
                <td class="px-5 py-4">
                  <code class="text-xs bg-slate-100 px-2 py-1 rounded font-mono text-slate-700">{{ key.keyPrefix }}...</code>
                </td>
                <td class="px-5 py-4">
                  @if (key.isActive) {
                    <span class="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-700 bg-emerald-50 border border-emerald-200 px-2.5 py-1 rounded-full">
                      <span class="w-1.5 h-1.5 bg-emerald-500 rounded-full"></span>
                      Active
                    </span>
                  } @else {
                    <span class="inline-flex items-center gap-1.5 text-xs font-medium text-slate-500 bg-slate-100 border border-slate-200 px-2.5 py-1 rounded-full">
                      <span class="w-1.5 h-1.5 bg-slate-400 rounded-full"></span>
                      Revoked
                    </span>
                  }
                </td>
                <td class="px-5 py-4 text-slate-500">
                  {{ key.lastUsedAt ? (key.lastUsedAt | date:'mediumDate') : '—' }}
                </td>
                <td class="px-5 py-4 text-slate-500">{{ key.createdAt | date:'mediumDate' }}</td>
                <td class="px-5 py-4 text-right">
                  @if (key.isActive) {
                    <button
                      (click)="revokeKey(key)"
                      [disabled]="revokingId() === key.id"
                      class="text-xs font-medium text-red-600 hover:text-red-700 disabled:opacity-50 transition-colors"
                    >
                      {{ revokingId() === key.id ? 'Revoking...' : 'Revoke' }}
                    </button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class ApiKeysComponent {
  private adminApi = inject(AdminApiService);

  protected keys = signal<ApiKeyResponse[]>([]);
  protected loading = signal(true);
  protected showCreateForm = signal(false);
  protected newKeyName = signal('');
  protected creating = signal(false);
  protected createError = signal<string | null>(null);
  protected newlyCreatedKey = signal<CreateApiKeyResponse | null>(null);
  protected copied = signal(false);
  protected revokingId = signal<string | null>(null);

  constructor() {
    this.loadKeys();
  }

  private loadKeys(): void {
    this.loading.set(true);
    this.adminApi.listApiKeys().subscribe({
      next: (keys) => {
        this.keys.set(keys);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected createKey(): void {
    const name = this.newKeyName().trim();
    if (!name) return;

    this.creating.set(true);
    this.createError.set(null);

    this.adminApi.createApiKey(name).subscribe({
      next: (res) => {
        this.newlyCreatedKey.set(res);
        this.creating.set(false);
        this.showCreateForm.set(false);
        this.newKeyName.set('');
        this.loadKeys();
      },
      error: () => {
        this.createError.set('Failed to create API key. Please try again.');
        this.creating.set(false);
      },
    });
  }

  protected cancelCreate(): void {
    this.showCreateForm.set(false);
    this.newKeyName.set('');
    this.createError.set(null);
  }

  protected revokeKey(key: ApiKeyResponse): void {
    this.revokingId.set(key.id);
    this.adminApi.revokeApiKey(key.id).subscribe({
      next: () => {
        this.revokingId.set(null);
        this.loadKeys();
      },
      error: () => this.revokingId.set(null),
    });
  }

  protected copyKey(key: string): void {
    navigator.clipboard.writeText(key).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }
}
