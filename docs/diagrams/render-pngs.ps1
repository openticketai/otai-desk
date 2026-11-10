# Wrapper for otai-desk/docs/diagrams/render_diagrams.py
# Usage (from this directory):  .\render-pngs.ps1 [--index-only] [--diagram <id>]

$ErrorActionPreference = "Stop"
$script = Join-Path $PSScriptRoot "render_diagrams.py"

if (Get-Command uv -ErrorAction SilentlyContinue) {
    & uv run $script @args
} else {
    & python $script @args
}
