$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/operation-merchant-landscape-v01/17-operation-merchant.png")
$patch=[Drawing.Bitmap]::new("$b/screen-brand-removal-v01/16-no-brand.png")
$g=[Drawing.Graphics]::FromImage($im)
$logo=$patch.Clone([Drawing.Rectangle]::new(650,0,390,66),$patch.PixelFormat);$g.DrawImageUnscaled($logo,650,0)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(11,17,19))
$muted=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(166,174,174))
$f=[Drawing.Font]::new('Malgun Gothic',27,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$small=[Drawing.Font]::new('Malgun Gothic',18,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(104,116,118),2)
foreach($r in @(@(725,766,253,'이용'),@(1287,765,257,'구매'))){
 $x=[int]$r[0];$y=[int]$r[1];$w=[int]$r[2];$label=[string]$r[3];$g.FillRectangle($dark,$x,$y,$w,65)
 $pts=[Drawing.Point[]]@([Drawing.Point]::new(($x+18),($y+8)),[Drawing.Point]::new(($x+$w-18),($y+8)),[Drawing.Point]::new(($x+$w-7),($y+19)),[Drawing.Point]::new(($x+$w-7),($y+46)),[Drawing.Point]::new(($x+$w-18),($y+57)),[Drawing.Point]::new(($x+18),($y+57)),[Drawing.Point]::new(($x+7),($y+46)),[Drawing.Point]::new(($x+7),($y+19)))
 $g.DrawPolygon($pen,$pts);$size=$g.MeasureString($label,$f);$g.DrawString($label,$f,$muted,($x+($w-$size.Width)/2),($y+12))
}
$g.FillRectangle($dark,832,692,179,25);$g.DrawString('최대 내구도',$small,$muted,837,691)
$g.FillRectangle($dark,1395,692,206,25);$g.DrawString('생존 요원 모두 최대 체력',$small,$muted,1397,691)
$g.Dispose();$im.Save("$PSScriptRoot/67-merchant-no-healing.png",[Drawing.Imaging.ImageFormat]::Png)
$im.Dispose();$patch.Dispose();$logo.Dispose();$dark.Dispose();$muted.Dispose();$f.Dispose();$small.Dispose();$pen.Dispose()
