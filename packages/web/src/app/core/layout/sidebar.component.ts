import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../auth/auth.service';

interface NavItem {
  label: string;
  route: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <aside class="w-64 bg-slate-900 text-white min-h-screen flex flex-col">
      <div class="p-6 border-b border-slate-700">
        <h1 class="text-lg font-bold tracking-tight">Tungsten</h1>
        <p class="text-xs text-slate-400 mt-1">Supply Chain Compliance</p>
      </div>
      <nav class="flex-1 p-4 space-y-1">
        @for (item of navItems(); track item.route) {
          <a
            [routerLink]="item.route"
            routerLinkActive="bg-slate-700 text-white"
            class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-slate-300 hover:bg-slate-800 hover:text-white transition-colors"
          >
            {{ item.label }}
          </a>
        }
      </nav>
    </aside>
  `,
})
export class SidebarComponent {
  private auth = inject(AuthService);

  readonly navItems = computed<NavItem[]>(() => {
    const role = this.auth.role();
    switch (role) {
      case 'SUPPLIER':
        return [
          { label: 'Dashboard', route: '/supplier' },
          { label: 'Submit Event', route: '/supplier/submit' },
        ];
      case 'BUYER':
        return [
          { label: 'Dashboard', route: '/buyer' },
        ];
      case 'PLATFORM_ADMIN':
        return [
          { label: 'Dashboard', route: '/admin' },
          { label: 'Users', route: '/admin/users' },
          { label: 'RMAP Data', route: '/admin/rmap' },
          { label: 'Compliance', route: '/admin/compliance' },
        ];
      default:
        return [];
    }
  });
}
