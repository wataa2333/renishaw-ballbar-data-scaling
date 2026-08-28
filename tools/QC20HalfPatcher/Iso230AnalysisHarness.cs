using System;
using System.Collections.Generic;
using System.Globalization;
using Renishaw.Calibration.Ballbar.Analysis.Algorithm.Iso230;
using Renishaw.Calibration.Ballbar.Analysis.AlgorithmInterface;

internal static class Iso230AnalysisHarness
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine(
                    "Usage: Iso230AnalysisHarness <factor>");
                return 2;
            }

            double factor;
            if (!double.TryParse(
                    args[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out factor) ||
                double.IsNaN(factor) ||
                double.IsInfinity(factor) ||
                factor <= 0.0)
            {
                Console.Error.WriteLine("Invalid factor: " + args[0]);
                return 2;
            }

            IDictionary<string, ErrorResult> full = Analyse(1.0);
            IDictionary<string, ErrorResult> scaled = Analyse(factor);

            Console.WriteLine("FACTOR=" +
                factor.ToString("R", CultureInfo.InvariantCulture));
            Console.WriteLine("FEATURE|FULL|SCALED|RATIO");
            foreach (KeyValuePair<string, ErrorResult> pair in full)
            {
                ErrorResult scaledResult;
                if (!scaled.TryGetValue(pair.Key, out scaledResult))
                    continue;

                double ratio = pair.Value.Value == 0.0
                    ? double.NaN
                    : scaledResult.Value / pair.Value.Value;

                Console.WriteLine(
                    "{0}|{1}|{2}|{3}",
                    pair.Key,
                    pair.Value.Value.ToString("R", CultureInfo.InvariantCulture),
                    scaledResult.Value.ToString("R", CultureInfo.InvariantCulture),
                    ratio.ToString("R", CultureInfo.InvariantCulture));
            }

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

    private static IDictionary<string, ErrorResult> Analyse(double scale)
    {
        TestRuns runs = new TestRuns();
        ITestRuns testRuns = runs;
        testRuns.TestRuns.Add(CreateRun(Direction.CW, scale));
        testRuns.TestRuns.Add(CreateRun(Direction.CCW, scale));

        Iso230V2005Analysis analysis = new Iso230V2005Analysis();
        analysis.Analyse(testRuns);
        return new Dictionary<string, ErrorResult>(analysis.Results);
    }

    private static RunData CreateRun(Direction direction, double scale)
    {
        const int sampleCount = 361;
        Vector readings = new Vector(sampleCount);

        for (int index = 0; index < sampleCount; index++)
        {
            double angle = 2.0 * Math.PI * index / (sampleCount - 1);
            double directionTerm = direction == Direction.CW ? 0.0015 : -0.0015;
            double value =
                0.012 * Math.Sin(angle) +
                0.004 * Math.Cos(2.0 * angle) +
                directionTerm * Math.Sin(3.0 * angle) +
                0.0008 * Math.Cos(7.0 * angle);
            readings.Add(value * scale);
        }

        RunData run = new RunData();
        SetFullReadings(run, readings);
        run.RunDirection = direction;
        run.TestStartAngle = 0.0;
        run.TestEndAngle = 360.0;
        run.OvershootAngle = 0.0;
        run.NominalLength = 100.0;
        run.TestRadius = 100.0;
        run.ProgrammedFeedrate = 1000.0;
        run.IsCalibrated = true;
        run.ExpansionCoefficient = 0.0;
        run.Temperature = 20.0;
        run.ActualSampleRate = 60.0;
        run.StartIndex = 0;
        run.EndIndex = sampleCount - 1;
        run.TestPlane = Plane.XY;
        return run;
    }

    private static void SetFullReadings(RunData run, Vector readings)
    {
        var field = typeof(RunData).GetField(
            "m_fullReadings",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        field.SetValue(run, readings);
    }
}
