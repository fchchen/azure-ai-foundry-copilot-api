#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 3 ]]; then
  echo "Usage: $0 <tenant-id> <client-id> <api-base-url>"
  echo "Example: $0 11111111-2222-3333-4444-555555555555 aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee https://contoso-api.azurewebsites.net"
  exit 1
fi

TENANT_ID="$1"
CLIENT_ID="$2"
API_BASE_URL="${3%/}"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
AI_PLUGIN_FILE="$ROOT_DIR/appPackage/ai-plugin.json"
OPENAPI_FILE="$ROOT_DIR/appPackage/openapi.json"

if [[ ! -f "$AI_PLUGIN_FILE" || ! -f "$OPENAPI_FILE" ]]; then
  echo "Expected appPackage/ai-plugin.json and appPackage/openapi.json to exist."
  exit 1
fi

AUTH_URL="https://login.microsoftonline.com/${TENANT_ID}/oauth2/v2.0/authorize"
TOKEN_URL="https://login.microsoftonline.com/${TENANT_ID}/oauth2/v2.0/token"
SCOPE="api://${CLIENT_ID}/access_as_user"

perl -0pi -e "s#\"authorization_url\":\\s*\"[^\"]*\"#\"authorization_url\": \"${AUTH_URL}\"#g" "$AI_PLUGIN_FILE"
perl -0pi -e "s#\"token_url\":\\s*\"[^\"]*\"#\"token_url\": \"${TOKEN_URL}\"#g" "$AI_PLUGIN_FILE"
perl -0pi -e "s#\"scope\":\\s*\"[^\"]*\"#\"scope\": \"${SCOPE}\"#g" "$AI_PLUGIN_FILE"

perl -0pi -e "s#\"authorizationUrl\":\\s*\"[^\"]*\"#\"authorizationUrl\": \"${AUTH_URL}\"#g" "$OPENAPI_FILE"
perl -0pi -e "s#\"tokenUrl\":\\s*\"[^\"]*\"#\"tokenUrl\": \"${TOKEN_URL}\"#g" "$OPENAPI_FILE"
perl -0pi -e "s#api://[^\"]+/access_as_user#${SCOPE}#g" "$OPENAPI_FILE"
perl -0pi -e "s#\"url\":\\s*\"https://[^\"]+\"#\"url\": \"${API_BASE_URL}\"#g" "$OPENAPI_FILE"

echo "Updated appPackage OAuth values:"
echo "- tenant: ${TENANT_ID}"
echo "- client: ${CLIENT_ID}"
echo "- apiBaseUrl: ${API_BASE_URL}"
