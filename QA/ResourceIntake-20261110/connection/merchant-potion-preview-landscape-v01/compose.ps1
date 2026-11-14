$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/merchant-soldout-landscape-v01/62-merchant-soldout.png")
$src=[Drawing.Bitmap]::new('D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-merchant-potion-v24-illustration/potion-preview.png')
$faces=[Drawing.Bitmap]::new("$b/equipment-fidelity-rebuild-v01/16-restored.png")
$panel=$src.Clone([Drawing.Rectangle]::new(146,478,650,916),$src.PixelFormat)
$pg=[Drawing.Graphics]::FromImage($panel);$pg.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(3,23,29));$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(247,237,213))
$font=[Drawing.Font]::new('Malgun Gothic',25,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$names=@('레이나','카일','이브','세라','닉스');$xs=@(850,978,1098,1220,1340);$ys=@(690,788,884,980,1080)
for($i=0;$i -lt 5;$i++){
 $face=$faces.Clone([Drawing.Rectangle]::new($xs[$i],247,112,110),$faces.PixelFormat)
 $pg.DrawImage($face,[Drawing.Rectangle]::new(42,($ys[$i]-478),98,87));$face.Dispose()
 $pg.FillRectangle($dark,151,($ys[$i]-478+9),92,37);$pg.DrawString($names[$i],$font,$ink,153,($ys[$i]-478+12))
}
$texture=$src.Clone([Drawing.Rectangle]::new(251,1200,60,74),$src.PixelFormat);$tb=[Drawing.TextureBrush]::new($texture);$pg.FillRectangle($tb,210,733,80,70)
$chest=$im.Clone([Drawing.Rectangle]::new(1118,13,49,38),$im.PixelFormat);$pg.DrawImageUnscaled($chest,225,750)
$pg.Dispose()
$g=[Drawing.Graphics]::FromImage($im);$shade=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(165,0,0,0));$g.FillRectangle($shade,0,0,$im.Width,$im.Height)
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($panel,[Drawing.Rectangle]::new(550,112,572,806))
$medal=$src.Clone([Drawing.Rectangle]::new(405,408,130,130),$src.PixelFormat)
$path=[Drawing.Drawing2D.GraphicsPath]::new();$path.AddEllipse(778,50,115,115);$g.SetClip($path);$g.DrawImage($medal,[Drawing.Rectangle]::new(778,50,115,115));$g.ResetClip()
$g.Dispose();$im.Save("$PSScriptRoot/69-merchant-potion-preview.png",[Drawing.Imaging.ImageFormat]::Png)
foreach($v in @($im,$src,$faces,$panel,$dark,$ink,$font,$texture,$tb,$chest,$shade,$medal,$path)){$v.Dispose()}
