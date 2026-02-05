#!/bin/bash
# Create Microsoft Foundry instance with project and model deployment
set -e

# Configuration
RESOURCE_GROUP="my-foundry-rg"
LOCATION="eastus"
FOUNDRY_RESOURCE="my-foundry-resource"
PROJECT_NAME="my-foundry-project"
MODEL_NAME="gpt-4.1-mini"
MODEL_VERSION="2025-04-14"

# Helpers
check() { "$@" &>/dev/null; }
info() { echo "→ $*"; }
fail() { echo "ERROR: $*" >&2; exit 1; }

# Prerequisites
command -v az &>/dev/null || fail "Azure CLI not installed"
MIN_VERSION="2.78.0"
CURRENT_VERSION=$(az version --query '"azure-cli"' -o tsv)
[[ "$(printf '%s\n' "$MIN_VERSION" "$CURRENT_VERSION" | sort -V | head -1)" == "$MIN_VERSION" ]] || \
    fail "Azure CLI $CURRENT_VERSION < $MIN_VERSION required. Run: az upgrade --yes"
check az account show || fail "Not signed in. Run: az login"

info "Signed in: $(az account show --query user.name -o tsv) @ $(az account show --query name -o tsv)"

# Create resources (idempotent)
info "Creating resource group..."
check az group show -n "$RESOURCE_GROUP" || az group create -n "$RESOURCE_GROUP" -l "$LOCATION" -o none

info "Creating Foundry resource..."
check az cognitiveservices account show -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" || \
    az cognitiveservices account create -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" \
        --kind AIServices --sku s0 -l "$LOCATION" --yes -o none

info "Enabling managed identity..."
PRINCIPAL=$(az cognitiveservices account show -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" --query identity.principalId -o tsv 2>/dev/null)
[[ -n "$PRINCIPAL" && "$PRINCIPAL" != "null" ]] || \
    az cognitiveservices account identity assign -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" -o none

info "Enabling project management..."
az cognitiveservices account update -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" --allow-project-management true -o none 2>/dev/null || true

info "Setting custom subdomain..."
SUBDOMAIN=$(az cognitiveservices account show -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" --query properties.customSubDomainName -o tsv)
if [[ -z "$SUBDOMAIN" || "$SUBDOMAIN" == "null" ]]; then
    SUBDOMAIN="${FOUNDRY_RESOURCE}-$(date +%s | tail -c 5)"
    az cognitiveservices account update -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" --custom-domain "$SUBDOMAIN" -o none
fi

info "Creating project..."
check az cognitiveservices account project show -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" --project-name "$PROJECT_NAME" || \
    az cognitiveservices account project create -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" \
        --project-name "$PROJECT_NAME" -l "$LOCATION" -o none

info "Deploying model $MODEL_NAME..."
check az cognitiveservices account deployment show -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" --deployment-name "$MODEL_NAME" || \
    az cognitiveservices account deployment create -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" \
        --deployment-name "$MODEL_NAME" --model-name "$MODEL_NAME" --model-version "$MODEL_VERSION" \
        --model-format OpenAI --sku-capacity 10 --sku-name Standard -o none

# Output
PROJECT_ENDPOINT=$(az cognitiveservices account project show -n "$FOUNDRY_RESOURCE" -g "$RESOURCE_GROUP" \
    --project-name "$PROJECT_NAME" --query 'properties.endpoints."AI Foundry API"' -o tsv)

echo ""
echo "=== Foundry Ready ==="
echo "Project endpoint: $PROJECT_ENDPOINT"
echo "Model: $MODEL_NAME"
