using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WD.SoapGen.Code;

class TypeCoalescer
{
    readonly Context _ctx;
    readonly HashSet<ClassDeclarationSyntax> _classes = new HashSet<ClassDeclarationSyntax>(new ClassDeclarationEquality());

    public TypeCoalescer(Context ctx)
    {
        _ctx = ctx;
    }

    public IEnumerable<ClassDeclarationSyntax> GetAnchoredTypes()
    {
        var stack = new Stack<ClassDeclarationSyntax>(_ctx.GetDependentRoots());

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            _classes.Add(current);

            var members = GetTypeMembers(current);

            foreach (var fp in members)
            {
                var ty = MaybeUnwrapType(fp);
                if (ty is null)
                {
                    continue;
                }
                stack.Push(ty);
            }
        }

        return _classes;
    }

    ClassDeclarationSyntax? MaybeUnwrapType(TypeSyntax type)
    {
        if (type is not GenericNameSyntax g)
        {
            var name = _ctx.GetName(type);
            if (!_ctx.Types.TryGetValue(name.Identifier.Text, out var outer))
            {
                // NOTE(cspital) not part of this namespace, skip
                return null;
            }
            return outer;
        }

        var arg = g.TypeArgumentList.Arguments.First();
        var inner = _ctx.GetName(arg);

        if (!_ctx.Types.TryGetValue(inner.Identifier.Text, out var cls))
        {
            // NOTE(cspital) not part of this namespace, skip
            return null;
        }
        return cls;
    }

    IEnumerable<TypeSyntax> GetTypeMembers(ClassDeclarationSyntax cls)
    {
        var data = cls.Members
            .OfType<MemberDeclarationSyntax>()
            .Where(m =>
            {
                if (!m.Modifiers.Any() || m.Modifiers.First().Text != "public")
                {
                    return false;
                }
                return m is PropertyDeclarationSyntax || m is FieldDeclarationSyntax;
            });

        foreach (var d in data)
        {
            var ret = d switch
            {
                PropertyDeclarationSyntax p => p.Type,
                FieldDeclarationSyntax f => f.Declaration.Type
            };

            yield return ret;
        }
    }
}
