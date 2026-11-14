$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$im=[Drawing.Bitmap]::new('D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/merchant-soldout-landscape-v01/62-merchant-soldout.png')
$g=[Drawing.Graphics]::FromImage($im)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(11,17,19))
$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(248,235,204))
$muted=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(166,174,174))
$f=[Drawing.Font]::new('Malgun Gothic',27,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$small=[Drawing.Font]::new('Malgun Gothic',21,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(104,116,118),2)
function DisabledButton([int]$x,[int]$y,[int]$w,[string]$label){
 $g.FillRectangle($dark,$x,$y,$w,65)
 $pts=[Drawing.Point[]]@([Drawing.Point]::new(($x+18),($y+8)),[Drawing.Point]::new(($x+$w-18),($y+8)),[Drawing.Point]::new(($x+$w-7),($y+19)),[Drawing.Point]::new(($x+$w-7),($y+46)),[Drawing.Point]::new(($x+$w-18),($y+57)),[Drawing.Point]::new(($x+18),($y+57)),[Drawing.Point]::new(($x+7),($y+46)),[Drawing.Point]::new(($x+7),($y+19)))
 $g.DrawPolygon($pen,$pts)
 $size=$g.MeasureString($label,$f);$g.DrawString($label,$f,$muted,($x+($w-$size.Width)/2),($y+12))
}
$g.FillRectangle($dark,1299,15,48,35);$g.DrawString('12',$f,$ink,1300,16)
$g.FillRectangle($dark,741,691,83,26);$g.DrawString('재고 0',$small,$muted,743,690)
DisabledButton 921 476 251 '구매'
DisabledButton 1298 476 251 '구매'
DisabledButton 725 766 253 '사용 완료'
DisabledButton 1287 765 257 '구매'
$g.Dispose();$im.Save("$PSScriptRoot/63-merchant-repair-used.png",[Drawing.Imaging.ImageFormat]::Png)
$im.Dispose();$dark.Dispose();$ink.Dispose();$muted.Dispose();$f.Dispose();$small.Dispose();$pen.Dispose()
