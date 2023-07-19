using System;
using WD.SoapGen.Tooling;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using WD.SoapGen.Ext;
using System.Diagnostics.CodeAnalysis;
using WD.SoapGen.Code;
using System.Reflection;

namespace WD.SoapGen
{
    class Program
    {
        static int Main(string[] args)
        {
            var root = new RootCommand(@"Generate Workday's SOAP Client Library Content
  This tool depends on dotnet-svcutil and dotnet-xscgen.
    dotnet-svcutil: dotnet tool install --global dotnet-svcutil
    dotnet-xscgen: dotnet tool install --global dotnet-xscgen")
            {
                new Argument<string>("wsdl", @"Link to the wsdl.
Use links from:
  https://community.workday.com/sites/default/files/file-hosting/productionapi/versions/index.html"),
                new Option<string>(new string[] { "--dir", "-d" }, () => Directory.GetCurrentDirectory(), "Target project directory. Must contain a csproj file."),
                new Option<string>(new string[] { "--namespace", "-n" }, "Namespace to generate code for. Default project name."),
                new Option<bool>(new string[] { "--clean", "-c" }, () => true, "Clean previously generated files."),
                new Option<bool>(new string[] { "--no-install", "-x" }, () => false, "Do not install ServiceModel dependencies.")
            };

            root.Handler = CommandHandler.Create<string, string, string, bool, bool>((wsdl, dir, @namespace, clean, noInstall) =>
            {
                if (!TryDigestArgs(wsdl, dir, @namespace, out var sa))
                {
                    return 1;
                }

                if (clean)
                {
                    Console.WriteLine($"Cleaning up files in {sa.Directory} ...");
                    Project.CleanUp(sa);
                }

                if (!Project.IsReady(sa, out var conflicts))
                {
                    Console.Error.WriteLine($"Project directory {sa.Directory} already has generated content, try again with --clean if you're regenerating.");
                    foreach (var conflict in conflicts)
                    {
                        Console.Error.WriteLine($"  {Path.GetRelativePath(sa.Directory, conflict)}");
                    }
                    return 1;
                }

                try
                {
                    var tooling = Stage.Generate(sa);

                    Stage.Correct(sa);

                    if (!noInstall)
                    {
                        Stage.InstallDependencies(sa);
                    }

                    var files = Stage.Coalesce(sa, tooling);

                    Stage.Overwrite(sa, files);

                    Console.WriteLine("Done.");

                    return 0;
                }
                catch (ToolingException te)
                {
                    Console.Error.WriteLine($"{te.Tool} encountered an error!");
                    Console.Error.WriteLine(te.Message);
                    return 1;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Unexpected error encountered!");
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.ToString());
                    return 1;
                }
            });

            return root.Invoke(args);
        }

        static bool TryDigestArgs(string wsdl, string dir, string @namespace, [NotNullWhen(true)] out SoapGenArguments? args)
        {
            args = null;

            if (!DotnetTool.IsInstalled("xscgen"))
            {
                Console.Error.WriteLine("dotnet-xscgen is not installed, please install with:");
                Console.Error.WriteLine("  dotnet tool install --global dotnet-xscgen");
                return false;
            }

            if (!DotnetTool.IsInstalled("dotnet-svcutil"))
            {
                Console.Error.WriteLine("dotnet-svcutil is not installed, please install with:");
                Console.Error.WriteLine("  dotnet tool install --global dotnet-svcutil");
                return false;
            }

            if (!wsdl.StartsWith("https://community.workday.com"))
            {
                // NOTE(cspital) probably too restrictive
                Console.Error.WriteLine("Only use this tool to generate clients from Workday's Community API site.");
                Console.Error.WriteLine("  https://community.workday.com/sites/default/files/file-hosting/productionapi/versions/index.html");
                return false;
            }

            if (!wsdl.EndsWith(".wsdl"))
            {
                Console.Error.WriteLine($"wsdl must end in .wsdl");
                return false;
            }

            var xsd = Path.ChangeExtension(wsdl, ".xsd");

            dir = Path.GetFullPath(dir);

            if (!Project.TryResolveProject(dir, out var proj))
            {
                Console.Error.WriteLine($"Could not find a csproj file in {dir}, --dir must contain a csproj file.");
                return false;
            }

            if (@namespace.None())
            {
                @namespace = proj.Replace(" ", "_");
            }

            args = new SoapGenArguments
            {
                Wsdl = wsdl,
                Xsd = xsd,
                Directory = dir,
                Project = proj,
                Namespace = @namespace,
                Service = Path.GetFileNameWithoutExtension(wsdl)
            };

            return true;
        }
    }

    public record SoapGenArguments
    {
        public string Wsdl { get; init; } = "";
        public string Xsd { get; set; } = "";
        public string Directory { get; init; } = "";
        public string Project { get; init; } = "";
        public string Namespace { get; init; } = "";
        public string Service { get; init; } = "";

        public string XscgenFile()
        {
            return Path.Combine(Directory, $"{Namespace}.cs");
        }

        public string SvcutilFile()
        {
            return Path.Combine(Directory, "Service", "Reference.cs");
        }

        public string SvcutilConfigFile()
        {
            return Path.Combine(Directory, "Service", "dotnet-svcutil.params.json");
        }

        public string ServiceDirectory()
        {
            return Path.Combine(Directory, "Service");
        }

        public string ProjectFile()
        {
            return Path.Combine(Directory, $"{Project}.csproj");
        }

        public string Coalesced(string filename)
        {
            return Path.Combine(Directory, filename);

        }
    }
}
