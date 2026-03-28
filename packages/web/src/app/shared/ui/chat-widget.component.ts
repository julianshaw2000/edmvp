import {
  Component,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy,
  ElementRef,
  ViewChild,
  AfterViewChecked,
} from '@angular/core';
import { AdminApiService } from '../../features/admin/data/admin-api.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Floating button -->
    @if (!open()) {
      <button
        (click)="open.set(true)"
        class="fixed bottom-6 right-6 z-50 w-14 h-14 bg-indigo-600 hover:bg-indigo-700 text-white rounded-full shadow-lg shadow-indigo-600/30 flex items-center justify-center transition-all duration-200 hover:scale-105"
        title="Chat with auditraks assistant"
      >
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
        </svg>
      </button>
    }

    <!-- Chat panel -->
    @if (open()) {
      <div class="fixed bottom-6 right-6 z-50 w-[360px] max-w-[calc(100vw-2rem)] bg-white rounded-2xl shadow-2xl border border-slate-200 flex flex-col overflow-hidden" style="height: 500px;">
        <!-- Header -->
        <div class="flex items-center justify-between px-4 py-3 bg-indigo-600 text-white">
          <div class="flex items-center gap-2">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
            </svg>
            <span class="font-semibold text-sm">auditraks Assistant</span>
          </div>
          <button
            (click)="open.set(false)"
            class="text-indigo-200 hover:text-white transition-colors"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <!-- Messages -->
        <div #messagesContainer class="flex-1 overflow-y-auto px-4 py-3 space-y-3">
          @if (messages().length === 0) {
            <div class="flex flex-col items-center justify-center h-full text-center px-4">
              <div class="w-12 h-12 rounded-full bg-indigo-50 flex items-center justify-center mb-3">
                <svg class="w-6 h-6 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
                </svg>
              </div>
              <p class="text-sm font-medium text-slate-700">Hi! I'm the auditraks assistant.</p>
              <p class="text-xs text-slate-500 mt-1">Ask me about compliance, batches, custody events, or how to use the platform.</p>
            </div>
          }
          @for (msg of messages(); track $index) {
            <div [class]="msg.role === 'user' ? 'flex justify-end' : 'flex justify-start'">
              <div
                [class]="msg.role === 'user'
                  ? 'bg-indigo-600 text-white rounded-2xl rounded-tr-sm px-3.5 py-2.5 max-w-[80%] text-sm leading-relaxed'
                  : 'bg-slate-100 text-slate-800 rounded-2xl rounded-tl-sm px-3.5 py-2.5 max-w-[80%] text-sm leading-relaxed'"
              >
                {{ msg.content }}
              </div>
            </div>
          }
          @if (sending()) {
            <div class="flex justify-start">
              <div class="bg-slate-100 text-slate-500 rounded-2xl rounded-tl-sm px-3.5 py-2.5 text-sm">
                <span class="inline-flex gap-1 items-center">
                  <span class="w-1.5 h-1.5 bg-slate-400 rounded-full animate-bounce" style="animation-delay: 0ms"></span>
                  <span class="w-1.5 h-1.5 bg-slate-400 rounded-full animate-bounce" style="animation-delay: 150ms"></span>
                  <span class="w-1.5 h-1.5 bg-slate-400 rounded-full animate-bounce" style="animation-delay: 300ms"></span>
                </span>
              </div>
            </div>
          }
        </div>

        <!-- Input -->
        <div class="px-3 py-3 border-t border-slate-100">
          <form (submit)="sendMessage($event)" class="flex gap-2">
            <input
              #inputField
              type="text"
              [(value)]="inputText"
              (input)="inputText = $any($event.target).value"
              placeholder="Ask a question..."
              [disabled]="sending()"
              class="flex-1 text-sm border border-slate-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent disabled:opacity-50 placeholder-slate-400"
            />
            <button
              type="submit"
              [disabled]="sending() || !inputText.trim()"
              class="w-9 h-9 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg flex items-center justify-center transition-colors disabled:opacity-50 disabled:cursor-not-allowed shrink-0"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"/>
              </svg>
            </button>
          </form>
        </div>
      </div>
    }
  `,
})
export class ChatWidgetComponent implements AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer?: ElementRef<HTMLDivElement>;

  private adminApi = inject(AdminApiService);

  protected open = signal(false);
  protected sending = signal(false);
  protected messages = signal<ChatMessage[]>([]);
  protected inputText = '';

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  private scrollToBottom() {
    if (this.messagesContainer) {
      const el = this.messagesContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }

  sendMessage(event: Event) {
    event.preventDefault();
    const text = this.inputText.trim();
    if (!text || this.sending()) return;

    const userMsg: ChatMessage = { role: 'user', content: text };
    this.messages.update(msgs => [...msgs, userMsg]);
    this.inputText = '';
    this.sending.set(true);

    const history = this.messages().slice(0, -1).map(m => ({ role: m.role, content: m.content }));

    this.adminApi.chatWithAssistant(text, history).subscribe({
      next: (res) => {
        this.messages.update(msgs => [...msgs, { role: 'assistant', content: res.reply }]);
        this.sending.set(false);
      },
      error: (err) => {
        const status = err?.status ?? 'unknown';
        const detail = err?.error?.error ?? err?.message ?? 'Connection failed';
        console.error('[Chat] Error:', status, detail, err);
        const msg = status === 401
          ? 'Session expired. Please refresh the page and try again.'
          : `Sorry, I had trouble connecting (${status}). Please try again.`;
        this.messages.update(msgs => [...msgs, { role: 'assistant', content: msg }]);
        this.sending.set(false);
      },
    });
  }
}
