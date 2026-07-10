# Convert single-line XML doc comments to multi-line format
# Handles: /// <summary>text</summary>
# Also handles: /// <param name="x">text</param>, /// <returns>text</returns>, etc.

$devicesDir = "g:\PS02Item\src\libs\DeviceLink\devices"

# Get all .cs files
$csFiles = Get-ChildItem -Path $devicesDir -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch '\\obj\\' }

$fixed = 0
$skipped = 0

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Pattern to match single-line XML doc comments
    # Matches: /// <tag>text</tag> where tag can be summary, param, returns, remarks, exception, etc.
    $pattern = '(\s*///\s*<)(summary|param|returns|remarks|exception|example|note|warning|seealso|include|permission)([^>]*>)(.*?)(</\2>)'
    
    $lines = $content -split "`r`n"
    $newLines = @()
    $changed = $false
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        # Check if line matches single-line XML doc pattern
        if ($line -match ('^\s*' + $pattern + '\s*$')) {
            $indent = $line -replace '^(\s*).*', '$1'
            $openTag = $matches[1] + $matches[2] + $matches[3]
            $text = $matches[4].Trim()
            $closeTag = $matches[5]
            
            # Only convert if there's actual text content
            if ($text.Length -gt 0) {
                # Convert to multi-line
                $newLines += "$indent/// <$($matches[2])$($matches[3])"
                $newLines += "$indent/// $text"
                $newLines += "$indent/// </$($matches[2])>"
                $changed = $true
            } else {
                $newLines += $line
            }
        } else {
            $newLines += $line
        }
    }
    
    if ($changed) {
        $newContent = $newLines -join "`r`n"
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -NoNewline
        Write-Host "Fixed: $($file.Name)"
        $fixed++
    } else {
        $skipped++
    }
}

Write-Host "`nSummary:"
Write-Host "  Fixed: $fixed files"
Write-Host "  Skipped: $skipped files (no single-line XML docs)"
