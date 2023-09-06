using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WD.SoapGen.Ext;

/* Assumptions:
 * - Return types are Task wrapped.
 */

namespace WD.SoapGen.Code;

public class SyntaxCoalescer
{
    public static CoalescedFiles Coalesce(SoapGenArguments args, ToolingContext toolingContext)
    {
        var svcutilTree = CSharpSyntaxTree.ParseText(File.ReadAllText(args.SvcutilFile()));
        var svcutilRoot = svcutilTree.GetRoot();
        var xscgenTree = CSharpSyntaxTree.ParseText(File.ReadAllText(args.XscgenFile()));
        var xscgenRoot = xscgenTree.GetRoot();
        var svcParams = GetSvcParams(args);

        var context = GetContext(svcutilRoot, xscgenRoot, svcParams);

        var extractor = new RequirementExtractor(context.Types);
        var classes = extractor.GetRequirements(context.Port);

        var renderer = new ContextRenderer(context, toolingContext);

        return renderer.Render(classes);
    }

    static SvcutilParamsFile GetSvcParams(SoapGenArguments args)
    {
        if (!File.Exists(args.SvcutilConfigFile()))
        {
            return new SvcutilParamsFile();
        }
        try
        {
            var content = File.ReadAllText(args.SvcutilConfigFile());
            return JsonSerializer.Deserialize<SvcutilParamsFile>(content)!;
        }
        catch
        {
            return new SvcutilParamsFile();
        }
    }

    class SvcutilParamsFile
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";
    }

    static Context GetContext(SyntaxNode svcutilRoot, SyntaxNode xscgenRoot, SvcutilParamsFile svcParams)
    {
        var ns = xscgenRoot
            .DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .First();

        var triv = ns.GetLeadingTrivia().ToString();

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

        foreach (var c in svcUtilClasses.Concat(xscGenClasses))
        {
            if (!b.TryAdd(c.Identifier.Text, c))
            {
                throw new InvalidOperationException($"Type collision: {c.Identifier.Text}");
            }
        }

        var d = b.ToImmutable();

        return new Context
        {
            HeaderTrivia = triv,
            SvcutilVersion = svcParams?.Version,
            Namespace = ns,
            Port = port,
            PortChannel = portChannel,
            Client = client,
            Types = d,
        };
    }
}

public class CoalescedFiles
{
    public NewFile Interfaces { get; set; } = new();
    public NewFile Client { get; set; } = new();
    public NewFile Types { get; set; } = new();
}

public class NewFile
{
    public string Filename { get; set; } = "";
    public string Content { get; set; } = "";
}
