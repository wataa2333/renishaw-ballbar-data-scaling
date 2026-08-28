using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

internal static class RealtimeReadingHarness
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(
                    "Usage: RealtimeReadingHarness <DeviceService.dll> <Application directory>");
                return 2;
            }

            string targetDll = Path.GetFullPath(args[0]);
            string applicationDirectory = Path.GetFullPath(args[1]);

            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs eventArgs)
            {
                string name = new AssemblyName(eventArgs.Name).Name + ".dll";
                string dependency = Path.Combine(applicationDirectory, name);
                return File.Exists(dependency) ? Assembly.LoadFrom(dependency) : null;
            };

            Assembly ballbarAssembly = Assembly.LoadFrom(Path.Combine(
                applicationDirectory,
                "Renishaw.Calibration.Ballbar.dll"));
            Assembly unitAssembly = Assembly.LoadFrom(Path.Combine(
                applicationDirectory,
                "Renishaw.Calibration.XmlUnitService.dll"));
            Assembly deviceServiceAssembly = Assembly.LoadFile(targetDll);

            object unit = CreateUnit(unitAssembly);
            object reading = CreateReading(ballbarAssembly, 12.5);
            object qc20 = CreateQC20(ballbarAssembly, reading);
            object service = CreateService(deviceServiceAssembly, qc20, unit);

            Type serviceType = service.GetType();
            MethodInfo getter = serviceType.GetMethod(
                "get_LatestReading",
                BindingFlags.Instance | BindingFlags.Public);
            object measurement = getter.Invoke(service, null);

            Type measurementType = measurement.GetType();
            double primary = (double)measurementType
                .GetProperty("PrimaryQuantity")
                .GetValue(measurement, null);
            double quantity = (double)measurementType
                .GetProperty("Quantity")
                .GetValue(measurement, null);

            Console.WriteLine("ASSEMBLY=" + Path.GetFileName(targetDll));
            Console.WriteLine("DEVICE_VALUE=12.5");
            Console.WriteLine("PRIMARY_QUANTITY=" + primary);
            Console.WriteLine("DISPLAY_QUANTITY=" + quantity);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("ERROR_TYPE=" + exception.GetType().FullName);
            Console.Error.WriteLine("ERROR_MESSAGE=" + exception.Message);
            if (exception.InnerException != null)
            {
                Console.Error.WriteLine(
                    "INNER_TYPE=" + exception.InnerException.GetType().FullName);
                Console.Error.WriteLine(
                    "INNER_MESSAGE=" + exception.InnerException.Message);
            }
            return 1;
        }
    }

    private static object CreateUnit(Assembly unitAssembly)
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
            new object[] { "Length" });
        object scale = Activator.CreateInstance(
            scaleType,
            new[] { (object)"Default", dimension });
        object converter = Activator.CreateInstance(
            converterType,
            new object[] { 1.0, 0.0 });

        return Activator.CreateInstance(
            unitType,
            new[] { (object)"Millimetre", "mm", "mm", 6, converter, scale });
    }

    private static object CreateReading(Assembly ballbarAssembly, double value)
    {
        Type readingType = ballbarAssembly.GetType(
            "Renishaw.Calibration.Ballbar.BallbarReading",
            true);
        ConstructorInfo constructor = readingType.GetConstructor(
            new[] { typeof(double), typeof(int) });
        return constructor.Invoke(new object[] { value, 0 });
    }

    private static object CreateQC20(
        Assembly ballbarAssembly,
        object reading)
    {
        Type qc20Type = ballbarAssembly.GetType(
            "Renishaw.Calibration.Ballbar.QC20Ballbar",
            true);
        object qc20 = FormatterServices.GetUninitializedObject(qc20Type);
        FieldInfo field = qc20Type.GetField(
            "m_latestReading",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(qc20, reading);
        return qc20;
    }

    private static object CreateService(
        Assembly deviceServiceAssembly,
        object qc20,
        object unit)
    {
        Type serviceType = deviceServiceAssembly.GetType(
            "Renishaw.Calibration.Ballbar.BallbarDeviceService." +
            "QC20BallbarDevice",
            true);
        object service = FormatterServices.GetUninitializedObject(serviceType);

        serviceType.GetField(
            "m_device",
            BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(service, qc20);
        serviceType.GetField(
            "m_primaryUnitOfLength",
            BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(service, unit);
        return service;
    }
}
