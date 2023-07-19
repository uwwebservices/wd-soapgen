using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

/* Assumptions:
 * - Return types are Task wrapped.
 */

namespace WD.SoapGen.Code;

public class Context
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public NamespaceDeclarationSyntax Namespace { get; set; }
    public string HeaderTrivia { get; set; }
    public string? SvcutilVersion { get; set; }
    public InterfaceDeclarationSyntax Port { get; set; }
    public InterfaceDeclarationSyntax PortChannel { get; set; }
    public ClassDeclarationSyntax Client { get; set; }
    public ImmutableDictionary<string, ClassDeclarationSyntax> Types { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}

