using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using WD.SoapGen.Ext;

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

        public static void CleanUp(string directory, string project)
        {
            var xscgenOutput = Path.Combine(directory, $"{project}.cs");
            if (File.Exists(xscgenOutput))
            {
                Console.WriteLine($"Deleting {xscgenOutput} ...");
                File.Delete(xscgenOutput);
            }

            var serviceDirectory = Path.Combine(directory, "Service");
            if (!Directory.Exists(serviceDirectory))
            {
                Console.WriteLine($"Deleting {serviceDirectory} ...");
                Directory.Delete(serviceDirectory, recursive: true);
            }
        }

        public static bool IsReady(string directory, string project, out IEnumerable<string> files)
        {
            var fis = new List<string>();
            var ret = true;

            var xscgenOutput = Path.Combine(directory, $"{project}.cs");
            if (File.Exists(xscgenOutput))
            {
                fis.Add(xscgenOutput);
                ret = false;
            }

            var serviceDirectory = Path.Combine(directory, "Service");
            if (Directory.Exists(serviceDirectory))
            {
                var referenceFile = Path.Combine(serviceDirectory, "Reference.cs");
                if (File.Exists(referenceFile))
                {
                    fis.Add(referenceFile);
                    ret = false;
                }
            }

            files = fis;
            return ret;
        }
    }
}
