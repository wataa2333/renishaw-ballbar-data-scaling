using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

internal static class SimulationHarness
{
    private static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("ERROR_TYPE=" + exception.GetType().FullName);
            try { Console.Error.WriteLine("ERROR_MESSAGE=" + exception.Message); }
            catch { Console.Error.WriteLine("ERROR_MESSAGE=<unavailable>"); }
            try
            {
                if (exception.InnerException != null)
                {
                    Console.Error.WriteLine(
                        "INNER_TYPE=" + exception.InnerException.GetType().FullName);
                    Console.Error.WriteLine(
                        "INNER_MESSAGE=" + exception.InnerException.Message);
                }
            }
            catch { Console.Error.WriteLine("INNER=<unavailable>"); }
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: SimulationHarness <Ballbar.dll> <Application directory>");
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

        Console.WriteLine("STEP=LOAD_IO");
        Assembly ioAssembly = Assembly.LoadFrom(
            Path.Combine(applicationDirectory, "Renishaw.IO.dll"));
        Console.WriteLine("STEP=LOAD_BALLBAR");
        Assembly ballbarAssembly = Assembly.LoadFile(targetDll);

        Console.WriteLine("STEP=RESOLVE_TYPES");
        Type triggerType = ioAssembly.GetType(
            "Renishaw.IO.Acquisition.Trigger", true);
        Type bufferingType = ioAssembly.GetType(
            "Renishaw.IO.ChannelFilters.BufferingFilter", true);
        Type triggeredBufferType = ioAssembly.GetType(
            "Renishaw.IO.ChannelFilters.TriggeredBufferingFilter", true);

        Console.WriteLine("STEP=CREATE_BUFFER");
        object startTrigger = Activator.CreateInstance(triggerType);
        object endTrigger = Activator.CreateInstance(triggerType);
        object buffer = Activator.CreateInstance(
            triggeredBufferType,
            new[] { startTrigger, endTrigger });

        Console.WriteLine("STEP=SEED_BUFFER");
        FieldInfo internalListField = bufferingType.GetField(
            "m_buffer",
            BindingFlags.Instance | BindingFlags.NonPublic);
        IList internalList = (IList)internalListField.GetValue(buffer);
        if (internalList == null)
        {
            internalList = (IList)Activator.CreateInstance(internalListField.FieldType);
            internalListField.SetValue(buffer, internalList);
        }

        double[] source = { 10.0, -4.0, 0.125, 8.0, 100.0 };
        foreach (double value in source)
            internalList.Add(value);

        Console.WriteLine("STEP=CREATE_INSTRUMENT");
        Type instrumentType = ballbarAssembly.GetType(
            "Renishaw.Calibration.Ballbar.BallbarInstrument", true);
        object instrument = FormatterServices.GetUninitializedObject(instrumentType);
        FieldInfo instrumentBufferField = instrumentType.GetField(
            "m_buffer",
            BindingFlags.Instance | BindingFlags.NonPublic);
        instrumentBufferField.SetValue(instrument, buffer);

        Console.WriteLine("STEP=INVOKE");
        MethodInfo readRecord = instrumentType.GetMethod(
            "ReadRecord",
            BindingFlags.Instance | BindingFlags.Public);

        double[] destination = { 999.0, 999.0, 999.0, 999.0, 999.0, 999.0 };
        object[] callArguments = { 1, destination, 2, 3 };
        int returned = (int)readRecord.Invoke(instrument, callArguments);

        Console.WriteLine("ASSEMBLY=" + Path.GetFileName(targetDll));
        Console.WriteLine("RETURNED=" + returned);
        Console.WriteLine("SOURCE=" + Join(source));
        Console.WriteLine("DESTINATION=" + Join(destination));

        bool sentinelsUnchanged =
            destination[0] == 999.0 &&
            destination[1] == 999.0 &&
            destination[5] == 999.0;
        Console.WriteLine("SENTINELS_UNCHANGED=" + sentinelsUnchanged);

        return 0;
    }

    private static string Join(double[] values)
    {
        string[] text = new string[values.Length];
        for (int i = 0; i < values.Length; i++)
            text[i] = values[i].ToString("R", CultureInfo.InvariantCulture);
        return string.Join(",", text);
    }
}
