param (
    [Parameter()]
    [string]
    $SolutionDir
)

$DistPath = Join-Path -Path $SolutionDir -ChildPath "dist"

New-Item -Path $DistPath -ItemType Directory -ErrorAction SilentlyContinue

while (!$Files) {
    Start-Sleep -Seconds 1
    $Files = Get-ChildItem -Path $SolutionDir -Recurse -Filter "*.nupkg"
}
Get-ChildItem -Path $SolutionDir -Recurse -Filter "*.nupkg" | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $DistPath -Force
}