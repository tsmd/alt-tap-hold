param([string]$Output = "$PSScriptRoot\..\Assets\AltTapHold.ico")

Add-Type -AssemblyName System.Drawing
$size = 256
$bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $key = [System.Drawing.RectangleF]::new(25, 42, 206, 172)
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $radius = 44; $diameter = $radius * 2
    $path.AddArc($key.X, $key.Y, $diameter, $diameter, 180, 90); $path.AddArc($key.Right - $diameter, $key.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($key.Right - $diameter, $key.Bottom - $diameter, $diameter, $diameter, 0, 90); $path.AddArc($key.X, $key.Bottom - $diameter, $diameter, $diameter, 90, 90); $path.CloseFigure()
    $graphics.FillPath([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#172033')), $path)
    $graphics.DrawPath([System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#f8fafc'), 10), $path)
    $teal = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#31d6c6'), 16); $teal.StartCap = $teal.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $orange = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#ff9d42'), 16); $orange.StartCap = $orange.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine($teal, 47, 126, 96, 126); $graphics.DrawLine($teal, 81, 111, 96, 126); $graphics.DrawLine($teal, 96, 126, 81, 141)
    $graphics.DrawLine($orange, 209, 126, 160, 126); $graphics.DrawLine($orange, 175, 111, 160, 126); $graphics.DrawLine($orange, 160, 126, 175, 141)
    $graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#f8fafc')), 105, 111, 46, 30)
    # ICO permits a PNG payload. Using it keeps the transparent 256px source intact without an external converter.
    $temporaryPng = [System.IO.Path]::ChangeExtension($Output, '.tmp.png')
    $bitmap.Save($temporaryPng, [System.Drawing.Imaging.ImageFormat]::Png)
    $png = [System.IO.File]::ReadAllBytes($temporaryPng)
    $stream = [System.IO.File]::Open($Output, [System.IO.FileMode]::Create)
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]1)
        $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([UInt16]1); $writer.Write([UInt16]32); $writer.Write([UInt32]$png.Length); $writer.Write([UInt32]22); $writer.Write($png)
    } finally { $writer.Dispose(); Remove-Item -LiteralPath $temporaryPng -Force }
} finally { $graphics.Dispose(); $bitmap.Dispose() }
