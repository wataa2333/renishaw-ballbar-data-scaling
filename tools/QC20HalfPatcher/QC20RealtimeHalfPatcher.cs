using System;
using System.Globalization;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

internal static class QC20RealtimeHalfPatcher
{
    private static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine(
                "Usage: QC20RealtimeScalePatcher <input.dll> <output.dll> <factor>");
            return 2;
        }

        string inputPath = Path.GetFullPath(args[0]);
        string outputPath = Path.GetFullPath(args[1]);
        double factor;
        if (!double.TryParse(
                args[2],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out factor) ||
            double.IsNaN(factor) ||
            double.IsInfinity(factor) ||
            factor <= 0.0 ||
            factor > 1.0)
        {
            Console.Error.WriteLine(
                "Factor must be a finite invariant-culture number in (0, 1].");
            return 2;
        }

        using (ModuleDefMD module = ModuleDefMD.Load(inputPath))
        {
            TypeDef type = module.Find(
                "Renishaw.Calibration.Ballbar.BallbarDeviceService." +
                "QC20BallbarDevice",
                false);
            if (type == null)
                throw new InvalidOperationException(
                    "QC20BallbarDevice type was not found.");

            MethodDef method = type.Methods.SingleOrDefault(m =>
                m.Name == "get_LatestReading" &&
                m.MethodSig != null &&
                m.MethodSig.Params.Count == 0);
            if (method == null || !method.HasBody)
                throw new InvalidOperationException(
                    "QC20 get_LatestReading method was not found.");

            Instruction getValue = method.Body.Instructions.SingleOrDefault(i =>
            {
                IMethod called = i.Operand as IMethod;
                return i.OpCode == OpCodes.Callvirt &&
                       called != null &&
                       called.DeclaringType.FullName ==
                           "Renishaw.Calibration.Ballbar.BallbarReading" &&
                       called.Name == "get_Value";
            });
            if (getValue == null)
                throw new InvalidOperationException(
                    "BallbarReading.get_Value call was not found.");

            int insertionIndex = method.Body.Instructions.IndexOf(getValue) + 1;
            method.Body.Instructions.Insert(
                insertionIndex,
                Instruction.Create(OpCodes.Ldc_R8, factor));
            method.Body.Instructions.Insert(
                insertionIndex + 1,
                Instruction.Create(OpCodes.Mul));

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            ModuleWriterOptions options = new ModuleWriterOptions(module);
            options.Logger = DummyLogger.NoThrowInstance;
            module.Write(outputPath, options);
        }

        Console.WriteLine("Patched: " + inputPath);
        Console.WriteLine("Output:  " + outputPath);
        Console.WriteLine(
            "QC20 get_LatestReading now multiplies the live value by " +
            factor.ToString("R", CultureInfo.InvariantCulture) + ".");
        return 0;
    }
}
