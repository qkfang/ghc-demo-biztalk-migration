using './main.bicep'

// ── Development environment ──────────────────────────────────────────────────
param env = 'dev'
param project = 'superfund'
param location = 'australiaeast'
param fundAdminApiUrl = 'https://func-superfund-dev.azurewebsites.net/api/allocations'
