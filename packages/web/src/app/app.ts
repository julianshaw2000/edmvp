import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { EventMessage, EventType, InteractionStatus } from '@azure/msal-browser';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class AppComponent implements OnInit {
  private msal = inject(MsalService);
  private broadcastService = inject(MsalBroadcastService);
  private destroyRef = inject(DestroyRef);

  ngOnInit() {
    // Required: process redirect response before any other MSAL calls
    this.msal.handleRedirectObservable().subscribe();

    // Set active account once MSAL finishes any in-progress interaction
    this.broadcastService.inProgress$
      .pipe(
        filter(status => status === InteractionStatus.None),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        const accounts = this.msal.instance.getAllAccounts();
        if (accounts.length > 0 && !this.msal.instance.getActiveAccount()) {
          this.msal.instance.setActiveAccount(accounts[0]);
        }
      });

    // Set active account immediately on successful login
    this.broadcastService.msalSubject$
      .pipe(
        filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        const accounts = this.msal.instance.getAllAccounts();
        if (accounts.length > 0) {
          this.msal.instance.setActiveAccount(accounts[0]);
        }
      });
  }
}
