import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../notifications/notification.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [DatePipe],
  template: `
    <header class="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6 shadow-[0_1px_3px_0_rgba(0,0,0,0.04)]">
      <!-- Left: Breadcrumb area -->
      <div class="flex items-center gap-2 text-sm text-slate-500">
        <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
        </svg>
        <span class="text-slate-400">/</span>
        <span class="text-slate-700 font-medium">{{ auth.role() === 'SUPPLIER' ? 'Supplier Portal' : auth.role() === 'BUYER' ? 'Buyer Portal' : 'Admin' }}</span>
      </div>

      <div class="flex items-center gap-3">
        <!-- Notification Bell -->
        <div class="relative">
          <button
            (click)="toggleNotifications()"
            class="relative p-2 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100 transition-all duration-150"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
            </svg>
            @if (notifications.hasUnread()) {
              <span class="absolute top-1 right-1 w-2.5 h-2.5 bg-rose-500 rounded-full ring-2 ring-white"></span>
            }
          </button>

          @if (showNotifications()) {
            <div class="absolute right-0 mt-2 w-80 bg-white rounded-xl shadow-lg border border-slate-200 z-50 max-h-96 overflow-y-auto">
              <div class="px-4 py-3 border-b border-slate-100 flex items-center justify-between">
                <span class="font-semibold text-sm text-slate-900">Notifications</span>
                @if (notifications.hasUnread()) {
                  <span class="text-xs font-medium text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded-full">
                    {{ notifications.unreadCount() }} new
                  </span>
                }
              </div>
              @for (n of notifications.notifications(); track n.id) {
                <div
                  (click)="onNotificationClick(n.id)"
                  class="px-4 py-3 border-b border-slate-50 hover:bg-slate-50 cursor-pointer transition-colors"
                  [class.bg-indigo-50/50]="!n.isRead"
                >
                  <p class="text-sm font-medium text-slate-900">{{ n.title }}</p>
                  <p class="text-xs text-slate-500 mt-0.5 line-clamp-2">{{ n.message }}</p>
                  <p class="text-[11px] text-slate-400 mt-1">{{ n.createdAt | date:'short' }}</p>
                </div>
              } @empty {
                <div class="px-4 py-8 text-center">
                  <svg class="w-8 h-8 text-slate-300 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                  </svg>
                  <p class="text-sm text-slate-400">No notifications yet</p>
                </div>
              }
            </div>
          }
        </div>

        <!-- Divider -->
        <div class="w-px h-8 bg-slate-200"></div>

        <!-- User Menu -->
        <div class="relative">
          <button
            (click)="toggleUserMenu()"
            class="flex items-center gap-2.5 px-2 py-1.5 rounded-lg hover:bg-slate-100 transition-all duration-150"
          >
            <div class="w-8 h-8 rounded-full bg-indigo-600 flex items-center justify-center text-xs font-semibold text-white">
              {{ auth.profile()?.displayName?.charAt(0)?.toUpperCase() }}
            </div>
            <div class="hidden sm:block text-left">
              <p class="text-sm font-medium text-slate-700">{{ auth.profile()?.displayName }}</p>
              <p class="text-[11px] text-slate-400">{{ formatRole(auth.role()) }}</p>
            </div>
            <svg class="w-4 h-4 text-slate-400 hidden sm:block" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          @if (showUserMenu()) {
            <div class="absolute right-0 mt-2 w-48 bg-white rounded-xl shadow-lg border border-slate-200 z-50 py-1">
              <button
                (click)="auth.logout(); showUserMenu.set(false)"
                class="w-full px-4 py-2.5 text-left text-sm text-slate-600 hover:bg-slate-50 hover:text-slate-900 flex items-center gap-2 transition-colors"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                </svg>
                Sign out
              </button>
            </div>
          }
        </div>
      </div>
    </header>
  `,
})
export class TopbarComponent {
  protected auth = inject(AuthService);
  protected notifications = inject(NotificationService);

  showNotifications = signal(false);
  showUserMenu = signal(false);

  constructor() {
    this.notifications.load();
  }

  toggleNotifications() {
    this.showNotifications.update(v => !v);
    this.showUserMenu.set(false);
    if (this.showNotifications()) {
      this.notifications.load();
    }
  }

  toggleUserMenu() {
    this.showUserMenu.update(v => !v);
    this.showNotifications.set(false);
  }

  onNotificationClick(id: string) {
    this.notifications.markAsRead(id);
  }

  formatRole(role: string | null): string {
    if (!role) return '';
    return role.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase()).toLowerCase().replace(/\b\w/g, l => l.toUpperCase());
  }
}
