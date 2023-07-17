using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;

/* Assumptions:
 * - Return types are Task wrapped.
 */

namespace WD.SoapGen.Code;

class Context
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public NamespaceDeclarationSyntax Namespace { get; set; }
    public InterfaceDeclarationSyntax Port { get; set; }
    public InterfaceDeclarationSyntax PortChannel { get; set; }
    public ClassDeclarationSyntax Client { get; set; }
    public ImmutableDictionary<string, ClassDeclarationSyntax> Types { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public IEnumerable<ClassDeclarationSyntax> GetDependentRoots()
    {
        var set = new HashSet<ClassDeclarationSyntax>(new ClassDeclarationEquality());
        var methods = Port
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            set.Add(GetContractReturnType(method));
            foreach (var parm in GetContractParameters(method))
            {
                set.Add(parm);
            }
            foreach (var attr in GetFaultAttributes(method))
            {
                set.Add(attr);
            }
        }

        return set;
    }

    IEnumerable<ClassDeclarationSyntax> GetFaultAttributes(MethodDeclarationSyntax method)
    {
        foreach (var attr in method.AttributeLists.SelectMany(a => a.Attributes))
        {
            var name = attr.Name switch
            {
                QualifiedNameSyntax q => q.Right.Identifier.Text,
                SimpleNameSyntax s => s.Identifier.Text
            };

            if (name != "FaultContractAttribute")
            {
                continue;
            }

            var arg = attr.ArgumentList?.Arguments.First();
            // TODO(cspital) extract the typeof value and find the class declaration

            if (!Types.TryGetValue(name, out var type))
            {
                throw new InvalidOperationException($"Type not found: {name}");
            }

            yield return type;
        }
    }

    ClassDeclarationSyntax GetContractReturnType(MethodDeclarationSyntax method)
    {
        var ret = method.ReturnType;
        var ty = GetName(ret);

        if (ty is GenericNameSyntax g)
        {
            var arg = g.TypeArgumentList.Arguments.First();
            ty = GetName(arg);
        }

        if (!Types.TryGetValue(ty.Identifier.Text, out var cls))
        {
            throw new InvalidOperationException($"Type not found: {ty.Identifier.Text}");
        }
        return cls;
    }

    public SimpleNameSyntax GetName(TypeSyntax type)
    {
        return type switch
        {
            IdentifierNameSyntax i => i,
            QualifiedNameSyntax q => q.Right,
            SimpleNameSyntax s => s,
            _ => throw new NotImplementedException($"NameOf({type.GetType()})")
        };
    }

    IEnumerable<ClassDeclarationSyntax> GetContractParameters(MethodDeclarationSyntax method)
    {
        var parms = method.ParameterList.Parameters;
        foreach (var param in parms)
        {
            var name = param.Type switch
            {
                QualifiedNameSyntax q => q.Right.Identifier.Text,
                SimpleNameSyntax s => s.Identifier.Text
            };

            if (!Types.TryGetValue(name, out var type))
            {
                throw new InvalidOperationException($"Type not found: {name}");
            }

            yield return type;
        }
    }
}

class ClassDeclarationEquality : IEqualityComparer<ClassDeclarationSyntax>
{
    public bool Equals(ClassDeclarationSyntax? x, ClassDeclarationSyntax? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        return x.Identifier.Equals(y.Identifier);
    }

    public int GetHashCode([DisallowNull] ClassDeclarationSyntax obj)
    {
        return obj.Identifier.GetHashCode();
    }
}