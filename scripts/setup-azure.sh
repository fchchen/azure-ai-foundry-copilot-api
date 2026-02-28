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
# Fill these in before running.

GITHUB_ORG="YOUR-GITHUB-USERNAME-OR-ORG"        # e.g. fchch
GITHUB_REPO="azure-ai-foundry-copilot-api"

LOCATION="eastus"                                # Azure region
RESOURCE_GROUP="rg-copilot-api"
CONTAINER_APP_ENV="cae-copilot-api"
CONTAINER_APP_NAME="copilot-api"
KEY_VAULT_NAME="kv-copilot-api"                  # globally unique, 3-24 chars

AI_FOUNDRY_ENDPOINT="https://YOUR-RESOURCE.openai.azure.com"
AI_FOUNDRY_DEPLOYMENT="gpt-4o-mini"
AI_FOUNDRY_API_KEY="YOUR-AI-FOUNDRY-API-KEY"    # stored in Key Vault, not in code

ENTRA_TENANT_ID="YOUR-TENANT-ID"
ENTRA_CLIENT_ID="YOUR-API-APP-CLIENT-ID"        # app registration for the API itself

# ── Derived values (do not edit) ──────────────────────────────────────────────
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
IMAGE="ghcr.io/${GITHUB_ORG}/${GITHUB_REPO}:latest"

echo ">>> Subscription: ${SUBSCRIPTION_ID}"

# ── 1. Resource Group ─────────────────────────────────────────────────────────
echo ">>> Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

# ── 2. Key Vault ──────────────────────────────────────────────────────────────
# Standard tier — effectively free at low operation volume.
echo ">>> Creating Key Vault..."
az keyvault create \
  --name "$KEY_VAULT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku standard \
  --enable-rbac-authorization true \
  --output none

# Store the AI Foundry API key (matches ApiKeySecretName in appsettings).
echo ">>> Storing AI Foundry API key in Key Vault..."
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "AzureAiFoundry--ApiKey" \
  --value "$AI_FOUNDRY_API_KEY" \
  --output none

# ── 3. Container Apps Environment (consumption — free at zero traffic) ─────────
echo ">>> Creating Container Apps environment..."
az containerapp env create \
  --name "$CONTAINER_APP_ENV" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

# ── 4. Container App (min-replicas=0 → scales to zero, no idle cost) ──────────
echo ">>> Creating Container App..."
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
    "KeyVault__VaultUri=https://${KEY_VAULT_NAME}.vault.azure.net" \
    "EntraId__Enabled=true" \
    "EntraId__TenantId=${ENTRA_TENANT_ID}" \
    "EntraId__ClientId=${ENTRA_CLIENT_ID}" \
    "EntraId__Audience=api://${ENTRA_CLIENT_ID}" \
    "AzureAiFoundry__Endpoint=${AI_FOUNDRY_ENDPOINT}" \
    "AzureAiFoundry__Deployment=${AI_FOUNDRY_DEPLOYMENT}" \
    "AzureAiFoundry__UseMockResponses=false" \
  --output none

# ── 5. Grant Container App Managed Identity access to Key Vault ───────────────
echo ">>> Assigning Key Vault Secrets User role to Container App identity..."
PRINCIPAL_ID=$(az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query identity.principalId -o tsv)

KV_ID=$(az keyvault show \
  --name "$KEY_VAULT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query id -o tsv)

az role assignment create \
  --assignee "$PRINCIPAL_ID" \
  --role "Key Vault Secrets User" \
  --scope "$KV_ID" \
  --output none

# ── 6. App Registration for GitHub Actions OIDC ───────────────────────────────
# This is separate from the API's own Entra ID app registration.
echo ">>> Creating App Registration for GitHub Actions OIDC..."
APP_ID=$(az ad app create \
  --display-name "sp-github-copilot-api-deploy" \
  --query appId -o tsv)

SP_ID=$(az ad sp create \
  --id "$APP_ID" \
  --query id -o tsv)

# Federated credential — allows GitHub Actions to authenticate without a secret.
az ad app federated-credential create \
  --id "$APP_ID" \
  --parameters "{
    \"name\": \"github-deploy\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none

# Grant Contributor on the resource group (scoped — not subscription-wide).
az role assignment create \
  --assignee "$SP_ID" \
  --role "Contributor" \
  --scope "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/${RESOURCE_GROUP}" \
  --output none

# ── 7. Output GitHub secrets/variables ────────────────────────────────────────
APP_URL=$(az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.configuration.ingress.fqdn -o tsv)

echo ""
echo "════════════════════════════════════════════════════"
echo " Add these to your GitHub repo → Settings → Secrets"
echo "════════════════════════════════════════════════════"
echo "  AZURE_CLIENT_ID       = ${APP_ID}"
echo "  AZURE_TENANT_ID       = ${ENTRA_TENANT_ID}"
echo "  AZURE_SUBSCRIPTION_ID = ${SUBSCRIPTION_ID}"
echo ""
echo "══════════════════════════════════════════════════════"
echo " Add these to GitHub repo → Settings → Variables"
echo "══════════════════════════════════════════════════════"
echo "  AZURE_RESOURCE_GROUP     = ${RESOURCE_GROUP}"
echo "  AZURE_CONTAINER_APP_NAME = ${CONTAINER_APP_NAME}"
echo ""
echo "════════════════════════════════════"
echo " Container App URL"
echo "════════════════════════════════════"
echo "  https://${APP_URL}"
echo ""
echo "Done. Run the GitHub Actions deploy workflow to push your first image."
