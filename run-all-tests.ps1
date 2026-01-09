#!/usr/bin/env pwsh
# ===============================================
# ONLINE TECH STORE - TUM TESTLERI CALISTIR
# ===============================================
# Bu script hem Backend (xUnit) hem de Frontend (Vitest) testlerini calistirir
# 
# Kullanim:
#   ./run-all-tests.ps1              # Tum testleri calistir
#   ./run-all-tests.ps1 -Backend     # Sadece Backend testleri
#   ./run-all-tests.ps1 -Frontend    # Sadece Frontend testleri
#   ./run-all-tests.ps1 -Coverage    # Coverage raporu ile

param(
    [switch]$Backend,
    [switch]$Frontend,
    [switch]$Coverage,
    [switch]$Watch
)

$ErrorActionPreference = "Continue"
$RootPath = $PSScriptRoot
if (-not $RootPath) { $RootPath = Get-Location }

# Renk fonksiyonlari
function Write-Header($text) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " $text" -ForegroundColor Cyan
    Write-Host "========================================"  -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success($text) {
    Write-Host "[OK] $text" -ForegroundColor Green
}

function Write-Fail($text) {
    Write-Host "[FAIL] $text" -ForegroundColor Red
}

function Write-Info($text) {
    Write-Host "[INFO] $text" -ForegroundColor Yellow
}

# Hicbir flag verilmediyse hepsini calistir
if (-not $Backend -and -not $Frontend) {
    $Backend = $true
    $Frontend = $true
}

$backendSuccess = $true
$frontendSuccess = $true

# ===============================================
# BACKEND TESTS (xUnit)
# ===============================================
if ($Backend) {
    Write-Header "BACKEND TESTS (xUnit + .NET 8)"
    
    Push-Location $RootPath
    
    try {
        Write-Info "Backend testleri calistiriliyor..."
        
        if ($Coverage) {
            dotnet test Tests/Tests.csproj --verbosity normal --collect:"XPlat Code Coverage" --results-directory:"./TestResults"
        } else {
            dotnet test Tests/Tests.csproj --verbosity normal
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Backend testleri basariyla tamamlandi!"
        } else {
            Write-Fail "Backend testlerinde hatalar var!"
            $backendSuccess = $false
        }
    }
    catch {
        Write-Fail "Backend testleri calistirilirken hata: $_"
        $backendSuccess = $false
    }
    finally {
        Pop-Location
    }
}

# ===============================================
# FRONTEND TESTS (Vitest)
# ===============================================
if ($Frontend) {
    Write-Header "FRONTEND TESTS (Vitest + React Testing Library)"
    
    Push-Location "$RootPath/Frontend"
    
    try {
        Write-Info "Frontend testleri calistiriliyor..."
        
        if ($Watch) {
            npm test
        } else {
            if ($Coverage) {
                npm test -- --run --coverage
            } else {
                npm test -- --run
            }
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Frontend testleri basariyla tamamlandi!"
        } else {
            Write-Fail "Frontend testlerinde hatalar var!"
            $frontendSuccess = $false
        }
    }
    catch {
        Write-Fail "Frontend testleri calistirilirken hata: $_"
        $frontendSuccess = $false
    }
    finally {
        Pop-Location
    }
}

# ===============================================
# OZET
# ===============================================
Write-Header "TEST SONUC OZETI"

if ($Backend) {
    if ($backendSuccess) {
        Write-Success "Backend (xUnit): BASARILI"
    } else {
        Write-Fail "Backend (xUnit): BASARISIZ"
    }
}

if ($Frontend) {
    if ($frontendSuccess) {
        Write-Success "Frontend (Vitest): BASARILI"
    } else {
        Write-Fail "Frontend (Vitest): BASARISIZ"
    }
}

# Exit code
if ($backendSuccess -and $frontendSuccess) {
    Write-Host ""
    Write-Host "Tum testler basariyla tamamlandi!" -ForegroundColor Green
    Write-Host ""
    exit 0
} else {
    Write-Host ""
    Write-Host "Bazi testler basarisiz oldu!" -ForegroundColor Red
    Write-Host ""
    exit 1
}
