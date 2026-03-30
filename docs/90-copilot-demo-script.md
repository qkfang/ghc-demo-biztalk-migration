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
/create-agent  create or update `biztalk-migration.agent.md` to include requirements and guildlines. Keep it simple.
```

---

### Step 4

Open **GitHub Copilot CLI** and create a GitHub issue to track the migration work.

```
#agent create an issue ticket in github copilot for biztalk migration

title: 'migrate Biztalk integration to Azure Functions'
body: 'migrate existing BizTalk application inside `biztalk` folder to new integration app on Azure.
- create the integraiton logics as a c# function app inside `az\funcapp` with swagger ui
- create tests for the integration inside `az\funcapp`
- create IaC deployment for azure inside `az\bicep`

keep the init migration process simple and as it as'

```

---

### Step 5

Go to **github.com**, open the issue, and assign it to GitHub Copilot to begin autonomous implementation.

---

### Step 6

Observe the Copilot agent working through the migration for 1-2 minutes. Then navigate to an already implemented ticket to show the final implementation.

---


### Step 7

Check out the agent's working branch locally, run the Function App tests, and show swagger endpoint and do a test.

```
<?xml version="1.0" encoding="UTF-8"?>
<SuperContributionRequest xmlns="http://SuperFundManagement.Schemas.SuperContribution">
    <ContributionId>CONT-2024-001</ContributionId>
    <EmployerId>EMP-001</EmployerId>
    <EmployerName>Acme Corporation Pty Ltd</EmployerName>
    <EmployerABN>51824753556</EmployerABN>
    <PayPeriodEndDate>2024-06-30</PayPeriodEndDate>
    <Members>
        <Member>
            <MemberAccountNumber>SF-100001</MemberAccountNumber>
            <MemberName>Jane Smith</MemberName>
            <ContributionType>SuperannuationGuarantee</ContributionType>
            <GrossAmount>875.00</GrossAmount>
        </Member>
    </Members>
    <TotalContribution>875.00</TotalContribution>
    <Currency>AUD</Currency>
    <PaymentReference>Test100</PaymentReference>
</SuperContributionRequest>
```


---

### Step 8

The above swagger test will fail and there is an error message because mock fund admin endpoint not running. Add below prompt to copilot chat and it will start up the mock app. then submit swagger payload again.

```
fix this

Result: Failed to send allocation to fund admin API
Type:
Exception: System.Net.Http.HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5050)
```

call the endpoint to verify the migrated integration returns the expected response.

---

### Step 9

a: Ask a question in the chat to ask agent to 

```
find mapping logics for `ContributionMapHelper.cs`
```

b: Ask copilot to create a github action pipeline to deploy `az\bicep` to azure

```
create a github action pipeline to deploy `az\bicep` to azure, and microsoft learn for best practise.
```

c: Use work iq to ask a question about my email

```
find the meeting invite for 'Enabling ART’s 2030 Strategy' in m365
```

---


