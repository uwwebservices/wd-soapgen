using System;
using WD.SoapGen.Tooling;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using WD.SoapGen.Ext;

namespace WD.SoapGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var root = new RootCommand("Generate SOAP Client Library Content")
            {
                new Option<string>("--wsdl", "Link to the wsdl. Use links from https://community.workday.com/sites/default/files/file-hosting/productionapi/versions/index.html")
                {
                    IsRequired = true
                },
                new Option<string>("--dir", () => Directory.GetCurrentDirectory(), "Target project directory. Must contain a csproj file."),
                new Option<string>("--namespace", "Namespace to generate code for. Default project name."),
                new Option<bool>("--regen", "Regenerate the client files from WSDL.")
            };

            root.Handler = CommandHandler.Create<string, string, string, bool>((wsdl, dir, @namespace, regen) =>
            {
                if (!wsdl.StartsWith("https://community.workday.com"))
                {
                    // NOTE(cspital) probably too restrictive
                    Console.WriteLine("Only use this tool to generate clients from Workday's Community API site.");
                    Console.WriteLine("  https://community.workday.com/sites/default/files/file-hosting/productionapi/versions/index.html");
                    return;
                }

                if (!wsdl.EndsWith(".wsdl"))
                {
                    Console.WriteLine($"--wsdl must end in .wsdl");
                    return;
                }

                var xsd = Path.ChangeExtension(wsdl, ".xsd");

                dir = Path.GetFullPath(dir);

                if (!Project.TryResolveProject(dir, out var proj))
                {
                    Console.WriteLine($"Could not find a csproj file in {dir}, --dir must contain a csproj file.");
                    return;
                }

                if (@namespace.None())
                {
                    @namespace = proj.Replace(" ", "_");
                }

                if (regen)
                {
                    Console.WriteLine($"Regenerating files in {dir} ...");
                    Project.CleanUp(dir, proj);
                }

                // TODO(cspital) call xscgen first like: xscgen -o UWD.Lib/ -n "|Resource_Management.xsd=UWD.Lib" --order Resource_Management.xsd
                // TODO(cspital) call dotnet-svcutil like: dotnet-svcutil Resource_Management.wsdl --outputDir Service --serializer XmlSerializer --projectFile UWD.Lib/UWD.Lib.csproj --namespace "*,UWD.Lib" --reference UWD.Lib/UWD.Lib.csproj
                if (!Project.IsReady(dir, proj, out var conflicts))
                {
                    Console.WriteLine($"Project directory {dir} already has generated content, try again with --regen if you wish to regenerate.");
                    foreach (var conflict in conflicts)
                    {
                        Console.WriteLine($"  {Path.GetRelativePath(dir, conflict)}");
                    }
                    return;
                }

                try
                {
                    var xscgenOutput = DotnetTool.Xscgen(new XscgenArguments
                    {
                        Directory = dir,
                        Project = proj,
                        Namespace = @namespace,
                        Xsd = xsd
                    });
                }
                catch (ToolingException te)
                {
                    Console.WriteLine(te.Message);
                    return;
                }
                

                Console.WriteLine(proj);
            });

            root.Invoke(args);
        }
    }
}
