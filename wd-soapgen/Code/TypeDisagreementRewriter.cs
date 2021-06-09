using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Linq;
using System.Text;

namespace WD.SoapGen.Code
{
    public class DisagreementRewriter : CSharpSyntaxRewriter
    {
        readonly IEnumerable<TypeDisagreement> disagreements;

        public DisagreementRewriter(IEnumerable<TypeDisagreement> disagreements)
        {
            this.disagreements = disagreements;
        }

        public override SyntaxNode? VisitParameter(ParameterSyntax node)
        {
            var disagree = disagreements.FirstOrDefault(d => node.Type!.ToString().EndsWith(d.TypeName));
            if (disagree != null)
            {
                var repl = node.Type!.ToFullString().Replace(disagree.TypeName, disagree.ClassName);
                return base.VisitParameter(node.WithType(SyntaxFactory.ParseTypeName(repl)));
            }
            return base.VisitParameter(node);
        }

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var disagree = disagreements.FirstOrDefault(d => node.Type.ToString().EndsWith(d.TypeName));
            if (disagree != null)
            {
                var repl = node.Type.ToFullString().Replace(disagree.TypeName, disagree.ClassName);
                return base.VisitPropertyDeclaration(node.WithType(SyntaxFactory.ParseTypeName(repl)));
            }
            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var disagree = disagreements.FirstOrDefault(d => node.Type.ToString().EndsWith(d.TypeName));
            if (disagree != null)
            {
                var repl = node.Type.ToFullString().Replace(disagree.TypeName, disagree.ClassName);
                return base.VisitObjectCreationExpression(node.WithType(SyntaxFactory.ParseTypeName(repl)));
            }
            return base.VisitObjectCreationExpression(node);
        }

        public override SyntaxNode? VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var disagree = disagreements.FirstOrDefault(d => node.Type.ToString().EndsWith(d.TypeName));
            if (disagree != null)
            {
                var repl = node.Type.ToFullString().Replace(disagree.TypeName, disagree.ClassName);
                return base.VisitVariableDeclaration(node.WithType(SyntaxFactory.ParseTypeName(repl)));
            }

            return base.VisitVariableDeclaration(node);
        }
    }
}
