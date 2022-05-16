# Durable Functions Slack Demo

Azure Durable Functions with Slack Interaction Demo

## Purpose

This code creates a demonstration workflow using Azure durable functions. The workflow provides interactivity with Slack for a simulated approval process.

## Technology Stack

* Visual Studio 2022
* Azure Durable Functions
* Azure Functions v4
* .NET 6.0

## Workflow Diagram

![Workflow Diagram](/docs/images/DurableFunctionsSlackDemo.drawio.png)

## Slack Application Setup

[Create a new Slack application](https://api.slack.com/apps/)

1. Click Create New App
1. Select *From Scratch*
1. Provide an application name and select your workspace
1. Click *Incoming Webhooks*
1. Enable incoming webhooks
1. Click *Add New Webhook to Workspace*
1. Select a channel for output
1. Click *Allow*
1. Copy the new webhook URL and save for configuration later.

## Configuration

The Azure Function application can be configured to optionally use an Azure Key Vault or can use environment configuration.

|Key|Description|
|---|-----------|
|AzureWebJobsStorage|Storage Account connection string used by Azure function and also used for durable function task hub|
|KeyVaultUrl|(Optional) Azure Key Vault URL is using key vault for configuration|
|SlackApprovalServiceOptions:SlackWebhookUrl|Slack application web hook URL (set this if not using key vault)|

### Azure Key Vault

1. Create an Azure Key Vault if you do not have one already.
1. Create a secret named *SlackApprovalServiceOptions--SlackWebhookUrl*. Set the value to the Slack application webhook URL from the Slack setup section.
1. Set KeyVaultUrl configuration of Azure function app to point to Azure Key Vault URL
1. Set up managed identity and provide Key Vault secret access to Azure Function application
