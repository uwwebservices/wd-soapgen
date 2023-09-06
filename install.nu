def main [] {
    let svcutil = $"($env.USERPROFILE)\\.dotnet\\tools\\dotnet-svcutil.exe"
    let xscgen = $"($env.USERPROFILE)\\.dotnet\\tools\\xscgen.exe"
    let soapgen = $"($env.USERPROFILE)\\.dotnet\\tools\\wd-soapgen.exe"

    echo "Checking dotnet tool dependencies..."

    if (not ($svcutil | path exists)) {
        echo "dotnet-svcutil not found, installing..."
        dotnet tool install -g dotnet-svcutil
    } else {
        echo "dotnet-svcutil found..."
    }

    echo "Packing wd-soapgen..."
    dotnet pack -c Release
    if ($soapgen | path exists) {
        echo "wd-soapgen already installed, updating..."
        dotnet tool update -g --add-source ./wd-soapgen/nupkg wd-soapgen
    } else {
        echo "wd-soapgen not found, installing..."
        dotnet tool install -g --add-source ./wd-soapgen/nupkg wd-soapgen
    }
}