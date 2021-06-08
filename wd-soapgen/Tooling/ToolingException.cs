using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WD.SoapGen.Tooling
{
    public class ToolingException : Exception
    {
        public ToolingException(string tool, int exitCode)
            : base($"Dotnet tool: {tool} encountered an error and returned {exitCode}")
        {

        }

        public ToolingException(string message) : base(message)
        {

        }
    }
}
