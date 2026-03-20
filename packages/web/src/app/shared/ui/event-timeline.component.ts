import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-event-timeline',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    <div class="space-y-1">
      @for (event of events(); track event.id) {
        <div class="flex gap-4 group">
          <div class="flex flex-col items-center">
            <div
              class="w-3.5 h-3.5 rounded-full ring-4 ring-white shrink-0"
              [class]="event.isCorrection ? 'bg-amber-400' : 'bg-indigo-500'"
            ></div>
            @if (!$last) {
              <div class="w-0.5 flex-1 bg-slate-200 mt-1"></div>
            }
          </div>
          <div class="flex-1 pb-6">
            <div class="flex items-center gap-2">
              <span class="font-semibold text-slate-900 text-sm">{{ event.eventType.replace('_', ' ') }}</span>
              @if (event.isCorrection) {
                <span class="text-[10px] font-semibold bg-amber-50 text-amber-700 ring-1 ring-amber-600/20 px-2 py-0.5 rounded-full">Correction</span>
              }
            </div>
            <p class="text-sm text-slate-500 mt-0.5">
              {{ event.location }} &middot; {{ event.actorName }}
            </p>
            <p class="text-xs text-slate-400 mt-1">{{ event.eventDate | date:'medium' }}</p>
          </div>
        </div>
      } @empty {
        <div class="flex flex-col items-center py-8">
          <svg class="w-8 h-8 text-slate-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-sm text-slate-400">No events yet</p>
        </div>
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
