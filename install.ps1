$svcutil = "$env:USERPROFILE\.dotnet\tools\dotnet-svcutil.exe"
$xscgen = "$env:USERPROFILE\.dotnet\tools\xscgen.exe"
$soapgen = "$env:USERPROFILE\.dotnet\tools\wd-soapgen.exe"

Write-Host "Checking dotnet tool dependencies..."

if (!(Test-Path $svcutil)) {
    Write-Host "dotnet-svcutil not found, installing..."
    dotnet tool install -g dotnet-svcutil
} else {
    Write-Host "dotnet-svcutil found..."
}

Write-Host "Packing wd-soapgen..."
dotnet pack -c Release

if (Test-Path $soapgen) {
    Write-Host "wd-soapgen already installed, updating..."
    dotnet tool update -g --add-source ./wd-soapgen/nupkg wd-soapgen
} else {
    Write-Host "wd-soapgen not found, installing..."
    dotnet tool install -g --add-source ./wd-soapgen/nupkg wd-soapgen
}
