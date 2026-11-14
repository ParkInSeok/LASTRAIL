$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/equipment-fidelity-rebuild-v01/16-restored.png")
$src=[Drawing.Bitmap]::new("$b/operation-equipment-landscape-v01/16-operation-equipment.png")
$g=[Drawing.Graphics]::FromImage($im);$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(10,15,17));$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(250,235,199))
$font=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
function Crop($x,$y,$w,$h){return $src.Clone([Drawing.Rectangle]::new($x,$y,$w,$h),$src.PixelFormat)}
$cards=@(@(363,241,197,182),@(577,241,195,182),@(142,440,205,183),@(364,439,196,185),@(577,440,195,184),@(142,641,205,184),@(365,641,195,184),@(577,641,195,184),@(577,641,195,184))
$g.FillRectangle($dark,134,230,640,605)
for($i=0;$i -lt 9;$i++){$r=$cards[$i];$p=Crop $r[0] $r[1] $r[2] $r[3];$g.DrawImage($p,[Drawing.Rectangle]::new(@(144,362,576)[$i%3],@(240,440,640)[[int][Math]::Floor($i/3)],198,184));$p.Dispose()}
$g.FillRectangle($dark,1275,89,91,35);$g.DrawString('7 / 30',$font,$ink,1278,91)
$slot=Crop 1080 456 177 120
$g.FillRectangle($dark,860,450,199,129);$g.DrawImageUnscaled($slot,868,456);$g.FillRectangle($dark,881,461,20,29);$g.DrawString('1',$font,$ink,880,460)
$g.DrawImageUnscaled($slot,1080,456)
$power=Crop 170 666 151 102;$armor=Crop 850 601 152 114;$stars=Crop 1014 689 61 27;$badge=Crop 374 246 44 44
$g.DrawImage($power,[Drawing.Rectangle]::new(910,472,105,71));$g.DrawImage($armor,[Drawing.Rectangle]::new(1125,467,105,79))
foreach($x in @(910,1122)){$g.DrawImage($badge,[Drawing.Rectangle]::new($x,466,26,26));$g.DrawImage($stars,[Drawing.Rectangle]::new(($x+20),546,61,27))}
$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(100,235,255),3);$pts=[Drawing.Point[]]@([Drawing.Point]::new(1092,458),[Drawing.Point]::new(1245,458),[Drawing.Point]::new(1255,468),[Drawing.Point]::new(1255,564),[Drawing.Point]::new(1245,574),[Drawing.Point]::new(1092,574),[Drawing.Point]::new(1082,564),[Drawing.Point]::new(1082,468));$g.DrawPolygon($pen,$pts)
$g.Dispose();$im.Save("$PSScriptRoot/73-equipment-slot-equipped.png",[Drawing.Imaging.ImageFormat]::Png)
foreach($v in @($im,$src,$dark,$ink,$font,$slot,$power,$armor,$stars,$badge,$pen)){$v.Dispose()}

