﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WD.SoapGen.Ext;
using System.Diagnostics;
using System.Collections.ObjectModel;

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
        public static string Xscgen(SoapGenArguments args)
        {
            Console.WriteLine($"Generating types with xscgen...");
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
                        $"urn:com.workday/bsvc={args.Namespace}",
                        "--order",
                        args.Xsd
                    },
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = args.Directory,
                },
            };

            return Run("xscgen", proc, TimeSpan.FromSeconds(60));
        }

        /// <summary>
        /// Call dotnet-svcutil like:
        /// dotnet-svcutil Resource_Management.wsdl --outputDir Service --serializer XmlSerializer --projectFile UWD.Lib/UWD.Lib.csproj --namespace "*,UWD.Lib" 
        /// </summary>
        /// <param name="args"></param>
        public static string Svcutil(SoapGenArguments args)
        {
            Console.WriteLine($"Generating client with dotnet-svcutil...");
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ResolveBin("dotnet-svcutil"),
                    ArgumentList =
                    {
                        args.Wsdl,
                        "--outputDir",
                        args.Directory,
                        "--outputFile",
                        args.SvcutilFile(),
                        "--serializer",
                        "XmlSerializer",
                        "--projectFile",
                        args.ProjectFile(),
                        "--namespace",
                        $"\"*,{args.Namespace}\"",
                        "--noLogo"
                    },
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = args.Directory
                }
            };

            return Run("dotnet-svcutil", proc, TimeSpan.FromSeconds(60));
        }

        public static void AddPackage(SoapGenArguments args, string package, string version = "")
        {
            var projectfile = args.ProjectFile();
            Console.WriteLine($"Adding ServiceModel dependencies to {projectfile}...");

            var arglist = new Collection<string>
            {
                "add",
                projectfile,
                "package",
                package
            };

            if (version.Some())
            {
                arglist.Add("--version");
                arglist.Add(version);
            }

            var start = new ProcessStartInfo
            {
                FileName = "dotnet",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = args.Directory
            };
            foreach (var ar in arglist)
            {
                start.ArgumentList.Add(ar);
            }

            var proc = new Process
            {
                StartInfo = start
            };

            Run("dotnet", proc, TimeSpan.FromSeconds(30));
        }

        static string Run(string toolName, Process proc, TimeSpan timeout)
        {
            var args = $"{toolName} {string.Join(" ", proc.StartInfo.ArgumentList)}";
            proc.Start();

            if (!proc.WaitForExit((int)timeout.TotalMilliseconds))
            {
                proc.Kill();
                WriteProcOutput(proc);
                throw new ToolingException($"{toolName} timed out after {(int)timeout.TotalSeconds} seconds");
            }

            WriteProcOutput(proc);

            if (proc.ExitCode != 0)
            {
                throw new ToolingException(toolName, proc.ExitCode);
            }

            return args;
        }

        static void WriteProcOutput(Process proc)
        {
            var output = proc.StandardOutput.ReadToEnd().TrimEnd();
            if (!string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Split(Environment.NewLine);
                foreach (var line in lines)
                {
                    Console.WriteLine($"  {line}");
                }
            }
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
}
