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
    public class ClientParser
    {
        readonly CSharpSyntaxRewriter _rewriter;

        public ClientParser(CSharpSyntaxRewriter rewriter)
        {
            _rewriter = rewriter ?? throw new ArgumentNullException(nameof(rewriter));
        }

        public ServiceFile Extract(string path)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
            var root = tree.GetRoot();

            if (_rewriter != null)
            {
                root = _rewriter.Visit(root);
            }

            var ns = root
                .DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .First();

            var classes = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c =>
                {
                    if (c.Identifier.Text.EndsWithEither("Input", "Output"))
                    {
                        return true;
                    }

                    return c.BaseList?.Types.Any(b => b.Type.ToFullString().Contains("ClientBase<")) ?? false;
                });

            var ifaces = root
                .DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>();

            return new ServiceFile(ns, ((IEnumerable<CSharpSyntaxNode>)classes).Concat(ifaces));
        }
    }
}
