import { Component, input, computed } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `
    <span [class]="badgeClass()">{{ status() }}</span>
  `,
})
export class StatusBadgeComponent {
  status = input.required<string>();

  badgeClass = computed(() => {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium';
    switch (this.status()) {
      case 'COMPLIANT':
      case 'PASS':
        return `${base} bg-green-100 text-green-800`;
      case 'FLAGGED':
      case 'FLAG':
        return `${base} bg-amber-100 text-amber-800`;
      case 'NON_COMPLIANT':
      case 'FAIL':
        return `${base} bg-red-100 text-red-800`;
      case 'INSUFFICIENT_DATA':
        return `${base} bg-yellow-100 text-yellow-800`;
      case 'PENDING':
        return `${base} bg-slate-100 text-slate-600`;
      default:
        return `${base} bg-slate-100 text-slate-600`;
    }
  });
}
