$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/equipment-slot-equipped-landscape-v01/73-equipment-slot-equipped.png")
$src=[Drawing.Bitmap]::new('D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-equipped-detail-v39-illustration/equipped-detail.png')
$panel=$src.Clone([Drawing.Rectangle]::new(60,140,824,1370),$src.PixelFormat)
$pg=[Drawing.Graphics]::FromImage($panel)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(3,23,29));$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(247,237,213))
$font=[Drawing.Font]::new('Malgun Gothic',36,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$pg.FillRectangle($dark,330,650,290,40);$pg.DrawString('레이나 · 슬롯 2',$font,$ink,333,650)
$pg.FillRectangle($dark,314,865,438,57);$pg.DrawString('방어력 +25%',$font,$ink,323,869)
foreach($y in @(976,1112)){$pg.FillRectangle($dark,314,$y,438,77);$pg.DrawString('없음',$font,$ink,323,($y+12))}
$pg.Dispose()
$g=[Drawing.Graphics]::FromImage($im);$shade=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(190,0,0,0));$g.FillRectangle($shade,0,0,$im.Width,$im.Height)
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.DrawImage($panel,[Drawing.Rectangle]::new(546,65,510,850))
$art=[Drawing.Bitmap]::new("$b/operation-equipment-landscape-v01/16-operation-equipment.png")
$card=$art.Clone([Drawing.Rectangle]::new(363,241,197,182),$art.PixelFormat)
$g.FillRectangle($dark,699,171,208,205);$g.DrawImage($card,[Drawing.Rectangle]::new(699,177,208,192))
$g.Dispose();$im.Save("$PSScriptRoot/74-equipment-equipped-detail.png",[Drawing.Imaging.ImageFormat]::Png)
foreach($v in @($im,$src,$panel,$dark,$ink,$font,$shade,$art,$card)){$v.Dispose()}
