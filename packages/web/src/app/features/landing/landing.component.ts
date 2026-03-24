import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Nav -->
    <nav class="fixed top-0 left-0 right-0 z-50 bg-white/80 backdrop-blur-sm border-b border-slate-200">
      <div class="max-w-6xl mx-auto px-6 h-16 flex items-center justify-between">
        <a routerLink="/" class="text-xl font-bold text-indigo-600 tracking-tight">auditraks</a>
        <div class="flex items-center gap-4">
          <a routerLink="/login" class="text-sm font-medium text-slate-600 hover:text-slate-900 transition-colors">Login</a>
          <a routerLink="/signup" class="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-indigo-700 transition-colors shadow-sm">Start Free Trial</a>
        </div>
      </div>
    </nav>

    <!-- Hero -->
    <section class="pt-32 pb-20 px-6">
      <div class="max-w-3xl mx-auto text-center">
        <h1 class="text-4xl sm:text-5xl font-bold text-slate-900 leading-tight tracking-tight">
          3TG supply chain compliance, automated.
        </h1>
        <p class="mt-6 text-lg text-slate-600 leading-relaxed max-w-2xl mx-auto">
          Track custody from mine to refinery for tungsten, tin, tantalum, and gold. SHA-256 hash chains,
          RMAP + OECD compliance checks, Material Passport generation — all in one platform.
        </p>
        <div class="mt-10">
          <a routerLink="/signup" class="inline-flex items-center gap-2 bg-indigo-600 text-white px-8 py-3.5 rounded-xl text-base font-semibold hover:bg-indigo-700 transition-all shadow-lg shadow-indigo-600/20 hover:shadow-indigo-600/30">
            Start 60-day free trial
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
            </svg>
          </a>
          <p class="mt-4 text-sm text-slate-500">No credit card required to explore. $249/month after trial.</p>
        </div>
      </div>
    </section>

    <!-- Features -->
    <section class="py-20 px-6 bg-slate-50">
      <div class="max-w-6xl mx-auto">
        <h2 class="text-2xl font-bold text-slate-900 text-center mb-12">Built for mineral compliance</h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
          <!-- Feature 1 -->
          <div class="bg-white rounded-2xl p-8 shadow-sm border border-slate-200">
            <div class="w-12 h-12 rounded-xl bg-indigo-50 flex items-center justify-center mb-5">
              <svg class="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
              </svg>
            </div>
            <h3 class="text-lg font-semibold text-slate-900 mb-2">Tamper-Evident Tracking</h3>
            <p class="text-slate-600 text-sm leading-relaxed">
              Every custody event is SHA-256 hashed and chained to the previous one. Alter one record
              and the entire chain breaks — detectable instantly.
            </p>
          </div>

          <!-- Feature 2 -->
          <div class="bg-white rounded-2xl p-8 shadow-sm border border-slate-200">
            <div class="w-12 h-12 rounded-xl bg-emerald-50 flex items-center justify-center mb-5">
              <svg class="w-6 h-6 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
              </svg>
            </div>
            <h3 class="text-lg font-semibold text-slate-900 mb-2">Automated Compliance</h3>
            <p class="text-slate-600 text-sm leading-relaxed">
              Five automated checks on every batch: RMAP smelter verification, OECD origin risk,
              sanctions screening, mass balance, and sequence integrity.
            </p>
          </div>

          <!-- Feature 3 -->
          <div class="bg-white rounded-2xl p-8 shadow-sm border border-slate-200">
            <div class="w-12 h-12 rounded-xl bg-amber-50 flex items-center justify-center mb-5">
              <svg class="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
            </div>
            <h3 class="text-lg font-semibold text-slate-900 mb-2">Material Passports</h3>
            <p class="text-slate-600 text-sm leading-relaxed">
              Generate PDF Material Passports with QR codes. Share with auditors via secure links.
              Public verification — no account needed.
            </p>
          </div>
        </div>
      </div>
    </section>

    <!-- Pricing -->
    <section class="py-20 px-6">
      <div class="max-w-lg mx-auto text-center">
        <h2 class="text-2xl font-bold text-slate-900 mb-12">Simple, transparent pricing</h2>
        <div class="bg-white rounded-2xl p-10 shadow-lg border border-slate-200">
          <p class="text-sm font-semibold text-indigo-600 uppercase tracking-wider mb-2">Pro Plan</p>
          <div class="flex items-baseline justify-center gap-1 mb-2">
            <span class="text-5xl font-bold text-slate-900">$249</span>
            <span class="text-slate-500 text-lg">/month</span>
          </div>
          <p class="text-slate-500 text-sm mb-8">60-day free trial included</p>

          <ul class="text-left space-y-3 mb-10">
            <li class="flex items-center gap-3 text-sm text-slate-700">
              <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
              </svg>
              Unlimited batches
            </li>
            <li class="flex items-center gap-3 text-sm text-slate-700">
              <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
              </svg>
              Unlimited users
            </li>
            <li class="flex items-center gap-3 text-sm text-slate-700">
              <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
              </svg>
              Automated RMAP + OECD compliance checks
            </li>
            <li class="flex items-center gap-3 text-sm text-slate-700">
              <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
              </svg>
              Material Passport generation with QR codes
            </li>
            <li class="flex items-center gap-3 text-sm text-slate-700">
              <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
              </svg>
              SHA-256 hash chain integrity
            </li>
            <li class="flex items-center gap-3 text-sm text-slate-700">
              <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
              </svg>
              Admin dashboard + audit log
            </li>
          </ul>

          <a routerLink="/signup" class="block w-full bg-indigo-600 text-white py-3.5 rounded-xl text-base font-semibold hover:bg-indigo-700 transition-colors shadow-sm text-center">
            Start Free Trial
          </a>
        </div>
      </div>
    </section>

    <!-- Footer -->
    <footer class="py-8 px-6 border-t border-slate-200">
      <div class="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
        <p class="text-sm text-slate-500">&copy; 2026 auditraks. All rights reserved.</p>
        <div class="flex gap-6">
          <a routerLink="/login" class="text-sm text-slate-500 hover:text-slate-700 transition-colors">Login</a>
          <a routerLink="/signup" class="text-sm text-slate-500 hover:text-slate-700 transition-colors">Sign Up</a>
        </div>
      </div>
    </footer>
  `,
})
export class LandingComponent {}
