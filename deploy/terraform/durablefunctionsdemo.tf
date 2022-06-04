provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = lower("${var.prefix}_rg")
  location = var.location
}

variable "prefix" {
  description = "Prefix set appropriately to ensure that resources are unique."
}

variable "location" {
  description = "The Azure Region in which all resources in this example should be created."
}

variable "slack-url" {
  description = "Slack Webhook URL."
}

resource "azurerm_storage_account" "storage" {
  name                      = lower("${var.prefix}storage")
  resource_group_name       = azurerm_resource_group.rg.name
  location                  = azurerm_resource_group.rg.location
  account_tier              = "Standard"
  account_kind              = "StorageV2"
  account_replication_type  = "LRS"
  enable_https_traffic_only = true
}

output "storage-connectionstring" {
  value     = azurerm_storage_account.storage.primary_blob_connection_string
  sensitive = true
}

resource "azurerm_service_plan" "app-plan" {
  name                = lower("${var.prefix}-app-plan")
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Windows"
  sku_name            = "S1"
}

resource "azurerm_application_insights" "appinsights" {
  name                = lower("${var.prefix}-appinsights")
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
  tags                = {}
}

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "keyvault" {
  name                        = lower("${var.prefix}-keyvault")
  location                    = azurerm_resource_group.rg.location
  resource_group_name         = azurerm_resource_group.rg.name
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false

  sku_name = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get","List","Set","Delete"
    ]
  }
}

resource "azurerm_key_vault_secret" "slackurl" {
  name         = "SlackApprovalServiceOptions--SlackWebhookUrl"
  value        = var.slack-url
  key_vault_id = azurerm_key_vault.keyvault.id
}

resource "azurerm_user_assigned_identity" "user-identity" {
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  name                = lower("${var.prefix}-user-identity")
}

resource "azurerm_key_vault_access_policy" "access-policy" {
  key_vault_id = azurerm_key_vault.keyvault.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_user_assigned_identity.user-identity.principal_id

  secret_permissions = [
    "Get","List"
  ]
}

resource "azurerm_windows_function_app" "durable-functions-slack-demo" {
  name                        = lower("${var.prefix}-durable-functions-slack-demo")
  location                    = azurerm_resource_group.rg.location
  resource_group_name         = azurerm_resource_group.rg.name
  service_plan_id             = azurerm_service_plan.app-plan.id
  storage_account_name        = azurerm_storage_account.storage.name
  storage_account_access_key  = azurerm_storage_account.storage.primary_access_key
  tags                        = {}
  functions_extension_version = "~4"

  app_settings = {
    "SlackApprovalServiceOptions:SlackWebhookUrl" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.keyvault.name};SecretName=SlackApprovalServiceOptions--SlackWebhookUrl)"
  }
  
  key_vault_reference_identity_id = azurerm_user_assigned_identity.user-identity.id

  identity {
      type = "UserAssigned"
      identity_ids = [ azurerm_user_assigned_identity.user-identity.id ]
  }

  site_config {
    always_on                              = true
    application_insights_key               = azurerm_application_insights.appinsights.instrumentation_key
    application_insights_connection_string = azurerm_application_insights.appinsights.connection_string
    application_stack {
      dotnet_version = 6
    }
    
  }
}

