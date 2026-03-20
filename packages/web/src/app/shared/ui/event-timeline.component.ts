import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-event-timeline',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    <div class="space-y-4">
      @for (event of events(); track event.id) {
        <div class="flex gap-4">
          <div class="flex flex-col items-center">
            <div class="w-3 h-3 rounded-full" [class]="event.isCorrection ? 'bg-amber-400' : 'bg-blue-500'"></div>
            @if (!$last) {
              <div class="w-0.5 flex-1 bg-slate-200 mt-1"></div>
            }
          </div>
          <div class="flex-1 pb-4">
            <div class="flex items-center gap-2">
              <span class="font-medium text-slate-900 text-sm">{{ event.eventType }}</span>
              @if (event.isCorrection) {
                <span class="text-xs bg-amber-100 text-amber-700 px-1.5 py-0.5 rounded">Correction</span>
              }
            </div>
            <p class="text-sm text-slate-500 mt-0.5">
              {{ event.location }} &middot; {{ event.actorName }}
            </p>
            <p class="text-xs text-slate-400 mt-0.5">{{ event.eventDate | date:'medium' }}</p>
          </div>
        </div>
      } @empty {
        <p class="text-slate-400 text-center py-4">No events yet</p>
      }
    </div>
  `,
})
export class EventTimelineComponent {
  events = input.required<{
    id: string; eventType: string; eventDate: string;
    location: string; actorName: string; isCorrection: boolean;
  }[]>();
}
