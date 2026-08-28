using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class DataSourceAuditor
{
    private static readonly HashSet<string> InterestingMethodNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "ReadRecord",
            "ReadBuffer",
            "get_Readings",
            "get_LatestReading",
            "ParseReadingsString",
            "CreateReadingsString",
            "PopulateRunResult",
            "PopulateSimulationResult",
            "LoadRtbRunResults",
            "StoreRunResults",
            "ReadRunBlock",
            "ReadDoubleField"
        };

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: DataSourceAuditor <directory> [directory...]");
            return 2;
        }

        foreach (string directoryArgument in args)
        {
            string directory = Path.GetFullPath(directoryArgument);
            foreach (string file in EnumerateAssemblies(directory))
                AuditAssembly(file);
        }
        return 0;
    }

    private static IEnumerable<string> EnumerateAssemblies(string directory)
    {
        foreach (string file in Directory.GetFiles(directory, "*.dll"))
            yield return file;
        foreach (string file in Directory.GetFiles(directory, "*.exe"))
            yield return file;
    }

    private static void AuditAssembly(string file)
    {
        ModuleDefMD module;
        try
        {
            module = ModuleDefMD.Load(file);
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
                    if (type.FullName ==
                            "Renishaw.Calibration.Ballbar.BallbarDeviceService." +
                            "QC20BallbarDevice" &&
                        method.Name == "get_LatestReading" &&
                        method.HasBody)
                    {
                        Console.WriteLine(
                            "METHODBODY|{0}|{1}::{2}",
                            Path.GetFileName(file),
                            type.FullName,
                            method.Name);
                        foreach (Instruction bodyInstruction in method.Body.Instructions)
                            Console.WriteLine("IL|" + bodyInstruction);
                    }

                    if (Path.GetFileName(file).IndexOf(
                            "Simulator",
                            StringComparison.OrdinalIgnoreCase) >= 0 &&
                        (type.FullName.IndexOf(
                             "Sim",
                             StringComparison.OrdinalIgnoreCase) >= 0 ||
                         method.Name.IndexOf(
                             "Read",
                             StringComparison.OrdinalIgnoreCase) >= 0 ||
                         method.Name.IndexOf(
                             "Result",
                             StringComparison.OrdinalIgnoreCase) >= 0 ||
                         method.Name.IndexOf(
                             "Capture",
                             StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        Console.WriteLine(
                            "SIMDEF|{0}|{1}::{2}",
                            Path.GetFileName(file),
                            type.FullName,
                            method.Name);
                    }

                    if (InterestingMethodNames.Contains(method.Name))
                    {
                        Console.WriteLine(
                            "DEF|{0}|{1}::{2}",
                            Path.GetFileName(file),
                            type.FullName,
                            method.Name);
                    }

                    if (!method.HasBody)
                        continue;

                    foreach (Instruction instruction in method.Body.Instructions)
                    {
                        IMethod called = instruction.Operand as IMethod;
                        if (called == null ||
                            !InterestingMethodNames.Contains(called.Name))
                            continue;

                        Console.WriteLine(
                            "CALL|{0}|{1}::{2}|{3}::{4}",
                            Path.GetFileName(file),
                            type.FullName,
                            method.Name,
                            called.DeclaringType.FullName,
                            called.Name);
                    }
                }
            }
        }
    }
}
