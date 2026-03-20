import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../notifications/notification.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [DatePipe],
  template: `
    <header class="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6">
      <div></div>
      <div class="flex items-center gap-4">
        <!-- Notification Bell -->
        <div class="relative">
          <button
            (click)="toggleNotifications()"
            class="relative p-2 text-slate-500 hover:text-slate-700 transition-colors"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
            </svg>
            @if (notifications.hasUnread()) {
              <span class="absolute -top-0.5 -right-0.5 bg-red-500 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center">
                {{ notifications.unreadCount() }}
              </span>
            }
          </button>

          @if (showNotifications()) {
            <div class="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-slate-200 z-50 max-h-96 overflow-y-auto">
              <div class="p-3 border-b border-slate-100 font-medium text-sm text-slate-900">
                Notifications
              </div>
              @for (n of notifications.notifications(); track n.id) {
                <div
                  (click)="onNotificationClick(n.id)"
                  class="p-3 border-b border-slate-50 hover:bg-slate-50 cursor-pointer"
                  [class.bg-blue-50]="!n.isRead"
                >
                  <p class="text-sm font-medium text-slate-900">{{ n.title }}</p>
                  <p class="text-xs text-slate-500 mt-0.5">{{ n.message }}</p>
                  <p class="text-xs text-slate-400 mt-1">{{ n.createdAt | date:'short' }}</p>
                </div>
              } @empty {
                <div class="p-4 text-center text-sm text-slate-400">No notifications</div>
              }
            </div>
          }
        </div>

        <span class="text-sm text-slate-600">{{ auth.profile()?.displayName }}</span>
        <button
          (click)="auth.logout()"
          class="text-sm text-slate-500 hover:text-slate-700 transition-colors"
        >
          Sign out
        </button>
      </div>
    </header>
  `,
})
export class TopbarComponent {
  protected auth = inject(AuthService);
  protected notifications = inject(NotificationService);

  showNotifications = signal(false);

  constructor() {
    this.notifications.load();
  }

  toggleNotifications() {
    this.showNotifications.update(v => !v);
    if (this.showNotifications()) {
      this.notifications.load();
    }
  }

  onNotificationClick(id: string) {
    this.notifications.markAsRead(id);
  }
}
