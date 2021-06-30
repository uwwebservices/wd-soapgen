using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WD.SoapGen.Tooling
{
    public class ToolingException : Exception
    {
        public string Tool { get; init; } = "";

        public ToolingException(string tool, int exitCode)
            : base($"Dotnet tool: {tool} encountered an error and returned {exitCode}")
        {
            Tool = tool;
        }

        public ToolingException(string message) : base(message)
        {

        }
    }
}
