$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$im=[Drawing.Bitmap]::new('D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/equipment-fidelity-rebuild-v01/16-restored.png')
$src=[Drawing.Bitmap]::new('D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-empty-slot-v42-illustration/empty-slot-equipped-badge.png')
$panel=$src.Clone([Drawing.Rectangle]::new(28,120,886,1436),$src.PixelFormat)
$pg=[Drawing.Graphics]::FromImage($panel)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(3,23,29));$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(247,237,213))
$font=[Drawing.Font]::new('Malgun Gothic',36,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$pg.FillRectangle($dark,101,208,282,42);$pg.DrawString('레이나 · 슬롯 2',$font,$ink,104,205);$pg.Dispose()
$g=[Drawing.Graphics]::FromImage($im);$shade=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(190,0,0,0));$g.FillRectangle($shade,0,0,$im.Width,$im.Height)
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.DrawImage($panel,[Drawing.Rectangle]::new(535,60,532,862))
$g.Dispose();$im.Save("$PSScriptRoot/71-equipment-empty-slot.png",[Drawing.Imaging.ImageFormat]::Png)
foreach($v in @($im,$src,$panel,$dark,$ink,$font,$shade)){$v.Dispose()}
