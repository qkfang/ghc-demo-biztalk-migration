# ── Helpers ──────────────────────────────────────────────────────────────────
function Write-Section($title) {
    Write-Host "`n$("─" * 60)" -ForegroundColor Cyan
    Write-Host "  $title" -ForegroundColor White
    Write-Host "$("─" * 60)" -ForegroundColor Cyan
}

function Write-Ok($msg)   { Write-Host "  ✓  $msg" -ForegroundColor Green  }
function Write-Warn($msg) { Write-Host "  !  $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "  ✗  $msg" -ForegroundColor Red    }


# ── 2. Build request ──────────────────────────────────────────────────────────
$sourceRef = "PAY-REF-$([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())"
$requestBody = "<?xml version=""1.0""?><SuperContributionRequest xmlns=""http://SuperFundManagement.Schemas.SuperContribution""><ContributionId>CONT-2024-001</ContributionId><EmployerId>EMP-001</EmployerId><EmployerName>Acme Corporation Pty Ltd</EmployerName><EmployerABN>51824753556</EmployerABN><PayPeriodEndDate>2024-06-30</PayPeriodEndDate><Members><Member><MemberAccountNumber>SF-100001</MemberAccountNumber><MemberName>Jane Smith</MemberName><ContributionType>SuperannuationGuarantee</ContributionType><GrossAmount>875.00</GrossAmount></Member></Members><TotalContribution>875.00</TotalContribution><Currency>AUD</Currency><PaymentReference>$sourceRef</PaymentReference></SuperContributionRequest>"

Write-Section "REQUEST  →  POST http://localhost:7071/api/SuperFundManagement/Receive"
Write-Host ([System.Xml.Linq.XDocument]::Parse($requestBody).ToString()) -ForegroundColor White

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
        Write-Host $pretty -ForegroundColor White
    } catch {
        Write-Host $response.Content -ForegroundColor White
    }
} catch {
    $status = $_.Exception.Response.StatusCode.value__
    Write-Err "Request failed — HTTP $status"
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host ""
Write-Host ""
Write-Host ""

