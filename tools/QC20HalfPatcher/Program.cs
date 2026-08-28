using System;
using System.Globalization;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine(
                "Usage: QC20ScalePatcher <input.dll> <output.dll> <factor>");
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
                "Renishaw.Calibration.Ballbar.BallbarInstrument",
                false);
            if (type == null)
                throw new InvalidOperationException("BallbarInstrument type was not found.");

            MethodDef method = type.Methods.SingleOrDefault(m =>
                m.Name == "ReadRecord" &&
                m.MethodSig != null &&
                m.MethodSig.Params.Count == 4 &&
                m.MethodSig.RetType.ElementType == ElementType.I4);
            if (method == null)
                throw new InvalidOperationException("Expected ReadRecord method was not found.");

            FieldDef field = type.Fields.SingleOrDefault(f => f.Name == "m_buffer");
            if (field == null)
                throw new InvalidOperationException("m_buffer field was not found.");

            Instruction originalCall = method.Body.Instructions.SingleOrDefault(instruction =>
            {
                IMethod called = instruction.Operand as IMethod;
                return instruction.OpCode == OpCodes.Callvirt &&
                       called != null &&
                       called.Name == "ReadBuffer";
            });
            if (originalCall == null)
                throw new InvalidOperationException("ReadBuffer call was not found.");

            IMethod readBufferMethod = (IMethod)originalCall.Operand;
            CilBody body = method.Body;
            body.ExceptionHandlers.Clear();
            body.Instructions.Clear();
            body.Variables.Clear();
            body.InitLocals = true;
            body.MaxStack = 4;

            Local readCount = new Local(module.CorLibTypes.Int32);
            Local endIndex = new Local(module.CorLibTypes.Int32);
            Local loopIndex = new Local(module.CorLibTypes.Int32);
            body.Variables.Add(readCount);
            body.Variables.Add(endIndex);
            body.Variables.Add(loopIndex);

            Instruction loopBody = Instruction.Create(OpCodes.Ldarg_2);
            Instruction loopCheck = Instruction.Create(OpCodes.Ldloc, loopIndex);

            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, method.Parameters[4]));
            body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, readBufferMethod));
            body.Instructions.Add(Instruction.Create(OpCodes.Stloc, readCount));

            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, readCount));
            body.Instructions.Add(Instruction.Create(OpCodes.Add));
            body.Instructions.Add(Instruction.Create(OpCodes.Stloc, endIndex));

            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            body.Instructions.Add(Instruction.Create(OpCodes.Stloc, loopIndex));
            body.Instructions.Add(Instruction.Create(OpCodes.Br, loopCheck));

            body.Instructions.Add(loopBody);
            body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, loopIndex));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, loopIndex));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_R8));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R8, factor));
            body.Instructions.Add(Instruction.Create(OpCodes.Mul));
            body.Instructions.Add(Instruction.Create(OpCodes.Stelem_R8));

            body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, loopIndex));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            body.Instructions.Add(Instruction.Create(OpCodes.Add));
            body.Instructions.Add(Instruction.Create(OpCodes.Stloc, loopIndex));

            body.Instructions.Add(loopCheck);
            body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, endIndex));
            body.Instructions.Add(Instruction.Create(OpCodes.Blt, loopBody));

            body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, readCount));
            body.Instructions.Add(Instruction.Create(OpCodes.Ret));

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
            "ReadRecord now multiplies each returned sample by " +
            factor.ToString("R", CultureInfo.InvariantCulture) + ".");
        return 0;
    }
}
