# wd-soapgen
Meld [xscgen](https://www.nuget.org/packages/dotnet-xscgen/) and [dotnet-svcutil](https://www.nuget.org/packages/dotnet-svcutil) tools to create corrected Workday SOAP client libraries.

## Install
```
git clone https://github.com/uwwebservices/wd-soapgen.git
cd wd-soapgen
.\install.ps1
```

## Project Assumptions
This tool assumes that you isolate generated service code to dedicated projects. For example:
```
- WorkdayProject
  - WorkdayProject.sln
  - WD.V35.ResourceManagement
    - WD.V35.ResourceManagement.csproj (classlib)
  - WorkdayWorker
    - WorkdayWorker.csproj (console)
    - Program.cs
```
After running `wd-soapgen` in `WD.V35.ResourceManagement` the file system layout would look like:
```
- WorkdayProject
  - WorkdayProject.sln
  - WD.V35.ResourceManagement
    - WD.V35.ResourceManagement.csproj (classlib)
    - WD.V35.ResourceManagement.cs
    - Service
      - dotnet-svcutil.params.json
      - Reference.cs
  - WorkdayWorker
    - WorkdayWorker.csproj (console)
    - Program.cs
```

## Usage
### First-time
```
cd WD.V35.ResourceManagement
wd-soapgen https://community.workday.com/sites/default/files/file-hosting/productionapi/Resource_Management/v35.0/Resource_Management.wsdl
```
### Regenerate (and update dependencies)
```
cd WD.V35.ResourceManagement
wd-soapgen --clean https://community.workday.com/sites/default/files/file-hosting/productionapi/Resource_Management/v35.0/Resource_Management.wsdl
```

### Regenerate (and do not update dependencies)
```
cd WD.V35.ResourceManagement
wd-soapgen --clean --no-install https://community.workday.com/sites/default/files/file-hosting/productionapi/Resource_Management/v35.0/Resource_Management.wsdl
```

## Why?
The WCF team has acknowledged a [bug](https://github.com/dotnet/wcf/issues/3812) in `dotnet-svcutil` that prevents `dotnet-svcutil` from reusing types in referenced assemblies. This is a problem because `dotnet-svcutil` has also acknowledged another [bug](https://github.com/dotnet/wcf/issues/2219) that produces incorrect types and annotations when collapsing SOME container types into jagged arrays. When these bugs are fixed, it MAY be trivial to generate classes with `xscgen` and then point `dotnet-svcutil` at those types or just use `dotnet-svcutil` directly. Until then, this application creates files with both tools, then uses roslyn to make corrections to the `dotnet-svcutil` `Service/Reference.cs` file so that it uses the types created by `xscgen`.

## Contributing
Contributions are welcome, please open an issue first.
