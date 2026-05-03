$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.Windows.Forms

$projectRoot = (Get-Location).Path
$spriteRoot = Join-Path $projectRoot "Assets\_Project\Sprites\UI_Rebuild"
$prefabRoot = Join-Path $projectRoot "Assets\_Project\Prefabs\UIRebuild"
$fontMetaPath = Join-Path $projectRoot "Assets\_Project\Fonts\neodgm.ttf.meta"
$previewRoot = Join-Path $projectRoot "Assets\_Project\Sprites\UI_Rebuild\Preview"

$ImageGuid = "fe87c0e1cc204ed48ad3b37840f39efc"
$TextGuid = "5f7201a12d95ffc409449d95f23cf332"
$OutlineGuid = "e19747de3f5aca642ab2be37e372fb86"
$CanvasScalerGuid = "0cd44c1031e13a943bb63640046fad76"
$GraphicRaycasterGuid = "dc42784cf147c0c48a680349fa168899"
$ButtonGuid = "4e29b1a8efbd4b44bb3f3716e73f07ff"
$ScrollRectGuid = "1aa08ab6e0800fa44ae55d278d1423e3"
$MaskGuid = "31a19414c41e5ae4aae2af33fee712f6"

$PanelTextColor = @{ r = "0.13333334"; g = "0.10980392"; b = "0.07058824"; a = "1" }
$SubtleTextColor = @{ r = "0.3019608"; g = "0.24705882"; b = "0.16862746"; a = "1" }
$AccentTextColor = @{ r = "0.1254902"; g = "0.3019608"; b = "0.17254902"; a = "1" }
$OutlineTextColor = @{ r = "1"; g = "0.972549"; b = "0.909804"; a = "0.75" }
$PreviewBackgroundColor = @{ r = "0.95686275"; g = "0.9254902"; b = "0.85490197"; a = "1" }
$WhiteColor = @{ r = "1"; g = "1"; b = "1"; a = "1" }
$PopupFillColor = @{ r = "0.96862745"; g = "0.9411765"; b = "0.8784314"; a = "1" }
$PopupBackdropColor = @{ r = "0.101960786"; g = "0.08627451"; b = "0.06666667"; a = "0.62" }
$MaskColor = @{ r = "1"; g = "1"; b = "1"; a = "0.01" }

$BorderSpecs = @{
    "UI_Common_MainPanel_Base_L" = @{ x = 0.11; y = 0.14; min = 18; max = 88 }
    "UI_Common_SectionBox_M" = @{ x = 0.10; y = 0.16; min = 14; max = 56 }
    "UI_Common_SummaryBox_S" = @{ x = 0.12; y = 0.18; min = 12; max = 44 }
    "UI_Common_Button_Wide_Normal" = @{ x = 0.13; y = 0.24; min = 12; max = 40 }
    "UI_Common_Button_Wide_Active" = @{ x = 0.13; y = 0.24; min = 12; max = 40 }
    "UI_Common_Button_Wide_Disabled" = @{ x = 0.13; y = 0.24; min = 12; max = 40 }
    "UI_Common_Tab_M_Normal" = @{ x = 0.13; y = 0.22; min = 12; max = 36 }
    "UI_Common_Tab_M_Active" = @{ x = 0.13; y = 0.22; min = 12; max = 36 }
    "UI_Common_Tab_M_Secondary" = @{ x = 0.13; y = 0.22; min = 12; max = 36 }
    "UI_HUD_TopBar_Base" = @{ x = 0.09; y = 0.24; min = 14; max = 60 }
    "UI_HUD_InfoBox_Small" = @{ x = 0.12; y = 0.22; min = 10; max = 32 }
    "UI_BottomNav_Base" = @{ x = 0.09; y = 0.26; min = 14; max = 68 }
    "UI_BottomNav_Tab_Normal" = @{ x = 0.12; y = 0.24; min = 12; max = 36 }
    "UI_BottomNav_Tab_Active" = @{ x = 0.12; y = 0.24; min = 12; max = 36 }
    "UI_Economy_SummaryBox" = @{ x = 0.11; y = 0.18; min = 12; max = 44 }
    "UI_Economy_DualInfoBox" = @{ x = 0.10; y = 0.16; min = 14; max = 54 }
    "UI_Economy_DetailBox" = @{ x = 0.10; y = 0.14; min = 16; max = 64 }
    "UI_Review_SummaryBox" = @{ x = 0.11; y = 0.18; min = 12; max = 44 }
    "UI_Review_ListBox" = @{ x = 0.10; y = 0.15; min = 14; max = 60 }
    "UI_Review_EventLogBox" = @{ x = 0.10; y = 0.15; min = 14; max = 60 }
    "UI_Review_EmptyStateBox" = @{ x = 0.10; y = 0.16; min = 14; max = 48 }
    "UI_Popup_Base_Large" = @{ x = 0.10; y = 0.14; min = 18; max = 78 }
    "UI_Popup_Header" = @{ x = 0.09; y = 0.24; min = 12; max = 42 }
    "UI_Popup_ListRow" = @{ x = 0.10; y = 0.22; min = 10; max = 32 }
}

$BorderAssetNames = @(
    "UI_Common_MainPanel_Base_L",
    "UI_Common_SectionBox_M",
    "UI_Common_SummaryBox_S",
    "UI_Common_Button_Wide_Normal",
    "UI_Common_Button_Wide_Active",
    "UI_Common_Button_Wide_Disabled",
    "UI_Common_Tab_M_Normal",
    "UI_Common_Tab_M_Active",
    "UI_Common_Tab_M_Secondary",
    "UI_HUD_TopBar_Base",
    "UI_HUD_InfoBox_Small",
    "UI_BottomNav_Base",
    "UI_BottomNav_Tab_Normal",
    "UI_BottomNav_Tab_Active",
    "UI_Economy_SummaryBox",
    "UI_Economy_DualInfoBox",
    "UI_Economy_DetailBox",
    "UI_Review_SummaryBox",
    "UI_Review_ListBox",
    "UI_Review_EventLogBox",
    "UI_Review_EmptyStateBox",
    "UI_Popup_Base_Large",
    "UI_Popup_Header",
    "UI_Popup_ListRow"
)

$script:fontGuid = ((Get-Content $fontMetaPath | Select-String "^guid:\s+([0-9a-f]+)$").Matches[0].Groups[1].Value)
$script:nextId = [int64]1000000000000000000
$script:modifiedSpriteCount = 0
$script:spriteGuids = @{}
$script:prefabGuids = @{}
$script:spriteBitmapCache = @{}
$script:previewFontCollection = New-Object System.Drawing.Text.PrivateFontCollection
$script:previewFontLoaded = $false

function New-DetGuid([string]$seed) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($seed)
        $hash = $sha.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hash[0..15])).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Get-ExistingGuid([string]$metaPath, [string]$seed) {
    if (Test-Path $metaPath) {
        $match = (Get-Content $metaPath | Select-String "^guid:\s+([0-9a-f]+)$").Matches
        if ($match.Count -gt 0) {
            return $match[0].Groups[1].Value
        }
    }
    return (New-DetGuid $seed)
}

function Get-InternalId([string]$seed) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("$seed|internal")
        $hash = $sha.ComputeHash($bytes)
        return [BitConverter]::ToInt64($hash, 0)
    }
    finally {
        $sha.Dispose()
    }
}

function Get-SpriteId([string]$seed) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("$seed|sprite")
        $hash = $sha.ComputeHash($bytes)
        $head = ([System.BitConverter]::ToString($hash[0..7])).Replace("-", "").ToLowerInvariant()
        return "$head" + "0800000000000000"
    }
    finally {
        $sha.Dispose()
    }
}

function Get-RelativeAssetPath([string]$fullPath) {
    return $fullPath.Substring($projectRoot.Length + 1).Replace("\", "/")
}

function Get-PngSize([string]$path) {
    $img = [System.Drawing.Image]::FromFile($path)
    try {
        return @{ Width = $img.Width; Height = $img.Height }
    }
    finally {
        $img.Dispose()
    }
}

function Get-Border([string]$assetName, [int]$width, [int]$height) {
    if (-not $BorderSpecs.ContainsKey($assetName)) {
        return @{ left = 0; bottom = 0; right = 0; top = 0 }
    }

    $spec = $BorderSpecs[$assetName]
    $horizontal = [Math]::Round($width * $spec.x)
    $vertical = [Math]::Round($height * $spec.y)
    $horizontal = [Math]::Max($spec.min, [Math]::Min($horizontal, [Math]::Min($spec.max, [Math]::Floor($width / 3))))
    $vertical = [Math]::Max($spec.min, [Math]::Min($vertical, [Math]::Min($spec.max, [Math]::Floor($height / 3))))
    return @{ left = [int]$horizontal; bottom = [int]$vertical; right = [int]$horizontal; top = [int]$vertical }
}

function Write-SpriteMeta([string]$pngPath) {
    $assetPath = Get-RelativeAssetPath $pngPath
    $metaPath = "$pngPath.meta"
    $guid = Get-ExistingGuid $metaPath $assetPath
    $size = Get-PngSize $pngPath
    $assetName = [System.IO.Path]::GetFileNameWithoutExtension($pngPath)
    $border = Get-Border $assetName $size.Width $size.Height
    $internalId = Get-InternalId $assetPath
    $spriteId = Get-SpriteId $assetPath
    $spriteName = "${assetName}_0"

    $content = @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable:
  - first:
      213: $internalId
    second: $spriteName
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: $($border.left), y: $($border.bottom), z: $($border.right), w: $($border.top)}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: iPhone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: $spriteName
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: $($size.Width)
        height: $($size.Height)
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: $($border.left), y: $($border.bottom), z: $($border.right), w: $($border.top)}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $spriteId
      internalID: $internalId
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
      ${spriteName}: $internalId
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

    Set-Content -LiteralPath $metaPath -Value $content -Encoding utf8
    $script:spriteGuids[$assetPath.Replace("Assets/_Project/Sprites/UI_Rebuild/", "").Replace(".png", "")] = $guid
    $script:modifiedSpriteCount++
}

function Load-SpriteGuids() {
    Get-ChildItem -Path $spriteRoot -Recurse -Filter *.png | ForEach-Object {
        Write-SpriteMeta $_.FullName
    }
}

function Get-SpriteGuid([string]$key) {
    if (-not $script:spriteGuids.ContainsKey($key)) {
        throw "Missing sprite guid for $key"
    }
    return $script:spriteGuids[$key]
}

function New-Id() {
    $script:nextId++
    return [string]$script:nextId
}

function New-Prefab([string]$name, [double]$width, [double]$height, [bool]$canvasRoot = $true) {
    $children = New-Object System.Collections.ArrayList
    $nodes = New-Object System.Collections.ArrayList
    $root = [ordered]@{
        Kind = if ($canvasRoot) { "CanvasRoot" } else { "Root" }
        Name = $name
        X = 0
        Y = 0
        Width = $width
        Height = $height
        ScaleX = 1
        ScaleY = 1
        Parent = $null
        Children = $children
    }
    [void]$nodes.Add($root)
    return [ordered]@{
        Name = $name
        Width = $width
        Height = $height
        Root = $root
        Nodes = $nodes
    }
}

function Add-Node($prefab, $node) {
    [void]$prefab.Nodes.Add($node)
    [void]$node.Parent.Children.Add($node)
    return $node
}

function Add-Container($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$width, [double]$height, [double]$scaleX = 1, [double]$scaleY = 1) {
    $node = [ordered]@{
        Kind = "Container"
        Name = $name
        X = $x
        Y = $y
        Width = $width
        Height = $height
        ScaleX = $scaleX
        ScaleY = $scaleY
        Parent = $parent
        Children = (New-Object System.Collections.ArrayList)
    }
    return (Add-Node $prefab $node)
}

function Add-ScrollRoot($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$width, [double]$height, [double]$scrollSensitivity = 26) {
    $node = [ordered]@{
        Kind = "ScrollRoot"
        Name = $name
        X = $x
        Y = $y
        Width = $width
        Height = $height
        ScaleX = 1
        ScaleY = 1
        Parent = $parent
        Children = (New-Object System.Collections.ArrayList)
        ScrollSensitivity = $scrollSensitivity
        ViewportNode = $null
        ContentNode = $null
    }
    return (Add-Node $prefab $node)
}

function Set-ScrollContent($scrollRoot, $viewportNode, $contentNode) {
    $scrollRoot.ViewportNode = $viewportNode
    $scrollRoot.ContentNode = $contentNode
}

function Add-Image($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$width, [double]$height, [string]$spriteKey, [int]$imageType = 0, [hashtable]$color = $null, [bool]$preserveAspect = $false, [double]$scaleX = 1, [double]$scaleY = 1, [bool]$isButton = $false, [bool]$raycastTarget = $false, [bool]$useMask = $false, [bool]$showMaskGraphic = $false) {
    $node = [ordered]@{
        Kind = "Image"
        Name = $name
        X = $x
        Y = $y
        Width = $width
        Height = $height
        ScaleX = $scaleX
        ScaleY = $scaleY
        Parent = $parent
        Children = (New-Object System.Collections.ArrayList)
        SpriteKey = $spriteKey
        ImageType = $imageType
        Color = if ($null -ne $color) { $color } else { $WhiteColor }
        PreserveAspect = $preserveAspect
        IsButton = $isButton
        RaycastTarget = $(if ($isButton) { $true } else { $raycastTarget })
        UseMask = $useMask
        ShowMaskGraphic = $showMaskGraphic
    }
    return (Add-Node $prefab $node)
}

function Add-Text($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$width, [double]$height, [string]$text, [int]$fontSize, [string]$alignment = "MiddleLeft", [hashtable]$color = $null, [hashtable]$outline = $null) {
    $node = [ordered]@{
        Kind = "Text"
        Name = $name
        X = $x
        Y = $y
        Width = $width
        Height = $height
        ScaleX = 1
        ScaleY = 1
        Parent = $parent
        Children = (New-Object System.Collections.ArrayList)
        Text = $text
        FontSize = $fontSize
        Alignment = $alignment
        Color = if ($null -ne $color) { $color } else { $PanelTextColor }
        Outline = if ($null -ne $outline) { $outline } else { $OutlineTextColor }
    }
    return (Add-Node $prefab $node)
}

function Color-ToString($c) {
    return "{r: $($c.r), g: $($c.g), b: $($c.b), a: $($c.a)}"
}

function Escape-Text([string]$text) {
    $text = $text.Replace("\", "\\").Replace('"', '\"')
    return $text.Replace("`r`n", "\n").Replace("`n", "\n")
}

function Get-AlignmentValue([string]$name) {
    switch ($name) {
        "UpperLeft" { return 0 }
        "UpperCenter" { return 1 }
        "UpperRight" { return 2 }
        "MiddleLeft" { return 3 }
        "MiddleCenter" { return 4 }
        "MiddleRight" { return 5 }
        "LowerLeft" { return 6 }
        "LowerCenter" { return 7 }
        "LowerRight" { return 8 }
        default { return 3 }
    }
}

function Ensure-PreviewFontLoaded() {
    if ($script:previewFontLoaded) { return }
    $fontPath = Join-Path $projectRoot "Assets\_Project\Fonts\neodgm.ttf"
    if (Test-Path $fontPath) {
        $script:previewFontCollection.AddFontFile($fontPath)
    }
    $script:previewFontLoaded = $true
}

function Get-PreviewFont([float]$size) {
    Ensure-PreviewFontLoaded
    if ($script:previewFontCollection.Families.Count -gt 0) {
        return New-Object System.Drawing.Font($script:previewFontCollection.Families[0], $size, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
    }
    return New-Object System.Drawing.Font("Arial", $size, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
}

function Get-DrawingColor($c) {
    $a = [int]([double]$c.a * 255)
    $r = [int]([double]$c.r * 255)
    $g = [int]([double]$c.g * 255)
    $b = [int]([double]$c.b * 255)
    return [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Get-SpriteBitmap([string]$spriteKey) {
    if ([string]::IsNullOrWhiteSpace($spriteKey)) { return $null }
    if ($script:spriteBitmapCache.ContainsKey($spriteKey)) {
        return $script:spriteBitmapCache[$spriteKey]
    }
    $path = Join-Path $spriteRoot ($spriteKey.Replace("/", "\") + ".png")
    if (-not (Test-Path $path)) { return $null }
    $bitmap = New-Object System.Drawing.Bitmap($path)
    $script:spriteBitmapCache[$spriteKey] = $bitmap
    return $bitmap
}

function Get-StringFormat([string]$alignment) {
    $format = New-Object System.Drawing.StringFormat
    switch ($alignment) {
        "UpperLeft" {
            $format.Alignment = [System.Drawing.StringAlignment]::Near
            $format.LineAlignment = [System.Drawing.StringAlignment]::Near
        }
        "UpperCenter" {
            $format.Alignment = [System.Drawing.StringAlignment]::Center
            $format.LineAlignment = [System.Drawing.StringAlignment]::Near
        }
        "UpperRight" {
            $format.Alignment = [System.Drawing.StringAlignment]::Far
            $format.LineAlignment = [System.Drawing.StringAlignment]::Near
        }
        "MiddleCenter" {
            $format.Alignment = [System.Drawing.StringAlignment]::Center
            $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        }
        "MiddleRight" {
            $format.Alignment = [System.Drawing.StringAlignment]::Far
            $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        }
        "LowerLeft" {
            $format.Alignment = [System.Drawing.StringAlignment]::Near
            $format.LineAlignment = [System.Drawing.StringAlignment]::Far
        }
        "LowerCenter" {
            $format.Alignment = [System.Drawing.StringAlignment]::Center
            $format.LineAlignment = [System.Drawing.StringAlignment]::Far
        }
        "LowerRight" {
            $format.Alignment = [System.Drawing.StringAlignment]::Far
            $format.LineAlignment = [System.Drawing.StringAlignment]::Far
        }
        default {
            $format.Alignment = [System.Drawing.StringAlignment]::Near
            $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        }
    }
    return $format
}

function Draw-TextBlock($graphics, $node, [float]$x, [float]$y, [float]$width, [float]$height) {
    $rect = New-Object System.Drawing.RectangleF($x, $y, $width, $height)
    $font = Get-PreviewFont([float]$node.FontSize)
    $format = Get-StringFormat $node.Alignment
    $textValue = $node.Text -replace "\\n", "`n"
    if ($textValue -notmatch "`n") {
        $format.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap
    }
    $outlineColor = Get-DrawingColor $node.Outline
    $fillColor = Get-DrawingColor $node.Color
    $outlineBrush = New-Object System.Drawing.SolidBrush($outlineColor)
    $fillBrush = New-Object System.Drawing.SolidBrush($fillColor)
    try {
        foreach ($offset in @(@(-1,0), @(1,0), @(0,-1), @(0,1))) {
            $shadowRect = New-Object System.Drawing.RectangleF(($x + $offset[0]), ($y + $offset[1]), $width, $height)
            $graphics.DrawString($textValue, $font, $outlineBrush, $shadowRect, $format)
        }
        $graphics.DrawString($textValue, $font, $fillBrush, $rect, $format)
    }
    finally {
        $font.Dispose()
        $format.Dispose()
        $outlineBrush.Dispose()
        $fillBrush.Dispose()
    }
}

function Render-Node($graphics, $node, [float]$offsetX, [float]$offsetY, [float]$scaleX, [float]$scaleY) {
    $nodeScaleX = $scaleX * [float]$node.ScaleX
    $nodeScaleY = $scaleY * [float]$node.ScaleY
    $x = $offsetX + ([float]$node.X * $scaleX)
    $y = $offsetY + ([float]$node.Y * $scaleY)
    $width = [float]$node.Width * $nodeScaleX
    $height = [float]$node.Height * $nodeScaleY

    $clipState = $null
    if ($node.Kind -eq "Image") {
        $bitmap = Get-SpriteBitmap $node.SpriteKey
        if ($null -ne $bitmap) {
            $dest = New-Object System.Drawing.RectangleF($x, $y, $width, $height)
            $graphics.DrawImage($bitmap, $dest)
        }
        elseif ($node.Color) {
            $brush = New-Object System.Drawing.SolidBrush((Get-DrawingColor $node.Color))
            try { $graphics.FillRectangle($brush, $x, $y, $width, $height) } finally { $brush.Dispose() }
        }
        if ($node.UseMask) {
            $clipState = $graphics.Save()
            $graphics.SetClip((New-Object System.Drawing.RectangleF($x, $y, $width, $height)))
        }
    }
    elseif ($node.Kind -eq "Text") {
        Draw-TextBlock $graphics $node $x $y $width $height
    }

    foreach ($child in $node.Children) {
        Render-Node $graphics $child $x $y $nodeScaleX $nodeScaleY
    }

    if ($null -ne $clipState) {
        $graphics.Restore($clipState)
    }
}

function Render-PreviewImage($prefab, [string]$name, [int]$targetWidth, [int]$targetHeight) {
    if (-not (Test-Path $previewRoot)) {
        New-Item -ItemType Directory -Force -Path $previewRoot | Out-Null
    }
    $bitmap = New-Object System.Drawing.Bitmap($targetWidth, $targetHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit
        $scaleX = $targetWidth / [float]$prefab.Width
        $scaleY = $targetHeight / [float]$prefab.Height
        Render-Node $graphics $prefab.Root 0 0 $scaleX $scaleY
    }
    finally {
        $graphics.Dispose()
    }
    $path = Join-Path $previewRoot ("$name.png")
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    return $path
}

function Assign-Ids($prefab) {
    foreach ($node in $prefab.Nodes) {
        $node.GoId = New-Id
        $node.RectId = New-Id
        switch ($node.Kind) {
            "CanvasRoot" {
                $node.CanvasId = New-Id
                $node.CanvasScalerId = New-Id
                $node.RaycasterId = New-Id
            }
            "Image" {
                $node.CanvasRendererId = New-Id
                $node.ImageId = New-Id
                if ($node.IsButton) {
                    $node.ButtonId = New-Id
                }
                if ($node.UseMask) {
                    $node.MaskId = New-Id
                }
            }
            "Text" {
                $node.CanvasRendererId = New-Id
                $node.TextId = New-Id
                $node.OutlineId = New-Id
            }
            "ScrollRoot" {
                $node.ScrollRectId = New-Id
            }
        }
    }
}

function Emit-GameObject($node) {
    $components = @("- component: {fileID: $($node.RectId)}")
    if ($node.Kind -eq "CanvasRoot") {
        $components += "- component: {fileID: $($node.CanvasId)}"
        $components += "- component: {fileID: $($node.CanvasScalerId)}"
        $components += "- component: {fileID: $($node.RaycasterId)}"
    }
    elseif ($node.Kind -eq "Image") {
        $components += "- component: {fileID: $($node.CanvasRendererId)}"
        $components += "- component: {fileID: $($node.ImageId)}"
        if ($node.UseMask) {
            $components += "- component: {fileID: $($node.MaskId)}"
        }
        if ($node.IsButton) {
            $components += "- component: {fileID: $($node.ButtonId)}"
        }
    }
    elseif ($node.Kind -eq "Text") {
        $components += "- component: {fileID: $($node.CanvasRendererId)}"
        $components += "- component: {fileID: $($node.TextId)}"
        $components += "- component: {fileID: $($node.OutlineId)}"
    }
    elseif ($node.Kind -eq "ScrollRoot") {
        $components += "- component: {fileID: $($node.ScrollRectId)}"
    }

    $componentBlock = ($components | ForEach-Object { "  $_" }) -join "`n"
    return @"
--- !u!1 &$($node.GoId)
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
$componentBlock
  m_Layer: 0
  m_Name: $($node.Name)
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"@
}

function Emit-RectTransform($node) {
    $children = if ($node.Children.Count -eq 0) {
        "[]"
    }
    else {
        "`n" + (($node.Children | ForEach-Object { "  - {fileID: $($_.RectId)}" }) -join "`n")
    }

    if ($null -eq $node.Parent) {
        $anchorMin = "{x: 0.5, y: 0.5}"
        $anchorMax = "{x: 0.5, y: 0.5}"
        $anchoredPosition = "{x: 0, y: 0}"
        $pivot = "{x: 0.5, y: 0.5}"
        $father = "{fileID: 0}"
    }
    else {
        $anchorMin = "{x: 0, y: 1}"
        $anchorMax = "{x: 0, y: 1}"
        $anchoredPosition = "{x: $($node.X), y: -$($node.Y)}"
        $pivot = "{x: 0, y: 1}"
        $father = "{fileID: $($node.Parent.RectId)}"
    }

    return @"
--- !u!224 &$($node.RectId)
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: $($node.ScaleX), y: $($node.ScaleY), z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: $children
  m_Father: $father
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: $anchorMin
  m_AnchorMax: $anchorMax
  m_AnchoredPosition: $anchoredPosition
  m_SizeDelta: {x: $($node.Width), y: $($node.Height)}
  m_Pivot: $pivot
"@
}

function Emit-CanvasRootComponents($node, [double]$refWidth, [double]$refHeight) {
    return @"
--- !u!223 &$($node.CanvasId)
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 1
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 0
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0
--- !u!114 &$($node.CanvasScalerId)
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $CanvasScalerGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.CanvasScaler
  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {x: $refWidth, y: $refHeight}
  m_ScreenMatchMode: 0
  m_MatchWidthOrHeight: 0.5
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
--- !u!114 &$($node.RaycasterId)
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $GraphicRaycasterGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GraphicRaycaster
  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
"@
}

function Emit-CanvasRenderer($id, $goId) {
    return @"
--- !u!222 &$id
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $goId}
  m_CullTransparentMesh: 1
"@
}

function Emit-Image($node) {
    $spriteRef = if ([string]::IsNullOrWhiteSpace($node.SpriteKey)) {
        "{fileID: 0}"
    }
    else {
        "{fileID: 21300000, guid: $(Get-SpriteGuid $node.SpriteKey), type: 3}"
    }

    return @"
--- !u!114 &$($node.ImageId)
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $ImageGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: $(Color-ToString $node.Color)
  m_RaycastTarget: $(if ($node.RaycastTarget) { 1 } else { 0 })
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: $spriteRef
  m_Type: $($node.ImageType)
  m_PreserveAspect: $(if ($node.PreserveAspect) { 1 } else { 0 })
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
"@
}

function Emit-Mask($node) {
    return @"
--- !u!114 &$($node.MaskId)
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $MaskGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Mask
  m_ShowMaskGraphic: $(if ($node.ShowMaskGraphic) { 1 } else { 0 })
"@
}

function Emit-Button($node) {
    return @"
--- !u!114 &$($node.ButtonId)
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $ButtonGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: $($node.ImageId)}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
"@
}

function Emit-ScrollRect($node) {
    if ($null -eq $node.ViewportNode -or $null -eq $node.ContentNode) {
        throw "ScrollRoot missing viewport/content: $($node.Name)"
    }

    return @"
--- !u!114 &$($node.ScrollRectId)
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $ScrollRectGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ScrollRect
  m_Content: {fileID: $($node.ContentNode.RectId)}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 2
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: $($node.ScrollSensitivity)
  m_Viewport: {fileID: $($node.ViewportNode.RectId)}
  m_HorizontalScrollbar: {fileID: 0}
  m_VerticalScrollbar: {fileID: 0}
  m_HorizontalScrollbarVisibility: 0
  m_VerticalScrollbarVisibility: 0
  m_HorizontalScrollbarSpacing: 0
  m_VerticalScrollbarSpacing: 0
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []
"@
}

function Emit-Text($node) {
    $alignment = Get-AlignmentValue $node.Alignment
    $escaped = Escape-Text $node.Text
    return @"
--- !u!114 &$($node.TextId)
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $TextGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text
  m_Material: {fileID: 0}
  m_Color: $(Color-ToString $node.Color)
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: $script:fontGuid, type: 3}
    m_FontSize: $($node.FontSize)
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 60
    m_Alignment: $alignment
    m_AlignByGeometry: 1
    m_RichText: 0
    m_HorizontalOverflow: 1
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: "$escaped"
"@
}

function Emit-Outline($node) {
    return @"
--- !u!114 &$($node.OutlineId)
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($node.GoId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $OutlineGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Outline
  m_EffectColor: $(Color-ToString $node.Outline)
  m_EffectDistance: {x: 1, y: -1}
  m_UseGraphicAlpha: 1
"@
}

function Emit-Prefab($prefab, [string]$path) {
    Assign-Ids $prefab
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("%YAML 1.1")
    [void]$sb.AppendLine("%TAG !u! tag:unity3d.com,2011:")

    foreach ($node in $prefab.Nodes) {
        [void]$sb.AppendLine((Emit-GameObject $node))
        [void]$sb.AppendLine((Emit-RectTransform $node))
        if ($node.Kind -eq "CanvasRoot") {
            [void]$sb.AppendLine((Emit-CanvasRootComponents $node $prefab.Width $prefab.Height))
        }
        elseif ($node.Kind -eq "Image") {
            [void]$sb.AppendLine((Emit-CanvasRenderer $node.CanvasRendererId $node.GoId))
            [void]$sb.AppendLine((Emit-Image $node))
            if ($node.UseMask) {
                [void]$sb.AppendLine((Emit-Mask $node))
            }
            if ($node.IsButton) {
                [void]$sb.AppendLine((Emit-Button $node))
            }
        }
        elseif ($node.Kind -eq "Text") {
            [void]$sb.AppendLine((Emit-CanvasRenderer $node.CanvasRendererId $node.GoId))
            [void]$sb.AppendLine((Emit-Text $node))
            [void]$sb.AppendLine((Emit-Outline $node))
        }
        elseif ($node.Kind -eq "ScrollRoot") {
            [void]$sb.AppendLine((Emit-ScrollRect $node))
        }
    }

    $dir = Split-Path -Parent $path
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    Set-Content -LiteralPath $path -Value $sb.ToString() -Encoding utf8
}

function Ensure-PrefabMeta([string]$prefabPath) {
    $metaPath = "$prefabPath.meta"
    if (Test-Path $metaPath) { return }
    $assetPath = Get-RelativeAssetPath $prefabPath
    $guid = New-DetGuid $assetPath
    $content = @"
fileFormatVersion: 2
guid: $guid
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    Set-Content -LiteralPath $metaPath -Value $content -Encoding utf8
}

function Add-ScreenButton($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$w, [double]$h, [string]$sprite, [string]$label, [int]$fontSize) {
    $button = Add-Image $prefab $parent $name $x $y $w $h $sprite 1 $null $false 1 1 $true
    Add-Text $prefab $button "${name}_Label" 0 0 $w $h $label $fontSize "MiddleCenter" $PanelTextColor | Out-Null
    return $button
}

function Add-StaticSpriteLabel($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$w, [double]$h, [string]$sprite, [string]$label, [int]$fontSize) {
    $node = Add-Image $prefab $parent $name $x $y $w $h $sprite 1
    Add-Text $prefab $node "${name}_Label" 0 0 $w $h $label $fontSize "MiddleCenter" $PanelTextColor | Out-Null
    return $node
}

function Add-IconButton($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$w, [double]$h, [string]$sprite) {
    return (Add-Image $prefab $parent $name $x $y $w $h $sprite 0 $null $true 1 1 $true)
}

function Add-PreviewCard($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$w, [double]$h, [string]$label) {
    $card = Add-Image $prefab $parent $name $x $y $w $h "Common/UI_Common_SectionBox_M" 1
    Add-Text $prefab $card "${name}_Label" 24 22 ($w - 48) 26 $label 24 "MiddleLeft" $PanelTextColor | Out-Null
    return $card
}

function Add-TitleVisual($prefab, $root) {
    Add-Image $prefab $root "Background" 0 0 1080 1920 $null 0 $PreviewBackgroundColor | Out-Null
    $board = Add-Image $prefab $root "TitleBoard" 110 130 860 1450 "Common/UI_Common_MainPanel_Base_L" 1
    $logoFrame = Add-Image $prefab $board "LogoFrame" 90 90 680 250 "Common/UI_Common_SectionBox_M" 1
    Add-Text $prefab $logoFrame "GameTitle" 0 52 680 80 "GYM MANAGER" 58 "MiddleCenter" $PanelTextColor | Out-Null
    Add-Text $prefab $logoFrame "SubTitle" 0 146 680 32 "Neighborhood management tycoon" 22 "MiddleCenter" $SubtleTextColor | Out-Null
    Add-ScreenButton $prefab $board "ContinueButton" 180 390 500 112 "Common/UI_Common_Button_Wide_Active" "Continue" 34 | Out-Null
    Add-ScreenButton $prefab $board "NewGameButton" 180 530 500 112 "Common/UI_Common_Button_Wide_Normal" "New Game" 34 | Out-Null
    Add-Text $prefab $board "SlotsLabel" 112 714 320 30 "Save Slots" 28 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $board "SlotsHint" 112 748 420 24 "Auto save before month end" 18 "MiddleLeft" $SubtleTextColor | Out-Null

    $slotNames = @("Base Gym", "Station Branch", "Premium Club")
    $slotMeta = @("Lot 16x16  |  Members 124", "Lot 8x8  |  Ready to start", "Lot 32x32  |  Expansion candidate")
    for ($i = 0; $i -lt 3; $i++) {
        $slot = Add-Image $prefab $board "Slot_$($i+1)" 110 (810 + ($i * 174)) 640 144 "Common/UI_Common_SectionBox_M" 1
        $slotTitle = "Slot $($i + 1)  |  $($slotNames[$i])"
        $slotState = if ($i -eq 0) { "Live" } else { "Standby" }
        Add-Text $prefab $slot "SlotTitle_$($i+1)" 26 24 420 28 $slotTitle 22 "MiddleLeft" $PanelTextColor | Out-Null
        Add-Text $prefab $slot "SlotMeta_$($i+1)" 26 60 380 22 $slotMeta[$i] 18 "MiddleLeft" $SubtleTextColor | Out-Null
        $state = Add-Image $prefab $slot "SlotState_$($i+1)" 432 28 166 84 "Common/UI_Common_SummaryBox_S" 1
        Add-Text $prefab $state "StateLabel_$($i+1)" 0 12 166 20 "State" 14 "MiddleCenter" $SubtleTextColor | Out-Null
        Add-Text $prefab $state "StateValue_$($i+1)" 0 36 166 26 $slotState 18 "MiddleCenter" $AccentTextColor | Out-Null
    }
}

function Add-TopHudVisual($prefab, $root) {
    $bar = Add-Image $prefab $root "TopBar" 0 0 1080 168 "HUD/UI_HUD_TopBar_Base" 1
    $labels = @(
        @{ name = "Date"; x = 24; w = 208; label = "2026.04"; value = "Week 4 | Day 2" }
        @{ name = "Cash"; x = 248; w = 222; label = "Cash"; value = "KRW 128,450" }
        @{ name = "Star"; x = 486; w = 190; label = "Star Coin"; value = "34" }
        @{ name = "Speed"; x = 692; w = 136; label = "Speed"; value = "2x" }
    )
    foreach ($item in $labels) {
        $box = Add-Image $prefab $bar "$($item.name)Box" $item.x 18 $item.w 58 "HUD/UI_HUD_InfoBox_Small" 1
        $icon = Add-Image $prefab $box "$($item.name)Icon" 8 8 42 42 "HUD/UI_HUD_IconSlot_Frame" 0
        Add-Text $prefab $icon "$($item.name)Glyph" 0 0 42 42 $item.label.Substring(0,1) 18 "MiddleCenter" $AccentTextColor | Out-Null
        Add-Text $prefab $box "$($item.name)Label" 58 6 ($item.w - 68) 18 $item.label 14 "MiddleLeft" $SubtleTextColor | Out-Null
        Add-Text $prefab $box "$($item.name)Value" 58 24 ($item.w - 68) 24 $item.value 18 "MiddleLeft" $PanelTextColor | Out-Null
    }

    $btns = @(
        @{ name = "Staff"; x = 842; label = "Staff"; sprite = "HUD/UI_HUD_Button_Square_Normal" }
        @{ name = "Menu"; x = 914; label = "Menu"; sprite = "HUD/UI_HUD_Button_Square_Normal" }
        @{ name = "Build"; x = 986; label = "Build"; sprite = "HUD/UI_HUD_Button_Square_Active" }
    )
    foreach ($btn in $btns) {
        $node = Add-IconButton $prefab $bar "$($btn.name)Button" $btn.x 18 56 56 $btn.sprite
        Add-Text $prefab $bar "$($btn.name)Label" $btn.x 78 56 18 $btn.label 12 "MiddleCenter" $PanelTextColor | Out-Null
    }

    $chips = @("1x", "2x", "4x")
    for ($i = 0; $i -lt $chips.Count; $i++) {
        $chip = Add-Image $prefab $bar "SpeedChip_$($chips[$i])" (842 + ($i * 82)) 92 70 42 "HUD/UI_HUD_InfoBox_Small" 1
        Add-Text $prefab $chip "SpeedChipLabel_$($chips[$i])" 0 0 70 42 $chips[$i] 16 "MiddleCenter" $PanelTextColor | Out-Null
    }
}

function Add-BottomNavVisual($prefab, $root) {
    $nav = Add-Image $prefab $root "BottomBar" 0 0 1080 188 "BottomNav/UI_BottomNav_Base" 1
    $tabs = @(
        @{ key = "Operate"; label = "Operate"; x = 70; sprite = "BottomNav/UI_BottomNav_Tab_Active"; glyph = "O" }
        @{ key = "Install"; label = "Install"; x = 310; sprite = "BottomNav/UI_BottomNav_Tab_Normal"; glyph = "I" }
        @{ key = "Finance"; label = "Finance"; x = 550; sprite = "BottomNav/UI_BottomNav_Tab_Normal"; glyph = "F" }
        @{ key = "Review"; label = "Review"; x = 790; sprite = "BottomNav/UI_BottomNav_Tab_Normal"; glyph = "R" }
    )
    foreach ($tab in $tabs) {
        $node = Add-Image $prefab $nav "Tab_$($tab.key)" $tab.x 40 196 108 $tab.sprite 1 $null $false 1 1 $true
        $icon = Add-Image $prefab $node "Icon_$($tab.key)" 20 18 42 42 "BottomNav/UI_BottomNav_IconFrame" 0
        Add-Text $prefab $icon "Glyph_$($tab.key)" 0 0 42 42 $tab.glyph 20 "MiddleCenter" $PanelTextColor | Out-Null
        Add-Text $prefab $node "Label_$($tab.key)" 0 20 196 68 $tab.label 24 "MiddleCenter" $PanelTextColor | Out-Null
    }
}

function Add-PanelHeader($prefab, $panel, [string]$title, [string]$subtitle) {
    Add-Text $prefab $panel "${title}_Header" 56 46 360 36 $title 34 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $panel "${title}_Sub" 56 84 520 22 $subtitle 18 "MiddleLeft" $SubtleTextColor | Out-Null
}

function Add-SubTabs($prefab, $panel, [string]$active, [string]$secondary) {
    Add-ScreenButton $prefab $panel "SubTabPrimary" 56 118 170 68 "Common/UI_Common_Tab_M_Active" $active 22 | Out-Null
    Add-ScreenButton $prefab $panel "SubTabSecondary" 238 118 170 68 "Common/UI_Common_Tab_M_Secondary" $secondary 22 | Out-Null
}

function Add-OperateVisual($prefab, $root) {
    $panel = Add-Image $prefab $root "OperatePanel" 0 0 980 1220 "Common/UI_Common_MainPanel_Base_L" 1
    Add-PanelHeader $prefab $panel "Operate" "Today overview and member reaction"
    Add-SubTabs $prefab $panel "Summary" "Schedule"
    $summary = Add-Image $prefab $panel "SummaryRow" 56 208 868 192 "Panels/Operate/UI_Operate_SummaryRow_4Slot" 0
    $metrics = @(
        @{ x = 18; label = "Members"; value = "124" }
        @{ x = 232; label = "Visits"; value = "81" }
        @{ x = 446; label = "Satisfaction"; value = "94%" }
        @{ x = 660; label = "Queue"; value = "6" }
    )
    foreach ($metric in $metrics) {
        Add-Text $prefab $summary "MetricLabel_$($metric.label)" $metric.x 30 170 24 $metric.label 18 "MiddleCenter" $SubtleTextColor | Out-Null
        Add-Text $prefab $summary "MetricValue_$($metric.label)" $metric.x 64 170 34 $metric.value 28 "MiddleCenter" $AccentTextColor | Out-Null
    }
    $dual = Add-Image $prefab $panel "DualInfo" 56 434 868 272 "Panels/Operate/UI_Operate_InfoBox_Dual" 0
    Add-Text $prefab $dual "DualLeftTitle" 40 32 280 24 "Peak zone" 24 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $dual "DualLeftBody" 40 74 320 60 "Cardio area is busiest.\nTreadmill queue: 3" 20 "UpperLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $dual "DualRightTitle" 452 32 260 24 "Staff notice" 24 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $dual "DualRightBody" 452 74 320 60 "Front desk is handling two new consultations." 20 "UpperLeft" $SubtleTextColor | Out-Null
    $memo = Add-Image $prefab $panel "MemoBox" 56 736 868 390 "Panels/Operate/UI_Operate_MemoBox" 0
    Add-Text $prefab $memo "MemoTitle" 34 28 260 28 "Memo" 26 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $memo "MemoBody" 34 80 780 220 "Today notes:\n- Expand treadmill zone to spread traffic\n- Recovery stay time is raising satisfaction\n- Keep premium pass push before month end" 22 "UpperLeft" $SubtleTextColor | Out-Null
}

function Add-InstallCard($prefab, $parent, [string]$name, [double]$x, [double]$y, [string]$spriteKey, [string]$category, [string]$title, [string]$price, [string]$actionLabel, [double]$cardWidth = 396, [double]$cardHeight = 186) {
    $card = Add-Image $prefab $parent $name $x $y $cardWidth $cardHeight $spriteKey 0
    $icon = Add-Image $prefab $card "${name}_Icon" 20 20 96 96 "HUD/UI_HUD_IconSlot_Frame" 0
    Add-Text $prefab $icon "${name}_Glyph" 0 0 96 96 $category.Substring(0, 1) 36 "MiddleCenter" $AccentTextColor | Out-Null
    Add-Text $prefab $card "${name}_Category" 132 18 ($cardWidth - 150) 22 $category 16 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $card "${name}_Title" 132 42 ($cardWidth - 150) 32 $title 24 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $card "${name}_Price" 132 84 180 28 $price 22 "MiddleLeft" $AccentTextColor | Out-Null
    Add-StaticSpriteLabel $prefab $card "${name}_Action" ($cardWidth - 128) 124 108 42 "Panels/Install/UI_Install_StatusButton" $actionLabel 18 | Out-Null
}

function Add-InstallVisual($prefab, $root) {
    $panel = Add-Image $prefab $root "InstallPanel" 0 0 980 1220 "Common/UI_Common_MainPanel_Base_L" 1
    Add-PanelHeader $prefab $panel "Install" "2-column card list with full-panel scroll"
    $cats = @(
        @{ label = "Cardio"; x = 56; sprite = "Panels/Install/UI_Install_CategoryTab_Active" }
        @{ label = "Weights"; x = 264; sprite = "Panels/Install/UI_Install_CategoryTab_Normal" }
        @{ label = "Recovery"; x = 472; sprite = "Panels/Install/UI_Install_CategoryTab_Normal" }
        @{ label = "Convenience"; x = 680; sprite = "Panels/Install/UI_Install_CategoryTab_Normal" }
    )
    foreach ($cat in $cats) {
        Add-ScreenButton $prefab $panel "Category_$($cat.label)" $cat.x 118 188 76 $cat.sprite $cat.label 22 | Out-Null
    }
    $desc = Add-Image $prefab $panel "CategoryDescription" 56 206 828 82 "Common/UI_Common_SectionBox_M" 1
    Add-Text $prefab $desc "DescLabel" 24 10 180 18 "Current category" 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $desc "DescValue" 24 34 760 28 "Cardio equipment stabilizes early attendance and average stay time." 20 "MiddleLeft" $PanelTextColor | Out-Null
    $scroll = Add-ScrollRoot $prefab $panel "InstallScroll" 56 308 828 634 24
    $viewport = Add-Image $prefab $scroll "Viewport" 0 0 828 634 $null 0 $MaskColor $false 1 1 $false $false $true $false
    $content = Add-Container $prefab $viewport "Content" 0 0 828 804
    Set-ScrollContent $scroll $viewport $content
    Add-InstallCard $prefab $content "Card01" 18 0 "Panels/Install/UI_Install_Card_Selected" "Cardio" "Treadmill" "KRW 12,000" "Pick" 384 186 | Out-Null
    Add-InstallCard $prefab $content "Card02" 426 0 "Panels/Install/UI_Install_Card_Base" "Cardio" "Premium Treadmill" "KRW 28,000" "Place" 384 186 | Out-Null
    Add-InstallCard $prefab $content "Card03" 18 210 "Panels/Install/UI_Install_Card_Base" "Weights" "Bench Press" "KRW 18,500" "Place" 384 186 | Out-Null
    Add-InstallCard $prefab $content "Card04" 426 210 "Panels/Install/UI_Install_Card_Base" "Convenience" "Water Station" "KRW 4,500" "Add" 384 186 | Out-Null
    Add-Image $prefab $panel "ScrollRail" 900 312 20 626 "Common/UI_Common_ScrollRail_V" 0 | Out-Null
    Add-Image $prefab $panel "ScrollHandle" 900 382 20 164 "Common/UI_Common_ScrollHandle_V" 0 | Out-Null
    $select = Add-Image $prefab $panel "SelectionBar" 56 968 868 174 "Panels/Install/UI_Install_SelectionBar" 0
    Add-Text $prefab $select "SelectionLabel" 28 24 220 20 "Selected item" 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $select "SelectionName" 28 50 344 30 "Treadmill B" 28 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $select "SelectionMeta" 28 94 430 24 "Demand +8  |  Stay time +4" 18 "MiddleLeft" $SubtleTextColor | Out-Null
    $priceBox = Add-Image $prefab $select "SelectionPrice" 524 30 156 84 "Common/UI_Common_SummaryBox_S" 1
    Add-Text $prefab $priceBox "SelectionPriceLabel" 0 14 150 18 "Price" 14 "MiddleCenter" $SubtleTextColor | Out-Null
    Add-Text $prefab $priceBox "SelectionPriceValue" 0 38 150 22 "KRW 12,000" 18 "MiddleCenter" $AccentTextColor | Out-Null
    Add-ScreenButton $prefab $select "ConfirmPlacement" 698 34 144 78 "Common/UI_Common_Button_Wide_Active" "Place" 22 | Out-Null
}

function Add-EconomyVisual($prefab, $root) {
    $panel = Add-Image $prefab $root "EconomyPanel" 0 0 980 1220 "Common/UI_Common_MainPanel_Base_L" 1
    Add-PanelHeader $prefab $panel "Economy" "Readable finance overview"
    Add-SubTabs $prefab $panel "Daily" "Month End"
    $summaries = @(
        @{ label = "Revenue"; value = "KRW 128,450"; x = 56 }
        @{ label = "Spend"; value = "KRW 49,200"; x = 274 }
        @{ label = "Net"; value = "KRW 79,250"; x = 492 }
        @{ label = "Passes"; value = "62"; x = 710 }
    )
    foreach ($item in $summaries) {
        $box = Add-Image $prefab $panel "Summary_$($item.label)" $item.x 204 190 164 "Panels/Economy/UI_Economy_SummaryBox" 1
        Add-Text $prefab $box "SummaryLabel_$($item.label)" 0 28 190 20 $item.label 18 "MiddleCenter" $SubtleTextColor | Out-Null
        $summaryColor = if ($item.label -eq "Spend") { $SubtleTextColor } else { $AccentTextColor }
        Add-Text $prefab $box "SummaryValue_$($item.label)" 0 68 190 28 $item.value 26 "MiddleCenter" $summaryColor | Out-Null
    }
    $left = Add-Image $prefab $panel "MemberMixBox" 56 396 408 256 "Panels/Economy/UI_Economy_DualInfoBox" 1
    Add-Text $prefab $left "MemberMixTitle" 28 24 220 24 "Member mix" 24 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $left "MemberMixBody" 28 74 188 126 "Standard 58%`nPremium 28%`nUpper tier 14%" 21 "UpperLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $left "MemberMixMeta" 228 74 148 96 "Renewals 41`nReferrals 9" 19 "UpperLeft" $AccentTextColor | Out-Null
    $right = Add-Image $prefab $panel "CostBox" 516 396 408 256 "Panels/Economy/UI_Economy_DualInfoBox" 1
    Add-Text $prefab $right "CostTitle" 28 24 220 24 "Cost split" 24 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $right "CostBody" 28 74 188 126 "Payroll 22,000`nPower 7,600`nUpkeep 6,100" 21 "UpperLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $right "CostMeta" 228 74 148 96 "Ads 5,200`nOther 8,300" 19 "UpperLeft" $AccentTextColor | Out-Null
    $detail = Add-Image $prefab $panel "DetailBox" 56 684 868 442 "Panels/Economy/UI_Economy_DetailBox" 1
    Add-Text $prefab $detail "DetailTitle" 28 24 260 24 "Ledger" 26 "MiddleLeft" $PanelTextColor | Out-Null
    $rows = @(
        "Standard pass revenue              KRW 74,000",
        "Premium pass revenue               KRW 36,500",
        "PT revenue                         KRW 17,950",
        "Payroll                            KRW -22,000",
        "Facility upkeep                    KRW -13,700",
        "Marketing                          KRW -5,200",
        "Net profit                         KRW 79,250"
    )
    for ($i = 0; $i -lt $rows.Count; $i++) {
        $detailColor = if ($i -eq ($rows.Count - 1)) { $AccentTextColor } else { $SubtleTextColor }
        Add-Text $prefab $detail "DetailRow_$($i+1)" 34 (82 + ($i * 40)) 760 26 $rows[$i] 21 "MiddleLeft" $detailColor | Out-Null
    }
}

function Add-ReviewVisual($prefab, $root) {
    $panel = Add-Image $prefab $root "ReviewPanel" 0 0 980 1220 "Common/UI_Common_MainPanel_Base_L" 1
    Add-PanelHeader $prefab $panel "Review" "Recent feedback and logs"
    Add-SubTabs $prefab $panel "Recent" "History"
    $summaries = @(
        @{ label = "Score"; value = "4.8"; x = 56 }
        @{ label = "Reviews"; value = "128"; x = 274 }
        @{ label = "Return"; value = "82%"; x = 492 }
        @{ label = "Events"; value = "5"; x = 710 }
    )
    foreach ($item in $summaries) {
        $box = Add-Image $prefab $panel "Summary_$($item.label)" $item.x 204 190 164 "Panels/Review/UI_Review_SummaryBox" 1
        Add-Text $prefab $box "SummaryLabel_$($item.label)" 0 28 190 20 $item.label 18 "MiddleCenter" $SubtleTextColor | Out-Null
        Add-Text $prefab $box "SummaryValue_$($item.label)" 0 68 190 28 $item.value 26 "MiddleCenter" $AccentTextColor | Out-Null
    }
    $review = Add-Image $prefab $panel "ReviewListBox" 56 396 868 304 "Panels/Review/UI_Review_ListBox" 1
    Add-Text $prefab $review "ReviewTitle" 28 24 260 24 "Recent reviews" 24 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $review "ReviewLine1" 30 68 770 22 "5/5  Queue is shorter and workouts feel smoother." 18 "UpperLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $review "ReviewLine2" 30 114 770 22 "4/5  Shower room is cleaner, so I renewed my pass." 18 "UpperLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $review "ReviewLine3" 30 160 770 22 "5/5  Recovery mats are roomy and comfortable." 18 "UpperLeft" $SubtleTextColor | Out-Null
    $event = Add-Image $prefab $panel "EventLogBox" 56 724 868 230 "Panels/Review/UI_Review_EventLogBox" 1
    Add-Text $prefab $event "EventTitle" 28 20 260 24 "Event log" 24 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $event "EventLine1" 30 58 780 22 "09:10  Three new members joined" 17 "UpperLeft" $AccentTextColor | Out-Null
    Add-Text $prefab $event "EventLine2" 30 96 780 22 "12:40  Premium treadmill interest spiked" 17 "UpperLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $event "EventLine3" 30 134 780 22 "18:20  Recovery zone reached record stay time" 17 "UpperLeft" $SubtleTextColor | Out-Null
    $empty = Add-Image $prefab $panel "EmptyStateBox" 56 978 868 148 "Panels/Review/UI_Review_EmptyStateBox" 1
    Add-Text $prefab $empty "EmptyText" 0 32 868 28 "No unresolved negative reviews." 24 "MiddleCenter" $AccentTextColor | Out-Null
    Add-Text $prefab $empty "EmptySub" 0 76 868 22 "Current operation direction is stable." 18 "MiddleCenter" $SubtleTextColor | Out-Null
}

function Add-ModalChrome($prefab, $root, [string]$panelName, [string]$title, [string]$subtitle, [double]$x, [double]$y, [double]$width, [double]$height) {
    Add-Image $prefab $root "${panelName}_Backdrop" 0 0 1080 1920 $null 0 $PopupBackdropColor | Out-Null
    $panel = Add-Image $prefab $root $panelName $x $y $width $height "Popups/UI_Popup_Base_Large" 1
    $header = Add-Image $prefab $panel "${panelName}_Header" 24 20 ($width - 96) 86 "Popups/UI_Popup_Header" 1
    Add-Text $prefab $header "${panelName}_Title" 26 18 ($width - 180) 26 $title 28 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $header "${panelName}_Sub" 26 46 ($width - 180) 20 $subtitle 16 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-IconButton $prefab $panel "${panelName}_Close" ($width - 76) 28 48 48 "Popups/UI_Popup_Close_Normal" | Out-Null
    return $panel
}

function Add-PopupListRow($prefab, $parent, [string]$name, [double]$x, [double]$y, [double]$width, [string]$title, [string]$meta, [string]$rightText, [string]$actionLabel = "", [string]$actionSprite = "Popups/UI_Popup_Button_Green", [bool]$actionInteractive = $true) {
    $row = Add-Image $prefab $parent $name $x $y $width 88 "Popups/UI_Popup_ListRow" 1
    Add-Text $prefab $row "${name}_Title" 22 14 260 24 $title 22 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $row "${name}_Meta" 22 44 360 20 $meta 16 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $row "${name}_Right" 410 28 170 24 $rightText 18 "MiddleLeft" $AccentTextColor | Out-Null
    if (-not [string]::IsNullOrWhiteSpace($actionLabel)) {
        if ($actionInteractive) {
            Add-ScreenButton $prefab $row "${name}_Action" ($width - 138) 22 112 42 $actionSprite $actionLabel 18 | Out-Null
        }
        else {
            Add-StaticSpriteLabel $prefab $row "${name}_Action" ($width - 138) 22 112 42 $actionSprite $actionLabel 18 | Out-Null
        }
    }
    return $row
}

function Add-GameMenuPopupVisual($prefab, $root) {
    $panel = Add-ModalChrome $prefab $root "GameMenuPopup" "Game Menu" "Branch management and navigation" 150 220 780 1020
    Add-Image $prefab $panel "BranchInfoFill" 48 136 684 138 $null 0 $PopupFillColor | Out-Null
    $branch = Add-Image $prefab $panel "BranchInfo" 38 126 704 158 "Common/UI_Common_SectionBox_M" 1
    Add-Text $prefab $branch "BranchName" 24 24 320 28 "Current branch" 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $branch "BranchValue" 24 52 360 34 "Station District Gym" 28 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $branch "BranchMeta" 24 94 620 28 "Lot 16x16  |  Members 124  |  Cash KRW 128,450" 18 "MiddleLeft" $SubtleTextColor | Out-Null

    Add-Image $prefab $panel "SummaryFill" 48 326 684 230 $null 0 $PopupFillColor | Out-Null
    $summary = Add-Image $prefab $panel "SummaryList" 38 316 704 250 "Common/UI_Common_SectionBox_M" 1
    Add-Text $prefab $summary "SummaryTitle" 24 22 260 24 "Current status" 24 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $prefab $summary "SummaryLine1" 24 70 620 22 "Member satisfaction is holding above 90%." 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $summary "SummaryLine2" 24 106 620 22 "Relocation can unlock the next lot size upgrade." 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $summary "SummaryLine3" 24 142 620 22 "Premium equipment demand is trending upward." 18 "MiddleLeft" $SubtleTextColor | Out-Null

    Add-ScreenButton $prefab $panel "RelocateButton" 84 620 612 88 "Popups/UI_Popup_Button_Yellow" "Relocate branch" 28 | Out-Null
    Add-ScreenButton $prefab $panel "ReturnTitleButton" 84 726 612 88 "Popups/UI_Popup_Button_Beige" "Return to title" 28 | Out-Null
    Add-ScreenButton $prefab $panel "ClosePopupButton" 84 832 612 88 "Popups/UI_Popup_Button_Green" "Close menu" 28 | Out-Null
}

function Add-StaffPopupVisual($prefab, $root) {
    $panel = Add-ModalChrome $prefab $root "StaffPopup" "Staff" "Working members and applicant preview" 130 160 820 1200
    Add-ScreenButton $prefab $panel "StaffTab_Working" 44 124 174 68 "Common/UI_Common_Tab_M_Active" "Working" 22 | Out-Null
    Add-ScreenButton $prefab $panel "StaffTab_Applicants" 232 124 174 68 "Common/UI_Common_Tab_M_Secondary" "Applicants" 22 | Out-Null

    $header = Add-Image $prefab $panel "StaffListHeader" 44 214 732 54 "Common/UI_Common_SectionBox_M" 1
    Add-Text $prefab $header "HeaderName" 20 14 160 22 "Name" 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $header "HeaderRole" 248 14 150 22 "Role" 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $header "HeaderShift" 556 14 150 22 "Hours" 18 "MiddleLeft" $SubtleTextColor | Out-Null

    $scroll = Add-ScrollRoot $prefab $panel "StaffScroll" 44 286 732 646 22
    $viewport = Add-Image $prefab $scroll "Viewport" 0 0 732 646 $null 0 $MaskColor $false 1 1 $false $false $true $false
    $content = Add-Container $prefab $viewport "Content" 0 0 732 732
    Set-ScrollContent $scroll $viewport $content
    Add-PopupListRow $prefab $content "StaffRow01" 0 0 732 "Mina" "Front desk lead" "" "08-16" "Popups/UI_Popup_Button_Beige" $false | Out-Null
    Add-PopupListRow $prefab $content "StaffRow02" 0 102 732 "Hyun" "Trainer" "" "10-18" "Popups/UI_Popup_Button_Beige" $false | Out-Null
    Add-PopupListRow $prefab $content "StaffRow03" 0 204 732 "Jae" "Cleaner" "" "07-15" "Popups/UI_Popup_Button_Beige" $false | Out-Null
    Add-PopupListRow $prefab $content "StaffRow04" 0 306 732 "Ara" "Counselor" "" "12-20" "Popups/UI_Popup_Button_Beige" $false | Out-Null
    Add-PopupListRow $prefab $content "StaffRow05" 0 408 732 "Yuna" "Locker care" "" "09-17" "Popups/UI_Popup_Button_Beige" $false | Out-Null
    Add-PopupListRow $prefab $content "StaffRow06" 0 510 732 "Kio" "PT support" "" "11-19" "Popups/UI_Popup_Button_Beige" $false | Out-Null
    Add-PopupListRow $prefab $content "StaffRow07" 0 612 732 "Rin" "Reception" "" "13-21" "Popups/UI_Popup_Button_Beige" $false | Out-Null

    Add-ScreenButton $prefab $panel "StaffCloseButton" 548 1026 228 74 "Popups/UI_Popup_Button_Beige" "Close" 24 | Out-Null
}

function Add-RecruitPopupVisual($prefab, $root) {
    $panel = Add-ModalChrome $prefab $root "RecruitPopup" "Recruit" "Applicant list with sample hire actions" 130 160 820 1200
    Add-ScreenButton $prefab $panel "RecruitTab_Working" 44 124 174 68 "Common/UI_Common_Tab_M_Secondary" "Working" 22 | Out-Null
    Add-ScreenButton $prefab $panel "RecruitTab_Applicants" 232 124 174 68 "Common/UI_Common_Tab_M_Active" "Applicants" 22 | Out-Null

    $header = Add-Image $prefab $panel "RecruitListHeader" 44 214 732 54 "Common/UI_Common_SectionBox_M" 1
    Add-Text $prefab $header "RecruitHeaderName" 20 14 160 22 "Name" 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $header "RecruitHeaderSpec" 248 14 160 22 "Specialty" 18 "MiddleLeft" $SubtleTextColor | Out-Null
    Add-Text $prefab $header "RecruitHeaderPay" 470 14 130 22 "Salary" 18 "MiddleLeft" $SubtleTextColor | Out-Null

    $scroll = Add-ScrollRoot $prefab $panel "RecruitScroll" 44 286 732 646 22
    $viewport = Add-Image $prefab $scroll "Viewport" 0 0 732 646 $null 0 $MaskColor $false 1 1 $false $false $true $false
    $content = Add-Container $prefab $viewport "Content" 0 0 732 732
    Set-ScrollContent $scroll $viewport $content
    Add-PopupListRow $prefab $content "RecruitRow01" 0 0 732 "Sora" "PT sales" "KRW 3,200" "Hire" "Popups/UI_Popup_Button_Green" | Out-Null
    Add-PopupListRow $prefab $content "RecruitRow02" 0 102 732 "Dain" "Strength coach" "KRW 3,600" "Hire" "Popups/UI_Popup_Button_Green" | Out-Null
    Add-PopupListRow $prefab $content "RecruitRow03" 0 204 732 "Nari" "Member care" "KRW 2,900" "Hire" "Popups/UI_Popup_Button_Green" | Out-Null
    Add-PopupListRow $prefab $content "RecruitRow04" 0 306 732 "Jun" "Recovery guide" "KRW 3,100" "Hire" "Popups/UI_Popup_Button_Green" | Out-Null
    Add-PopupListRow $prefab $content "RecruitRow05" 0 408 732 "Lia" "Night trainer" "KRW 3,400" "Hire" "Popups/UI_Popup_Button_Green" | Out-Null
    Add-PopupListRow $prefab $content "RecruitRow06" 0 510 732 "Noel" "Front desk" "KRW 2,800" "Hire" "Popups/UI_Popup_Button_Green" | Out-Null
    Add-PopupListRow $prefab $content "RecruitRow07" 0 612 732 "Haru" "Stretch coach" "KRW 3,000" "Hire" "Popups/UI_Popup_Button_Green" | Out-Null

    Add-ScreenButton $prefab $panel "RecruitCloseButton" 548 1026 228 74 "Popups/UI_Popup_Button_Beige" "Close" 24 | Out-Null
}

function Build-ScreenPrefabs() {
    $title = New-Prefab "PF_UI_TitleScreen" 1080 1920 $true
    Add-TitleVisual $title $title.Root
    Emit-Prefab $title (Join-Path $prefabRoot "Title\PF_UI_TitleScreen.prefab")
    Render-PreviewImage $title "Preview_TitleScreen" 540 960 | Out-Null

    $hud = New-Prefab "PF_UI_TopHUD" 1080 168 $true
    Add-TopHudVisual $hud $hud.Root
    Emit-Prefab $hud (Join-Path $prefabRoot "HUD\PF_UI_TopHUD.prefab")
    Render-PreviewImage $hud "Preview_TopHUD" 1080 168 | Out-Null

    $bottom = New-Prefab "PF_UI_BottomNav" 1080 188 $true
    Add-BottomNavVisual $bottom $bottom.Root
    Emit-Prefab $bottom (Join-Path $prefabRoot "BottomNav\PF_UI_BottomNav.prefab")
    Render-PreviewImage $bottom "Preview_BottomNav" 1080 188 | Out-Null

    $operate = New-Prefab "PF_UI_OperatePanel" 980 1220 $true
    Add-OperateVisual $operate $operate.Root
    Emit-Prefab $operate (Join-Path $prefabRoot "Panels\PF_UI_OperatePanel.prefab")
    Render-PreviewImage $operate "Preview_OperatePanel" 980 1220 | Out-Null

    $install = New-Prefab "PF_UI_InstallPanel" 980 1220 $true
    Add-InstallVisual $install $install.Root
    Emit-Prefab $install (Join-Path $prefabRoot "Panels\PF_UI_InstallPanel.prefab")
    Render-PreviewImage $install "Preview_InstallPanel" 980 1220 | Out-Null

    $economy = New-Prefab "PF_UI_EconomyPanel" 980 1220 $true
    Add-EconomyVisual $economy $economy.Root
    Emit-Prefab $economy (Join-Path $prefabRoot "Panels\PF_UI_EconomyPanel.prefab")
    Render-PreviewImage $economy "Preview_EconomyPanel" 980 1220 | Out-Null

    $review = New-Prefab "PF_UI_ReviewPanel" 980 1220 $true
    Add-ReviewVisual $review $review.Root
    Emit-Prefab $review (Join-Path $prefabRoot "Panels\PF_UI_ReviewPanel.prefab")
    Render-PreviewImage $review "Preview_ReviewPanel" 980 1220 | Out-Null
}

function Build-PopupPrefabs() {
    $menu = New-Prefab "PF_UI_GameMenuPopup" 1080 1920 $true
    Add-GameMenuPopupVisual $menu $menu.Root
    Emit-Prefab $menu (Join-Path $prefabRoot "Popups\PF_UI_GameMenuPopup.prefab")
    Render-PreviewImage $menu "Preview_GameMenuPopup" 1080 1920 | Out-Null

    $staff = New-Prefab "PF_UI_StaffPopup" 1080 1920 $true
    Add-StaffPopupVisual $staff $staff.Root
    Emit-Prefab $staff (Join-Path $prefabRoot "Popups\PF_UI_StaffPopup.prefab")
    Render-PreviewImage $staff "Preview_StaffPopup" 1080 1920 | Out-Null

    $recruit = New-Prefab "PF_UI_RecruitPopup" 1080 1920 $true
    Add-RecruitPopupVisual $recruit $recruit.Root
    Emit-Prefab $recruit (Join-Path $prefabRoot "Popups\PF_UI_RecruitPopup.prefab")
    Render-PreviewImage $recruit "Preview_RecruitPopup" 1080 1920 | Out-Null
}

function Build-PreviewRoot() {
    $root = New-Prefab "PF_UIRoot_Canvas" 2560 3200 $true
    Add-Image $root $root.Root "PreviewBackground" 0 0 2560 3200 $null 0 $PreviewBackgroundColor | Out-Null
    Add-Text $root $root.Root "PreviewTitle" 80 54 1000 42 "UI Rebuild Preview" 40 "MiddleLeft" $PanelTextColor | Out-Null
    Add-Text $root $root.Root "PreviewSub" 80 108 1600 26 "Title / HUD / BottomNav / Operate / Install / Economy / Review" 22 "MiddleLeft" $SubtleTextColor | Out-Null

    $titleCard = Add-PreviewCard $root $root.Root "TitleCard" 40 160 560 860 "Title"
    $titleContainer = Add-Container $root $titleCard "TitlePreview" 44 64 1080 1920 0.40 0.40
    Add-TitleVisual $root $titleContainer

    $hudCard = Add-PreviewCard $root $root.Root "HudCard" 640 160 820 280 "Top HUD"
    $hudContainer = Add-Container $root $hudCard "HudPreview" 16 84 1080 168 0.73 0.73
    Add-TopHudVisual $root $hudContainer

    $bottomCard = Add-PreviewCard $root $root.Root "BottomCard" 1480 160 820 340 "Bottom Nav"
    $bottomContainer = Add-Container $root $bottomCard "BottomPreview" 16 96 1080 188 0.73 0.73
    Add-BottomNavVisual $root $bottomContainer

    $operateCard = Add-PreviewCard $root $root.Root "OperateCard" 40 1060 1100 930 "Operate"
    $operateContainer = Add-Container $root $operateCard "OperatePreview" 198 46 980 1220 0.72 0.72
    Add-OperateVisual $root $operateContainer

    $installCard = Add-PreviewCard $root $root.Root "InstallCard" 1180 1060 1100 930 "Install"
    $installContainer = Add-Container $root $installCard "InstallPreview" 198 46 980 1220 0.72 0.72
    Add-InstallVisual $root $installContainer

    $economyCard = Add-PreviewCard $root $root.Root "EconomyCard" 40 2050 1100 930 "Economy"
    $economyContainer = Add-Container $root $economyCard "EconomyPreview" 198 46 980 1220 0.72 0.72
    Add-EconomyVisual $root $economyContainer

    $reviewCard = Add-PreviewCard $root $root.Root "ReviewCard" 1180 2050 1100 930 "Review"
    $reviewContainer = Add-Container $root $reviewCard "ReviewPreview" 198 46 980 1220 0.72 0.72
    Add-ReviewVisual $root $reviewContainer

    $path = Join-Path $prefabRoot "PF_UIRoot_Canvas.prefab"
    Emit-Prefab $root $path
    Ensure-PrefabMeta $path
    Render-PreviewImage $root "Preview_UIRoot_Canvas" 1440 1800 | Out-Null
}

function Ensure-ExistingPrefabMetas() {
    @(
        (Join-Path $prefabRoot "Title\PF_UI_TitleScreen.prefab"),
        (Join-Path $prefabRoot "HUD\PF_UI_TopHUD.prefab"),
        (Join-Path $prefabRoot "BottomNav\PF_UI_BottomNav.prefab"),
        (Join-Path $prefabRoot "Panels\PF_UI_OperatePanel.prefab"),
        (Join-Path $prefabRoot "Panels\PF_UI_InstallPanel.prefab"),
        (Join-Path $prefabRoot "Panels\PF_UI_EconomyPanel.prefab"),
        (Join-Path $prefabRoot "Panels\PF_UI_ReviewPanel.prefab"),
        (Join-Path $prefabRoot "Popups\PF_UI_GameMenuPopup.prefab"),
        (Join-Path $prefabRoot "Popups\PF_UI_StaffPopup.prefab"),
        (Join-Path $prefabRoot "Popups\PF_UI_RecruitPopup.prefab")
    ) | ForEach-Object { Ensure-PrefabMeta $_ }
}

function Write-Report() {
    $reportPath = Join-Path $projectRoot "Temp\UIRebuildAssemblyReport.txt"
    if (-not (Test-Path (Split-Path -Parent $reportPath))) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $reportPath) | Out-Null
    }
    @(
        "ModifiedSpriteCount=$script:modifiedSpriteCount"
        "BorderAssets=$($BorderAssetNames -join ', ')"
        "UpdatedPrefabs=Assets/_Project/Prefabs/UIRebuild/Title/PF_UI_TitleScreen.prefab, Assets/_Project/Prefabs/UIRebuild/HUD/PF_UI_TopHUD.prefab, Assets/_Project/Prefabs/UIRebuild/BottomNav/PF_UI_BottomNav.prefab, Assets/_Project/Prefabs/UIRebuild/Panels/PF_UI_OperatePanel.prefab, Assets/_Project/Prefabs/UIRebuild/Panels/PF_UI_InstallPanel.prefab, Assets/_Project/Prefabs/UIRebuild/Panels/PF_UI_EconomyPanel.prefab, Assets/_Project/Prefabs/UIRebuild/Panels/PF_UI_ReviewPanel.prefab, Assets/_Project/Prefabs/UIRebuild/Popups/PF_UI_GameMenuPopup.prefab, Assets/_Project/Prefabs/UIRebuild/Popups/PF_UI_StaffPopup.prefab, Assets/_Project/Prefabs/UIRebuild/Popups/PF_UI_RecruitPopup.prefab, Assets/_Project/Prefabs/UIRebuild/PF_UIRoot_Canvas.prefab"
    ) | Set-Content -LiteralPath $reportPath -Encoding utf8
}

Load-SpriteGuids
Ensure-ExistingPrefabMetas
Build-ScreenPrefabs
Build-PopupPrefabs
Build-PreviewRoot
Write-Report
