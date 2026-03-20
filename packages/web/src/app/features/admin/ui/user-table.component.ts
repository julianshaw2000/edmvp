import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { UserResponse } from '../data/admin.models';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';

@Component({
  selector: 'app-user-table',
  standalone: true,
  imports: [StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-slate-200">
        <thead class="bg-slate-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Name</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Email</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Role</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Status</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-slate-200">
          @for (user of users(); track user.id) {
            <tr class="hover:bg-slate-50 transition-colors">
              <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-slate-900">{{ user.displayName }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-slate-500">{{ user.email }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-slate-500">{{ user.role }}</td>
              <td class="px-6 py-4 whitespace-nowrap">
                <app-status-badge [status]="user.isActive ? 'ACTIVE' : 'INACTIVE'" />
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                <button
                  (click)="editClicked.emit(user)"
                  class="text-blue-600 hover:text-blue-800 transition-colors"
                >
                  Edit
                </button>
                @if (user.isActive) {
                  <button
                    (click)="deactivateClicked.emit(user)"
                    class="text-red-600 hover:text-red-800 transition-colors"
                  >
                    Deactivate
                  </button>
                }
              </td>
            </tr>
          } @empty {
            <tr>
              <td colspan="5" class="px-6 py-12 text-center text-slate-400">No users found.</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class UserTableComponent {
  users = input.required<UserResponse[]>();
  editClicked = output<UserResponse>();
  deactivateClicked = output<UserResponse>();
}
