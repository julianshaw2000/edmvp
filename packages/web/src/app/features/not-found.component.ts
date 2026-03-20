import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50">
      <div class="text-center">
        <h1 class="text-6xl font-bold text-slate-300">404</h1>
        <p class="mt-4 text-lg text-slate-600">Page not found</p>
        <a routerLink="/" class="mt-6 inline-block text-blue-600 hover:text-blue-700 text-sm font-medium">
          Go back home
        </a>
      </div>
    </div>
  `,
})
export class NotFoundComponent {}
