$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
$enc=[Text.UTF8Encoding]::new($false)
$base='D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-composition-v60-review'
$root='D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-merchant-independent-v61-review'
$stage='D:/CodexData/Home/worktrees/5ca3/LASTRAIL/LocalTemp'
if(Test-Path -LiteralPath $root){throw 'Target exists; inspect before replacing'}
[IO.Directory]::CreateDirectory("$root/assets")|Out-Null
foreach($name in @('merchant.html','data.js','view.js','style.css')){Copy-Item -LiteralPath "$base/$name" -Destination "$root/$name"}
Copy-Item -LiteralPath "$base/assets/healing-potion.png" -Destination "$root/assets/healing-potion.png"
Copy-Item -LiteralPath 'D:/CodexData/Home/generated_images/01a0c3e9-35b6-7010-ae1c-6463f4047997/exec-387b6100-85f9-4397-9ad1-e9ee5cba8966.png' -Destination "$root/assets/merchant-background.png"
Copy-Item -LiteralPath 'D:/CodexData/Home/generated_images/01a0c3e9-35b6-7010-ae1c-6463f4047997/exec-3e3c9853-7d7b-4fef-ac4f-cb01134c7cdb.png' -Destination "$root/assets/train-repair.png"
foreach($pair in @(@('eq61-SCREEN_SPEC.md','SCREEN_SPEC.md'),@('eq61-PROMPTS.md','PROMPTS.md'),@('eq61-index.html','index.html'))){Copy-Item -LiteralPath "$stage/$($pair[0])" -Destination "$root/$($pair[1])"}
$v=[IO.File]::ReadAllText("$root/view.js").Replace('../d01-independent-v01/assets/skill-repair.png','assets/train-repair.png')
[IO.File]::WriteAllText("$root/view.js",$v,$enc)
$css=@'
/* v61 approved v60 composition retained; new merchant art is independently controlled. */
.merchant-scene{background-image:url('assets/merchant-background.png');background-position:center;background-size:cover}
.hide-background .merchant-scene{background-image:none}
.product.service>img{object-fit:contain}
'@
[IO.File]::AppendAllText("$root/style.css",$css,$enc)
$h=[IO.File]::ReadAllText("$root/merchant.html").Replace('상인·보관함 복귀','상인 배경·수리 독립 조합')
$h=$h.Replace('<button data-layer="labels">문구 숨김/표시</button>','<button data-layer="labels">문구 숨김/표시</button><button data-layer="background">배경 숨김/표시</button>')
$h=$h.Replace('승인 원화 재사용, 조합 후보 검토','승인 v60 조합 유지 · 새 배경/수리 아이콘 검토 후보')
[IO.File]::WriteAllText("$root/merchant.html",$h,$enc)
$m=Get-Content -LiteralPath "$base/manifest.json" -Raw|ConvertFrom-Json
$m.version='direction.equipment-merchant-independent-v61.20261002';$m.status='new-background-repair-art-review-candidate'
$m.approval='new two assets PENDING; source v60 composition and potion USER APPROVED 20261002'
$m.screens=@('merchant.html');$m.remaining=@('new background/repair user art review','actual START owner/portrait mapping','real touch drag/synthesis/receipt/transaction','Unity import/runtime integration')
foreach($a in $m.assets){
 if($a.id -eq 'merchant-background'){$a.path='assets/merchant-background.png';$a.status='new independent background candidate derived from approved v12; user art pending'}
 if($a.id -eq 'repair'){$a.path='assets/train-repair.png';$a.status='new independent alpha service icon candidate derived from approved v12; user art pending'}
 if($a.path){$a.sha256=(Get-FileHash -LiteralPath (Join-Path $root $a.path) -Algorithm SHA256).Hash}
}
[IO.File]::WriteAllText("$root/manifest.json",($m|ConvertTo-Json -Depth 20),$enc)
Add-Type -AssemblyName System.Drawing
$im=[Drawing.Bitmap]::new("$root/assets/train-repair.png")
$clear=0;$partial=0;for($y=0;$y -lt $im.Height;$y+=4){for($x=0;$x -lt $im.Width;$x+=4){$a=$im.GetPixel($x,$y).A;if($a -eq 0){$clear++}elseif($a -lt 255){$partial++}}}
if($clear -eq 0){throw 'Repair asset has no real alpha'}
$alpha=@{sampleStride=4;width=$im.Width;height=$im.Height;clearSamples=$clear;partialSamples=$partial}
$im.Dispose()
$bg=[Drawing.Image]::FromFile("$root/assets/merchant-background.png");$alpha.backgroundWidth=$bg.Width;$alpha.backgroundHeight=$bg.Height;$bg.Dispose()
[IO.File]::WriteAllText("$root/assets/alpha-qa.json",($alpha|ConvertTo-Json),$enc)
$qjs=[IO.File]::ReadAllText("$base/verify.cjs").Replace('equipment-composition-v60-review','equipment-merchant-independent-v61-review').Replace("['equipment','inventory','expanded','merchant']","['merchant']")
$extra=@'
if(mode==='merchant'){
    check('new repair asset connected '+width,await page.locator('.product.service>img').first().getAttribute('src').then(s=>s==='assets/train-repair.png'));
    check('new independent background connected '+width,await page.locator('.merchant-scene').evaluate(e=>getComputedStyle(e).backgroundImage.includes('merchant-background.png')));
    await page.locator('[data-layer="background"]').click();
    check('background independently hides '+width,await page.locator('.merchant-scene').evaluate(e=>getComputedStyle(e).backgroundImage==='none'));
    await page.locator('[data-layer="background"]').click();
    await page.locator('[data-layer="icons"]').click();
    check('repair icon independently hides '+width,await page.locator('.product.service>img').first().evaluate(e=>getComputedStyle(e).visibility==='hidden'));
    await page.locator('[data-layer="icons"]').click();
'@
$qjs=$qjs.Replace("if(mode==='merchant'){",$extra)
$qjs=$qjs.Replace("version:'eq60-composition-browser-20261002'","version:'eq61-merchant-browser-20261002'")
[IO.File]::WriteAllText("$root/verify.cjs",$qjs,$enc)
'New merchant assets and candidate composition saved'
& 'C:/Users/dlstj/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe' "$root/verify.cjs"

