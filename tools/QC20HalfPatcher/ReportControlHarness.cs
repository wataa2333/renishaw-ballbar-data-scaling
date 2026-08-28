using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class ReportControlHarness
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string applicationDirectory = Path.GetFullPath(args[0]);
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs eventArgs)
            {
                string file = Path.Combine(
                    applicationDirectory,
                    new AssemblyName(eventArgs.Name).Name + ".dll");
                return File.Exists(file) ? Assembly.LoadFrom(file) : null;
            };

            Assembly interfaceAssembly = Assembly.LoadFrom(Path.Combine(
                applicationDirectory,
                "Renishaw.Calibration.Ballbar.TestDocumentService.Interface.dll"));
            Assembly reportAssembly = Assembly.LoadFrom(Path.Combine(
                applicationDirectory,
                "Renishaw.Calibration.Ballbar.Analysis.Iso230.dll"));

            Type statusType = interfaceAssembly.GetType(
                "Renishaw.Calibration.Ballbar.TestDocumentService.Interface.FeatureStatus",
                true);
            object ok = Enum.Parse(statusType, "Ok");

            TestCircular(reportAssembly, statusType, ok);
            TestRadial(reportAssembly, statusType, ok);
            TestBidirectional(reportAssembly, statusType, ok);
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

    private static void TestCircular(
        Assembly assembly,
        Type statusType,
        object ok)
    {
        Type type = assembly.GetType(
            "Renishaw.Calibration.Ballbar.Analysis.Iso230.UI." +
            "IsoAnalysisCircularDeviationReportResultsControl",
            true);
        object control = Activator.CreateInstance(type);
        MethodInfo method = type.GetMethod(
            "PopulateCircularDeviation",
            new[] { typeof(string), statusType });
        method.Invoke(control, new[] { (object)"5.150596", ok });
        Console.WriteLine(
            "CIRCULAR_LABEL=" +
            GetLabelText(type, control, "lblCircularDeviationValueValue"));
        ((IDisposable)control).Dispose();
    }

    private static void TestRadial(
        Assembly assembly,
        Type statusType,
        object ok)
    {
        Type type = assembly.GetType(
            "Renishaw.Calibration.Ballbar.Analysis.Iso230.UI." +
            "IsoAnalysisRadialDeviationReportResults",
            true);
        object control = Activator.CreateInstance(type);
        MethodInfo method = type.GetMethod(
            "PopulateRadialDeviation",
            new[]
            {
                typeof(string), statusType, typeof(string),
                typeof(string), statusType, typeof(string), typeof(bool)
            });
        method.Invoke(
            control,
            new[]
            {
                (object)"2.575298", ok, "269.750693°",
                "-2.575299", ok, "89.252078°", true
            });
        Console.WriteLine(
            "RADIAL_MAX_LABEL=" +
            GetLabelText(type, control, "lblMaxRadialDeviationValueValue"));
        Console.WriteLine(
            "RADIAL_MIN_LABEL=" +
            GetLabelText(type, control, "lblMinRadialDeviationValueValue"));
        ((IDisposable)control).Dispose();
    }

    private static void TestBidirectional(
        Assembly assembly,
        Type statusType,
        object ok)
    {
        Type type = assembly.GetType(
            "Renishaw.Calibration.Ballbar.Analysis.Iso230.UI." +
            "IsoAnalysisBidirectionalReportResults",
            true);
        object control = Activator.CreateInstance(type);
        MethodInfo method = type.GetMethod(
            "PopulateBidirectionalCircularDeviation",
            new[] { typeof(string), statusType });
        method.Invoke(control, new[] { (object)"0.045034", ok });
        Console.WriteLine(
            "BIDIRECTIONAL_LABEL=" +
            GetLabelText(type, control, "lblBidirectionalValueValue"));
        ((IDisposable)control).Dispose();
    }

    private static string GetLabelText(
        Type type,
        object control,
        string fieldName)
    {
        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return ((Label)field.GetValue(control)).Text;
    }
}
