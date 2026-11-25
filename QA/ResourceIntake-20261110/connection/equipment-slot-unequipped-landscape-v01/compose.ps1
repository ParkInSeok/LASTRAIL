$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/equipment-slot-equipped-landscape-v01/73-equipment-slot-equipped.png")
$src=[Drawing.Bitmap]::new("$b/operation-equipment-landscape-v01/16-operation-equipment.png")
$g=[Drawing.Graphics]::FromImage($im)
$slot=$src.Clone([Drawing.Rectangle]::new(1078,454,181,124),$src.PixelFormat);$g.DrawImageUnscaled($slot,1078,454)
$armor=$src.Clone([Drawing.Rectangle]::new(363,241,197,182),$src.PixelFormat)
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.DrawImage($armor,[Drawing.Rectangle]::new(362,640,198,184))
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(10,15,17));$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(250,235,199))
$font=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$g.FillRectangle($dark,1275,89,100,40);$g.DrawString('8 / 30',$font,$ink,1278,91)
$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(100,235,255),3)
$pts=[Drawing.Point[]]@([Drawing.Point]::new(372,640),[Drawing.Point]::new(550,640),[Drawing.Point]::new(560,650),[Drawing.Point]::new(560,814),[Drawing.Point]::new(550,824),[Drawing.Point]::new(372,824),[Drawing.Point]::new(362,814),[Drawing.Point]::new(362,650));$g.DrawPolygon($pen,$pts)
$g.Dispose();$im.Save("$PSScriptRoot/75-equipment-slot-unequipped.png",[Drawing.Imaging.ImageFormat]::Png)
foreach($v in @($im,$src,$slot,$armor,$dark,$ink,$font,$pen)){$v.Dispose()}
