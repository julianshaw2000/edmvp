# Phase 14: Landing Page — Design Spec

**Date:** 2026-03-23
**Status:** Approved
**Prerequisite:** Phase 13 complete (Stripe billing + signup flow)

---

## Overview

Replace the current redirect-to-login with a public marketing landing page that explains the product, shows pricing, and drives visitors to the signup flow.

---

## Route Change

- Current: `{ path: '', redirectTo: 'login' }`
- New: `{ path: '', loadComponent: () => import('./features/landing/landing.component') }`
- The landing page is public (no auth guard)

---

## Page Sections

### 1. Navigation Bar
- auditraks logo (text-based)
- "Login" link → `/login`
- "Start Free Trial" button → `/signup`
- Sticky on scroll

### 2. Hero
- Headline: "Tungsten supply chain compliance, automated."
- Subtitle: "Track custody from mine to refinery. SHA-256 hash chains, RMAP + OECD compliance checks, Material Passport generation — all in one platform."
- CTA button: "Start 60-day free trial" → `/signup`
- Clean, centered layout with generous whitespace

### 3. Feature Cards (3 cards in a row)

**Card 1: Tamper-Evident Tracking**
- Icon: shield/lock
- "Every custody event is SHA-256 hashed and chained. Alter one record and the entire chain breaks — detectable instantly."

**Card 2: Automated Compliance**
- Icon: checkmark/clipboard
- "Five automated checks run on every batch: RMAP smelter verification, OECD origin risk, sanctions screening, mass balance, and sequence integrity."

**Card 3: Material Passports**
- Icon: document/QR
- "Generate PDF Material Passports with QR codes. Share with auditors via secure links. Public verification — no account needed."

### 4. Pricing Section
- Single card: "Pro Plan"
- Price: "$249/month"
- Trial: "60-day free trial included"
- Feature list:
  - Unlimited batches
  - Unlimited users
  - Automated compliance checks
  - Material Passport generation
  - SHA-256 hash chain integrity
  - Admin dashboard + audit log
- CTA: "Start Free Trial" → `/signup`

### 5. Footer
- "© 2026 auditraks. All rights reserved."
- Links: Login, Sign Up

---

## Technical Implementation

- Single Angular standalone component: `packages/web/src/app/features/landing/landing.component.ts`
- No API calls — purely static content
- Tailwind CSS styling consistent with existing app design (indigo accents, slate text, rounded corners)
- ChangeDetectionStrategy.OnPush
- Uses RouterLink for navigation

---

## Success Criteria

1. Visiting `https://accutrac-web.onrender.com/` shows the landing page (not a login redirect)
2. "Start Free Trial" buttons link to `/signup`
3. "Login" links to `/login`
4. Page is responsive (works on mobile)
5. No authentication required to view the page
