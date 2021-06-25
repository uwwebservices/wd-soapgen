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
    public class ServiceRewriter : CSharpSyntaxRewriter
    {
        readonly SoapGenArguments args;
        readonly string portName;
        readonly IEnumerable<TypeDisagreement> disagreements;

        public ServiceRewriter(SoapGenArguments args, IEnumerable<TypeDisagreement> disagreements)
        {
            this.args = args;
            this.portName = $"{args.Service}Port";
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

        public override SyntaxNode? VisitAttributeArgumentList(AttributeArgumentListSyntax node)
        {
            var idecl = node.Parent?.Parent?.Parent?.Parent;

            if (idecl is null ||
                idecl is not InterfaceDeclarationSyntax iface ||
                !iface.Identifier.Text.Equals(portName))
            {
                return base.VisitAttributeArgumentList(node);
            }

            var mdecl = node.Parent?.Parent?.Parent;
            if (mdecl is null ||
                mdecl.Kind() != SyntaxKind.MethodDeclaration)
            {
                return base.VisitAttributeArgumentList(node);
            }

            var attr = node.Parent as AttributeSyntax;
            if (attr is null)
            {
                return base.VisitAttributeArgumentList(node);
            }

            if (!attr.Name.ToFullString().Equals("System.ServiceModel.FaultContractAttribute"))
            {
                return base.VisitAttributeArgumentList(node);
            }

            var first = node.Arguments.FirstOrDefault(a => a.ToFullString().Contains("Validation_ErrorType[]"));
            if (first is null)
            {
                return base.VisitAttributeArgumentList(node);
            }

            return node.ReplaceNode(first, first.WithExpression(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName($"{args.Namespace}.Validation_FaultType"))));
        }
    }
}
