#!/usr/bin/env bash
# setup-azure.sh — One-time Azure infrastructure setup for CI/CD deployment.
# Run this locally once before triggering the GitHub Actions deploy job.
#
# Prerequisites:
#   az login
#   az extension add --name containerapp
#
# Usage:
#   chmod +x scripts/setup-azure.sh
#   ./scripts/setup-azure.sh
set -euo pipefail

# ── Variables ─────────────────────────────────────────────────────────────────
# Only AOAI_RESOURCE_NAME and KEY_VAULT_NAME must be globally unique.

GITHUB_ORG="fchchen"
GITHUB_REPO="azure-ai-foundry-copilot-api"

LOCATION="eastus2"
RESOURCE_GROUP="rg-copilot-api"
CONTAINER_APP_ENV="cae-copilot-api"
CONTAINER_APP_NAME="copilot-api"

AOAI_RESOURCE_NAME="aoai-copilot-api"           # must be globally unique
AOAI_DEPLOYMENT="gpt-4.1-mini"                  # replaces gpt-4o-mini (retires Mar 31 2026)
AOAI_MODEL_VERSION="2025-04-14"

KEY_VAULT_NAME="kv-copilot-api"                  # must be globally unique, 3-24 chars

ENTRA_TENANT_ID="0cb89c24-085c-4720-9a32-5cd389a23ee2"

# ── Derived values (do not edit) ──────────────────────────────────────────────
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
IMAGE="ghcr.io/${GITHUB_ORG}/${GITHUB_REPO}:latest"

echo ">>> Subscription: ${SUBSCRIPTION_ID}"
echo ">>> Location:     ${LOCATION}"
echo ">>> Model:        ${AOAI_DEPLOYMENT} ${AOAI_MODEL_VERSION}"
echo ""

# ── 1. Resource Group ─────────────────────────────────────────────────────────
echo ">>> [1/9] Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

# ── 2. Azure OpenAI Resource + Model Deployment ───────────────────────────────
echo ">>> [2/9] Creating Azure OpenAI resource..."
az cognitiveservices account create \
  --name "$AOAI_RESOURCE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --kind "OpenAI" \
  --sku "S0" \
  --output none

echo ">>> [2/9] Deploying model: ${AOAI_DEPLOYMENT} ${AOAI_MODEL_VERSION}..."
az cognitiveservices account deployment create \
  --name "$AOAI_RESOURCE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --deployment-name "$AOAI_DEPLOYMENT" \
  --model-name "$AOAI_DEPLOYMENT" \
  --model-version "$AOAI_MODEL_VERSION" \
  --model-format "OpenAI" \
  --sku-capacity 10 \
  --sku-name "Standard" \
  --output none

AOAI_ENDPOINT=$(az cognitiveservices account show \
  --name "$AOAI_RESOURCE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.endpoint -o tsv)

AOAI_API_KEY=$(az cognitiveservices account keys list \
  --name "$AOAI_RESOURCE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query key1 -o tsv)

echo "    Endpoint: ${AOAI_ENDPOINT}"

# ── 3. Key Vault + store API key ──────────────────────────────────────────────
# Standard tier — effectively free at low operation volume.
echo ">>> [3/9] Creating Key Vault..."
az keyvault create \
  --name "$KEY_VAULT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku standard \
  --enable-rbac-authorization true \
  --output none

echo ">>> [3/9] Storing Azure OpenAI API key in Key Vault..."
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "AzureAiFoundry--ApiKey" \
  --value "$AOAI_API_KEY" \
  --output none

KV_URI="https://${KEY_VAULT_NAME}.vault.azure.net"
KV_ID=$(az keyvault show \
  --name "$KEY_VAULT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query id -o tsv)

# ── 4. Entra ID App Registration for the API ──────────────────────────────────
echo ">>> [4/9] Creating Entra ID app registration for the API..."
API_APP_ID=$(az ad app create \
  --display-name "app-copilot-api" \
  --sign-in-audience "AzureADMyOrg" \
  --query appId -o tsv)

# Set App ID URI (used as Audience in JWT validation)
az ad app update \
  --id "$API_APP_ID" \
  --identifier-uris "api://${API_APP_ID}" \
  --output none

# Expose access_as_user scope
SCOPE_ID=$(uuidgen)
az ad app update \
  --id "$API_APP_ID" \
  --set "api.oauth2PermissionScopes=[{
    \"adminConsentDescription\": \"Access the Copilot API on behalf of the signed-in user\",
    \"adminConsentDisplayName\": \"Access copilot-api\",
    \"id\": \"${SCOPE_ID}\",
    \"isEnabled\": true,
    \"type\": \"User\",
    \"userConsentDescription\": \"Access the Copilot API on your behalf\",
    \"userConsentDisplayName\": \"Access copilot-api\",
    \"value\": \"access_as_user\"
  }]" \
  --output none

echo "    API Client ID: ${API_APP_ID}"

# ── 5. Container Apps Environment (consumption — free at zero traffic) ─────────
echo ">>> [5/9] Creating Container Apps environment..."
az containerapp env create \
  --name "$CONTAINER_APP_ENV" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

# ── 6. Container App (min-replicas=0 → scales to zero, no idle cost) ──────────
echo ">>> [6/9] Creating Container App..."
az containerapp create \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --environment "$CONTAINER_APP_ENV" \
  --image "$IMAGE" \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 \
  --max-replicas 3 \
  --system-assigned \
  --env-vars \
    "KeyVault__Enabled=true" \
    "KeyVault__VaultUri=${KV_URI}" \
    "EntraId__Enabled=true" \
    "EntraId__TenantId=${ENTRA_TENANT_ID}" \
    "EntraId__ClientId=${API_APP_ID}" \
    "EntraId__Audience=api://${API_APP_ID}" \
    "AzureAiFoundry__Endpoint=${AOAI_ENDPOINT}" \
    "AzureAiFoundry__Deployment=${AOAI_DEPLOYMENT}" \
    "AzureAiFoundry__UseMockResponses=false" \
  --output none

# ── 7. Grant Container App Managed Identity access to Key Vault ───────────────
echo ">>> [7/9] Assigning Key Vault Secrets User role to Container App identity..."
PRINCIPAL_ID=$(az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query identity.principalId -o tsv)

az role assignment create \
  --assignee "$PRINCIPAL_ID" \
  --role "Key Vault Secrets User" \
  --scope "$KV_ID" \
  --output none

# ── 8. App Registration for GitHub Actions OIDC ───────────────────────────────
echo ">>> [8/9] Creating App Registration for GitHub Actions OIDC..."
GH_APP_ID=$(az ad app create \
  --display-name "sp-github-copilot-api-deploy" \
  --query appId -o tsv)

GH_SP_ID=$(az ad sp create \
  --id "$GH_APP_ID" \
  --query id -o tsv)

# Federated credential — allows GitHub Actions to authenticate without a secret.
az ad app federated-credential create \
  --id "$GH_APP_ID" \
  --parameters "{
    \"name\": \"github-deploy\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none

# Grant Contributor on the resource group (scoped — not subscription-wide).
az role assignment create \
  --assignee "$GH_SP_ID" \
  --role "Contributor" \
  --scope "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/${RESOURCE_GROUP}" \
  --output none

# ── 9. Output GitHub secrets/variables ────────────────────────────────────────
APP_URL=$(az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.configuration.ingress.fqdn -o tsv)

echo ""
echo "════════════════════════════════════════════════════"
echo " GitHub repo → Settings → Secrets and variables"
echo "════════════════════════════════════════════════════"
echo ""
echo " Secrets:"
echo "   AZURE_CLIENT_ID       = ${GH_APP_ID}"
echo "   AZURE_TENANT_ID       = ${ENTRA_TENANT_ID}"
echo "   AZURE_SUBSCRIPTION_ID = ${SUBSCRIPTION_ID}"
echo ""
echo " Variables:"
echo "   AZURE_RESOURCE_GROUP     = ${RESOURCE_GROUP}"
echo "   AZURE_CONTAINER_APP_NAME = ${CONTAINER_APP_NAME}"
echo ""
echo "════════════════════════════════════════════════════"
echo " Container App URL"
echo "════════════════════════════════════════════════════"
echo "  https://${APP_URL}"
echo ""
echo "════════════════════════════════════════════════════"
echo " Azure OpenAI"
echo "════════════════════════════════════════════════════"
echo "  Endpoint:   ${AOAI_ENDPOINT}"
echo "  Deployment: ${AOAI_DEPLOYMENT} (${AOAI_MODEL_VERSION})"
echo "  API key stored in Key Vault — not printed here."
echo ""
echo "════════════════════════════════════════════════════"
echo " API Entra ID App Registration"
echo "════════════════════════════════════════════════════"
echo "  Client ID: ${API_APP_ID}"
echo "  Audience:  api://${API_APP_ID}"
echo ""
echo "Done. Run the GitHub Actions deploy workflow to push your first image."
