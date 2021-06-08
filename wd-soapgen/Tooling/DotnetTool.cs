using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WD.SoapGen.Ext;
using System.Diagnostics;

namespace WD.SoapGen.Tooling
{
    public class DotnetTool
    {
        public static bool IsInstalled(string toolName)
        {
            var tool = ResolveBin(toolName);

            return File.Exists(tool);
        }

        /// <summary>
        /// Call dotnet-xscgen like:
        /// xscgen -o UWD.Lib/ -n "|Resource_Management.xsd=UWD.Lib" --order Resource_Management.xsd
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="namespace"></param>
        /// <param name="xsd"></param>
        /// <returns></returns>
        public static string Xscgen(XscgenArguments args)
        {
            Console.WriteLine($"Generating types with xscgen from {args.Xsd}...");
            var document = Path.GetFileName(args.Xsd);
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ResolveBin("xscgen"),
                    ArgumentList =
                    {
                        "-o",
                        args.Directory,
                        "-n",
                        $"|{document}={args.Namespace}",
                        "--order",
                        args.Xsd
                    },
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = args.Directory,
                },
            };

            proc.Start();

            if (!proc.WaitForExit((int)TimeSpan.FromSeconds(60).TotalMilliseconds))
            {
                proc.Kill();
                throw new ToolingException("xscgen timed out after 60 seconds");
            }

            if (proc.ExitCode != 0)
            {
                throw new ToolingException("xscgen", proc.ExitCode);
            }

            return args.GetGeneratedFile();
        }

        static string ResolveBin(string toolName)
        {
            // os specific location inspection
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                toolName += ".exe";
            }

            var home = Environment.SpecialFolder.UserProfile.GetFolderPath();

            return Path.Combine(home, ".dotnet", "tools", toolName);
        }
    }

    public record XscgenArguments
    {
        public string Directory { get; init; } = "";
        public string Project { get; init; } = "";
        public string Namespace { get; init; } = "";
        public string Xsd { get; init; } = "";

        public string GetGeneratedFile()
        {
            return Path.Combine(Directory, $"{Project}.cs");
        }
    }
}
