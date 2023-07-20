using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WD.SoapGen.Code;

public static class TypeExt
{
    public static string GetNameString(this TypeSyntax type)
    {
        return type switch
        {
            GenericNameSyntax g => g.Identifier.Text,
            IdentifierNameSyntax i => i.Identifier.Text,
            QualifiedNameSyntax q => q.Right.Identifier.Text,
            SimpleNameSyntax s => s.Identifier.Text,
            PredefinedTypeSyntax p => p.Keyword.Text,
            ArrayTypeSyntax => "[]",
            _ => throw new NotImplementedException($"GetNameString({type.GetType()})")
        };
    }

    public static bool TryGetName(this TypeSyntax type, out SimpleNameSyntax? name)
    {
        name = type switch
        {
            GenericNameSyntax g => g,
            IdentifierNameSyntax i => i,
            QualifiedNameSyntax q => q.Right,
            SimpleNameSyntax s => s,
            _ => null
        };
        return name is not null;
    }

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
    public static IEnumerable<TypeSyntax> GetInstanceMembers(this ClassDeclarationSyntax cls)
    {
        var data = cls.Members
            .OfType<MemberDeclarationSyntax>()
            .Where(m =>
            {
                if (!m.IsPublicInstance())
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
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

    public static bool IsPublicInstance(this MemberDeclarationSyntax m)
    {
        return m.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) ||
            !m.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
    }
}