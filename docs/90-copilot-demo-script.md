# Copilot Migration Demo Guide


## Demo Guide

---

### Setup Checklist

- MCP tools enabled

---

### Step 1

Quickly show existing legacy biztalk project and explain the integraiton


---

### Step 2

Understand current BizTalk project using `github copilot chat`, use an Opus 4.6 model.

```
#agent create 'biztalk.md' mermaid diagram markdown file to describe current biztalk project.
biztalk source code is under `app-biztalk` folder. Keep it simple, include these:
- End-to-End Message Flow
- Schema Structures
- Field-by-Field Mapping Table
```

---

### Step 3

Create a customised BizTalk migration agent using `Claude` agent.

```
/create-agent  create or update `biztalk-migration.agent.md` to include requirements and guildlines. Keep it simple, 
```

---

### Step 4

run `copilot` in CLI to open github copilot cli, and create a migration ticket

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

Use GitHub coding agent to implement the code. go to github.com and assign the ticket to github copilot.

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

Watch agent integration and start up. then switch to an existing ticket to show the outcome

---


### Step 7

Switch to the agent working branch and checkout the code to run. run func app test and endpoint

do a test to see func app returns value

---
