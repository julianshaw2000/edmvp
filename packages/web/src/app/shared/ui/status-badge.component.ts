import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="badgeClass()">
      <span [innerHTML]="iconSvg()"></span>
      {{ displayLabel() }}
    </span>
  `,
})
export class StatusBadgeComponent {
  status = input.required<string>();

  displayLabel = computed(() => {
    const s = this.status();
    return s.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  });

  iconSvg = computed(() => {
    switch (this.status()) {
      case 'COMPLIANT':
      case 'PASS':
      case 'ACTIVE':
        return '<svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/></svg>';
      case 'FLAGGED':
      case 'FLAG':
        return '<svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z"/></svg>';
      case 'NON_COMPLIANT':
      case 'FAIL':
      case 'INACTIVE':
        return '<svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M6 18L18 6M6 6l12 12"/></svg>';
      case 'PENDING':
      case 'INSUFFICIENT_DATA':
        return '<svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>';
      default:
        return '';
    }
  });

  badgeClass = computed(() => {
    const base = 'inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold whitespace-nowrap';
    switch (this.status()) {
      case 'COMPLIANT':
      case 'PASS':
      case 'ACTIVE':
        return `${base} bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/20`;
      case 'FLAGGED':
      case 'FLAG':
        return `${base} bg-amber-50 text-amber-700 ring-1 ring-amber-600/20`;
      case 'NON_COMPLIANT':
      case 'FAIL':
      case 'INACTIVE':
        return `${base} bg-rose-50 text-rose-700 ring-1 ring-rose-600/20`;
      case 'INSUFFICIENT_DATA':
        return `${base} bg-rose-50 text-rose-600 ring-1 ring-rose-600/20`;
      case 'PENDING':
        return `${base} bg-slate-100 text-slate-600 ring-1 ring-slate-600/10`;
      default:
        return `${base} bg-slate-100 text-slate-600 ring-1 ring-slate-600/10`;
    }
  });
}
