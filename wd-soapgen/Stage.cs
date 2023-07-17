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
    public static void Generate(SoapGenArguments sa)
    {
        DotnetTool.Xscgen(sa);
        DotnetTool.Svcutil(sa);
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

    public static void Coalesce(SoapGenArguments sa)
    {
        /* TODO(cspital) 
         * locate the ***Port interface
         * identity all input/output types
         * foreach type
         *   depth first search public child properties (or inner type in the case of generic wrappers) and add all encountered types from the same namespace to hashset
         *   only add child properties that are defined in the type pool
         * create new file with the interface definitions
         * create new file with only the types from search result
         * create new file with only the concrete implementation
         * delete early files
         */

        var coalescer = new SyntaxCoalescer(sa);

        var files = coalescer.Coalesce();

        _ = true;
    }
}
