$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/operation-merchant-landscape-v01/17-operation-merchant.png")
$patch=[Drawing.Bitmap]::new("$b/screen-brand-removal-v01/16-no-brand.png")
$g=[Drawing.Graphics]::FromImage($im)
$logo=$patch.Clone([Drawing.Rectangle]::new(650,0,390,66),$patch.PixelFormat);$g.DrawImageUnscaled($logo,650,0)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(11,17,19))
$cyan=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(112,239,250))
$gray=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(169,166,153))
$small=[Drawing.Font]::new('Malgun Gothic',20,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$large=[Drawing.Font]::new('Malgun Gothic',27,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$strike=[Drawing.Pen]::new($gray.Color,2)
foreach($r in @(@(678,436,8,7),@(1052,436,16,14),@(1426,436,32,28),@(851,727,20,18),@(1416,727,24,21))){
 $x=$r[0];$y=$r[1];$g.FillRectangle($dark,$x,$y,76,32)
 $old=[string]$r[2];$g.DrawString($old,$small,$gray,$x,($y+5));$w=$g.MeasureString($old,$small).Width
 $g.DrawLine($strike,$x,($y+18),($x+$w-3),($y+18))
 $g.DrawString([string]$r[3],$large,$cyan,($x+32),($y-1))
}
$g.FillRectangle($dark,723,105,149,35);$g.DrawString('13% 할인',$large,$cyan,729,104)
$g.Dispose();$im.Save("$PSScriptRoot/65-merchant-discount.png",[Drawing.Imaging.ImageFormat]::Png)
$im.Dispose();$patch.Dispose();$logo.Dispose();$dark.Dispose();$cyan.Dispose();$gray.Dispose();$small.Dispose();$large.Dispose();$strike.Dispose()
