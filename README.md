# wd-soapgen [![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](./LICENSE)
Meld [xscgen](https://www.nuget.org/packages/dotnet-xscgen/) and [dotnet-svcutil](https://www.nuget.org/packages/dotnet-svcutil) tools to create corrected Workday SOAP client libraries.

## Install
```
git clone https://github.com/uwwebservices/wd-soapgen.git
cd wd-soapgen
.\install.ps1
```

Tested with `dotnet-svcutil@2.1.0` and `xscgen@2.0.810.0`.

## Project Assumptions
This tool assumes that you isolate generated service code to dedicated projects. For example:
```
- WorkdayProject
  - WorkdayProject.sln
  - WD.V35.FinancialManagement
    - WD.V35.FinancialManagement.csproj (classlib)
  - WorkdayWorker
    - WorkdayWorker.csproj (console)
    - Program.cs
```
After running:

`wd-soapgen https://community.workday.com/sites/default/files/file-hosting/productionapi/Financial_Management/v35.0/Financial_Management.wsdl`

in `WD.V35.FinancialManagement` the file system layout would look like:
```
- WorkdayProject
  - WorkdayProject.sln
  - WD.V35.FinancialManagement
    - WD.V35.FinancialManagement.csproj (classlib)
    - Interfaces.g.cs
    - Client.g.cs
    - Types.g.cs
    - dotnet-svcutil.params.json
  - WorkdayWorker
    - WorkdayWorker.csproj (console)
    - Program.cs
```

## Usage
### First-time
```
cd WD.V35.FinancialManagement
wd-soapgen https://community.workday.com/sites/default/files/file-hosting/productionapi/Financial_Management/v35.0/Financial_Management.wsdl
```
### Regenerate (and update dependencies)
```
cd WD.V35.FinancialManagement
wd-soapgen https://community.workday.com/sites/default/files/file-hosting/productionapi/Financial_Management/v35.0/Financial_Management.wsdl
```

### Regenerate (and do not update dependencies)
```
cd WD.V35.FinancialManagement
wd-soapgen --no-install https://community.workday.com/sites/default/files/file-hosting/productionapi/Financial_Management/v35.0/Financial_Management.wsdl
```

## Why?
The WCF team has acknowledged a [bug](https://github.com/dotnet/wcf/issues/3812) in `dotnet-svcutil` that prevents `dotnet-svcutil` from reusing types in referenced assemblies. This is a problem because `dotnet-svcutil` has also acknowledged another [bug](https://github.com/dotnet/wcf/issues/2219) that produces incorrect types and annotations when collapsing SOME container types into jagged arrays. When these bugs are fixed, it MAY be trivial to generate classes with `xscgen` and then point `dotnet-svcutil` at those types or just use `dotnet-svcutil` directly. Until then, this application:
- Creates files with both tools.
  - WD.V35.FinancialManagement.cs
  - Svcutil.cs
- Merges Svcutil Input/Output types with XscGen's types, issuing corrections to the Svcutil file.
- Tree shakes dead code using the Port interface as root.
- Coalesces interfaces, types, and the client their own respective files.
