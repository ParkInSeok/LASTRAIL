$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/operation-merchant-landscape-v01/17-operation-merchant.png")
$patch=[Drawing.Bitmap]::new("$b/screen-brand-removal-v01/16-no-brand.png")
$g=[Drawing.Graphics]::FromImage($im)
$logo=$patch.Clone([Drawing.Rectangle]::new(650,0,390,66),$patch.PixelFormat);$g.DrawImageUnscaled($logo,650,0)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(11,17,19))
$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(248,235,204))
$font=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$g.FillRectangle($dark,1551,15,91,35);$g.DrawString('29 / 30',$font,$ink,1551,17)
$g.Dispose();$im.Save("$PSScriptRoot/70-merchant-cleared.png",[Drawing.Imaging.ImageFormat]::Png)
foreach($v in @($im,$patch,$logo,$dark,$ink,$font)){$v.Dispose()}
