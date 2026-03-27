#!/bin/bash
# Register demo users on auditraks
# Run after deploy so pending| users exist in the database
# Password for all demo accounts: Demo1234!

API="https://accutrac-api.onrender.com"
PASSWORD="Demo1234!"

echo "=== Registering demo users ==="
echo ""

# Supplier
echo "1. supplier@auditraks.com (SUPPLIER)"
curl -sk -X POST "$API/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"supplier@auditraks.com\",\"password\":\"$PASSWORD\",\"displayName\":\"Jean-Pierre Habimana (Nyungwe Mining Co.)\"}"
echo ""

# Buyer
echo "2. buyer@auditraks.com (BUYER)"
curl -sk -X POST "$API/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"buyer@auditraks.com\",\"password\":\"$PASSWORD\",\"displayName\":\"Klaus Steinberger (Wolfram Bergbau)\"}"
echo ""

# Tenant Admin
echo "3. admin@auditraks.com (TENANT_ADMIN)"
curl -sk -X POST "$API/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@auditraks.com\",\"password\":\"$PASSWORD\",\"displayName\":\"Marie Uwimana (Compliance Director)\"}"
echo ""

echo ""
echo "=== Confirming emails (bypassing email verification) ==="
echo ""

# Now we need to confirm the emails. Since we can't click email links,
# we'll need to do this through the API or database.
# For now, the users will get confirmation emails via Resend.
echo "Check inbox for confirmation emails, or run this SQL to auto-confirm:"
echo ""
echo "  UPDATE identity.\"AspNetUsers\""
echo "  SET \"EmailConfirmed\" = true"
echo "  WHERE \"Email\" IN ('supplier@auditraks.com', 'buyer@auditraks.com', 'admin@auditraks.com');"
echo ""
echo "=== Demo accounts ==="
echo "  supplier@auditraks.com / $PASSWORD  — Supplier portal"
echo "  buyer@auditraks.com    / $PASSWORD  — Buyer portal"
echo "  admin@auditraks.com    / $PASSWORD  — Admin dashboard"
echo "  julianshaw2000@gmail.com / (your password) — Platform Admin"
echo ""
echo "=== Demo batches (pre-seeded) ==="
echo "  W-2026-041 — COMPLIANT, full Rwanda journey (6 events)"
echo "  W-2026-038 — FLAGGED, DRC high-risk origin (4 events)"
echo "  W-2026-045 — PENDING, no events yet (Bolivia)"
echo "  W-2026-035 — COMPLIANT, Cassiterite/Rwanda (5 events)"
