using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class SecurityReportScanner
{
    private static readonly string[] SecurityTerms =
    {
        "authenticode", "winverifytrust", "x509", "certificate",
        "strongname", "getpublickeytoken", "signedcms",
        "cryptqueryobject", "signature"
    };

    private static readonly string[] ReportTerms =
    {
        "report", "print", "preview", "document", "export", "pdf"
    };

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine(
                "Usage: SecurityReportScanner <directory> [directory...]");
            return 2;
        }

        foreach (string argument in args)
        {
            string directory = Path.GetFullPath(argument);
            foreach (string file in EnumerateAssemblies(directory))
                Scan(file);
        }
        return 0;
    }

    private static IEnumerable<string> EnumerateAssemblies(string directory)
    {
        return Directory.EnumerateFiles(directory, "*.*")
            .Where(path =>
            {
                string extension = Path.GetExtension(path);
                return extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".exe", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static void Scan(string path)
    {
        ModuleDefMD module;
        try
        {
            module = ModuleDefMD.Load(path);
        }
        catch
        {
            return;
        }

        using (module)
        {
            foreach (TypeDef type in module.GetTypes())
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.IsPinvokeImpl && method.ImplMap != null)
                    {
                        string pinvoke = method.ImplMap.Module.Name + "!" +
                                         method.ImplMap.Name;
                        if (ContainsAny(pinvoke, SecurityTerms))
                            Emit("SEC_PINVOKE", path, method, pinvoke);
                    }

                    if (!method.HasBody)
                        continue;

                    foreach (Instruction instruction in method.Body.Instructions)
                    {
                        IMethod called = instruction.Operand as IMethod;
                        if (called != null)
                        {
                            string target = called.DeclaringType.FullName + "::" +
                                            called.Name;
                            if (ContainsAny(target, SecurityTerms))
                                Emit("SEC_CALL", path, method, target);

                            if (IsReportContext(type.FullName, method.Name) &&
                                ContainsAny(target, ReportTerms))
                                Emit("REPORT_CALL", path, method, target);
                        }

                        string literal = instruction.Operand as string;
                        if (literal != null)
                        {
                            if (ContainsAny(literal, SecurityTerms))
                                Emit("SEC_STRING", path, method, literal);

                            if (IsReportContext(type.FullName, method.Name) &&
                                ContainsAny(literal, ReportTerms))
                                Emit("REPORT_STRING", path, method, literal);
                        }
                    }

                    if (IsReportContext(type.FullName, method.Name))
                    {
                        Console.WriteLine(
                            "REPORT_METHOD|{0}|{1}::{2}",
                            Path.GetFileName(path),
                            type.FullName,
                            method.Name);
                    }
                }
            }
        }
    }

    private static bool IsReportContext(string typeName, string methodName)
    {
        return ContainsAny(typeName, ReportTerms) ||
               ContainsAny(methodName, ReportTerms);
    }

    private static bool ContainsAny(string value, IEnumerable<string> terms)
    {
        return terms.Any(term =>
            value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void Emit(
        string kind,
        string path,
        MethodDef caller,
        string detail)
    {
        Console.WriteLine(
            "{0}|{1}|{2}::{3}|{4}",
            kind,
            Path.GetFileName(path),
            caller.DeclaringType.FullName,
            caller.Name,
            detail.Replace("\r", "\\r").Replace("\n", "\\n"));
    }
}
