$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$out="$b/equipment-pending-ready-landscape-v01"
$im=[Drawing.Bitmap]::new("$b/equipment-pending-landscape-v01/47-equipment-pending.png")
$approved=[Drawing.Bitmap]::new("$b/popup-brand-removal-v01/47-no-brand.png")
$g=[Drawing.Graphics]::FromImage($im)
$piece=$approved.Clone([Drawing.Rectangle]::new(660,0,280,51),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g.DrawImageUnscaled($piece,660,0);$piece.Dispose();$g.Dispose()
$im.Save("$out/60-before.png",[Drawing.Imaging.ImageFormat]::Png)
$g=[Drawing.Graphics]::FromImage($im)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(12,11,8))
$light=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(250,235,199))
$font=[Drawing.Font]::new('Malgun Gothic',28,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$g.FillRectangle($dark,1275,95,108,34);$g.DrawString('53 / 54',$font,$light,1277,94)
$pts=[Drawing.Point[]]@([Drawing.Point]::new(192,278),[Drawing.Point]::new(443,278),[Drawing.Point]::new(461,296),[Drawing.Point]::new(461,565),[Drawing.Point]::new(443,584),[Drawing.Point]::new(192,584),[Drawing.Point]::new(175,565),[Drawing.Point]::new(175,296))
$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(80,0,210,255),8);$g.DrawPolygon($pen,$pts);$pen.Dispose()
$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(170,245,255),2);$g.DrawPolygon($pen,$pts);$pen.Dispose()
$g.Dispose();$im.Save("$out/60-ready.png",[Drawing.Imaging.ImageFormat]::Png)
$im.Dispose();$approved.Dispose();$dark.Dispose();$light.Dispose();$font.Dispose()
