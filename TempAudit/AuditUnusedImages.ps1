Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir ".."))
$AssetRoot = Join-Path $ProjectRoot "Assets\_Project"
$ReportPath = Join-Path $ProjectRoot "UnusedImageAudit_Report.md"

$ImageExtensions = @(".png", ".jpg", ".jpeg", ".webp", ".psd", ".tga")
$GuidSearchExtensions = @(
    ".unity", ".prefab", ".asset", ".mat", ".anim", ".controller",
    ".overridecontroller", ".playable", ".rendertexture", ".spriteatlas",
    ".asmdef", ".scenetemplate", ".preset", ".shader", ".shadergraph",
    ".compute", ".vfx", ".terrainlayer", ".lighting"
)
$StringSearchExtensions = @(
    ".cs", ".asmdef", ".json", ".txt", ".asset", ".prefab", ".unity",
    ".spriteatlas", ".mat", ".anim", ".controller", ".overridecontroller",
    ".scenetemplate"
)
$DisposableNamePattern = "(?i)(^|[_\-\s])(old|backup|bak|temp|test|draft|unused|legacy|curved|prototype|copy|sample)([_\-\s]|$)"
$GenericDynamicNames = @(
    "door", "window", "floor_tile", "wall_poster", "potted_plant", "water_cooler",
    "reception_desk", "treadmill", "exercise_bike", "bench_press", "dumbbell_rack"
)

function Get-RelativeProjectPath {
    param([string]$Path)
    $root = [System.IO.Path]::GetFullPath($ProjectRoot)
    if (-not $root.EndsWith("\")) {
        $root = $root + "\"
    }
    $full = [System.IO.Path]::GetFullPath($Path)
    $rootUri = New-Object System.Uri($root)
    $fileUri = New-Object System.Uri($full)
    return [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($fileUri).ToString()).Replace("\", "/")
}

function Remove-ExtensionFromPath {
    param(
        [string]$Path,
        [string]$Extension
    )
    if ([string]::IsNullOrEmpty($Extension)) {
        return $Path
    }
    return $Path.Substring(0, $Path.Length - $Extension.Length)
}

function Read-TextFile {
    param([string]$Path)
    try {
        return [System.IO.File]::ReadAllText($Path)
    }
    catch {
        return ""
    }
}

function Escape-Markdown {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) {
        return "-"
    }
    return $Text.Replace("|", "\|")
}

function Format-RefList {
    param([object[]]$Refs)
    if ($null -eq $Refs -or $Refs.Count -eq 0) {
        return "None"
    }
    $parts = foreach ($ref in ($Refs | Sort-Object Rel -Unique)) {
        if ($ref.PSObject.Properties.Name -contains "Variants" -and $ref.Variants.Count -gt 0) {
            "``$($ref.Rel)`` [$([string]::Join(", ", ($ref.Variants | Sort-Object -Unique)))]"
        }
        else {
            "``$($ref.Rel)``"
        }
    }
    return [string]::Join("; ", $parts)
}

function New-SearchItem {
    param([System.IO.FileInfo]$File)
    $extension = $File.Extension.ToLowerInvariant()
    [pscustomobject]@{
        FullPath = $File.FullName
        Rel = Get-RelativeProjectPath $File.FullName
        Extension = $extension
        Text = Read-TextFile $File.FullName
    }
}

if (-not (Test-Path -LiteralPath $AssetRoot)) {
    throw "Asset root not found: $AssetRoot"
}

$SearchRoots = @("Assets", "Packages", "ProjectSettings") |
    ForEach-Object { Join-Path $ProjectRoot $_ } |
    Where-Object { Test-Path -LiteralPath $_ }

$AllSearchFiles = foreach ($root in $SearchRoots) {
    Get-ChildItem -LiteralPath $root -File -Recurse -ErrorAction SilentlyContinue
}

$GuidSearchItems = $AllSearchFiles |
    Where-Object { $GuidSearchExtensions -contains $_.Extension.ToLowerInvariant() } |
    ForEach-Object { New-SearchItem $_ }

$StringSearchItems = $AllSearchFiles |
    Where-Object { $StringSearchExtensions -contains $_.Extension.ToLowerInvariant() } |
    ForEach-Object { New-SearchItem $_ }

$Images = Get-ChildItem -LiteralPath $AssetRoot -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $ImageExtensions -contains $_.Extension.ToLowerInvariant() } |
    Sort-Object FullName

$Results = foreach ($image in $Images) {
    $assetRel = Get-RelativeProjectPath $image.FullName
    $assetRelNoExt = Remove-ExtensionFromPath $assetRel $image.Extension
    $fileName = $image.Name
    $stem = $image.BaseName
    $metaPath = "$($image.FullName).meta"
    $guid = ""

    if (Test-Path -LiteralPath $metaPath) {
        $metaText = Read-TextFile $metaPath
        $guidMatch = [regex]::Match($metaText, "(?m)^guid:\s*([0-9a-fA-F]+)\s*$")
        if ($guidMatch.Success) {
            $guid = $guidMatch.Groups[1].Value
        }
    }

    $guidRefs = @()
    if (-not [string]::IsNullOrWhiteSpace($guid)) {
        $guidRefs = foreach ($item in $GuidSearchItems) {
            if ($item.Text.IndexOf($guid, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                [pscustomobject]@{ Rel = $item.Rel }
            }
        }
    }

    $resourcePathNoExt = ""
    $resourcePathWithExt = ""
    $generatedRuntimePathNoExt = ""
    $generatedRuntimePathWithExt = ""
    $inResources = $false

    $resourceMarker = "/Resources/"
    $resourceMarkerIndex = $assetRel.IndexOf($resourceMarker, [System.StringComparison]::OrdinalIgnoreCase)
    if ($resourceMarkerIndex -ge 0) {
        $inResources = $true
        $resourcePathWithExt = $assetRel.Substring($resourceMarkerIndex + $resourceMarker.Length)
        $resourcePathNoExt = Remove-ExtensionFromPath $resourcePathWithExt $image.Extension
    }

    $generatedMarker = "GeneratedRuntimeUI/"
    $generatedMarkerIndex = $assetRel.IndexOf($generatedMarker, [System.StringComparison]::OrdinalIgnoreCase)
    if ($generatedMarkerIndex -ge 0) {
        $generatedRuntimePathWithExt = $assetRel.Substring($generatedMarkerIndex)
        $generatedRuntimePathNoExt = Remove-ExtensionFromPath $generatedRuntimePathWithExt $image.Extension
    }

    $prefixHint = ""
    $prefixMatch = [regex]::Match($stem, "^(.*?)(\d{2,})$")
    if ($prefixMatch.Success -and $prefixMatch.Groups[1].Value.Length -ge 4) {
        $prefixHint = $prefixMatch.Groups[1].Value
    }

    $stringVariants = New-Object System.Collections.Generic.List[object]
    $stringVariants.Add([pscustomobject]@{ Type = "filename-with-extension"; Value = $fileName; MatchMode = "literal" })
    $stringVariants.Add([pscustomobject]@{ Type = "stem"; Value = $stem; MatchMode = "word" })
    if (-not [string]::IsNullOrWhiteSpace($resourcePathNoExt)) {
        $stringVariants.Add([pscustomobject]@{ Type = "resources-path"; Value = $resourcePathNoExt; MatchMode = "literal" })
        $stringVariants.Add([pscustomobject]@{ Type = "resources-path-with-extension"; Value = $resourcePathWithExt; MatchMode = "literal" })
    }
    if (-not [string]::IsNullOrWhiteSpace($generatedRuntimePathNoExt)) {
        $stringVariants.Add([pscustomobject]@{ Type = "generated-runtime-path"; Value = $generatedRuntimePathNoExt; MatchMode = "literal" })
        $stringVariants.Add([pscustomobject]@{ Type = "generated-runtime-path-with-extension"; Value = $generatedRuntimePathWithExt; MatchMode = "literal" })
    }
    if (-not [string]::IsNullOrWhiteSpace($prefixHint)) {
        $stringVariants.Add([pscustomobject]@{ Type = "dynamic-prefix-hint"; Value = $prefixHint; MatchMode = "literal" })
    }

    $stringRefs = foreach ($item in $StringSearchItems) {
        $matched = New-Object System.Collections.Generic.List[string]
        foreach ($variant in $stringVariants) {
            if ([string]::IsNullOrWhiteSpace($variant.Value)) {
                continue
            }
            $found = $false
            if ($variant.MatchMode -eq "word") {
                $pattern = "(?i)(?<![A-Za-z0-9_])" + [regex]::Escape($variant.Value) + "(?![A-Za-z0-9_])"
                $found = [regex]::IsMatch($item.Text, $pattern)
            }
            else {
                $found = $item.Text.IndexOf($variant.Value, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
            }
            if ($found) {
                $matched.Add($variant.Type)
            }
        }
        if ($matched.Count -gt 0) {
            [pscustomobject]@{
                Rel = $item.Rel
                Extension = $item.Extension
                Variants = @($matched | Sort-Object -Unique)
                HasResourcesLoadCall = ($item.Extension -eq ".cs" -and $item.Text.IndexOf("Resources.Load", [System.StringComparison]::OrdinalIgnoreCase) -ge 0)
            }
        }
    }

    $activeGuidRefs = @($guidRefs | Where-Object {
        $_.Rel -notlike "Assets/_Project/_Archive/*" -and
        $_.Rel -notlike "Assets/_Recovery/*"
    })
    $activeStringRefs = @($stringRefs | Where-Object {
        $_.Rel -notlike "Assets/_Project/_Archive/*" -and
        $_.Rel -notlike "Assets/_Recovery/*" -and
        $_.Extension -notin @(".json", ".txt", ".asmdef")
    })
    $resourceStringRefs = @($stringRefs | Where-Object {
        $_.Variants -contains "resources-path" -or
        $_.Variants -contains "resources-path-with-extension" -or
        $_.Variants -contains "generated-runtime-path" -or
        $_.Variants -contains "generated-runtime-path-with-extension"
    })
    $activeResourceStringRefs = @($activeStringRefs | Where-Object {
        $_.Variants -contains "resources-path" -or
        $_.Variants -contains "resources-path-with-extension" -or
        $_.Variants -contains "generated-runtime-path" -or
        $_.Variants -contains "generated-runtime-path-with-extension"
    })
    $filenameStringRefs = @($activeStringRefs | Where-Object { $_.Variants -contains "filename-with-extension" })
    $stemStringRefs = @($activeStringRefs | Where-Object { $_.Variants -contains "stem" })
    $dynamicPrefixRefs = @($activeStringRefs | Where-Object { $_.Variants -contains "dynamic-prefix-hint" })
    $resourcesLoadRefs = @($activeStringRefs | Where-Object { $_.HasResourcesLoadCall })
    $spriteAtlasRefs = @($activeGuidRefs | Where-Object { $_.Rel.ToLowerInvariant().EndsWith(".spriteatlas") })

    $isGuidUsed = @($activeGuidRefs).Count -gt 0
    $hasStrongStringRef = (@($activeResourceStringRefs).Count -gt 0 -or @($filenameStringRefs).Count -gt 0)
    $hasStemOnlyRef = (@($stemStringRefs).Count -gt 0 -and -not $hasStrongStringRef)
    $hasDynamicPrefixRef = (@($dynamicPrefixRefs).Count -gt 0)
    $looksDisposable = ($assetRel -match $DisposableNamePattern -or $stem -match $DisposableNamePattern)
    $isGenericDynamicName = $GenericDynamicNames -contains $stem.ToLowerInvariant()
    $isUiV2 = $assetRel -like "Assets/_Project/Resources/GeneratedRuntimeUI/ui_v2/*"
    $isOldGeneratedUi = $assetRel -like "Assets/_Project/Resources/GeneratedRuntimeUI/ui/*"

    $classification = "C. Hold"
    $reason = ""
    if ($isGuidUsed) {
        $classification = "A. Used confirmed"
        if (@($spriteAtlasRefs).Count -gt 0) {
            $reason = "GUID is referenced by Unity asset files including SpriteAtlas"
        }
        else {
            $reason = "GUID is referenced by Unity scene/prefab/asset files"
        }
    }
    elseif ($hasStrongStringRef) {
        $classification = "A. Used confirmed"
        if (@($resourceStringRefs).Count -gt 0) {
            $reason = "Resources-relative path or GeneratedRuntimeUI path string reference was found"
        }
        else {
            $reason = "Full filename string reference was found"
        }
    }
    elseif ($hasStemOnlyRef -and -not ($inResources -and ($isGenericDynamicName -or $stem.Length -le 5))) {
        $classification = "A. Used confirmed"
        $reason = "Extensionless filename string reference was found"
    }
    elseif (-not $isGuidUsed -and -not $hasStrongStringRef -and -not $hasStemOnlyRef -and -not $hasDynamicPrefixRef) {
        if ($looksDisposable -or $isOldGeneratedUi) {
            $classification = "B. Candidate unused"
            $reason = "No active GUID/source string reference; name or folder looks disposable/legacy"
        }
        elseif ($inResources) {
            $classification = "C. Hold"
            $reason = "Inside Resources with no active direct GUID/source string reference; dynamic loading needs manual confirmation"
        }
        else {
            $classification = "B. Candidate unused"
            $reason = "No active GUID/source string reference was found"
        }
    }
    elseif ($hasDynamicPrefixRef) {
        $classification = "C. Hold"
        $reason = "No direct individual-file reference, but numbered/prefix-based dynamic loading is possible"
    }
    else {
        $classification = "C. Hold"
        $reason = "Reference is stem-only or a generic name, so actual loading is ambiguous"
    }

    [pscustomobject]@{
        Path = $assetRel
        Guid = $(if ([string]::IsNullOrWhiteSpace($guid)) { "(missing)" } else { $guid })
        Classification = $classification
        Reason = $reason
        GuidRefs = @($guidRefs)
        StringRefs = @($stringRefs)
        ResourceStringRefs = @($activeResourceStringRefs)
        ResourcesLoadRefs = @($resourcesLoadRefs)
        SpriteAtlasRefs = @($spriteAtlasRefs)
        IsUiV2 = $isUiV2
        IsResources = $inResources
    }
}

$Used = @($Results | Where-Object { $_.Classification -eq "A. Used confirmed" })
$Candidate = @($Results | Where-Object { $_.Classification -eq "B. Candidate unused" })
$Hold = @($Results | Where-Object { $_.Classification -eq "C. Hold" })
$UiV2 = @($Results | Where-Object { $_.IsUiV2 })

function Write-ImageSection {
    param(
        [System.Text.StringBuilder]$Builder,
        [string]$Title,
        [object[]]$Items
    )
    [void]$Builder.AppendLine("## $Title")
    [void]$Builder.AppendLine("")
    if ($Items.Count -eq 0) {
        [void]$Builder.AppendLine("- None")
        [void]$Builder.AppendLine("")
        return
    }
    foreach ($item in ($Items | Sort-Object Path)) {
        [void]$Builder.AppendLine("- ``$($item.Path)``")
        [void]$Builder.AppendLine("  - guid: ``$($item.Guid)``")
        [void]$Builder.AppendLine("  - classification: $($item.Classification)")
        [void]$Builder.AppendLine("  - GUID reference found: $(if ($item.GuidRefs.Count -gt 0) { "yes" } else { "no" })")
        [void]$Builder.AppendLine("  - GUID reference files: $(Format-RefList $item.GuidRefs)")
        [void]$Builder.AppendLine("  - string reference found: $(if ($item.StringRefs.Count -gt 0) { "yes" } else { "no" })")
        [void]$Builder.AppendLine("  - string reference files: $(Format-RefList $item.StringRefs)")
        [void]$Builder.AppendLine("  - Resources.Load/path string reference found: $(if ($item.ResourceStringRefs.Count -gt 0 -or $item.ResourcesLoadRefs.Count -gt 0) { "yes" } else { "no" })")
        [void]$Builder.AppendLine("  - SpriteAtlas reference found: $(if ($item.SpriteAtlasRefs.Count -gt 0) { "yes" } else { "no" })")
        [void]$Builder.AppendLine("  - classification reason: $($item.Reason)")
    }
    [void]$Builder.AppendLine("")
}

$byFolder = $Results |
    Group-Object { Split-Path $_.Path -Parent } |
    Sort-Object Name |
    ForEach-Object {
        [pscustomobject]@{
            Folder = $_.Name.Replace("\", "/")
            Total = $_.Count
            Used = @($_.Group | Where-Object { $_.Classification -eq "A. Used confirmed" }).Count
            Candidate = @($_.Group | Where-Object { $_.Classification -eq "B. Candidate unused" }).Count
            Hold = @($_.Group | Where-Object { $_.Classification -eq "C. Hold" }).Count
        }
    }

$report = New-Object System.Text.StringBuilder
$generatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss K"
$extensionsText = [string]::Join(", ", $ImageExtensions)
[void]$report.AppendLine("# Unused Image Audit Report")
[void]$report.AppendLine("")
[void]$report.AppendLine("- Generated at: $generatedAt")
[void]$report.AppendLine("- Audit root: ``Assets/_Project``")
[void]$report.AppendLine("- Target extensions: ``$extensionsText``")
[void]$report.AppendLine("- Note: this is a read-only audit. ``B. Candidate unused`` means candidate only; it is not a deletion instruction.")
[void]$report.AppendLine("")
[void]$report.AppendLine("## Summary")
[void]$report.AppendLine("")
[void]$report.AppendLine("| Item | Count |")
[void]$report.AppendLine("| --- | ---: |")
[void]$report.AppendLine("| Total images | $($Results.Count) |")
[void]$report.AppendLine("| A. Used confirmed | $($Used.Count) |")
[void]$report.AppendLine("| B. Candidate unused | $($Candidate.Count) |")
[void]$report.AppendLine("| C. Hold | $($Hold.Count) |")
[void]$report.AppendLine("| GeneratedRuntimeUI/ui_v2 images | $($UiV2.Count) |")
[void]$report.AppendLine("")
[void]$report.AppendLine("## Method")
[void]$report.AppendLine("")
[void]$report.AppendLine("- The script reads each image ``.meta`` GUID and searches Unity YAML/text asset files for GUID references.")
[void]$report.AppendLine("- String references include full filename, extensionless filename, Resources-relative path, and GeneratedRuntimeUI path.")
[void]$report.AppendLine("- JSON/TXT/Archive hits are listed as evidence, but they do not by themselves promote an image to used-confirmed.")
[void]$report.AppendLine("- Resources items with ambiguous direct references are held for manual review because dynamic loading is possible.")
[void]$report.AppendLine("- ``.spriteatlas`` GUID references are reported separately.")
[void]$report.AppendLine("")
[void]$report.AppendLine("## Folder Summary")
[void]$report.AppendLine("")
[void]$report.AppendLine("| Folder | Total | Used confirmed | Candidate | Hold |")
[void]$report.AppendLine("| --- | ---: | ---: | ---: | ---: |")
foreach ($folder in $byFolder) {
    [void]$report.AppendLine("| ``$(Escape-Markdown $folder.Folder)`` | $($folder.Total) | $($folder.Used) | $($folder.Candidate) | $($folder.Hold) |")
}
[void]$report.AppendLine("")

Write-ImageSection $report "GeneratedRuntimeUI/ui_v2 Section" $UiV2
Write-ImageSection $report "A. Used Confirmed Images" $Used
Write-ImageSection $report "B. Candidate Unused Images (candidate only, do not delete)" $Candidate
Write-ImageSection $report "C. Hold Images" $Hold

[System.IO.File]::WriteAllText($ReportPath, $report.ToString(), [System.Text.Encoding]::UTF8)

[pscustomobject]@{
    ReportPath = $ReportPath
    TotalImages = $Results.Count
    Used = $Used.Count
    Candidate = $Candidate.Count
    Hold = $Hold.Count
    UiV2 = $UiV2.Count
} | Format-List
