import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from '../http/api-url.token';

export interface Notification {
  id: string;
  type: string;
  title: string;
  message: string;
  referenceId: string | null;
  isRead: boolean;
  createdAt: string;
}

interface NotificationResponse {
  items: Notification[];
  unreadCount: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  private _notifications = signal<Notification[]>([]);
  private _unreadCount = signal(0);

  readonly notifications = this._notifications.asReadonly();
  readonly unreadCount = this._unreadCount.asReadonly();
  readonly hasUnread = computed(() => this._unreadCount() > 0);

  load() {
    this.http.get<NotificationResponse>(`${this.apiUrl}/api/notifications`)
      .subscribe({
        next: (res) => {
          this._notifications.set(res.items);
          this._unreadCount.set(res.unreadCount);
        },
      });
  }

  markAsRead(id: string) {
    this.http.patch(`${this.apiUrl}/api/notifications/${id}/read`, {})
      .subscribe({
        next: () => {
          this._notifications.update(list =>
            list.map(n => n.id === id ? { ...n, isRead: true } : n)
          );
          this._unreadCount.update(c => Math.max(0, c - 1));
        },
      });
  }
}
