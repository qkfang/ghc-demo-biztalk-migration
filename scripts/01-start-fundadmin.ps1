# ── Helpers ──────────────────────────────────────────────────────────────────
function Write-Section($title) {
    Write-Host "`n$("─" * 60)" -ForegroundColor Cyan
    Write-Host "  $title" -ForegroundColor White
    Write-Host "$("─" * 60)" -ForegroundColor Cyan
}

function Write-Ok($msg)   { Write-Host "  ✓  $msg" -ForegroundColor Green  }
function Write-Warn($msg) { Write-Host "  !  $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "  ✗  $msg" -ForegroundColor Red    }

# ── 1. Ensure mock fund-admin API is running ──────────────────────────────────
Write-Section "Fund-Admin Mock API  (http://localhost:5050)"

$mockRunning = $false
try {
    $health = Invoke-RestMethod -Uri http://localhost:5050/health -TimeoutSec 2
    Write-Ok "Already running — status: $($health.status)"
    $mockRunning = $true
} catch { }

if (-not $mockRunning) {
    Write-Warn "Not running — starting app-fundadmin..."
    dotnet run --project "$PSScriptRoot\..\app-fundadmin\AllocationMockApi.csproj"
}

Write-Host ""
