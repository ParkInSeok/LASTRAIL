$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/merchant-soldout-landscape-v01/62-merchant-soldout.png")
$src=[Drawing.Bitmap]::new('D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-merchant-repair-v23-illustration/repair-preview.png')
$g=[Drawing.Graphics]::FromImage($im)
$shade=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(165,0,0,0));$g.FillRectangle($shade,0,0,$im.Width,$im.Height)
$piece=$src.Clone([Drawing.Rectangle]::new(146,478,650,790),$src.PixelFormat);$g.DrawImageUnscaled($piece,511,140)
$path=[Drawing.Drawing2D.GraphicsPath]::new();$path.AddEllipse(770,70,130,130);$g.SetClip($path)
$medal=$src.Clone([Drawing.Rectangle]::new(405,408,130,130),$src.PixelFormat);$g.DrawImageUnscaled($medal,770,70);$g.ResetClip()
# Replace obsolete cube symbol with the approved merchant supply chest.
$texture=$src.Clone([Drawing.Rectangle]::new(251,1074,60,74),$src.PixelFormat);$tb=[Drawing.TextureBrush]::new($texture);$g.FillRectangle($tb,718,735,80,70);$tb.Dispose()
$merchantSource=[Drawing.Bitmap]::new("$b/merchant-soldout-landscape-v01/62-merchant-soldout.png")
$chest=$merchantSource.Clone([Drawing.Rectangle]::new(1118,13,49,38),$merchantSource.PixelFormat);$g.DrawImageUnscaled($chest,734,752)
# Exact preview split, separate from the original approximate painted bar.
$blue=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(0,184,240));$green=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(104,236,47));$empty=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(43,47,48))
$g.FillRectangle($blue,581,617,337,28);$g.FillRectangle($green,918,617,104,28);$g.FillRectangle($empty,1022,617,78,28)
$g.Dispose();$im.Save("$PSScriptRoot/68-merchant-repair-preview.png",[Drawing.Imaging.ImageFormat]::Png)
foreach($v in @($im,$src,$shade,$piece,$path,$medal,$texture,$merchantSource,$chest,$blue,$green,$empty)){$v.Dispose()}


