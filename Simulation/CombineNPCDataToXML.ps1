$basePath = "Assets"
$dirs = @("NPC Worker Data", "NPC Worker Data 2")
$outputPath = Join-Path $basePath "CombinedNPCData.xml"

# XML Header
$xml = @"
<?xml version="1.0"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:o="urn:schemas-microsoft-com:office:office"
 xmlns:x="urn:schemas-microsoft-com:office:excel"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:html="http://www.w3.org/TR/REC-html40">
 <Styles>
  <Style ss:ID="Default" ss:Name="Normal">
   <Alignment ss:Vertical="Bottom"/>
   <Borders/>
   <Font ss:FontName="Calibri" x:Family="Swiss" ss:Size="11" ss:Color="#000000"/>
   <Interior/>
   <NumberFormat/>
   <Protection/>
  </Style>
 </Styles>
"@

Function Escape-Xml ($text) {
    if ([string]::IsNullOrEmpty($text)) { return "" }
    return $text.ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace('"', "&quot;").Replace("'", "&apos;")
}

foreach ($d in $dirs) {
    $dirPath = Join-Path $basePath $d
    if (Test-Path $dirPath) {
        $files = Get-ChildItem -Path $dirPath -Filter "*.csv"
        
        foreach ($f in $files) {
            # Sheet name (max 31 chars, forbidden chars replaced)
            $sheetName = $f.BaseName -replace '[\\/?*\[\]]', '_'
            if ($sheetName.Length -gt 31) {
                $sheetName = $sheetName.Substring(0, 31)
            }
            
            $xml += " <Worksheet ss:Name=`"$(Escape-Xml $sheetName)`">`n"
            $xml += "  <Table>`n"
            
            $data = Import-Csv -Path $f.FullName
            
            if ($data.Count -gt 0) {
                # Header Row
                $headers = $data[0].PSObject.Properties.Name
                $xml += "   <Row>`n"
                foreach ($h in $headers) {
                    $xml += "    <Cell><Data ss:Type=`"String`">$(Escape-Xml $h)</Data></Cell>`n"
                }
                $xml += "   </Row>`n"
                
                # Data Rows
                foreach ($row in $data) {
                    $xml += "   <Row>`n"
                    foreach ($h in $headers) {
                        $val = $row.$h
                        $xml += "    <Cell><Data ss:Type=`"String`">$(Escape-Xml $val)</Data></Cell>`n"
                    }
                    $xml += "   </Row>`n"
                }
            }
            
            $xml += "  </Table>`n"
            $xml += " </Worksheet>`n"
        }
    }
}

$xml += "</Workbook>"

# Write to file (UTF8)
[System.IO.File]::WriteAllText($outputPath, $xml, [System.Text.Encoding]::UTF8)
Write-Host "Successfully created $outputPath"
