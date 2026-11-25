$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$out="$b/equipment-claimed-landscape-v01"
$im=[Drawing.Bitmap]::new("$b/equipment-fidelity-rebuild-v01/16-restored.png")
$src=[Drawing.Bitmap]::new("$b/operation-equipment-landscape-v01/16-operation-equipment.png")
$g=[Drawing.Graphics]::FromImage($im)
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(10,15,17))
$light=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(250,235,199))
$font=[Drawing.Font]::new('Malgun Gothic',22,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$g.FillRectangle($dark,1275,89,91,35);$g.DrawString('54 / 54',$font,$light,1276,92)
# Full-capacity viewport fixture: no empty cell. Not a real itemID/order mapping.
$piece=$src.Clone([Drawing.Rectangle]::new(577,440,195,184),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g.DrawImageUnscaled($piece,577,641)
$piece.Dispose();$g.Dispose();$im.Save("$out/61-equipment-claimed.png",[Drawing.Imaging.ImageFormat]::Png)
$src.Dispose();$im.Dispose();$dark.Dispose();$light.Dispose();$font.Dispose()
