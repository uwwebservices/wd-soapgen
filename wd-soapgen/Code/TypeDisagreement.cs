using System;
using System.Collections.Generic;
using System.Text;

namespace WD.SoapGen.Code
{
    public record TypeDisagreement
    {
        public string ClassName { get; init; } = "";
        public string TypeName { get; init; } = "";
    }
}
