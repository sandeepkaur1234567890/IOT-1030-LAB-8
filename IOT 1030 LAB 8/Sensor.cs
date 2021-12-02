using System;
using System.Collections.Generic;


using Psim.Materials;
using Psim.Particles;

namespace Psim.ModelComponents
{
	class Sensor
	{
		private double heatCapacity;
		private List<double> temperatures = new() { };
		private List<double> xFluxes = new() { };
		private List<double> yFluxes = new() { };
		public int ID { get; }
		public double InitTemp { get; }
		public Material Material { get; }
		public Tuple<double, double>[] BaseTable { get; private set; }
		public Tuple<double, double>[] ScatterTable { get; private set; }
		public double HeatCapacity { get { return heatCapacity; } }
		public double Temperature { get; private set; }
		public double AreaCovered { get; private set; }

		public Sensor(int id, Material material, double initTemp)
		{
			ID = id;
			Material = material;
			InitTemp = initTemp;
			BaseTable = material.BaseData(initTemp, out heatCapacity);
			ScatterTable = material.ScatterTable(initTemp);
			Temperature = initTemp;
		}

		public void AddToArea(double area) => AreaCovered += area;
		public Tuple<double, double>[] GetEmitData(double temp, out double energy)
		{
			return Material.EmitData(temp, out energy);
		}

		public void TakeMeasurements(List<Phonon> phonons, double effEnergy, double tEq)
		{
			int energyUnits = 0;
			double xFlux = 0;
			double yFlux = 0;
			foreach (var p in phonons)
			{
				int sign = p.Sign;
				p.GetDirection(out double dx, out double dy);
				energyUnits += sign;
				xFlux += dx * p.Speed * sign;
				yFlux += dy * p.Speed * sign;
			}
			double fluxFactor = effEnergy / AreaCovered;

#if false
			double bound = 200;
			double eps = 0.0001;
			int maxIters = 30;
			double ub = InitTemp + bound;
			double lb = InitTemp - bound;
			double mid = 0, iter = 0, de = 0;

			while (ub - lb >= eps && ++iter != maxIters)
			{
				mid = (ub + lb) / 2;
				de = Material.TheoreticalEnergy(mid) * AreaCovered - (effEnergy * energyUnits);
				if (de < 0)
					lb = mid;
				else
					ub = mid;
			}
			temperatures.Add(mid);
#else
			temperatures.Add((energyUnits * effEnergy / (AreaCovered * HeatCapacity)) + tEq);
#endif
			xFluxes.Add(fluxFactor * xFlux);
			yFluxes.Add(fluxFactor * yFlux);
			UpdateParams();
		}

		public SensorMeasurements GetMeasurements()
		{
			SensorMeasurements measurements;
			measurements.InitTemp = InitTemp;
			measurements.Temperatures = temperatures;
			measurements.XFluxes = xFluxes;
			measurements.YFluxes = yFluxes;
			return measurements;
		}

		private void UpdateParams()
		{
			Temperature = temperatures[^1];
			BaseTable = Material.BaseData(Temperature, out heatCapacity);
			ScatterTable = Material.ScatterTable(Temperature);
		}
		public override string ToString() => $"Sensor {ID}: {Math.Round(Temperature, 2)}";
	}

	struct SensorMeasurements
	{
		public double InitTemp;
		public List<double> Temperatures;
		public List<double> XFluxes;
		public List<double> YFluxes;
		public void Deconstruct(out List<double> temps, out List<double> xfs, out List<double> yfs)
		{
			temps = Temperatures;
			xfs = XFluxes;
			yfs = YFluxes;
		}
	}
}
