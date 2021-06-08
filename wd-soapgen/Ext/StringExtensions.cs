using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace WD.SoapGen.Ext
{
    public static class StringExtensions
    {
        [return: NotNullIfNotNull("d")]
        public static string? None(this string? s, string? d)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return d;
            }

            return s;
        }

        public static bool None([NotNullWhen(false)] this string? s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static bool Some([NotNullWhen(true)] this string? s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        public static void Some(this string? s, Action cb)
        {
            if (s.Some())
            {
                cb?.Invoke();
            }
        }

        public static bool EndsWithEither(this string test, params string[] endings)
        {
            return endings.Any(e => test.EndsWith(e));
        }
    }
}