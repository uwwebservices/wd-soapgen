# wd-soapgen
Meld [xscgen](https://www.nuget.org/packages/dotnet-xscgen/) and [dotnet-svcutil](https://www.nuget.org/packages/dotnet-svcutil) tools to create corrected Workday SOAP client libraries

## Install
```
git clone https://github.com/uwwebservices/wd-soapgen.git
cd wd-soapgen
.\install.ps1
```

## Usage
```
cd WD.V35.ResourceManagement
wd-soapgen --clean https://community.workday.com/sites/default/files/file-hosting/productionapi/Resource_Management/v36.1/Resource_Management.wsdl
```

## Why?
The WCF team has acknowledged a [bug](https://github.com/dotnet/wcf/issues/3812) in `dotnet-svcutil` that prevents `dotnet-svcutil` from reusing types in referenced assemblies. This is a problem because `dotnet-svcutil` has also acknowledged another [bug](https://github.com/dotnet/wcf/issues/2219) that produces incorrect types and annotations when collapsing SOME container types into jagged arrays. When these bugs are fixed, it MAY be trivial to generate classes with `xscgen` and then point `dotnet-svcutil` at those types or just use `dotnet-svcutil` directly. Until then, this application, creates files with both tools, then uses roslyn to make corrections to the `dotnet-svcutil` `Reference.cs` file so that it uses the types created by `xscgen`.