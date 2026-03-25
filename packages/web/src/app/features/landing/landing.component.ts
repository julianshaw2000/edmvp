import { Component, ChangeDetectionStrategy, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(24px); }
      to { opacity: 1; transform: translateY(0); }
    }
    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes slideRight {
      from { transform: scaleX(0); }
      to { transform: scaleX(1); }
    }
    @keyframes float {
      0%, 100% { transform: translateY(0); }
      50% { transform: translateY(-8px); }
    }
    @keyframes pulse-glow {
      0%, 100% { opacity: 0.4; }
      50% { opacity: 0.8; }
    }
    @keyframes counter {
      from { opacity: 0; transform: translateY(12px); }
      to { opacity: 1; transform: translateY(0); }
    }
    .animate-fade-up { animation: fadeInUp 0.7s ease-out both; }
    .animate-fade-up-1 { animation: fadeInUp 0.7s ease-out 0.1s both; }
    .animate-fade-up-2 { animation: fadeInUp 0.7s ease-out 0.2s both; }
    .animate-fade-up-3 { animation: fadeInUp 0.7s ease-out 0.3s both; }
    .animate-fade-up-4 { animation: fadeInUp 0.7s ease-out 0.4s both; }
    .animate-fade-up-5 { animation: fadeInUp 0.7s ease-out 0.5s both; }
    .animate-fade-in { animation: fadeIn 1s ease-out both; }
    .animate-slide-right { animation: slideRight 0.8s ease-out 0.6s both; transform-origin: left; }
    .animate-float { animation: float 6s ease-in-out infinite; }
    .animate-float-delay { animation: float 6s ease-in-out 2s infinite; }
    .animate-pulse-glow { animation: pulse-glow 4s ease-in-out infinite; }

    .hero-gradient {
      background: radial-gradient(ellipse 80% 60% at 50% -20%, rgba(79, 70, 229, 0.15) 0%, transparent 70%),
                  radial-gradient(ellipse 60% 50% at 80% 50%, rgba(79, 70, 229, 0.08) 0%, transparent 60%);
    }

    .card-hover {
      transition: all 0.35s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .card-hover:hover {
      transform: translateY(-4px);
      box-shadow: 0 20px 40px -12px rgba(0, 0, 0, 0.08), 0 0 0 1px rgba(79, 70, 229, 0.1);
    }

    .stat-border {
      position: relative;
    }
    .stat-border::after {
      content: '';
      position: absolute;
      right: 0;
      top: 20%;
      height: 60%;
      width: 1px;
      background: linear-gradient(to bottom, transparent, #e2e8f0, transparent);
    }
    .stat-border:last-child::after {
      display: none;
    }

    .mesh-dot {
      background-image: radial-gradient(circle, #e2e8f0 1px, transparent 1px);
      background-size: 24px 24px;
    }
  `],
  template: `
    <!-- Nav -->
    <nav class="fixed top-0 left-0 right-0 z-50 bg-white/90 backdrop-blur-md border-b border-slate-100">
      <div class="max-w-7xl mx-auto px-6 lg:px-8 h-16 flex items-center justify-between">
        <a routerLink="/" class="flex items-center gap-2">
          <div class="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center">
            <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
            </svg>
          </div>
          <span class="text-lg font-bold text-slate-900 tracking-tight">auditraks</span>
        </a>
        <div class="hidden sm:flex items-center gap-8">
          <a href="#features" class="text-sm text-slate-500 hover:text-slate-900 transition-colors">Features</a>
          <a href="#compliance" class="text-sm text-slate-500 hover:text-slate-900 transition-colors">Compliance</a>
          <a href="#pricing" class="text-sm text-slate-500 hover:text-slate-900 transition-colors">Pricing</a>
        </div>
        <div class="flex items-center gap-3">
          <a routerLink="/login" class="text-sm font-medium text-slate-600 hover:text-slate-900 transition-colors px-3 py-2">Sign in</a>
          <a routerLink="/signup" class="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-indigo-700 transition-all shadow-sm hover:shadow-md">Start free trial</a>
        </div>
      </div>
    </nav>

    <!-- Hero -->
    <section class="relative pt-28 pb-20 lg:pt-36 lg:pb-28 px-6 hero-gradient overflow-hidden">
      <!-- Background decorations -->
      <div class="absolute top-20 right-10 w-72 h-72 rounded-full bg-indigo-100/40 blur-3xl animate-pulse-glow"></div>
      <div class="absolute bottom-0 left-10 w-96 h-96 rounded-full bg-blue-50/60 blur-3xl animate-pulse-glow" style="animation-delay: 2s"></div>

      <div class="max-w-7xl mx-auto">
        <div class="max-w-3xl">
          <div class="animate-fade-up">
            <span class="inline-flex items-center gap-2 bg-indigo-50 border border-indigo-100 text-indigo-700 text-xs font-semibold px-3 py-1.5 rounded-full mb-6">
              <span class="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-pulse"></span>
              Trusted by responsible mineral supply chains
            </span>
          </div>
          <h1 class="text-4xl sm:text-5xl lg:text-6xl font-extrabold text-slate-900 leading-[1.1] tracking-tight animate-fade-up-1">
            Responsible sourcing,<br>
            <span class="bg-gradient-to-r from-indigo-600 to-blue-500 bg-clip-text text-transparent">verified and automated.</span>
          </h1>
          <p class="mt-6 text-lg lg:text-xl text-slate-500 leading-relaxed max-w-2xl animate-fade-up-2">
            The compliance platform for tungsten, tin, tantalum, and gold supply chains.
            Automated RMAP and OECD due diligence with tamper-evident custody tracking.
          </p>
          <div class="mt-8 flex flex-wrap items-center gap-4 animate-fade-up-3">
            <a routerLink="/signup" class="inline-flex items-center gap-2 bg-indigo-600 text-white px-7 py-3.5 rounded-xl text-base font-semibold hover:bg-indigo-700 transition-all shadow-lg shadow-indigo-600/20 hover:shadow-indigo-600/30 hover:-translate-y-0.5">
              Start 60-day free trial
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8l4 4m0 0l-4 4m4-4H3"/>
              </svg>
            </a>
            <a href="#features" class="inline-flex items-center gap-2 text-slate-600 px-4 py-3.5 text-sm font-medium hover:text-indigo-600 transition-colors">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"/>
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              See how it works
            </a>
          </div>
          <p class="mt-4 text-sm text-slate-400 animate-fade-up-4">No credit card required &middot; 60-day trial &middot; Cancel anytime</p>
        </div>

        <!-- Stats bar -->
        <div class="mt-16 grid grid-cols-2 lg:grid-cols-4 gap-0 bg-white rounded-2xl border border-slate-200 shadow-sm animate-fade-up-5">
          <div class="stat-border p-6 lg:p-8 text-center">
            <p class="text-3xl lg:text-4xl font-bold text-slate-900">5</p>
            <p class="text-sm text-slate-500 mt-1">Compliance checks</p>
          </div>
          <div class="stat-border p-6 lg:p-8 text-center">
            <p class="text-3xl lg:text-4xl font-bold text-slate-900">SHA-256</p>
            <p class="text-sm text-slate-500 mt-1">Hash chain integrity</p>
          </div>
          <div class="stat-border p-6 lg:p-8 text-center">
            <p class="text-3xl lg:text-4xl font-bold text-slate-900">3TG</p>
            <p class="text-sm text-slate-500 mt-1">Full mineral coverage</p>
          </div>
          <div class="stat-border p-6 lg:p-8 text-center">
            <p class="text-3xl lg:text-4xl font-bold text-slate-900">60 days</p>
            <p class="text-sm text-slate-500 mt-1">Free trial</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Trust bar -->
    <section class="py-10 px-6 border-b border-slate-100">
      <div class="max-w-7xl mx-auto text-center">
        <p class="text-xs font-semibold text-slate-400 uppercase tracking-widest mb-6">Compliance frameworks supported</p>
        <div class="flex flex-wrap justify-center items-center gap-8 lg:gap-14">
          <span class="text-slate-400 font-semibold text-sm tracking-wide">RMAP</span>
          <span class="text-slate-300">|</span>
          <span class="text-slate-400 font-semibold text-sm tracking-wide">OECD DDG</span>
          <span class="text-slate-300">|</span>
          <span class="text-slate-400 font-semibold text-sm tracking-wide">Dodd-Frank §1502</span>
          <span class="text-slate-300">|</span>
          <span class="text-slate-400 font-semibold text-sm tracking-wide">EU 2017/821</span>
        </div>
      </div>
    </section>

    <!-- Features -->
    <section id="features" class="py-20 lg:py-28 px-6">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-16">
          <span class="text-xs font-semibold text-indigo-600 uppercase tracking-widest">Platform</span>
          <h2 class="mt-3 text-3xl lg:text-4xl font-bold text-slate-900 tracking-tight">Everything you need for mineral compliance</h2>
          <p class="mt-4 text-lg text-slate-500 max-w-2xl mx-auto">Replace manual spreadsheets and email chains with automated, tamper-evident digital tracking from mine to refinery.</p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <!-- Feature 1 -->
          <div class="card-hover bg-white rounded-2xl p-8 border border-slate-200 relative overflow-hidden group">
            <div class="absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl from-indigo-50 to-transparent rounded-bl-full opacity-0 group-hover:opacity-100 transition-opacity"></div>
            <div class="relative">
              <div class="w-12 h-12 rounded-xl bg-gradient-to-br from-indigo-500 to-indigo-600 flex items-center justify-center mb-6 shadow-lg shadow-indigo-500/20">
                <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
                </svg>
              </div>
              <h3 class="text-lg font-bold text-slate-900 mb-3">Tamper-Evident Tracking</h3>
              <p class="text-slate-500 text-sm leading-relaxed">
                Every custody event is SHA-256 hashed and cryptographically chained. Alter one record and the entire chain breaks — detectable instantly.
              </p>
            </div>
          </div>

          <!-- Feature 2 -->
          <div class="card-hover bg-white rounded-2xl p-8 border border-slate-200 relative overflow-hidden group">
            <div class="absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl from-emerald-50 to-transparent rounded-bl-full opacity-0 group-hover:opacity-100 transition-opacity"></div>
            <div class="relative">
              <div class="w-12 h-12 rounded-xl bg-gradient-to-br from-emerald-500 to-emerald-600 flex items-center justify-center mb-6 shadow-lg shadow-emerald-500/20">
                <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                </svg>
              </div>
              <h3 class="text-lg font-bold text-slate-900 mb-3">Automated Compliance</h3>
              <p class="text-slate-500 text-sm leading-relaxed">
                Five automated checks on every batch: RMAP smelter verification, OECD origin risk, sanctions screening, mass balance, and sequence integrity.
              </p>
            </div>
          </div>

          <!-- Feature 3 -->
          <div class="card-hover bg-white rounded-2xl p-8 border border-slate-200 relative overflow-hidden group">
            <div class="absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl from-amber-50 to-transparent rounded-bl-full opacity-0 group-hover:opacity-100 transition-opacity"></div>
            <div class="relative">
              <div class="w-12 h-12 rounded-xl bg-gradient-to-br from-amber-500 to-amber-600 flex items-center justify-center mb-6 shadow-lg shadow-amber-500/20">
                <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                </svg>
              </div>
              <h3 class="text-lg font-bold text-slate-900 mb-3">Material Passports</h3>
              <p class="text-slate-500 text-sm leading-relaxed">
                Generate PDF Material Passports with QR codes. Share with auditors via secure links. Public verification — no account needed.
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Compliance Deep Dive -->
    <section id="compliance" class="py-20 lg:py-28 px-6 bg-slate-50 relative overflow-hidden">
      <div class="absolute inset-0 mesh-dot opacity-30"></div>
      <div class="max-w-7xl mx-auto relative">
        <div class="text-center mb-16">
          <span class="text-xs font-semibold text-indigo-600 uppercase tracking-widest">Compliance Engine</span>
          <h2 class="mt-3 text-3xl lg:text-4xl font-bold text-slate-900 tracking-tight">Five automated checks on every batch</h2>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
          <div class="bg-white rounded-xl p-6 border border-slate-200 text-center card-hover">
            <div class="w-10 h-10 rounded-lg bg-indigo-100 flex items-center justify-center mx-auto mb-4">
              <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
              </svg>
            </div>
            <h4 class="font-bold text-slate-900 text-sm mb-1">RMAP</h4>
            <p class="text-xs text-slate-500">Smelter verification</p>
          </div>
          <div class="bg-white rounded-xl p-6 border border-slate-200 text-center card-hover">
            <div class="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center mx-auto mb-4">
              <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
            </div>
            <h4 class="font-bold text-slate-900 text-sm mb-1">OECD DDG</h4>
            <p class="text-xs text-slate-500">Origin country risk</p>
          </div>
          <div class="bg-white rounded-xl p-6 border border-slate-200 text-center card-hover">
            <div class="w-10 h-10 rounded-lg bg-rose-100 flex items-center justify-center mx-auto mb-4">
              <svg class="w-5 h-5 text-rose-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"/>
              </svg>
            </div>
            <h4 class="font-bold text-slate-900 text-sm mb-1">Sanctions</h4>
            <p class="text-xs text-slate-500">Entity screening</p>
          </div>
          <div class="bg-white rounded-xl p-6 border border-slate-200 text-center card-hover">
            <div class="w-10 h-10 rounded-lg bg-emerald-100 flex items-center justify-center mx-auto mb-4">
              <svg class="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3"/>
              </svg>
            </div>
            <h4 class="font-bold text-slate-900 text-sm mb-1">Mass Balance</h4>
            <p class="text-xs text-slate-500">Weight verification</p>
          </div>
          <div class="bg-white rounded-xl p-6 border border-slate-200 text-center card-hover">
            <div class="w-10 h-10 rounded-lg bg-violet-100 flex items-center justify-center mx-auto mb-4">
              <svg class="w-5 h-5 text-violet-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
              </svg>
            </div>
            <h4 class="font-bold text-slate-900 text-sm mb-1">Sequence</h4>
            <p class="text-xs text-slate-500">Event chain integrity</p>
          </div>
        </div>
      </div>
    </section>

    <!-- How it works -->
    <section class="py-20 lg:py-28 px-6">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-16">
          <span class="text-xs font-semibold text-indigo-600 uppercase tracking-widest">Workflow</span>
          <h2 class="mt-3 text-3xl lg:text-4xl font-bold text-slate-900 tracking-tight">Mine to refinery in three steps</h2>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div class="text-center">
            <div class="w-14 h-14 rounded-2xl bg-indigo-600 text-white flex items-center justify-center mx-auto mb-5 text-xl font-bold shadow-lg shadow-indigo-600/20">1</div>
            <h3 class="font-bold text-slate-900 text-lg mb-2">Track</h3>
            <p class="text-slate-500 text-sm leading-relaxed">Log custody events at every stage — extraction, assay, concentration, trading, smelting, export. Each event is SHA-256 hashed and chained.</p>
          </div>
          <div class="text-center">
            <div class="w-14 h-14 rounded-2xl bg-indigo-600 text-white flex items-center justify-center mx-auto mb-5 text-xl font-bold shadow-lg shadow-indigo-600/20">2</div>
            <h3 class="font-bold text-slate-900 text-lg mb-2">Verify</h3>
            <p class="text-slate-500 text-sm leading-relaxed">Five automated compliance checks run on every batch. RMAP smelter verification, OECD origin risk, sanctions, mass balance, and sequence integrity.</p>
          </div>
          <div class="text-center">
            <div class="w-14 h-14 rounded-2xl bg-indigo-600 text-white flex items-center justify-center mx-auto mb-5 text-xl font-bold shadow-lg shadow-indigo-600/20">3</div>
            <h3 class="font-bold text-slate-900 text-lg mb-2">Certify</h3>
            <p class="text-slate-500 text-sm leading-relaxed">Generate Material Passports with QR codes. Share with buyers and auditors via secure links. Public verification — no account needed.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Pricing -->
    <section id="pricing" class="py-20 lg:py-28 px-6 bg-slate-50">
      <div class="max-w-5xl mx-auto">
        <div class="text-center mb-16">
          <span class="text-xs font-semibold text-indigo-600 uppercase tracking-widest">Pricing</span>
          <h2 class="mt-3 text-3xl lg:text-4xl font-bold text-slate-900 tracking-tight">Start free, scale as you grow</h2>
          <p class="mt-4 text-lg text-slate-500">Both plans include a 60-day free trial. No credit card required to start.</p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 items-start">
          <!-- Starter -->
          <div class="card-hover bg-white rounded-2xl p-8 lg:p-10 border border-slate-200">
            <p class="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-4">Starter</p>
            <div class="flex items-baseline gap-1 mb-1">
              <span class="text-5xl font-extrabold text-slate-900">$99</span>
              <span class="text-slate-400 text-lg">/month</span>
            </div>
            <p class="text-slate-400 text-sm mb-8">after 60-day free trial</p>

            <ul class="space-y-4 mb-10">
              <li class="flex items-center gap-3 text-sm text-slate-600">
                <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                Up to 50 batches
              </li>
              <li class="flex items-center gap-3 text-sm text-slate-600">
                <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                Up to 5 users
              </li>
              <li class="flex items-center gap-3 text-sm text-slate-600">
                <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                All compliance checks
              </li>
              <li class="flex items-center gap-3 text-sm text-slate-600">
                <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                Material Passport generation
              </li>
              <li class="flex items-center gap-3 text-sm text-slate-600">
                <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                SHA-256 hash chains
              </li>
            </ul>

            <a routerLink="/signup" [queryParams]="{ plan: 'starter' }" class="block w-full bg-slate-900 text-white py-3.5 rounded-xl text-sm font-semibold hover:bg-slate-800 transition-all shadow-sm text-center">
              Start free trial
            </a>
          </div>

          <!-- Pro -->
          <div class="card-hover bg-gradient-to-br from-indigo-600 to-indigo-700 rounded-2xl p-8 lg:p-10 border border-indigo-500 relative shadow-xl shadow-indigo-600/10">
            <div class="absolute -top-3 left-1/2 -translate-x-1/2">
              <span class="bg-amber-400 text-amber-900 text-xs font-bold px-3 py-1 rounded-full uppercase tracking-wider shadow-sm">Most popular</span>
            </div>
            <p class="text-sm font-semibold text-indigo-200 uppercase tracking-wider mb-4">Pro</p>
            <div class="flex items-baseline gap-1 mb-1">
              <span class="text-5xl font-extrabold text-white">$249</span>
              <span class="text-indigo-300 text-lg">/month</span>
            </div>
            <p class="text-indigo-300 text-sm mb-8">after 60-day free trial</p>

            <ul class="space-y-4 mb-10">
              <li class="flex items-center gap-3 text-sm text-indigo-100">
                <svg class="w-5 h-5 text-indigo-300 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                <strong>Unlimited</strong>&nbsp;batches
              </li>
              <li class="flex items-center gap-3 text-sm text-indigo-100">
                <svg class="w-5 h-5 text-indigo-300 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                <strong>Unlimited</strong>&nbsp;users
              </li>
              <li class="flex items-center gap-3 text-sm text-indigo-100">
                <svg class="w-5 h-5 text-indigo-300 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                Everything in Starter
              </li>
              <li class="flex items-center gap-3 text-sm text-indigo-100">
                <svg class="w-5 h-5 text-indigo-300 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                API access + webhooks
              </li>
              <li class="flex items-center gap-3 text-sm text-indigo-100">
                <svg class="w-5 h-5 text-indigo-300 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                Priority support
              </li>
            </ul>

            <a routerLink="/signup" [queryParams]="{ plan: 'pro' }" class="block w-full bg-white text-indigo-600 py-3.5 rounded-xl text-sm font-semibold hover:bg-indigo-50 transition-all shadow-sm text-center">
              Start free trial
            </a>
          </div>
        </div>
      </div>
    </section>

    <!-- CTA -->
    <section class="py-20 lg:py-28 px-6">
      <div class="max-w-4xl mx-auto text-center">
        <h2 class="text-3xl lg:text-4xl font-bold text-slate-900 tracking-tight">Ready to automate your compliance?</h2>
        <p class="mt-4 text-lg text-slate-500 max-w-xl mx-auto">Join responsible mineral supply chains using auditraks. Start your 60-day free trial today.</p>
        <div class="mt-8">
          <a routerLink="/signup" class="inline-flex items-center gap-2 bg-indigo-600 text-white px-8 py-4 rounded-xl text-base font-semibold hover:bg-indigo-700 transition-all shadow-lg shadow-indigo-600/20 hover:shadow-indigo-600/30 hover:-translate-y-0.5">
            Get started for free
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8l4 4m0 0l-4 4m4-4H3"/>
            </svg>
          </a>
        </div>
      </div>
    </section>

    <!-- Footer -->
    <footer class="py-12 px-6 border-t border-slate-200 bg-white">
      <div class="max-w-7xl mx-auto">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-8">
          <div>
            <div class="flex items-center gap-2 mb-4">
              <div class="w-7 h-7 rounded-lg bg-indigo-600 flex items-center justify-center">
                <svg class="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                </svg>
              </div>
              <span class="font-bold text-slate-900">auditraks</span>
            </div>
            <p class="text-sm text-slate-500 leading-relaxed">3TG supply chain compliance, automated.</p>
          </div>
          <div>
            <h4 class="font-semibold text-slate-900 text-sm mb-4">Product</h4>
            <ul class="space-y-2.5">
              <li><a href="#features" class="text-sm text-slate-500 hover:text-indigo-600 transition-colors">Features</a></li>
              <li><a href="#compliance" class="text-sm text-slate-500 hover:text-indigo-600 transition-colors">Compliance</a></li>
              <li><a href="#pricing" class="text-sm text-slate-500 hover:text-indigo-600 transition-colors">Pricing</a></li>
            </ul>
          </div>
          <div>
            <h4 class="font-semibold text-slate-900 text-sm mb-4">Frameworks</h4>
            <ul class="space-y-2.5">
              <li><span class="text-sm text-slate-500">RMAP</span></li>
              <li><span class="text-sm text-slate-500">OECD DDG</span></li>
              <li><span class="text-sm text-slate-500">Dodd-Frank §1502</span></li>
              <li><span class="text-sm text-slate-500">EU 2017/821</span></li>
            </ul>
          </div>
          <div>
            <h4 class="font-semibold text-slate-900 text-sm mb-4">Account</h4>
            <ul class="space-y-2.5">
              <li><a routerLink="/login" class="text-sm text-slate-500 hover:text-indigo-600 transition-colors">Sign in</a></li>
              <li><a routerLink="/signup" class="text-sm text-slate-500 hover:text-indigo-600 transition-colors">Start free trial</a></li>
            </ul>
          </div>
        </div>
        <div class="mt-12 pt-8 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p class="text-xs text-slate-400">&copy; 2026 auditraks. All rights reserved.</p>
          <p class="text-xs text-slate-400">Tungsten &middot; Tin &middot; Tantalum &middot; Gold</p>
        </div>
      </div>
    </footer>
  `,
})
export class LandingComponent {}
