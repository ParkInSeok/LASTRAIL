$ErrorActionPreference="Stop"
$env:TEMP="D:/Project/LASTRAIL/LocalTemp"
$env:TMP=$env:TEMP
$env:TMPDIR=$env:TEMP
$source="D:/CodexData/Home/worktrees/5ca3/LASTRAIL/QA/DirectionImpact-20260921/equipment-composition-v60-review"
$dest="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-composition-v60-review"
if(Test-Path -LiteralPath $dest){throw "Target exists; inspect before republishing"}
[IO.Directory]::CreateDirectory("$dest/assets") | Out-Null
Get-ChildItem -LiteralPath $source -File | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $dest $_.Name)}
Copy-Item -LiteralPath "D:/CodexData/Home/generated_images/01a0c3e9-35b6-7010-ae1c-6463f4047997/exec-d7a1ede4-7080-461a-b958-1f50e676c962.png" -Destination "$dest/assets/healing-potion.png"
Add-Type -AssemblyName System.Drawing
$im=[Drawing.Bitmap]::new("$dest/assets/healing-potion.png")
$clear=0;$partial=0;for($y=0;$y -lt $im.Height;$y+=4){for($x=0;$x -lt $im.Width;$x+=4){$a=$im.GetPixel($x,$y).A;if($a -eq 0){$clear++}elseif($a -lt 255){$partial++}}}
if($clear -eq 0){throw "Potion lacks actual alpha"}
@{width=$im.Width;height=$im.Height;sampleStride=4;clearSamples=$clear;partialSamples=$partial;sha256=(Get-FileHash -LiteralPath "$dest/assets/healing-potion.png" -Algorithm SHA256).Hash}|ConvertTo-Json|Set-Content -LiteralPath "$dest/assets/alpha-qa.json" -Encoding utf8
$im.Dispose()
"Saved independent composition candidate and potion alpha evidence"
