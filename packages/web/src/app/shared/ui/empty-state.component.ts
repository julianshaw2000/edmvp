import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `
    <div class="text-center py-12">
      <p class="text-slate-400 text-lg">{{ message() }}</p>
    </div>
  `,
})
export class EmptyStateComponent {
  message = input('No data found');
}
