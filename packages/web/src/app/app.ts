import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { EventMessage, EventType, InteractionStatus } from '@azure/msal-browser';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class AppComponent implements OnInit, OnDestroy {
  private msal = inject(MsalService);
  private broadcastService = inject(MsalBroadcastService);
  private readonly destroying$ = new Subject<void>();

  ngOnInit() {
    // Required: process redirect response before any other MSAL calls
    this.msal.handleRedirectObservable().subscribe();

    // Set active account once MSAL finishes any in-progress interaction
    this.broadcastService.inProgress$
      .pipe(
        filter(status => status === InteractionStatus.None),
        takeUntil(this.destroying$),
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
        takeUntil(this.destroying$),
      )
      .subscribe(() => {
        const accounts = this.msal.instance.getAllAccounts();
        if (accounts.length > 0) {
          this.msal.instance.setActiveAccount(accounts[0]);
        }
      });
  }

  ngOnDestroy() {
    this.destroying$.next();
    this.destroying$.complete();
  }
}
