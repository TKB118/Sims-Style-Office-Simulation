$basePath = "Assets"
$dirs = @("NPC Worker Data", "NPC Worker Data 2")
$outputPath = Join-Path $basePath "CombinedNPCData.csv"

$allData = @()

foreach ($d in $dirs) {
    $dirPath = Join-Path $basePath $d
    if (Test-Path $dirPath) {
        $files = Get-ChildItem -Path $dirPath -Filter "*.csv"
        Write-Host "Found $($files.Count) CSVs in $d"
        
        foreach ($f in $files) {
            $data = Import-Csv -Path $f.FullName
            # Add SourceFile property
            $data | Add-Member -MemberType NoteProperty -Name "SourceFile" -Value $f.Name
            $allData += $data
        }
    } else {
        Write-Warning "Directory not found: $dirPath"
    }
}

if ($allData.Count -gt 0) {
    $allData | Export-Csv -Path $outputPath -NoTypeInformation -Encoding UTF8
    Write-Host "Successfully created $outputPath with $($allData.Count) rows."
} else {
    Write-Warning "No data found to combine."
}
