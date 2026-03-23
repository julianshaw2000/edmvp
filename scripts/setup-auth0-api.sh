#!/bin/bash
set -e

DOMAIN="dev-htzakhlu.us.auth0.com"
TOKEN=$(cat docs/auth0-token.tmp)

echo "=== Step 1: Create SPA Application ==="
SPA=$(curl -s -X POST "https://${DOMAIN}/api/v2/clients" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"name":"auditraks Web","app_type":"spa","callbacks":["http://localhost:4200","https://auditraks.com"],"allowed_logout_urls":["http://localhost:4200","https://auditraks.com"],"web_origins":["http://localhost:4200","https://auditraks.com"],"grant_types":["authorization_code","implicit"],"token_endpoint_auth_method":"none","oidc_conformant":true}')

CLIENT_ID=$(echo "$SPA" | sed 's/.*"client_id":"\([^"]*\)".*/\1/' | head -c 40)
echo "SPA Client ID: $CLIENT_ID"

echo ""
echo "=== Step 2: Create API ==="
API=$(curl -s -X POST "https://${DOMAIN}/api/v2/resource-servers" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"name":"auditraks API","identifier":"https://api.auditraks.com","signing_alg":"RS256","token_lifetime":28800}')

echo "API: $(echo "$API" | sed 's/.*"identifier":"\([^"]*\)".*/\1/' | head -c 50)"

echo ""
echo "=== Step 3: Enable Google connection for SPA ==="
CONNS=$(curl -s "https://${DOMAIN}/api/v2/connections?strategy=google-oauth2" \
  -H "Authorization: Bearer ${TOKEN}")
CONN_ID=$(echo "$CONNS" | sed 's/.*"id":"\([^"]*\)".*/\1/' | head -c 30)

if [ -n "$CONN_ID" ] && [ "$CONN_ID" != "$CONNS" ]; then
  curl -s -X PATCH "https://${DOMAIN}/api/v2/connections/${CONN_ID}" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{\"enabled_clients_add\":[\"${CLIENT_ID}\"]}" > /dev/null
  echo "Google connection enabled for auditraks Web"
else
  echo "No Google connection found"
fi

echo ""
echo "=== RESULTS ==="
echo "Domain: ${DOMAIN}"
echo "Client ID: ${CLIENT_ID}"
echo "Audience: https://api.auditraks.com"

# Save to secrets
cat > docs/auth0.secrets << EOFAUTH
Domain: ${DOMAIN}
Client ID: ${CLIENT_ID}
Audience: https://api.auditraks.com
EOFAUTH

echo "Saved to docs/auth0.secrets"
