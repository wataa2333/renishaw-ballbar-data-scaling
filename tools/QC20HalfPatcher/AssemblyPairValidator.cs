using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class AssemblyPairValidator
{
    private static int Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.Error.WriteLine(
                "Usage: AssemblyPairValidator <original.dll> <modified.dll> " +
                "<ReadRecord|Realtime> <factor>");
            return 2;
        }

        double factor;
        if (!double.TryParse(
                args[3],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out factor) ||
            double.IsNaN(factor) ||
            double.IsInfinity(factor))
        {
            Console.Error.WriteLine("Invalid factor.");
            return 2;
        }

        using (ModuleDefMD original = ModuleDefMD.Load(args[0]))
        using (ModuleDefMD modified = ModuleDefMD.Load(args[1]))
        {
            string mode = args[2];
            HashSet<string> originalApi = GetApi(original);
            HashSet<string> modifiedApi = GetApi(modified);
            string[] removed = originalApi.Except(modifiedApi).ToArray();
            string[] added = modifiedApi.Except(originalApi).ToArray();

            string[] originalResources = original.Resources
                .Select(r => r.Name.String)
                .OrderBy(n => n)
                .ToArray();
            string[] modifiedResources = modified.Resources
                .Select(r => r.Name.String)
                .OrderBy(n => n)
                .ToArray();

            bool identitySame =
                original.Assembly.FullName == modified.Assembly.FullName;
            bool resourcesSame =
                originalResources.SequenceEqual(modifiedResources);
            bool apiSame = removed.Length == 0 && added.Length == 0;
            bool patchPresent = mode == "ReadRecord"
                ? ValidateReadRecord(modified, factor)
                : ValidateRealtime(modified, factor);
            string expectedChangedMethod = mode == "ReadRecord"
                ? "System.Int32 Renishaw.Calibration.Ballbar.BallbarInstrument::ReadRecord(System.Int32,System.Double[],System.Int32,System.Int32)"
                : "Renishaw.Calibration.UnitService.Interface.Measurement Renishaw.Calibration.Ballbar.BallbarDeviceService.QC20BallbarDevice::get_LatestReading()";
            string[] changedMethods = GetChangedMethods(original, modified);
            bool onlyExpectedMethodChanged =
                changedMethods.Length == 1 &&
                changedMethods[0] == expectedChangedMethod;

            Console.WriteLine("PAIR=" + Path.GetFileName(args[0]));
            Console.WriteLine("IDENTITY_SAME=" + identitySame);
            Console.WriteLine("PUBLIC_API_SAME=" + apiSame);
            Console.WriteLine("RESOURCE_NAMES_SAME=" + resourcesSame);
            Console.WriteLine("ORIGINAL_API_COUNT=" + originalApi.Count);
            Console.WriteLine("MODIFIED_API_COUNT=" + modifiedApi.Count);
            Console.WriteLine("REMOVED_API_COUNT=" + removed.Length);
            Console.WriteLine("ADDED_API_COUNT=" + added.Length);
            Console.WriteLine("PATCH_PRESENT=" + patchPresent);
            Console.WriteLine(
                "CHANGED_METHOD_COUNT=" + changedMethods.Length);
            foreach (string changedMethod in changedMethods)
                Console.WriteLine("CHANGED_METHOD=" + changedMethod);
            Console.WriteLine(
                "ONLY_EXPECTED_METHOD_CHANGED=" + onlyExpectedMethodChanged);

            if (!apiSame)
            {
                foreach (string value in removed)
                    Console.WriteLine("REMOVED=" + value);
                foreach (string value in added)
                    Console.WriteLine("ADDED=" + value);
            }

            return identitySame &&
                   apiSame &&
                   resourcesSame &&
                   patchPresent &&
                   onlyExpectedMethodChanged
                ? 0
                : 1;
        }
    }

    private static HashSet<string> GetApi(ModuleDef module)
    {
        HashSet<string> api = new HashSet<string>(StringComparer.Ordinal);
        foreach (TypeDef type in module.GetTypes())
        {
            if (type.IsPublic || type.IsNestedPublic)
                api.Add("T|" + type.FullName);

            foreach (MethodDef method in type.Methods)
            {
                if (method.IsPublic)
                    api.Add("M|" + type.FullName + "|" + method.FullName);
            }
            foreach (FieldDef field in type.Fields)
            {
                if (field.IsPublic)
                    api.Add("F|" + type.FullName + "|" + field.FullName);
            }
            foreach (PropertyDef property in type.Properties)
            {
                bool isPublic =
                    (property.GetMethod != null && property.GetMethod.IsPublic) ||
                    (property.SetMethod != null && property.SetMethod.IsPublic);
                if (isPublic)
                    api.Add("P|" + type.FullName + "|" + property.FullName);
            }
            foreach (EventDef eventDef in type.Events)
            {
                bool isPublic =
                    eventDef.AddMethod != null && eventDef.AddMethod.IsPublic;
                if (isPublic)
                    api.Add("E|" + type.FullName + "|" + eventDef.FullName);
            }
        }
        return api;
    }

    private static bool ValidateReadRecord(ModuleDef module, double factor)
    {
        TypeDef type = module.Find(
            "Renishaw.Calibration.Ballbar.BallbarInstrument",
            false);
        MethodDef method = type.Methods.Single(m => m.Name == "ReadRecord");
        return ContainsScaleMultiply(method, factor);
    }

    private static bool ValidateRealtime(ModuleDef module, double factor)
    {
        TypeDef type = module.Find(
            "Renishaw.Calibration.Ballbar.BallbarDeviceService." +
            "QC20BallbarDevice",
            false);
        MethodDef method = type.Methods.Single(
            m => m.Name == "get_LatestReading");
        return ContainsScaleMultiply(method, factor);
    }

    private static bool ContainsScaleMultiply(MethodDef method, double factor)
    {
        IList<Instruction> instructions = method.Body.Instructions;
        for (int i = 0; i + 1 < instructions.Count; i++)
        {
            if (instructions[i].OpCode == OpCodes.Ldc_R8 &&
                (double)instructions[i].Operand == factor &&
                instructions[i + 1].OpCode == OpCodes.Mul)
                return true;
        }
        return false;
    }

    private static string[] GetChangedMethods(
        ModuleDef original,
        ModuleDef modified)
    {
        Dictionary<string, string> originalBodies = GetMethodBodies(original);
        Dictionary<string, string> modifiedBodies = GetMethodBodies(modified);
        return originalBodies.Keys
            .Union(modifiedBodies.Keys)
            .Where(key =>
            {
                string originalBody;
                string modifiedBody;
                return !originalBodies.TryGetValue(key, out originalBody) ||
                       !modifiedBodies.TryGetValue(key, out modifiedBody) ||
                       originalBody != modifiedBody;
            })
            .OrderBy(key => key)
            .ToArray();
    }

    private static Dictionary<string, string> GetMethodBodies(ModuleDef module)
    {
        Dictionary<string, string> result =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (TypeDef type in module.GetTypes())
        {
            foreach (MethodDef method in type.Methods)
                result[method.FullName] = NormalizeBody(method);
        }
        return result;
    }

    private static string NormalizeBody(MethodDef method)
    {
        if (!method.HasBody)
            return "<no-body>";

        IList<Instruction> instructions = method.Body.Instructions;
        Dictionary<Instruction, int> indices =
            new Dictionary<Instruction, int>();
        for (int i = 0; i < instructions.Count; i++)
            indices[instructions[i]] = i;

        StringBuilder builder = new StringBuilder();
        builder.Append("locals=");
        foreach (Local local in method.Body.Variables)
            builder.Append(local.Type.FullName).Append(';');

        for (int i = 0; i < instructions.Count; i++)
        {
            Instruction instruction = instructions[i];
            builder.Append('|').Append(instruction.OpCode.Name).Append(':');
            builder.Append(NormalizeOperand(instruction.Operand, indices));
        }
        return builder.ToString();
    }

    private static string NormalizeOperand(
        object operand,
        Dictionary<Instruction, int> indices)
    {
        if (operand == null)
            return string.Empty;

        Instruction target = operand as Instruction;
        if (target != null)
            return "B" + indices[target];

        IList<Instruction> targets = operand as IList<Instruction>;
        if (targets != null)
            return "S" + string.Join(
                ",",
                targets.Select(t => indices[t].ToString()).ToArray());

        IMethod method = operand as IMethod;
        if (method != null)
            return "M" + method.FullName;

        IField field = operand as IField;
        if (field != null)
            return "F" + field.FullName;

        ITypeDefOrRef type = operand as ITypeDefOrRef;
        if (type != null)
            return "T" + type.FullName;

        Local local = operand as Local;
        if (local != null)
            return "L" + local.Index;

        Parameter parameter = operand as Parameter;
        if (parameter != null)
            return "P" + parameter.Index;

        return Convert.ToString(operand, CultureInfo.InvariantCulture);
    }
}
