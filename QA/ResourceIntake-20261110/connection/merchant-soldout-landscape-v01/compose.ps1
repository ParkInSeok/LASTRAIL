$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/operation-merchant-landscape-v01/17-operation-merchant.png")
$patch=[Drawing.Bitmap]::new("$b/screen-brand-removal-v01/16-no-brand.png")
$g=[Drawing.Graphics]::FromImage($im)
$logo=$patch.Clone([Drawing.Rectangle]::new(650,0,390,66),$patch.PixelFormat)
$g.DrawImageUnscaled($logo,650,0)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(11,17,19))
$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(248,235,204))
$muted=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(166,174,174))
$f=[Drawing.Font]::new('Malgun Gothic',27,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$small=[Drawing.Font]::new('Malgun Gothic',21,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$g.FillRectangle($dark,1299,17,45,31);$g.DrawString('32',$f,$ink,1300,16)
$g.FillRectangle($dark,1555,17,84,31);$g.DrawString('9 / 30',$f,$ink,1555,16)
$g.FillRectangle($dark,628,399,92,25);$g.DrawString('재고 0',$small,$muted,639,397)
$g.FillRectangle($dark,545,476,251,63)
$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(104,116,118),2)
$points=[Drawing.Point[]]@([Drawing.Point]::new(564,484),[Drawing.Point]::new(776,484),[Drawing.Point]::new(787,495),[Drawing.Point]::new(787,520),[Drawing.Point]::new(776,531),[Drawing.Point]::new(564,531),[Drawing.Point]::new(553,520),[Drawing.Point]::new(553,495))
$g.DrawPolygon($pen,$points);$g.DrawString('품절',$f,$muted,640,488)
$g.Dispose();$im.Save("$PSScriptRoot/62-merchant-soldout.png",[Drawing.Imaging.ImageFormat]::Png)
$im.Dispose();$patch.Dispose();$logo.Dispose();$dark.Dispose();$ink.Dispose();$muted.Dispose();$f.Dispose();$small.Dispose();$pen.Dispose()
