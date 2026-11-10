#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$EnvFile = Join-Path $Root ".env"

function New-RandomHex {
    param([int]$ByteCount)
    $bytes = [byte[]]::new($ByteCount)
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return ($bytes | ForEach-Object { $_.ToString("x2") }) -join ""
}

if (-not (Test-Path $EnvFile)) {
    Write-Host "==> Generate .env with random secrets"
    $secret = New-RandomHex -ByteCount 32
    $dbPassword = New-RandomHex -ByteCount 16
    @"
POSTGRES_PASSWORD=$dbPassword
OTAI_DESK_SECRET_KEY=$secret
OTAI_DESK_CORS_ORIGINS=http://localhost
"@ | Set-Content -Path $EnvFile -Encoding utf8NoBOM
} else {
    Write-Host "==> .env already exists (skipping generation)"
}

Set-Location $Root
Write-Host "==> Build and start OTAI Desk"
docker compose up -d --build
docker compose ps

Write-Host ""
Write-Host "Open http://localhost"
