param (
    [Parameter()]
    [string]
    $SolutionDir
)


$ProjFiles = Get-ChildItem -Path $SolutionDir -Recurse -Filter "*.csproj"

foreach ($ProjFile in $ProjFiles) {
    [string[]]$Content = Get-Content -Path $ProjFile.FullName
    $VersionTag = $Content.Where({$_ -like "*<Version>*"})

    if (!$VersionTag) {
        continue
    }

    $Index = $Content.IndexOf($VersionTag)
    $VersionString = $VersionTag.Replace("<Version>", "").Replace("</Version>", "").Trim()

    [System.Version]$Version

    try {
        # Not sure why TryParse throws here.
        if (![System.Version]::TryParse($VersionString, [ref]$Version)) {
            $Version = [System.Version]::new(0,5,0)
        }
    }
    catch {
        $Version = [System.Version]::new(0,5,0)
    }

    $Version = [System.Version]::new($Version.Major, $Version.Minor, $Version.Build + 1)

    $Content[$Index] = "<Version>$Version</Version>"

    ($Content | Out-String).Trim() | Out-File -FilePath $ProjFile.FullName -Force -Encoding utf8
}
