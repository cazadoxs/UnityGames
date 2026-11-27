$ErrorActionPreference = 'Continue'
$p = 'C:\Users\Usuario\Documents\Claudeproyect\ImprentaInfinita'
$log = "$p\Builds\subida.txt"
$git = 'C:\Users\Usuario\Desktop\ddd\Git\cmd\git.exe'
$w = 'C:\Users\Usuario\Documents\Claudeproyect\_subida'
function L($m) { Add-Content $log ("$(Get-Date -Format HH:mm:ss) $m") }
Set-Content $log "inicio"
try {
  if (Test-Path $w) { Remove-Item $w -Recurse -Force }
  New-Item -ItemType Directory $w | Out-Null
  # 1) zip del juego en paralelo
  $zip = "$w\ImprentaInfinita-Windows.zip"
  $zj = Start-Job { param($src,$dst) Compress-Archive -Path "$src\*" -DestinationPath $dst -CompressionLevel Optimal } -ArgumentList "$p\Builds\Windows", $zip
  # 2) repo
  & $git clone --depth 1 https://github.com/cazadoxs/UnityGames.git "$w\repo" 2>&1 | % { L $_ }
  $d = "$w\repo\ImprentaInfinita"
  New-Item -ItemType Directory $d -Force | Out-Null
  foreach ($f in 'Assets','Packages','ProjectSettings','Tools') { robocopy "$p\$f" "$d\$f" /E /NFL /NDL /NJH /NJS /NP | Out-Null }
  foreach ($f in 'LEEME.md','.vsconfig') { if (Test-Path "$p\$f") { Copy-Item "$p\$f" $d } }
@"
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]serSettings/
*.csproj
*.sln
*.pidb.meta
*.pdb.meta
"@ | Set-Content "$d\.gitignore"
  Set-Location "$w\repo"
  & $git config user.name "cazadoxs"
  & $git config user.email "cazadoxs@users.noreply.github.com"
  & $git config http.postBuffer 524288000
  & $git add ImprentaInfinita 2>&1 | Out-Null
  & $git commit -q -m "Imprenta Infinita: proyecto Unity 6 (folios reales, humanos, tunel, optimizacion)" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01JE5uhFmfvBtwTikuPTfXaW" 2>&1 | % { L $_ }
  L "push..."
  & $git push origin HEAD 2>&1 | % { L $_ }
  L ("push exit " + $LASTEXITCODE)
  $br = & $git rev-parse --abbrev-ref HEAD
  L "rama $br"
  Wait-Job $zj | Out-Null; Receive-Job $zj 2>&1 | % { L $_ }
  L ("zip MB " + [math]::Round((Get-Item $zip).Length/1MB))
  $tag = "imprenta-" + (Get-Date -Format 'yyyyMMdd-HHmm')
  gh release create $tag $zip -R cazadoxs/UnityGames --target $br --title "Imprenta Infinita (Windows) $(Get-Date -Format 'dd-MM-yyyy')" --notes "Build de Windows de Imprenta Infinita. Descomprime y ejecuta ImprentaInfinita.exe. Codigo en la carpeta ImprentaInfinita/." 2>&1 | % { L $_ }
  L ("release exit " + $LASTEXITCODE)
} catch { L ("ERROR " + $_) }
L "fin"
