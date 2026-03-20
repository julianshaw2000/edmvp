#!/bin/bash
# Auth0 Setup Script for Tungsten Pilot MVP
#
# Prerequisites:
#   1. Create a free Auth0 account at https://auth0.com
#   2. Install Auth0 CLI: https://github.com/auth0/auth0-cli
#      - Windows: scoop install auth0-cli  OR  winget install Auth0.CLI
#      - Mac: brew install auth0/auth0-cli/auth0
#   3. Run: auth0 login
#
# This script creates:
#   - An Auth0 SPA application for the Angular frontend
#   - An Auth0 API for the .NET backend
#
# Usage: bash scripts/setup-auth0.sh

set -e

echo "=== Tungsten Pilot MVP — Auth0 Setup ==="
echo ""

# Check auth0 CLI
if ! command -v auth0 &> /dev/null; then
    echo "ERROR: auth0 CLI not found."
    echo "Install it:"
    echo "  Windows: scoop install auth0-cli"
    echo "  Mac: brew install auth0/auth0-cli/auth0"
    echo "Then run: auth0 login"
    exit 1
fi

# Get the tenant domain
DOMAIN=$(auth0 tenants list --json 2>/dev/null | jq -r '.[0].name' 2>/dev/null || echo "")
if [ -z "$DOMAIN" ]; then
    echo "ERROR: No Auth0 tenant found. Run 'auth0 login' first."
    exit 1
fi
echo "Using Auth0 tenant: $DOMAIN"

# Create the SPA application
echo ""
echo "Creating SPA application..."
SPA_OUTPUT=$(auth0 apps create \
    --name "Tungsten Web" \
    --type spa \
    --callbacks "http://localhost:4200,https://accutrac.org" \
    --logout-urls "http://localhost:4200,https://accutrac.org" \
    --origins "http://localhost:4200,https://accutrac.org" \
    --json 2>/dev/null)

CLIENT_ID=$(echo "$SPA_OUTPUT" | jq -r '.client_id')
echo "SPA Client ID: $CLIENT_ID"

# Create the API
echo ""
echo "Creating API..."
auth0 apis create \
    --name "Tungsten API" \
    --identifier "https://api.accutrac.org" \
    --json 2>/dev/null > /dev/null

API_AUDIENCE="https://api.accutrac.org"
echo "API Audience: $API_AUDIENCE"

# Output configuration
echo ""
echo "=== Configuration ==="
echo ""
echo "Add to packages/web/src/environments/environment.ts:"
echo "  auth0: {"
echo "    domain: '$DOMAIN',"
echo "    clientId: '$CLIENT_ID',"
echo "    audience: '$API_AUDIENCE',"
echo "  }"
echo ""
echo "Add to packages/api/src/Tungsten.Api/appsettings.Development.json:"
echo "  \"Auth0\": {"
echo "    \"Domain\": \"$DOMAIN\","
echo "    \"Audience\": \"$API_AUDIENCE\""
echo "  }"
echo ""

# Save to secrets file
cat > docs/auth0.secrets << EOF
Auth0 Configuration (generated $(date))

Domain: $DOMAIN
SPA Client ID: $CLIENT_ID
API Audience: $API_AUDIENCE

Angular environment.ts:
  auth0: {
    domain: '$DOMAIN',
    clientId: '$CLIENT_ID',
    audience: '$API_AUDIENCE',
  }

.NET appsettings:
  "Auth0": {
    "Domain": "$DOMAIN",
    "Audience": "$API_AUDIENCE"
  }
EOF

echo "Saved to docs/auth0.secrets"
echo ""
echo "=== Done! ==="
echo "Next steps:"
echo "  1. Update packages/web/src/environments/environment.ts with the values above"
echo "  2. Update packages/api/src/Tungsten.Api/appsettings.Development.json"
echo "  3. Create test users in Auth0 Dashboard > User Management > Users"
echo "     - Create a SUPPLIER user, a BUYER user, and a PLATFORM_ADMIN user"
echo "     - Then add them to the platform database via the seed data"
