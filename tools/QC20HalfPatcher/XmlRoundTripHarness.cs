using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Xml;

internal static class XmlRoundTripHarness
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(
                    "Usage: XmlRoundTripHarness <Application directory> <output directory>");
                return 2;
            }

            string applicationDirectory = Path.GetFullPath(args[0]);
            string outputDirectory = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(outputDirectory);

            AppDomain.CurrentDomain.AssemblyResolve +=
                delegate(object sender, ResolveEventArgs eventArgs)
                {
                    string fileName =
                        new AssemblyName(eventArgs.Name).Name + ".dll";
                    string candidate =
                        Path.Combine(applicationDirectory, fileName);
                    return File.Exists(candidate)
                        ? Assembly.LoadFrom(candidate)
                        : null;
                };

            Assembly serviceAssembly = Assembly.LoadFrom(Path.Combine(
                applicationDirectory,
                "Renishaw.Calibration.Ballbar.TestDocumentService.dll"));
            Assembly interfaceAssembly = Assembly.LoadFrom(Path.Combine(
                applicationDirectory,
                "Renishaw.Calibration.Ballbar.TestDocumentService.Interface.dll"));
            Assembly unitInterfaceAssembly = Assembly.LoadFrom(Path.Combine(
                applicationDirectory,
                "Renishaw.Calibration.UnitService.Interface.dll"));
            Assembly unitAssembly = Assembly.LoadFrom(Path.Combine(
                applicationDirectory,
                "Renishaw.Calibration.XmlUnitService.dll"));

            Type formatterType = serviceAssembly.GetType(
                "Renishaw.Calibration.Ballbar.TestDocumentService.Formatters." +
                "XmlDocumentFormatter",
                true);
            Type runResultType = serviceAssembly.GetType(
                "Renishaw.Calibration.Ballbar.TestDocumentService.RunResult",
                true);
            Type runDirectionType = interfaceAssembly.GetType(
                "Renishaw.Calibration.Ballbar.TestDocumentService.Interface." +
                "RunDirection",
                true);
            Type unitServiceInterfaceType = unitInterfaceAssembly.GetType(
                "Renishaw.Calibration.UnitService.Interface.IUnitService",
                true);
            Type measurementType = unitInterfaceAssembly.GetType(
                "Renishaw.Calibration.UnitService.Interface.Measurement",
                true);

            object direction = ChooseDirection(runDirectionType);
            object temperatureUnit = CreateUnit(
                unitAssembly,
                "UT_TEMPERATURE");
            object frequencyUnit = CreateUnit(
                unitAssembly,
                "UT_FREQUENCY");
            object unitServiceProxy =
                new UnitServiceProxy(
                    unitServiceInterfaceType,
                    measurementType,
                    temperatureUnit,
                    frequencyUnit)
                .GetTransparentProxy();

            double[] capturedHalfValues =
                { -2.0, 0.0625, 4.0, 0.0, 50.0 };
            object first = CreateRunResult(
                runResultType,
                direction,
                measurementType,
                temperatureUnit,
                frequencyUnit,
                capturedHalfValues);

            MethodInfo storeBallbarRun = formatterType.GetMethod(
                "StoreBallbarRun",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo populateRunResult = formatterType.GetMethod(
                "PopulateRunResult",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo createReadingsString = formatterType.GetMethod(
                "CreateReadingsString",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo parseReadingsString = formatterType.GetMethod(
                "ParseReadingsString",
                BindingFlags.Static | BindingFlags.NonPublic);

            object formatter = Activator.CreateInstance(
                formatterType,
                BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                null,
                new object[] { null },
                CultureInfo.InvariantCulture);
            formatterType.GetField(
                "m_unitService",
                BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(formatter, unitServiceProxy);

            string firstXml = StoreRunXml(
                storeBallbarRun,
                first,
                Path.Combine(outputDirectory, "roundtrip1_ballbar_run.xml"));
            object second = CreateRunResult(
                runResultType,
                direction,
                measurementType,
                temperatureUnit,
                frequencyUnit,
                new double[0]);
            PopulateFromXml(
                populateRunResult,
                formatter,
                second,
                firstXml);

            string secondXml = StoreRunXml(
                storeBallbarRun,
                second,
                Path.Combine(outputDirectory, "roundtrip2_ballbar_run.xml"));
            object third = CreateRunResult(
                runResultType,
                direction,
                measurementType,
                temperatureUnit,
                frequencyUnit,
                new double[0]);
            PopulateFromXml(
                populateRunResult,
                formatter,
                third,
                secondXml);

            double[] secondValues = GetReadings(second);
            double[] thirdValues = GetReadings(third);

            // Exercise the exact private codec once more in isolation. This
            // verifies that the XML run-block test did not pass accidentally
            // because of a caller-side copy.
            object firstReadings = GetReadingsObject(first);
            string encoded = (string)createReadingsString.Invoke(
                null,
                new[] { firstReadings });
            var decoded = new List<double>();
            parseReadingsString.Invoke(
                null,
                new object[] { encoded, decoded });

            bool firstPass = Equal(capturedHalfValues, secondValues);
            bool secondPass = Equal(capturedHalfValues, thirdValues);
            bool codecPass = Equal(capturedHalfValues, decoded.ToArray());
            bool notQuartered =
                thirdValues.Length > 0 &&
                Math.Abs(thirdValues[0] - capturedHalfValues[0]) < 1e-12;

            Console.WriteLine(
                "SERVICE_ASSEMBLY_VERSION=" +
                serviceAssembly.GetName().Version);
            Console.WriteLine(
                "RUN_DIRECTION=" + direction);
            Console.WriteLine(
                "CAPTURED_HALF_VALUES=" + Join(capturedHalfValues));
            Console.WriteLine(
                "XML_READINGS_1=" + ReadReadingsElement(firstXml));
            Console.WriteLine(
                "REOPENED_VALUES_1=" + Join(secondValues));
            Console.WriteLine(
                "XML_READINGS_2=" + ReadReadingsElement(secondXml));
            Console.WriteLine(
                "REOPENED_VALUES_2=" + Join(thirdValues));
            Console.WriteLine(
                "DIRECT_CODEC_VALUES=" + Join(decoded.ToArray()));
            Console.WriteLine(
                "ROUNDTRIP_1_EXACT=" + firstPass);
            Console.WriteLine(
                "ROUNDTRIP_2_EXACT=" + secondPass);
            Console.WriteLine(
                "DIRECT_CODEC_EXACT=" + codecPass);
            Console.WriteLine(
                "NO_SECOND_50_PERCENT_SCALE=" + notQuartered);
            Console.WriteLine(
                "XML_1=" +
                Path.Combine(outputDirectory, "roundtrip1_ballbar_run.xml"));
            Console.WriteLine(
                "XML_2=" +
                Path.Combine(outputDirectory, "roundtrip2_ballbar_run.xml"));

            return firstPass && secondPass && codecPass && notQuartered
                ? 0
                : 1;
        }
        catch (Exception exception)
        {
            PrintException(exception, 0);
            return 1;
        }
    }

    private static object ChooseDirection(Type runDirectionType)
    {
        string[] names = Enum.GetNames(runDirectionType);
        foreach (string name in names)
        {
            if (name.IndexOf(
                    "Clockwise",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                name.IndexOf(
                    "Counter",
                    StringComparison.OrdinalIgnoreCase) < 0)
                return Enum.Parse(runDirectionType, name);
        }
        return Enum.Parse(runDirectionType, names[0]);
    }

    private static object CreateRunResult(
        Type runResultType,
        object direction,
        Type measurementType,
        object temperatureUnit,
        object frequencyUnit,
        IEnumerable<double> values)
    {
        ConstructorInfo constructor =
            runResultType.GetConstructors(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)[0];
        object run = constructor.Invoke(new[] { null, direction });

        SetProperty(run, "StartIndex", 0);
        SetProperty(run, "EndIndex", 4);
        SetProperty(
            run,
            "RunTemperature",
            CreateMeasurement(measurementType, temperatureUnit, 20.0));
        SetProperty(
            run,
            "SampleRate",
            CreateMeasurement(measurementType, frequencyUnit, 250.0));

        object readings = GetReadingsObject(run);
        MethodInfo add = readings.GetType().GetMethod(
            "Add",
            new[] { typeof(double) });
        foreach (double value in values)
            add.Invoke(readings, new object[] { value });
        return run;
    }

    private static object CreateMeasurement(
        Type measurementType,
        object unit,
        double value)
    {
        foreach (ConstructorInfo constructor in measurementType.GetConstructors())
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            if (parameters.Length == 2 &&
                parameters[0].ParameterType == typeof(double))
                return constructor.Invoke(new[] { (object)value, unit });
        }
        throw new MissingMethodException(
            measurementType.FullName,
            ".ctor(double, IUnit)");
    }

    private static object CreateUnit(
        Assembly unitAssembly,
        string dimensionName)
    {
        Type dimensionType = unitAssembly.GetType(
            "Renishaw.Calibration.UnitService.Dimension",
            true);
        Type scaleType = unitAssembly.GetType(
            "Renishaw.Calibration.UnitService.Scale",
            true);
        Type converterType = unitAssembly.GetType(
            "Renishaw.Calibration.UnitService.LinearUnitConverter",
            true);
        Type unitType = unitAssembly.GetType(
            "Renishaw.Calibration.UnitService.Unit",
            true);

        object dimension = Activator.CreateInstance(
            dimensionType,
            new object[] { dimensionName });
        object scale = Activator.CreateInstance(
            scaleType,
            new[] { (object)(dimensionName + "_ProbeScale"), dimension });
        object converter = Activator.CreateInstance(
            converterType,
            new object[] { 1.0, 0.0 });
        return Activator.CreateInstance(
            unitType,
            new[]
            {
                (object)(dimensionName + "_ProbeUnit"),
                "probe",
                "probe",
                6,
                converter,
                scale
            });
    }

    private static string StoreRunXml(
        MethodInfo storeBallbarRun,
        object run,
        string outputPath)
    {
        var document = new XmlDocument();
        var fragment = (XmlDocumentFragment)storeBallbarRun.Invoke(
            null,
            new[] { (object)document, run });
        string xml = fragment.InnerXml;

        var output = new XmlDocument();
        output.LoadXml(xml);
        using (XmlWriter writer = XmlWriter.Create(
            outputPath,
            new XmlWriterSettings { Indent = true }))
            output.Save(writer);
        return xml;
    }

    private static void PopulateFromXml(
        MethodInfo populateRunResult,
        object formatter,
        object target,
        string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        populateRunResult.Invoke(
            formatter,
            new[] { (object)document.DocumentElement, target });
    }

    private static string ReadReadingsElement(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        XmlNode node = document.DocumentElement.SelectSingleNode("READINGS");
        return node == null ? "(missing)" : node.InnerText;
    }

    private static object GetReadingsObject(object run)
    {
        return run.GetType()
            .GetProperty("Readings")
            .GetValue(run, null);
    }

    private static double[] GetReadings(object run)
    {
        var values = new List<double>();
        foreach (object value in (IEnumerable)GetReadingsObject(run))
            values.Add((double)value);
        return values.ToArray();
    }

    private static void SetProperty(
        object target,
        string name,
        object value)
    {
        target.GetType()
            .GetProperty(name)
            .SetValue(target, value, null);
    }

    private static bool Equal(
        IList<double> expected,
        IList<double> actual)
    {
        if (expected.Count != actual.Count)
            return false;
        for (int index = 0; index < expected.Count; index++)
        {
            if (Math.Abs(expected[index] - actual[index]) > 1e-12)
                return false;
        }
        return true;
    }

    private static string Join(IEnumerable<double> values)
    {
        var strings = new List<string>();
        foreach (double value in values)
            strings.Add(value.ToString("R", CultureInfo.InvariantCulture));
        return string.Join(",", strings.ToArray());
    }

    private static void PrintException(Exception exception, int depth)
    {
        Console.Error.WriteLine(
            new string(' ', depth * 2) +
            "ERROR_TYPE=" +
            exception.GetType().FullName);
        Console.Error.WriteLine(
            new string(' ', depth * 2) +
            "ERROR_MESSAGE=" +
            exception.Message);
        if (exception.InnerException != null)
            PrintException(exception.InnerException, depth + 1);
    }

    private sealed class UnitServiceProxy : RealProxy
    {
        private readonly Type measurementType;
        private readonly object temperatureUnit;
        private readonly object frequencyUnit;

        public UnitServiceProxy(
            Type interfaceType,
            Type measurementType,
            object temperatureUnit,
            object frequencyUnit)
            : base(interfaceType)
        {
            this.measurementType = measurementType;
            this.temperatureUnit = temperatureUnit;
            this.frequencyUnit = frequencyUnit;
        }

        public override IMessage Invoke(IMessage message)
        {
            var call = (IMethodCallMessage)message;
            object result = null;

            if (call.MethodName == "CreateMeasurement" ||
                call.MethodName == "CreateLocaleMeasurement")
            {
                object targetUnit =
                    HasArgument(call, "UT_FREQUENCY")
                    ? frequencyUnit
                    : temperatureUnit;
                result = CreateMeasurement(
                    measurementType,
                    targetUnit,
                    Convert.ToDouble(
                        call.Args[0],
                        CultureInfo.InvariantCulture));
            }
            else if (call.MethodName == "FindUnit" ||
                call.MethodName == "FindLocaleUnit")
            {
                result = HasArgument(call, "UT_FREQUENCY")
                    ? frequencyUnit
                    : temperatureUnit;
            }

            return new ReturnMessage(
                result,
                null,
                0,
                call.LogicalCallContext,
                call);
        }

        private static bool HasArgument(
            IMethodCallMessage call,
            string expected)
        {
            foreach (object argument in call.Args)
            {
                if (string.Equals(
                        argument as string,
                        expected,
                        StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
