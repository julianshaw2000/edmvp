import { Component, computed, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../auth/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

interface NavGroup {
  title: string;
  items: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <aside class="w-64 bg-slate-900 text-white min-h-screen flex flex-col">
      <!-- Logo -->
      <div class="px-5 py-5 border-b border-slate-700/50">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 bg-indigo-600 rounded-lg flex items-center justify-center">
            <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
          </div>
          <div>
            <h1 class="text-base font-bold tracking-tight text-white">AccuTrac</h1>
            <p class="text-[10px] text-slate-400 font-medium uppercase tracking-wider">Supply Chain Compliance</p>
          </div>
        </div>
      </div>

      <!-- Navigation -->
      <nav class="flex-1 px-3 py-4 space-y-6 overflow-y-auto">
        @for (group of navGroups(); track group.title) {
          <div>
            <p class="px-3 mb-2 text-[10px] font-semibold text-slate-500 uppercase tracking-wider">{{ group.title }}</p>
            <div class="space-y-0.5">
              @for (item of group.items; track item.route) {
                <a
                  [routerLink]="item.route"
                  routerLinkActive="!bg-indigo-600/10 !text-white !border-l-indigo-500"
                  class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-slate-400 hover:bg-slate-800 hover:text-slate-200 border-l-2 border-transparent transition-all duration-150"
                >
                  <span class="w-5 h-5 flex-shrink-0" [innerHTML]="item.icon"></span>
                  <span>{{ item.label }}</span>
                </a>
              }
            </div>
          </div>
        }
      </nav>

      <!-- User section -->
      @if (auth.profile(); as user) {
        <div class="px-4 py-4 border-t border-slate-700/50">
          <div class="flex items-center gap-3">
            <div class="w-8 h-8 rounded-full bg-indigo-600 flex items-center justify-center text-xs font-semibold text-white flex-shrink-0">
              {{ user.displayName.charAt(0).toUpperCase() }}
            </div>
            <div class="min-w-0 flex-1">
              <p class="text-sm font-medium text-slate-200 truncate">{{ user.displayName }}</p>
              <p class="text-[11px] text-slate-500 truncate">{{ user.tenantName }}</p>
            </div>
          </div>
        </div>
      }
    </aside>
  `,
})
export class SidebarComponent {
  protected auth = inject(AuthService);

  readonly navGroups = computed<NavGroup[]>(() => {
    const role = this.auth.role();
    switch (role) {
      case 'SUPPLIER':
        return [{
          title: 'Supplier',
          items: [
            { label: 'Dashboard', route: '/supplier', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>' },
            { label: 'Submit Event', route: '/supplier/submit', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 4v16m8-8H4"/></svg>' },
          ],
        }];
      case 'BUYER':
        return [{
          title: 'Buyer',
          items: [
            { label: 'Dashboard', route: '/buyer', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>' },
          ],
        }];
      case 'PLATFORM_ADMIN':
        return [
          {
            title: 'Overview',
            items: [
              { label: 'Dashboard', route: '/admin', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>' },
            ],
          },
          {
            title: 'Management',
            items: [
              { label: 'Users', route: '/admin/users', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>' },
              { label: 'RMAP Data', route: '/admin/rmap', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4"/></svg>' },
              { label: 'Compliance', route: '/admin/compliance', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>' },
            ],
          },
        ];
      default:
        return [];
    }
  });
}
