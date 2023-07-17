using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WD.SoapGen.Ext;

/* Assumptions:
 * - Return types are Task wrapped.
 */

namespace WD.SoapGen.Code;

public partial class SyntaxCoalescer
{
    readonly SoapGenArguments _args;

    public SyntaxCoalescer(SoapGenArguments args)
    {
        _args = args;
    }

    public CoalescedFiles Coalesce()
    {
        var svcutilTree = CSharpSyntaxTree.ParseText(File.ReadAllText(_args.SvcutilFile()));
        var svcutilRoot = svcutilTree.GetRoot();
        var xscgenTree = CSharpSyntaxTree.ParseText(File.ReadAllText(_args.XscgenFile()));
        var xscgenRoot = xscgenTree.GetRoot();

        var context = GetContext(svcutilRoot, xscgenRoot);
        var typeCoalescer = new TypeCoalescer(context);
        var saves = typeCoalescer.GetAnchoredTypes();

        return new();
    }

    Context GetContext(SyntaxNode svcutilRoot, SyntaxNode xscgenRoot)
    {
        var ns = svcutilRoot
            .DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .First();

        var port = svcutilRoot
            .DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>()
            .Where(i => i.Identifier.Text.EndsWith("Port"))
            .Single();

        var portChannel = svcutilRoot
            .DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>()
            .Where(i => i.Identifier.Text.EndsWith("PortChannel"))
            .Single();

        var client = svcutilRoot
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.BaseList?.Types.Any(b => b.Type.ToFullString().Contains("ClientBase<")) ?? false)
            .Single();

        var svcUtilClasses = svcutilRoot
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c =>
            {
                return !c.BaseList?.Types.Any(b => b.Type.ToFullString().Contains("ClientBase<")) ?? true;
            });

        var xscGenClasses = xscgenRoot
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        var b = ImmutableDictionary.CreateBuilder<string, ClassDeclarationSyntax>();

        foreach ( var c in svcUtilClasses.Concat(xscGenClasses))
        {
            if (!b.TryAdd(c.Identifier.Text, c))
            {
                throw new InvalidOperationException($"Multiple classes detected with name: {c.Identifier.Text}");
            }
        }

        return new Context
        {
            Namespace = ns,
            Port = port,
            PortChannel = portChannel,
            Client = client,
            Types = b.ToImmutable()
        };
    }
}

public class CoalescedFiles
{

}