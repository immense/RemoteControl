param (
    [Parameter()]
    [string]
    $SolutionDir
)

function Find-Index([string[]]$Array, [string]$Pattern) {
    for ($i = 1; $i -lt $Array.Length; $i++)
    { 
        if ($Array[$i] -ilike "*$Pattern*") {
            return $i
        }
    }
    return -1
}

$ProjFiles = Get-ChildItem -Path $SolutionDir -Recurse -Filter "*.csproj"

foreach ($ProjFile in $ProjFiles) {
    [string[]]$Content = Get-Content -Path $ProjFile.FullName
    $VersionTag = $Content.Where({$_ -like "*<Version>*"})

    if (!$VersionTag) {
        Write-Warning "Version not found in $($ProjFile.FullName)"
        continue
    }

    $Index = Find-Index -Array $Content -Pattern $VersionTag
    
    if ($Index -eq -1) {
        Write-Warning "Index not found for Version in $($ProjFile.FullName)"
        continue
    }

    $VersionString = $VersionTag.Replace("<Version>", "").Replace("</Version>", "").Trim()

    [System.Version]$Version = [System.Version]::new(0,5,0)

    try {
        # Not sure why TryParse throws here.
        if (![System.Version]::TryParse($VersionString, [ref]$Version)) {
            Write-Warning "Failed to parse version string $VersionString"
            $Version = [System.Version]::new(0,5,0)
        }
    }
    catch {
        Write-Error $Error[0]
        $Version = [System.Version]::new(0,5,0)
    }

    $NewVersion = [System.Version]::new($Version.Major, $Version.Minor,$Version.Build + 1)
    Write-Information $NewVersion
    $Content[$Index] = "    <Version>$($NewVersion.ToString())</Version>"

    ($Content | Out-String).Trim() | Out-File -FilePath $ProjFile.FullName -Force -Encoding utf8
}
