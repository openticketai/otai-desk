#requires -Version 5.1
<#
.SYNOPSIS
  Uploads the OTAI Desk demo bundle to a GCE VM and runs docker compose.

.PARAMETER ProjectId
  GCP project ID.

.PARAMETER Zone
  Compute zone (e.g. europe-west3-a).

.PARAMETER VmName
  Instance name.

.PARAMETER RepoRoot
  Monorepo root containing otai-desk/ and otai-website/packages/otai-vue-ui/.
#>
param(
  [Parameter(Mandatory = $true)][string] $ProjectId,
  [Parameter(Mandatory = $true)][string] $Zone,
  [Parameter(Mandatory = $true)][string] $VmName,
  [string] $RepoRoot = "",
  [string] $RemoteUser = $env:USERNAME
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepoRoot) {
  $RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..\..\..")).Path
}

$bundle = Join-Path $env:TEMP ("otai-desk-demo-" + [Guid]::NewGuid().ToString("n") + ".tar.gz")
$remoteBundle = "${RemoteUser}@${VmName}:~/otai-desk-demo-bundle.tar.gz"
$remoteScript = "${RemoteUser}@${VmName}:~/02-bootstrap-vm.sh"

Write-Host "Archiving otai-desk + otai-vue-ui -> $bundle"
Push-Location $RepoRoot
try {
  & tar -czf $bundle `
    --exclude=.git `
    --exclude=node_modules `
    --exclude=__pycache__ `
    --exclude=.venv `
    --exclude=.pytest_cache `
    --exclude=.ruff_cache `
    otai-desk `
    otai-website/packages/otai-vue-ui
  if ($LASTEXITCODE -ne 0) { throw "tar failed with exit $LASTEXITCODE" }
}
finally {
  Pop-Location
}

Write-Host "Uploading bundle..."
& gcloud compute scp $bundle $remoteBundle --project=$ProjectId --zone=$Zone
if ($LASTEXITCODE -ne 0) { throw "gcloud scp (bundle) failed" }

Write-Host "Uploading bootstrap script..."
& gcloud compute scp (Join-Path $ScriptDir "02-bootstrap-vm.sh") $remoteScript --project=$ProjectId --zone=$Zone
if ($LASTEXITCODE -ne 0) { throw "gcloud scp (bootstrap) failed" }

$remoteCmd = @"
rm -rf ~/otai-desk-demo && mkdir -p ~/otai-desk-demo && tar -xzf ~/otai-desk-demo-bundle.tar.gz -C ~/otai-desk-demo && rm -f ~/otai-desk-demo-bundle.tar.gz && bash ~/02-bootstrap-vm.sh
"@
& gcloud compute ssh "${RemoteUser}@${VmName}" --project=$ProjectId --zone=$Zone --command=$remoteCmd
if ($LASTEXITCODE -ne 0) { throw "gcloud ssh failed" }

$ip = & gcloud compute instances describe $VmName --project=$ProjectId --zone=$Zone --format="value(networkInterfaces[0].accessConfigs[0].natIP)"
Remove-Item -Force $bundle -ErrorAction SilentlyContinue
Write-Host "Done. Desk demo: http://$ip/"
