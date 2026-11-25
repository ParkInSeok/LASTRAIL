$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$out="$b/equipment-equipped-synthesis-landscape-v01"
$src=[Drawing.Bitmap]::new("$b/operation-equipment-landscape-v01/16-operation-equipment.png")
$im=[Drawing.Bitmap]::new("$b/equipment-fidelity-rebuild-v01/51-restored.png")
$g=[Drawing.Graphics]::FromImage($im)
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(10,15,17))
$light=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(250,235,199))
$font=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$rects=@(@(363,241,197,182),@(577,241,195,182),@(142,440,205,183),@(364,439,196,185),@(577,440,195,184),@(142,641,205,184),@(365,641,195,184),@(577,641,195,184),@(577,641,195,184))
$g.FillRectangle($dark,134,230,640,605)
for($i=0;$i -lt 9;$i++){
 $r=$rects[$i];$piece=$src.Clone([Drawing.Rectangle]::new($r[0],$r[1],$r[2],$r[3]),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $x=@(144,362,576)[$i%3];$y=@(240,440,640)[[int][Math]::Floor($i/3)]
 $g.DrawImage($piece,[Drawing.Rectangle]::new($x,$y,198,184));$piece.Dispose()
}
$g.FillRectangle($dark,1275,89,91,35);$g.DrawString('7 / 30',$font,$light,1278,91)
$two=$src.Clone([Drawing.Rectangle]::new(929,545,62,23),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g.FillRectangle($dark,1133,546,74,23);$g.DrawImageUnscaled($two,1139,546);$two.Dispose()
$detail=$src.Clone([Drawing.Rectangle]::new(1016,689,96,28),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g.DrawImageUnscaled($detail,1016,689);$detail.Dispose()
$g.Dispose();$im.Save("$out/59-before.png",[Drawing.Imaging.ImageFormat]::Png)
$im.Dispose();$src.Dispose();$dark.Dispose();$light.Dispose();$font.Dispose()
# The final visual state is already approved: reuse bytes instead of regenerating it.
Copy-Item -LiteralPath "$b/equipment-fidelity-rebuild-v01/51-restored.png" -Destination "$out/59-after.png"
