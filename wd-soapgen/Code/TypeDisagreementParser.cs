using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Linq;
using System.Text;
using WD.SoapGen.Ext;

namespace WD.SoapGen.Code
{
    public class TypeDisagreementParser
    {
        public TypeDisagreementParser()
        {
        }

        public IEnumerable<TypeDisagreement> Parse(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("file not found", path);
            }

            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
            var root = tree.GetRoot();

            var ios = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.ValueText.EndsWithEither("RequestType", "ResponseType"));

            var results = new List<TypeDisagreement>();

            foreach (var clazz in ios)
            {
                var attributes = clazz.AttributeLists.SelectMany(a => a.Attributes);
                var xmlType = attributes.FirstOrDefault(a => a.Name?.ToString() == "System.Xml.Serialization.XmlTypeAttribute");

                if (xmlType is not null)
                {
                    var classname = clazz.Identifier.Text;
                    var typename = xmlType.ArgumentList!.Arguments
                        .First()
                        .ToFullString()
                        .Replace("\"", "") // unquote
                        .Replace("-", ""); // dotnet-svcutil does not use -, it is dropped

                    if (!classname.Equals(typename))
                    {
                        results.Add(new TypeDisagreement
                        {
                            ClassName = classname,
                            TypeName = typename
                        });
                    }
                }
            }

            return results;
        }
    }
}
