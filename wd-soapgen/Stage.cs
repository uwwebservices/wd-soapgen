using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WD.SoapGen.Code;
using WD.SoapGen.Tooling;

namespace WD.SoapGen;

internal class Stage
{
    public static ToolingContext Generate(SoapGenArguments sa)
    {
        var xscArgs = DotnetTool.Xscgen(sa);
        var svcArgs = DotnetTool.Svcutil(sa);

        return new ToolingContext
        {
            //XscGenArgs = xscArgs,
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
        var coalescer = new SyntaxCoalescer();

        return coalescer.Coalesce(sa, toolContext);
    }

    public static void Overwrite(SoapGenArguments sa, CoalescedFiles files)
    {
        Write(sa, files.Interfaces);
        Write(sa, files.Client);
        Write(sa, files.Types);
        File.Delete(sa.SvcutilFile());
        File.Delete(sa.XscgenFile());
    }

    static void Write(SoapGenArguments sa, NewFile file)
    {
        File.WriteAllText(sa.Coalesced(file.Filename), file.Content);
    }
}

public class ToolingContext
{
    public string XscGenArgs { get; set; }
    public string SvcutilArgs { get; set; }
}