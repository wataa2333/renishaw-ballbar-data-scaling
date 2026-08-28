using System;
using System.Globalization;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace Wataa.QC20Tool
{
    internal sealed class PatchedPair
    {
        public string DirectoryPath;
        public string BallbarPath;
        public string DeviceServicePath;
        public string BallbarHash;
        public string DeviceServiceHash;
        public double Factor;
    }

    internal static class Qc20PatchService
    {
        public static PatchedPair Create(
            BackupSet originals,
            double factor)
        {
            if (originals == null)
                throw new ArgumentNullException("originals");
            if (double.IsNaN(factor) ||
                double.IsInfinity(factor) ||
                factor < 0.01 ||
                factor > 5.0)
                throw new ArgumentOutOfRangeException(
                    "factor", "倍率必须在 0.01 到 5.00 之间。");

            string directory = Path.Combine(
                AppPaths.Temp,
                "patch-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string ballbar = Path.Combine(
                directory, AppInfo.BallbarFile);
            string device = Path.Combine(
                directory, AppInfo.DeviceServiceFile);

            try
            {
                PatchCapturedSamples(
                    originals.BallbarPath, ballbar, factor);
                PatchRealtimeReading(
                    originals.DeviceServicePath, device, factor);

                PatchedPair pair = new PatchedPair();
                pair.DirectoryPath = directory;
                pair.BallbarPath = ballbar;
                pair.DeviceServicePath = device;
                pair.BallbarHash = FileIntegrity.Sha256(ballbar);
                pair.DeviceServiceHash = FileIntegrity.Sha256(device);
                pair.Factor = factor;
                return pair;
            }
            catch
            {
                TryDelete(directory);
                throw;
            }
        }

        public static void Delete(PatchedPair pair)
        {
            if (pair != null)
                TryDelete(pair.DirectoryPath);
        }

        private static void PatchCapturedSamples(
            string inputPath,
            string outputPath,
            double factor)
        {
            using (ModuleDefMD module = ModuleDefMD.Load(inputPath))
            {
                TypeDef type = module.Find(
                    "Renishaw.Calibration.Ballbar.BallbarInstrument",
                    false);
                if (type == null)
                    throw new InvalidOperationException(
                        "未找到 BallbarInstrument 类型。");

                MethodDef method = type.Methods.SingleOrDefault(
                    delegate(MethodDef candidate)
                    {
                        return candidate.Name == "ReadRecord" &&
                            candidate.MethodSig != null &&
                            candidate.MethodSig.Params.Count == 4 &&
                            candidate.MethodSig.RetType.ElementType ==
                                ElementType.I4;
                    });
                if (method == null)
                    throw new InvalidOperationException(
                        "未找到预期 ReadRecord 方法。");

                FieldDef field = type.Fields.SingleOrDefault(
                    delegate(FieldDef candidate)
                    {
                        return candidate.Name == "m_buffer";
                    });
                if (field == null)
                    throw new InvalidOperationException(
                        "未找到 m_buffer 字段。");

                Instruction originalCall =
                    method.Body.Instructions.SingleOrDefault(
                        delegate(Instruction instruction)
                        {
                            IMethod called =
                                instruction.Operand as IMethod;
                            return instruction.OpCode ==
                                    OpCodes.Callvirt &&
                                called != null &&
                                called.Name == "ReadBuffer";
                        });
                if (originalCall == null)
                    throw new InvalidOperationException(
                        "未找到 ReadBuffer 调用。");

                IMethod readBufferMethod =
                    (IMethod)originalCall.Operand;
                CilBody body = method.Body;
                body.ExceptionHandlers.Clear();
                body.Instructions.Clear();
                body.Variables.Clear();
                body.InitLocals = true;
                body.MaxStack = 4;

                Local readCount =
                    new Local(module.CorLibTypes.Int32);
                Local endIndex =
                    new Local(module.CorLibTypes.Int32);
                Local loopIndex =
                    new Local(module.CorLibTypes.Int32);
                body.Variables.Add(readCount);
                body.Variables.Add(endIndex);
                body.Variables.Add(loopIndex);

                Instruction loopBody =
                    Instruction.Create(OpCodes.Ldarg_2);
                Instruction loopCheck =
                    Instruction.Create(OpCodes.Ldloc, loopIndex);

                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldarg_0));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldfld, field));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldarg_1));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldarg_2));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldarg_3));
                body.Instructions.Add(
                    Instruction.Create(
                        OpCodes.Ldarg, method.Parameters[4]));
                body.Instructions.Add(
                    Instruction.Create(
                        OpCodes.Callvirt, readBufferMethod));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Stloc, readCount));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldarg_3));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldloc, readCount));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Add));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Stloc, endIndex));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldarg_3));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Stloc, loopIndex));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Br, loopCheck));
                body.Instructions.Add(loopBody);
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldloc, loopIndex));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldarg_2));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldloc, loopIndex));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldelem_R8));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldc_R8, factor));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Mul));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Stelem_R8));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldloc, loopIndex));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldc_I4_1));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Add));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Stloc, loopIndex));
                body.Instructions.Add(loopCheck);
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldloc, endIndex));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Blt, loopBody));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ldloc, readCount));
                body.Instructions.Add(
                    Instruction.Create(OpCodes.Ret));

                Write(module, outputPath);
            }
        }

        private static void PatchRealtimeReading(
            string inputPath,
            string outputPath,
            double factor)
        {
            using (ModuleDefMD module = ModuleDefMD.Load(inputPath))
            {
                TypeDef type = module.Find(
                    "Renishaw.Calibration.Ballbar." +
                    "BallbarDeviceService.QC20BallbarDevice",
                    false);
                if (type == null)
                    throw new InvalidOperationException(
                        "未找到 QC20BallbarDevice 类型。");

                MethodDef method = type.Methods.SingleOrDefault(
                    delegate(MethodDef candidate)
                    {
                        return candidate.Name ==
                                "get_LatestReading" &&
                            candidate.MethodSig != null &&
                            candidate.MethodSig.Params.Count == 0;
                    });
                if (method == null || !method.HasBody)
                    throw new InvalidOperationException(
                        "未找到 QC20 实时读取方法。");

                Instruction getValue =
                    method.Body.Instructions.SingleOrDefault(
                        delegate(Instruction instruction)
                        {
                            IMethod called =
                                instruction.Operand as IMethod;
                            return instruction.OpCode ==
                                    OpCodes.Callvirt &&
                                called != null &&
                                called.DeclaringType.FullName ==
                                    "Renishaw.Calibration.Ballbar." +
                                    "BallbarReading" &&
                                called.Name == "get_Value";
                        });
                if (getValue == null)
                    throw new InvalidOperationException(
                        "未找到 BallbarReading.get_Value 调用。");

                int insertionIndex =
                    method.Body.Instructions.IndexOf(getValue) + 1;
                method.Body.Instructions.Insert(
                    insertionIndex,
                    Instruction.Create(OpCodes.Ldc_R8, factor));
                method.Body.Instructions.Insert(
                    insertionIndex + 1,
                    Instruction.Create(OpCodes.Mul));
                Write(module, outputPath);
            }
        }

        private static void Write(
            ModuleDefMD module,
            string outputPath)
        {
            ModuleWriterOptions options =
                new ModuleWriterOptions(module);
            options.Logger = DummyLogger.NoThrowInstance;
            module.Write(outputPath, options);
        }

        private static void TryDelete(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch
            {
            }
        }
    }
}
