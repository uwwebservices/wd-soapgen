using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WD.SoapGen.Code;

class RequirementExtractor
{
    readonly IDictionary<string, ClassDeclarationSyntax> _pluck;
    readonly HashSet<ClassDeclarationSyntax> _set = new(new ClassDeclarationEquality());

    public RequirementExtractor(IDictionary<string, ClassDeclarationSyntax> types)
    {
        _pluck = types;
    }

    public IEnumerable<ClassDeclarationSyntax> GetRequirements(InterfaceDeclarationSyntax port, ClassDeclarationSyntax client)
    {
        var portMethods = port
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        var clientMethods = client
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in portMethods.Concat(clientMethods))
        {
            foreach (var arg in GetContractReturnTypes(method))
            {
                _set.Add(arg);
            }
            foreach (var parm in GetContractParameters(method))
            {
                _set.Add(parm);
            }
            foreach (var attr in GetFaultAttributes(method))
            {
                _set.Add(attr);
            }
        }

        var search = new Stack<ClassDeclarationSyntax>(_set);

        while (search.Count > 0)
        {
            var current = search.Pop();
            foreach (var memberType in current.GetInstanceMembers())
            {
                foreach (var component in Unwrap(memberType))
                {
                    if (_set.Add(component))
                    {
                        search.Push(component);
                    }
                }
            }
        }

        return _set;
    }

    IEnumerable<ClassDeclarationSyntax> GetFaultAttributes(MethodDeclarationSyntax method)
    {
        foreach (var attr in method.AttributeLists.SelectMany(a => a.Attributes))
        {
            var name = attr.Name switch
            {
                QualifiedNameSyntax q => q.Right.Identifier.Text,
                SimpleNameSyntax s => s.Identifier.Text,
                AliasQualifiedNameSyntax a => a.Name.Identifier.Text,
                _ => attr.Name.ToString()
            };

            if (name != "FaultContractAttribute")
            {
                continue;
            }

            var arg = attr.ArgumentList?.Arguments.First();
            if (arg is null)
            {
                continue;
            }
            var to = (TypeOfExpressionSyntax)arg.Expression;
            name = to.Type.GetNameString();

            if (!_pluck.TryGetValue(name, out var type))
            {
                throw new InvalidOperationException($"Type not found: {name}");
            }

            yield return type;
        }
    }

    IEnumerable<ClassDeclarationSyntax> GetContractReturnTypes(MethodDeclarationSyntax method)
    {
        return Unwrap(method.ReturnType);
    }

    IEnumerable<ClassDeclarationSyntax> GetContractParameters(MethodDeclarationSyntax method)
    {
        var parms = method.ParameterList.Parameters;
        foreach (var param in parms)
        {
            foreach (var t in Unwrap(param.Type!))
            {
                yield return t;
            }
        }
    }

    IEnumerable<ClassDeclarationSyntax> Unwrap(TypeSyntax type)
    {
        if (type is PredefinedTypeSyntax)
        {
            yield break;
        }

        var name = type.GetNameString();
        if (_pluck.TryGetValue(name, out var cds))
        {
            yield return cds;
        }

        var args = GetTypeArguments(type);
        if (!args.Any())
        {
            yield break;
        }

        var stack = new Stack<TypeSyntax>(args);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is PredefinedTypeSyntax)
            {
                continue;
            }
            name = current.GetNameString();
            if (current is not PredefinedTypeSyntax && _pluck.TryGetValue(name, out cds))
            {
                yield return cds;
            }

            args = GetTypeArguments(current);
            if (!args.Any())
            {
                continue;
            }

            foreach (var arg in args)
            {
                stack.Push(arg);
            }
        }
    }

    static IReadOnlyList<TypeSyntax> GetTypeArguments(TypeSyntax type)
    {
        var args = new List<TypeSyntax>();
        if (type is PredefinedTypeSyntax)
        {
            return args;
        }

        if (type is ArrayTypeSyntax a)
        {
            args.Add(a.ElementType);
            return args;
        }

        if (!type.TryGetName(out var name))
        {
            return args;
        }

        if (name is GenericNameSyntax g)
        {
            args.AddRange(g.TypeArgumentList.Arguments);
        }
        return args;
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