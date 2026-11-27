$p='C:\Users\Usuario\Documents\Claudeproyect\ImprentaInfinita'
"inicio $(Get-Date)" | Out-File "$p\Builds\estado.txt"
Start-Process -Wait -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.1.4f1\Editor\Unity.exe' -ArgumentList "-batchmode -quit -projectPath `"$p`" -executeMethod Imprenta.EditorTools.ImprentaSetup.BuildWindows -logFile `"$p\Logs\build.log`""
$errs = Select-String -Path "$p\Logs\build.log" -Pattern 'error CS' | Select -First 5 | % { $_.Line }
Select-String -Path "$p\Logs\build.log" -Pattern 'Build Finished' | Select -First 1 | % { $_.Line } | Out-File -Append "$p\Builds\estado.txt"
if ($errs) { $errs | Out-File -Append "$p\Builds\estado.txt"; "fin ERROR $(Get-Date)" | Out-File -Append "$p\Builds\estado.txt"; exit }
Remove-Item "$p\Builds\autotest" -Recurse -Force -ErrorAction SilentlyContinue
$pr = Start-Process -PassThru -FilePath "$p\Builds\Windows\ImprentaInfinita.exe" -ArgumentList "-autotest -nuevo -autotestOut `"$p\Builds\autotest`""
$pr.WaitForExit(400000) | Out-Null
Add-Type -AssemblyName System.Drawing
$o="$p\Builds\autotest\jpg"; New-Item -ItemType Directory -Force $o | Out-Null
foreach($f in Get-ChildItem "$p\Builds\autotest\*.png"){ $i=[System.Drawing.Image]::FromFile($f.FullName); $b=New-Object System.Drawing.Bitmap 1280,720; $g=[System.Drawing.Graphics]::FromImage($b); $g.InterpolationMode='HighQualityBicubic'; $g.DrawImage($i,0,0,1280,720); $b.Save("$o\$($f.BaseName).jpg",[System.Drawing.Imaging.ImageFormat]::Jpeg); $g.Dispose(); $b.Dispose(); $i.Dispose() }
"fin $(Get-Date)" | Out-File -Append "$p\Builds\estado.txt"
