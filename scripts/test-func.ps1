# ── Helpers ──────────────────────────────────────────────────────────────────
function Write-Section($title) {
    Write-Host "`n$("─" * 60)" -ForegroundColor DarkGray
    Write-Host "  $title" -ForegroundColor Cyan
    Write-Host "$("─" * 60)" -ForegroundColor DarkGray
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
    Write-Warn "Not running — starting app-fundadmin in background..."
    $mockProc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project `"$PSScriptRoot\..\app-fundadmin\AllocationMockApi.csproj`"" `
        -NoNewWindow -PassThru
    Write-Host "  PID $($mockProc.Id) — waiting for health check..." -ForegroundColor DarkGray

    $ready = $false
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 500
        try {
            $null = Invoke-RestMethod -Uri http://localhost:5050/health -TimeoutSec 1
            $ready = $true; break
        } catch { }
    }
    if ($ready) { Write-Ok "Mock API is up" }
    else        { Write-Err "Mock API did not start in time — continuing anyway" }
}

# ── 2. Build request ──────────────────────────────────────────────────────────
$requestBody = '<?xml version="1.0"?><SuperContributionRequest xmlns="http://SuperFundManagement.Schemas.SuperContribution"><ContributionId>CONT-2024-001</ContributionId><EmployerId>EMP-001</EmployerId><EmployerName>Acme Corporation Pty Ltd</EmployerName><EmployerABN>51824753556</EmployerABN><PayPeriodEndDate>2024-06-30</PayPeriodEndDate><Members><Member><MemberAccountNumber>SF-100001</MemberAccountNumber><MemberName>Jane Smith</MemberName><ContributionType>SuperannuationGuarantee</ContributionType><GrossAmount>875.00</GrossAmount></Member></Members><TotalContribution>875.00</TotalContribution><Currency>AUD</Currency><PaymentReference>PAY-REF-20240630</PaymentReference></SuperContributionRequest>'

Write-Section "REQUEST  →  POST http://localhost:7071/api/SuperFundManagement/Receive"
Write-Host ([System.Xml.Linq.XDocument]::Parse($requestBody).ToString()) -ForegroundColor Yellow

# ── 3. Call Azure Function ────────────────────────────────────────────────────
Write-Section "Calling Azure Function..."

try {
    $response = Invoke-WebRequest -Uri http://localhost:7071/api/SuperFundManagement/Receive `
        -Method POST `
        -ContentType "application/xml" `
        -Body $requestBody `
        -ErrorAction Stop

    Write-Section "RESPONSE  (HTTP $($response.StatusCode))"
    Write-Ok "Status: $($response.StatusCode) $($response.StatusDescription)"
    try {
        $pretty = [System.Xml.Linq.XDocument]::Parse($response.Content).ToString()
        Write-Host $pretty -ForegroundColor Green
    } catch {
        Write-Host $response.Content -ForegroundColor Green
    }
} catch {
    $status = $_.Exception.Response.StatusCode.value__
    Write-Err "Request failed — HTTP $status"
    Write-Host $_.Exception.Message -ForegroundColor Red
}

