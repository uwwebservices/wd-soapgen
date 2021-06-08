using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WD.SoapGen.Ext
{
    public static class SpecialFolderExtensions
    {
        public static string GetFolderPath(this Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }
    }
}
