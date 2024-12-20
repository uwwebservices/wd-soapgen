﻿using System;
using System.Collections.Generic;
using System.IO;
using WD.SoapGen.Code;
using WD.SoapGen.Tooling;
using XmlSchemaClassGenerator;

namespace WD.SoapGen;

internal class Stage
{
    public static ToolingContext Generate(SoapGenArguments sa)
    {
        var xscgen = new Generator
        {
            OutputFolder = sa.Directory,
            NamespaceProvider = new Dictionary<NamespaceKey, string>
            {
                { new NamespaceKey(), sa.Namespace }
            }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = sa.Namespace }.NamespaceProvider.GenerateNamespace),
            EmitOrder = true,
            NamingProvider = new NamingProvider(NamingScheme.Direct)
        };
        xscgen.Generate(new[] { sa.Xsd });

        var svcArgs = DotnetTool.Svcutil(sa);

        return new ToolingContext
        {
            XscGenArgs = "",
            SvcutilArgs = svcArgs
        };
    }

    public static void Correct(SoapGenArguments sa)
    {
        Console.WriteLine("Merging xscgen and dotnet-svcutil types:");
        Console.WriteLine($"  xscgen: {sa.XscgenFile()}");
        Console.WriteLine($"  dotnet-svcutil: {sa.SvcutilFile()}");

        var disagreements = new TypeDisagreementParser().Parse(sa.XscgenFile());

        var parser = new ClientParser(new ServiceRewriter(sa, disagreements));
        var service = parser.Extract(sa.SvcutilFile());

        File.WriteAllText(sa.SvcutilFile(), service.ToString());
    }

    public static void InstallDependencies(SoapGenArguments sa)
    {
        Console.WriteLine("Installing ServiceModel dependencies...");
        Console.WriteLine("  System.ServiceModel.Duplex...");
        DotnetTool.AddPackage(sa, "System.ServiceModel.Duplex");

        Console.WriteLine("  System.ServiceModel.Http...");
        DotnetTool.AddPackage(sa, "System.ServiceModel.Http");

        Console.WriteLine("  System.ServiceModel.NetTcp...");
        DotnetTool.AddPackage(sa, "System.ServiceModel.NetTcp");

        Console.WriteLine("  System.ServiceModel.Security...");
        DotnetTool.AddPackage(sa, "System.ServiceModel.Security");
    }

    public static CoalescedFiles Coalesce(SoapGenArguments sa, ToolingContext toolContext)
    {
        Console.WriteLine("Coalescing artifacts...");
        return SyntaxCoalescer.Coalesce(sa, toolContext);
    }

    public static void Overwrite(SoapGenArguments sa, CoalescedFiles files)
    {
        Console.WriteLine("Overwrite files...");
        Write(sa, files.Interfaces);
        Write(sa, files.Client);
        Write(sa, files.Types);
        Console.WriteLine($"  Deleting {sa.SvcutilFile()}");
        File.Delete(sa.SvcutilFile());
        Console.WriteLine($"  Deleting {sa.XscgenFile()}");
        File.Delete(sa.XscgenFile());
    }

    static void Write(SoapGenArguments sa, NewFile file)
    {
        var path = sa.Coalesced(file.Filename);
        Console.WriteLine($"  Writing {path}");
        File.WriteAllText(path, file.Content);
    }
}

public class ToolingContext
{
    public string XscGenArgs { get; set; } = "";
    public string SvcutilArgs { get; set; } = "";
}