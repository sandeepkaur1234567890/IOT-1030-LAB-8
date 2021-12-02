using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Psim.ModelComponents;
namespace Psim.IOManagers
{
	class OutputManager
	{
		private const int INTERVAL = 20;
		private List<SensorMeasurements> measurements = new() { };
		private int numSteps;

		public OutputManager(int numSteps)
		{
			this.numSteps = numSteps;
		}

		public void ExportResults(string path)
		{
			ExportSteadyStateResults(path);
			ExportPeriodicResults(path);
		}

		public void AddMeasurement(in SensorMeasurements measurement)
		{
			measurements.Add(measurement);
		}

		private void ExportSteadyStateResults(string path)
		{
			var sb = new StringBuilder();
			var ssSteps = (int)(numSteps * 0.1);
			foreach (var measurement in measurements)
			{
				(var temps, var xfs, var yfs) = measurement;

				var ssTemps = temps.TakeLast(ssSteps).ToList();
				var ssXfs = xfs.TakeLast(ssSteps).ToList();
				var ssYfs = yfs.TakeLast(ssSteps).ToList();

				var stdTemps = StdDev(ssTemps);
				var stdXfs = StdDev(ssXfs);
				var stdYfs = StdDev(ssYfs);
				sb.AppendLine($"{ssTemps.Average()} {stdTemps} {ssXfs.Average()} {stdXfs} {ssYfs.Average()} {stdYfs}");
			}
			File.WriteAllText(path + "ss_results.txt", sb.ToString());
		}

		private void ExportPeriodicResults(string path)
		{
			var sb = new StringBuilder();
			int numSensors = measurements.Count;

			// Initial Info
			sb.AppendLine($"{0}\n{numSensors}");
			foreach (var measurement in measurements)
			{
				sb.AppendLine($"{measurement.InitTemp} {0} {0}");
			}

			for (int i = 1; i < numSteps; i += INTERVAL)
			{
				sb.AppendLine($"{(int)((i + INTERVAL) / 2)}\n{numSensors}");
				foreach (var measurement in measurements)
				{
					double temp = 0, xf = 0, yf = 0;
					int step = i;
					while (step < i + INTERVAL && step < numSteps)
					{
						temp += measurement.Temperatures[step];
						xf += measurement.XFluxes[step];
						yf += measurement.YFluxes[step];
						++step;
					}
					step -= i;
					temp /= step;
					xf /= step;
					yf /= step;
					sb.AppendLine($"{temp} {xf} {yf}");
				}
			}
			File.WriteAllText(path + "per_results.txt", sb.ToString());
		}
		private static double StdDev(List<double> values)
		{
			double mean = 0.0;
			double sum = 0.0;
			double stdDev = 0.0;
			int n = 0;
			foreach (double val in values)
			{
				n++;
				double delta = val - mean;
				mean += delta / n;
				sum += delta * (val - mean);
			}
			if (n > 1)
				stdDev = Math.Sqrt(sum / (n - 1));

			return stdDev / Math.Sqrt(n);
		}
	}
}
