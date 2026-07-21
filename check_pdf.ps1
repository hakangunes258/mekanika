$dir = 'C:\Users\hakan.gunes\Desktop\Mekanika_ai\Pages'
$files = Get-ChildItem $dir -Filter '*.razor'
foreach ($f in $files) {
    $text = [System.IO.File]::ReadAllText($f.FullName)
    $hasTrue  = $text.Contains('isSavingPdf = true')
    $hasFalse = $text.Contains('isSavingPdf = false')
    if ($hasTrue -and (-not $hasFalse)) {
        Write-Host ("PROBLEM: " + $f.Name + " has isSavingPdf = true but NO isSavingPdf = false")
    } elseif ($hasTrue -and $hasFalse) {
        Write-Host ("OK: " + $f.Name + " has both true and false")
    }
}
Write-Host "Done."
