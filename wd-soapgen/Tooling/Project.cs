using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using WD.SoapGen.Ext;
using WD.SoapGen.Code;

namespace WD.SoapGen.Tooling
{
    public class Project
    {
        public static bool TryResolveProject(string directory, [NotNullWhen(true)] out string? project)
        {
            project = null;
            var csproj = Directory.GetFiles(directory).FirstOrDefault(s => s.EndsWith(".csproj"));

            if (csproj.None())
            {
                return false;
            }

            project = Path.GetFileNameWithoutExtension(csproj);
            return true;
        }

        public static void CleanUp(SoapGenArguments args)
        {
            var xscgenOutput = args.XscgenFile();
            if (File.Exists(xscgenOutput))
            {
                Console.WriteLine($"Deleting {xscgenOutput} ...");
                File.Delete(xscgenOutput);
            }

            var serviceFile = args.SvcutilFile();
            if (File.Exists(serviceFile))
            {
                Console.WriteLine($"Deleting {serviceFile} ...");
                File.Delete(serviceFile);
            }

            var interfaceFile = args.Coalesced(ContextRenderer.InterfaceFile);
            if (File.Exists(interfaceFile))
            {
                Console.WriteLine($"Deleting {interfaceFile} ...");
                File.Delete(interfaceFile);
            }

            var clientFile = args.Coalesced(ContextRenderer.ClientFile);
            if (File.Exists(clientFile))
            {
                Console.WriteLine($"Deleting {clientFile} ...");
                File.Delete(clientFile);
            }

            var typesFile = args.Coalesced(ContextRenderer.TypesFile);
            if (File.Exists(typesFile))
            {
                Console.WriteLine($"Deleting {typesFile} ...");
                File.Delete(typesFile);
            }
        }

        public static bool IsReady(SoapGenArguments args, out IEnumerable<string> files)
        {
            var fis = new List<string>();
            var ret = true;

            var xscgenOutput = args.XscgenFile();
            if (File.Exists(xscgenOutput))
            {
                fis.Add(xscgenOutput);
                ret = false;
            }

            var serviceFile = args.SvcutilFile();
            if (File.Exists(serviceFile))
            {
                fis.Add(serviceFile);
                ret = false;
            }

            var interfaceFile = args.Coalesced(ContextRenderer.InterfaceFile);
            if (File.Exists(interfaceFile))
            {
                fis.Add(interfaceFile);
                ret = false;
            }

            var clientFile = args.Coalesced(ContextRenderer.ClientFile);
            if (File.Exists(clientFile))
            {
                fis.Add(clientFile);
                ret = false;
            }

            var typesFile = args.Coalesced(ContextRenderer.TypesFile);
            if (File.Exists(typesFile))
            {
                fis.Add(typesFile);
                ret = false;
            }

            files = fis;
            return ret;
        }
    }
}
