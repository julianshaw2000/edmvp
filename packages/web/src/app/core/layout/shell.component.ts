import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  template: `
    <div class="flex min-h-screen">
      <!-- Mobile overlay -->
      @if (sidebarOpen()) {
        <div
          class="fixed inset-0 bg-black/50 z-40 lg:hidden"
          (click)="sidebarOpen.set(false)"
        ></div>
      }

      <!-- Sidebar -->
      <div
        class="fixed lg:static inset-y-0 left-0 z-50 transform transition-transform duration-200 lg:transform-none"
        [class.-translate-x-full]="!sidebarOpen()"
        [class.translate-x-0]="sidebarOpen()"
      >
        <app-sidebar />
      </div>

      <div class="flex-1 flex flex-col min-w-0">
        <app-topbar />
        <!-- Mobile menu button -->
        <button
          (click)="sidebarOpen.set(true)"
          class="lg:hidden fixed bottom-4 left-4 z-30 bg-slate-900 text-white p-3 rounded-full shadow-lg"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <main class="flex-1 p-4 sm:p-6 bg-slate-50">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent {
  sidebarOpen = signal(false);
}
