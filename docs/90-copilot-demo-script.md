# Copilot Migration Demo Guide

## Overview

This guide walks through a live demo of migrating a legacy BizTalk Server integration to Azure Functions using GitHub Copilot.

---

### Setup Checklist

- MCP tools enabled

---

### Step 1

Briefly walk through the existing legacy BizTalk project and explain the integration scenario.


---

### Step 2

Use **GitHub Copilot Chat** (Claude Sonnet 4.6 model) to analyse the existing BizTalk project and generate a visual summary.

```
#agent create 'biztalk.md' mermaid diagram markdown file to describe current biztalk project.
biztalk source code is under `app-biztalk` folder. Keep it simple, include these:
- End-to-End Message Flow
- Schema Structures
- Field-by-Field Mapping Table
```

---

### Step 3

Create a customised BizTalk migration agent using the **Claude** agent mode.

```
/create-agent  create or update `biztalk-migration.agent.md` to include requirements and guildlines. Keep it simple, 
```

---

### Step 4

Open **GitHub Copilot CLI** and create a GitHub issue to track the migration work.

```
#agent create an issue ticket in github copilot for biztalk migration

title: 'migrate Biztalk integration to Azure Functions'
body: 'migrate existing BizTalk application inside `biztalk` folder to new integration app on Azure.
- create the integraiton logics as a c# function app inside `az\funcapp`
- create tests for the integration inside `az\funcapp`
- create IaC deployment for azure inside `az\bicep`

keep the init migration process simple and as it as'

```

---

### Step 5

Go to **github.com**, open the issue, and assign it to GitHub Copilot to begin autonomous implementation.

```
#agent create an issue ticket in github copilot for biztalk migration

title: 'migrate Biztalk integration to Azure Functions'
body: 'migrate existing BizTalk application inside `biztalk` folder to new integration app on Azure.
- create the integraiton logics as a c# function app inside `az\funcapp`
- create tests for the integration inside `az\funcapp`
- create IaC deployment for azure inside `az\bicep`

keep the init migration process simple and as it as'

```

---

### Step 6

Observe the Copilot agent working through the migration for 1-2 minutes. Then navigate to an already implemented ticket to show the final implementation.

---


### Step 7

Check out the agent's working branch locally, run the Function App tests, and call the endpoint to verify the migrated integration returns the expected response.

---

### Step 8

Ask a question in the chat to ask agent to find mapping logics for `ContributionMapHelper.cs`

Ask copilot to example one class in c# function app

Ask copilot to create a github action pipeline to deploy `az\bicep` to azure, and microsoft learn to explain what is funct app.


---

### Step 9

Include work context to github copilot via Work-IQ and Foundry-IQ

Ask questions about a file in my sharepoint and a knowledge question from AI Search.


---