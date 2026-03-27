import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { BatchActivity } from '../models/batch.models';

export const AUDIT_ACTION_LABELS: Record<string, string> = {
  CreateBatch: 'Created batch',
  UpdateBatchStatus: 'Updated batch status',
  SplitBatch: 'Split batch',
  CreateCustodyEvent: 'Logged custody event',
  CreateCorrection: 'Submitted correction',
  UploadDocument: 'Uploaded document',
  GeneratePassport: 'Generated Material Passport',
  GenerateDossier: 'Generated audit dossier',
  ShareDocument: 'Shared document',
  CreateUser: 'Created user',
  UpdateUser: 'Updated user',
  UploadRmapList: 'Uploaded RMAP smelter list',
};

@Component({
  selector: 'app-activity-feed',
  standalone: true,
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-3">
      @for (entry of activities(); track entry.id) {
        <div class="flex items-start gap-3 py-2 border-b border-gray-100 last:border-0">
          <div class="w-2 h-2 mt-2 rounded-full flex-shrink-0"
               [class]="entry.result === 'Success' ? 'bg-green-500' : 'bg-red-500'"></div>
          <div class="flex-1">
            <p class="text-sm text-gray-900">
              <span class="font-medium">{{ entry.userDisplayName }}</span>
              {{ actionLabels[entry.action] || entry.action }}
            </p>
            @if (entry.failureReason) {
              <p class="text-xs text-red-600 mt-0.5">{{ entry.failureReason }}</p>
            }
            <p class="text-xs text-gray-500 mt-0.5">{{ entry.timestamp | date:'medium' }}</p>
          </div>
        </div>
      } @empty {
        <p class="text-sm text-gray-500 py-4 text-center">No activity recorded yet.</p>
      }
    </div>
  `,
})
export class ActivityFeedComponent {
  activities = input.required<BatchActivity[]>();
  protected readonly actionLabels = AUDIT_ACTION_LABELS;
}
