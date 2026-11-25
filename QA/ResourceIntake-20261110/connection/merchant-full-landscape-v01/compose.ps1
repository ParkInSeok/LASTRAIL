$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/operation-merchant-landscape-v01/17-operation-merchant.png")
$patch=[Drawing.Bitmap]::new("$b/screen-brand-removal-v01/16-no-brand.png")
$g=[Drawing.Graphics]::FromImage($im)
$logo=$patch.Clone([Drawing.Rectangle]::new(650,0,390,66),$patch.PixelFormat);$g.DrawImageUnscaled($logo,650,0)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(11,17,19))
$gold=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,194,105))
$muted=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(166,174,174))
$f=[Drawing.Font]::new('Malgun Gothic',27,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$small=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(104,116,118),2)
foreach($x in @(545,921,1298)){
 $y=476;$w=251;$g.FillRectangle($dark,$x,$y,$w,65)
 $pts=[Drawing.Point[]]@([Drawing.Point]::new(($x+18),($y+8)),[Drawing.Point]::new(($x+$w-18),($y+8)),[Drawing.Point]::new(($x+$w-7),($y+19)),[Drawing.Point]::new(($x+$w-7),($y+46)),[Drawing.Point]::new(($x+$w-18),($y+57)),[Drawing.Point]::new(($x+18),($y+57)),[Drawing.Point]::new(($x+7),($y+46)),[Drawing.Point]::new(($x+7),($y+19)))
 $g.DrawPolygon($pen,$pts);$size=$g.MeasureString('구매',$f);$g.DrawString('구매',$f,$muted,($x+($w-$size.Width)/2),($y+12))
}
$g.FillRectangle($dark,1551,15,91,35);$g.DrawString('30 / 30',$small,$gold,1551,17)
$g.FillRectangle($dark,723,105,230,35);$g.DrawString('보관함 가득 참',$small,$gold,729,106)
$g.Dispose();$im.Save("$PSScriptRoot/66-merchant-full.png",[Drawing.Imaging.ImageFormat]::Png)
$im.Dispose();$patch.Dispose();$logo.Dispose();$dark.Dispose();$gold.Dispose();$muted.Dispose();$f.Dispose();$small.Dispose();$pen.Dispose()
