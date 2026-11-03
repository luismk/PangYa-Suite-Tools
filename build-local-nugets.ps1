[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string] $Configuration = "Release"
)

$versionStamp = [DateTime]::UtcNow.ToString("yyyyMMddHHmmssfff")
$packageVersion = "1.0.0-local.$versionStamp"
$solutionPath = Join-Path $PSScriptRoot "PangyaAPI\PangyaAPI.slnx"

Write-Host "Building PangyaAPI packages version $packageVersion"
& dotnet build $solutionPath `
    --configuration $Configuration `
    "-p:PackageVersion=$packageVersion"

if ($LASTEXITCODE -ne 0) {
    throw "PangyaAPI package build failed with exit code $LASTEXITCODE."
}

Write-Host "Packages written to D:\devtest\.nuget-local"
