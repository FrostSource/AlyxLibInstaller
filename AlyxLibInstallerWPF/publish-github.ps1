# publish-both.ps1
param(
    [string]$ProjectPath = "AlyxLibInstallerWPF.csproj",
    [string]$SCProfile = "SelfContained",  # Your self-contained profile name
    [string]$FDProfile = "FrameworkDependent", # Your framework-dependent profile name
    [string]$OutputDir = ".\bin\FinalBuilds"
)

Write-Host "Publishing with profile: $SCProfile" -ForegroundColor Cyan
dotnet publish $ProjectPath -p:PublishProfile=$SCProfile

Write-Host "Publishing with profile: $FDProfile" -ForegroundColor Cyan
dotnet publish $ProjectPath -p:PublishProfile=$FDProfile

# Create output directory if it doesn't exist
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Get the assembly name from the csproj
$assemblyName = (Select-Xml -Path $ProjectPath -XPath "//AssemblyName").Node.InnerText
if (-not $assemblyName) {
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
}

Write-Host "Copying and renaming executables..." -ForegroundColor Cyan

# Copy and rename self-contained
$scSource = "bin\Release\net9.0-windows7.0\publish\win-x64\self-contained\$assemblyName.exe"
$scDest = "$OutputDir\AlyxLibInstaller-SC.exe"
if (Test-Path $scSource) {
    Copy-Item $scSource $scDest -Force
    Write-Host "  ✓ Created $scDest" -ForegroundColor Green
} else {
    Write-Host "  ✗ Could not find $scSource" -ForegroundColor Red
}

# Copy and rename framework-dependent
$fdSource = "bin\Release\net9.0-windows7.0\publish\win-x64\framework-dependent\$assemblyName.exe"
$fdDest = "$OutputDir\AlyxLibInstaller-FD.exe"
if (Test-Path $fdSource) {
    Copy-Item $fdSource $fdDest -Force
    Write-Host "  ✓ Created $fdDest" -ForegroundColor Green
} else {
    Write-Host "  ✗ Could not find $fdSource" -ForegroundColor Red
}

Write-Host "`nDone! Executables are in $OutputDir" -ForegroundColor Green