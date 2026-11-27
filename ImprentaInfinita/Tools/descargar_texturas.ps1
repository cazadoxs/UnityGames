# Descarga texturas PBR CC0 de Poly Haven (2K, jpg) a Assets/Textures/PolyHaven
param([string[]]$Ids = @('hangar_concrete_floor','box_profile_metal_sheet','corrugated_iron','fine_grained_wood','dark_wood','metal_plate','metal_plate_02','rubber_tiles','plywood','painted_metal_shutter'))
$root = Join-Path $PSScriptRoot '..\Assets\Textures\PolyHaven'
New-Item -ItemType Directory -Force -Path $root | Out-Null
$want = @{ 'Diffuse'='color'; 'nor_gl'='normal'; 'Rough'='rough'; 'AO'='ao'; 'Metal'='metal' }
foreach ($id in $Ids) {
  try {
    $files = (Invoke-WebRequest -UseBasicParsing "https://api.polyhaven.com/files/$id" -TimeoutSec 30).Content | ConvertFrom-Json
    $dir = Join-Path $root $id; New-Item -ItemType Directory -Force -Path $dir | Out-Null
    foreach ($k in $want.Keys) {
      $m = $files.$k
      if ($null -eq $m) { continue }
      $url = $m.'2k'.jpg.url
      if (-not $url) { $url = $m.'2k'.png.url }
      if (-not $url) { continue }
      $ext = [IO.Path]::GetExtension($url)
      $out = Join-Path $dir ("{0}_{1}{2}" -f $id, $want[$k], $ext)
      if (-not (Test-Path $out)) { Invoke-WebRequest -UseBasicParsing $url -OutFile $out -TimeoutSec 120 }
    }
    "OK $id : " + ((Get-ChildItem $dir).Name -join ', ')
  } catch { "FALLO $id : " + $_.Exception.Message }
}
