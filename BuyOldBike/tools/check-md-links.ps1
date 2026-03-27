param(
  [switch]$Json
)

$ErrorActionPreference = "Stop"

$root = (Get-Location).Path

$mdFiles = @()
$codeExplanation = Join-Path $root "CODE_EXPLANATION.md"
if (Test-Path -LiteralPath $codeExplanation) { $mdFiles += $codeExplanation }

$docsDir = Join-Path $root "docs"
if (Test-Path -LiteralPath $docsDir) {
  $mdFiles += Get-ChildItem -Recurse -File -Path $docsDir -Filter *.md | Select-Object -ExpandProperty FullName
}

$rx = [regex]'file:///([^\)\s]+)'

$missing = @()
foreach ($f in $mdFiles) {
  $text = Get-Content -Raw -LiteralPath $f
  foreach ($m in $rx.Matches($text)) {
    $url = $m.Groups[1].Value
    $path = [uri]::UnescapeDataString($url) -replace '/', '\'
    $pathOnly = ($path -split '#', 2)[0]
    if ($path -match '^[a-zA-Z]:\\') {
      if (-not (Test-Path -LiteralPath $pathOnly)) {
        $missing += [pscustomobject]@{
          MdFile   = $f.Substring($root.Length + 1)
          LinkPath = $path
        }
      }
    }
  }
}

if ($missing.Count -eq 0) {
  if ($Json) {
    "[]"
  }
  else {
    "OK: No missing file:/// links found."
  }
  exit 0
}

$missing = $missing | Sort-Object MdFile, LinkPath
if ($Json) {
  $missing | ConvertTo-Json -Depth 4
  exit 2
}

$missing | Format-Table -AutoSize
exit 2
