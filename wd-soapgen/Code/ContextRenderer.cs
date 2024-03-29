﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WD.SoapGen.Code;

public class ContextRenderer
{
    readonly ToolingContext _tooling;
    readonly Context _ctx;
    readonly string _header;

    public ContextRenderer(Context ctx, ToolingContext tooling)
    {
        _ctx = ctx;
        _tooling = tooling;
        _header = GetHeader(ctx, tooling);
    }

    public CoalescedFiles Render(IEnumerable<ClassDeclarationSyntax> classes)
    {
        var i = RenderInterfaces();
        var c = RenderClient();
        var t = RenderTypes(classes);

        return new CoalescedFiles
        {
            Interfaces = i,
            Client = c,
            Types = t
        };
    }

    readonly StringBuilder _sb = new();

    void Reset()
    {
        _sb.Clear();
        _sb.AppendLine(_header);
        _sb.AppendLine();
    }

    string Assemble(params Action<StringBuilder>[] writers)
    {
        Reset();
        _sb.AppendLine($"namespace {_ctx.Namespace.Name.ToString()}");
        _sb.AppendLine(@"{");
        foreach (var writer in writers)
        {
            writer(_sb);
        }
        _sb.AppendLine(@"}");

        return _sb.ToString();
    }

    public const string InterfaceFile = "Interfaces.g.cs";
    NewFile RenderInterfaces()
    {
        return new NewFile
        {
            Filename = InterfaceFile,
            Content = Assemble(
                s => s.AppendLine(_ctx.Port.ToFullString()),
                s => s.AppendLine(_ctx.PortChannel.ToFullString())
            )
        };
    }

    public const string ClientFile = "Client.g.cs";
    NewFile RenderClient()
    {
        return new NewFile
        {
            Filename = ClientFile,
            Content = Assemble(
                s => s.Append(_ctx.Client.ToFullString())
            )
        };
    }

    public const string TypesFile = "Types.g.cs";
    NewFile RenderTypes(IEnumerable<ClassDeclarationSyntax> classes)
    {
        return new NewFile
        {
            Filename = TypesFile,
            Content = Assemble(
                s =>
                {
                    foreach (var cds in classes)
                    {
                        s.AppendLine(cds.ToFullString());
                    }
                }
            )
        };
    }

    static string GetHeader(Context c, ToolingContext t)
    {
        return $@"{c.HeaderTrivia}
// This code was generated by dotnet-svcutil {GetSvcutilVersionText(c)}using the following command:
// {t.SvcutilArgs}

// This code was merged by wd-soapgen version {Assembly.GetExecutingAssembly().GetName().Version} using the following command:
// wd-soapgen {string.Join(" ", Environment.GetCommandLineArgs().Skip(1))}";
    }

    static string GetSvcutilVersionText(Context c)
    {
        if (string.IsNullOrWhiteSpace(c.SvcutilVersion))
        {
            return "";
        }
        return $"version {c.SvcutilVersion} ";
    }
}
