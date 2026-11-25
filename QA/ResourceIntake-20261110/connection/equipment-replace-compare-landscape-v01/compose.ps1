$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp';$env:TMP=$env:TEMP;$env:TMPDIR=$env:TEMP
Add-Type -AssemblyName System.Drawing
$b='D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection'
$im=[Drawing.Bitmap]::new("$b/popup-brand-removal-v01/40-no-brand.png")
$art=[Drawing.Bitmap]::new("$b/operation-equipment-landscape-v01/16-operation-equipment.png")
$picker=[Drawing.Bitmap]::new('D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-replace-picker-v51-illustration/replace-picker.png')
$g=[Drawing.Graphics]::FromImage($im);$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(3,20,24));$ink=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(248,235,204));$cyan=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(112,239,250));$blue=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(5,67,112))
$large=[Drawing.Font]::new('Malgun Gothic',31,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel);$medium=[Drawing.Font]::new('Malgun Gothic',26,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel);$small=[Drawing.Font]::new('Malgun Gothic',19,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$g.FillRectangle($dark,705,161,276,39);$g.DrawString('레이나 · 슬롯 2',$large,$ink,710,160)
$card=$art.Clone([Drawing.Rectangle]::new(363,241,197,182),$art.PixelFormat);$g.DrawImage($card,[Drawing.Rectangle]::new(204,286,216,184))
$scope=$picker.Clone([Drawing.Rectangle]::new(356,779,236,251),$picker.PixelFormat);$g.DrawImage($scope,[Drawing.Rectangle]::new(904,286,211,184))
$g.FillRectangle($dark,389,540,340,36);$g.DrawString('방어력 +25%',$medium,$ink,395,540)
$g.FillRectangle($dark,420,738,1050,58)
$g.DrawString('장비 기여: 방어력 -25%p / 공격력 +25%p',$small,$ink,431,736)
$g.DrawString('철벽 2 → 1: 방어력 +10% 해제 · 최대 체력 +6% 유지',$small,$ink,431,756)
$g.DrawString('화력 0 → 1: 공격력 +4% 활성',$small,$cyan,431,776)
$g.FillRectangle($blue,838,839,346,50);$pen=[Drawing.Pen]::new($cyan.Color,2);$g.DrawRectangle($pen,838,839,346,50)
$text='교체 장착';$size=$g.MeasureString($text,$large);$g.DrawString($text,$large,$ink,(1011-$size.Width/2),843)
$g.Dispose();$im.Save("$PSScriptRoot/77-equipment-replace-compare.png",[Drawing.Imaging.ImageFormat]::Png)
foreach($v in @($im,$art,$picker,$dark,$ink,$cyan,$blue,$large,$medium,$small,$card,$scope,$pen)){$v.Dispose()}
