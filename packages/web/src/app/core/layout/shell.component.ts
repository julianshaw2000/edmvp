import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';
import { ChatWidgetComponent } from '../../shared/ui/chat-widget.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, ChatWidgetComponent],
  template: `
    <div class="flex min-h-screen bg-slate-50">
      <!-- Mobile overlay -->
      @if (sidebarOpen()) {
        <div
          class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-40 md:hidden transition-opacity"
          (click)="sidebarOpen.set(false)"
        ></div>
      }

      <!-- Sidebar -->
      <div
        class="fixed md:static inset-y-0 left-0 z-50 transform transition-transform duration-200 ease-in-out md:transform-none"
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
          class="md:hidden fixed bottom-5 left-5 z-30 bg-indigo-600 text-white p-3.5 rounded-full shadow-lg shadow-indigo-600/30 hover:bg-indigo-700 transition-all duration-150"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <main class="flex-1 p-5 sm:p-8 max-w-[1400px] w-full mx-auto">
          <router-outlet />
        </main>
      </div>
    </div>
    <app-chat-widget />
  `,
})
export class ShellComponent {
  sidebarOpen = signal(false);
}
