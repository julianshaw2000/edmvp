import {
  ChangeDetectionStrategy,
  Component,
  computed,
  output,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';

interface WizardStep {
  number: number;
  title: string;
  description: string;
  actionLabel?: string;
  actionRoute?: string;
}

const STEPS: WizardStep[] = [
  {
    number: 1,
    title: 'Welcome to auditraks!',
    description: "Let's set up your organization. This wizard will walk you through the key first steps to get your supply chain compliance platform running.",
  },
  {
    number: 2,
    title: 'Invite your team',
    description: 'Add suppliers and buyers to your organization so they can log custody events and access their portals.',
    actionLabel: 'Go to Users',
    actionRoute: '/admin/users',
  },
  {
    number: 3,
    title: 'Create your first batch',
    description: 'Track your first mineral batch through the supply chain. Each batch gets a unique ID and full custody trail.',
    actionLabel: 'Create Batch',
    actionRoute: '/supplier/new-batch',
  },
  {
    number: 4,
    title: 'Run compliance checks',
    description: 'Once you have custody events logged, compliance checks against RMAP and OECD DDG run automatically. No action needed.',
  },
];

@Component({
  selector: 'app-onboarding-wizard',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-6 bg-white border border-indigo-200 rounded-xl shadow-sm overflow-hidden">
      <!-- Header -->
      <div class="flex items-center justify-between px-6 py-4 bg-indigo-50 border-b border-indigo-100">
        <div class="flex items-center gap-2">
          <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M9 12l2 2 4-4M7.835 4.697a3.42 3.42 0 001.946-.806 3.42 3.42 0 014.438 0 3.42 3.42 0 001.946.806 3.42 3.42 0 013.138 3.138 3.42 3.42 0 00.806 1.946 3.42 3.42 0 010 4.438 3.42 3.42 0 00-.806 1.946 3.42 3.42 0 01-3.138 3.138 3.42 3.42 0 00-1.946.806 3.42 3.42 0 01-4.438 0 3.42 3.42 0 00-1.946-.806 3.42 3.42 0 01-3.138-3.138 3.42 3.42 0 00-.806-1.946 3.42 3.42 0 010-4.438 3.42 3.42 0 00.806-1.946 3.42 3.42 0 013.138-3.138z" />
          </svg>
          <span class="text-sm font-semibold text-indigo-800">Getting Started</span>
          <span class="text-xs text-indigo-500 font-medium ml-1">Step {{ currentStepIndex() + 1 }} of {{ steps.length }}</span>
        </div>
        <button
          (click)="dismiss()"
          class="text-xs font-medium text-indigo-500 hover:text-indigo-700 transition-colors px-2 py-1 rounded hover:bg-indigo-100"
          aria-label="Dismiss onboarding wizard"
        >
          Dismiss
        </button>
      </div>

      <!-- Step progress dots -->
      <div class="flex items-center gap-2 px-6 pt-4">
        @for (step of steps; track step.number) {
          <button
            (click)="goToStep($index)"
            class="flex items-center gap-1.5 group"
            [attr.aria-label]="'Go to step ' + step.number + ': ' + step.title"
          >
            <span
              class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold transition-colors"
              [class]="$index === currentStepIndex()
                ? 'bg-indigo-600 text-white'
                : $index < currentStepIndex()
                  ? 'bg-indigo-200 text-indigo-700'
                  : 'bg-slate-100 text-slate-400 group-hover:bg-indigo-100 group-hover:text-indigo-500'"
            >
              @if ($index < currentStepIndex()) {
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
                </svg>
              } @else {
                {{ step.number }}
              }
            </span>
            @if ($index < steps.length - 1) {
              <span class="w-8 h-px" [class]="$index < currentStepIndex() ? 'bg-indigo-300' : 'bg-slate-200'"></span>
            }
          </button>
        }
      </div>

      <!-- Step content -->
      <div class="px-6 py-5">
        <h3 class="text-base font-semibold text-slate-900 mb-1">{{ currentStep().title }}</h3>
        <p class="text-sm text-slate-600 leading-relaxed">{{ currentStep().description }}</p>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-between px-6 pb-5">
        <div class="flex items-center gap-2">
          @if (currentStepIndex() > 0) {
            <button
              (click)="prev()"
              class="text-sm font-medium text-slate-500 hover:text-slate-700 px-3 py-1.5 rounded-lg hover:bg-slate-100 transition-colors"
            >
              Previous
            </button>
          }
        </div>

        <div class="flex items-center gap-3">
          @if (currentStep().actionLabel && currentStep().actionRoute) {
            <a
              [routerLink]="currentStep().actionRoute"
              class="text-sm font-medium text-indigo-600 hover:text-indigo-700 px-3 py-1.5 rounded-lg border border-indigo-200 hover:bg-indigo-50 transition-colors"
            >
              {{ currentStep().actionLabel }}
            </a>
          }

          @if (currentStepIndex() < steps.length - 1) {
            <button
              (click)="next()"
              class="text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 px-4 py-1.5 rounded-lg transition-colors"
            >
              Next
            </button>
          } @else {
            <button
              (click)="dismiss()"
              class="text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 px-4 py-1.5 rounded-lg transition-colors"
            >
              Done
            </button>
          }
        </div>
      </div>
    </div>
  `,
})
export class OnboardingWizardComponent {
  readonly dismissed = output<void>();

  protected readonly steps = STEPS;
  protected readonly currentStepIndex = signal(0);
  protected readonly currentStep = computed(() => STEPS[this.currentStepIndex()]);

  protected next() {
    if (this.currentStepIndex() < STEPS.length - 1) {
      this.currentStepIndex.update((i) => i + 1);
    }
  }

  protected prev() {
    if (this.currentStepIndex() > 0) {
      this.currentStepIndex.update((i) => i - 1);
    }
  }

  protected goToStep(index: number) {
    this.currentStepIndex.set(index);
  }

  protected dismiss() {
    this.dismissed.emit();
  }
}
