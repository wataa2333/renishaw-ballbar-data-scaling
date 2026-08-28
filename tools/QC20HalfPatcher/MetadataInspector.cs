using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class MetadataInspector
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine(
                "Usage: MetadataInspector <assembly> <case-insensitive-pattern> [--il]");
            return 2;
        }

        string assemblyPath = Path.GetFullPath(args[0]);
        string pattern = args[1];
        bool printIl = args.Length > 2 && args[2] == "--il";

        using (ModuleDefMD module = ModuleDefMD.Load(assemblyPath))
        {
            foreach (TypeDef type in module.GetTypes())
            {
                bool typeMatches = Contains(type.FullName, pattern);
                foreach (InterfaceImpl implementedInterface in type.Interfaces)
                {
                    if (Contains(implementedInterface.Interface.FullName, pattern))
                        typeMatches = true;
                }
                var matchingMethods = new List<MethodDef>();
                var matchingFields = new List<FieldDef>();
                var matchingProperties = new List<PropertyDef>();

                foreach (MethodDef method in type.Methods)
                {
                    if (typeMatches || Contains(method.Name, pattern) ||
                        Contains(method.FullName, pattern))
                        matchingMethods.Add(method);
                }

                foreach (FieldDef field in type.Fields)
                {
                    if (typeMatches || Contains(field.Name, pattern) ||
                        Contains(field.FullName, pattern))
                        matchingFields.Add(field);
                }

                foreach (PropertyDef property in type.Properties)
                {
                    if (typeMatches || Contains(property.Name, pattern) ||
                        Contains(property.FullName, pattern))
                        matchingProperties.Add(property);
                }

                if (!typeMatches &&
                    matchingMethods.Count == 0 &&
                    matchingFields.Count == 0 &&
                    matchingProperties.Count == 0)
                    continue;

                Console.WriteLine(
                    "TYPE|{0}|attrs={1}|base={2}",
                    type.FullName,
                    type.Attributes,
                    type.BaseType == null ? "(none)" : type.BaseType.FullName);
                foreach (InterfaceImpl implementedInterface in type.Interfaces)
                    Console.WriteLine(
                        "INTERFACE|{0}|{1}",
                        type.FullName,
                        implementedInterface.Interface.FullName);

                foreach (FieldDef field in matchingFields)
                    Console.WriteLine(
                        "FIELD|{0}|attrs={1}",
                        field.FullName,
                        field.Attributes);

                foreach (PropertyDef property in matchingProperties)
                    Console.WriteLine("PROPERTY|{0}", property.FullName);

                foreach (MethodDef method in matchingMethods)
                {
                    Console.WriteLine(
                        "METHOD|{0}|attrs={1}|impl={2}",
                        method.FullName,
                        method.Attributes,
                        method.ImplAttributes);
                    if (!printIl || !method.HasBody)
                        continue;

                    foreach (Instruction instruction in method.Body.Instructions)
                        Console.WriteLine("IL|{0}", instruction);
                }
            }
        }

        return 0;
    }

    private static bool Contains(string value, string pattern)
    {
        return value != null &&
            value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
