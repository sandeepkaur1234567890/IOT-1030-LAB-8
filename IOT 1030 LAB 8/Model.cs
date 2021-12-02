using System;
using System.Collections.Generic;

using Psim.ModelComponents;
using Psim.Materials;
using Psim.IOManagers;

namespace Psim
{
	// Assume all cells have the same dimension and are linked to a single sensor.
	// Model is comprised of a single material
	// Temperatures constant?

	class Model
	{
		private const double TIME_STEP = 5e-12;
		private const int NUM_PHONONS = 100000;
		private Material material;
		private List<Cell> cells = new() { };
		private List<Sensor> sensors = new() { };
		private readonly double highTemp;
		private readonly double lowTemp;
		private readonly double simTime;
		private readonly double tEq;
		private OutputManager outputManager;

		public Model(Material material, double highTemp, double lowTemp, double simTime)
		{
			this.material = material;
			this.highTemp = highTemp;
			this.lowTemp = lowTemp;
			this.simTime = simTime;
			outputManager = new OutputManager((int)(simTime / TIME_STEP));
			tEq = (highTemp + lowTemp) / 2;
		}

		public void AddSensor(int sensorID, double initTemp)
		{
			foreach (var sensor in sensors)
			{
				if (sensor.ID == sensorID)
				{
					throw new ArgumentException($"Sensor ID: {sensorID} is not unique.");
				}
			}
			sensors.Add(new Sensor(sensorID, material, initTemp));
		}

		public void AddCell(double length, double width, int sensorID)
		{
			foreach (var sensor in sensors)
			{
				if (sensor.ID == sensorID)
				{
					cells.Add(new Cell(length, width, sensor));
					sensor.AddToArea(cells[^1].Area);
					return;
				}
			}
			throw new ArgumentException($"Sensor ID: {sensorID} does not exist in the model.");
		}
		public void RunSimulation(string exportPath)
		{
			SetSurfaces(tEq);
			double totalEnergy = GetTotalEnergy();
			double effEnergy = totalEnergy / NUM_PHONONS;
			SetEmitPhonons(tEq, effEnergy, TIME_STEP);
			ModelSimulator.RunSimulation(cells, tEq, effEnergy, simTime, TIME_STEP);
			ExportResults(exportPath);
		}

		private void SetSurfaces(double tEq)
		{
			int numCells = cells.Count;
			if (numCells < 2)
			{
				throw new InsufficientCellsException($"Only {numCells} detected. At least 2 cells are required.");
			}
			cells[0].SetEmitSurface(SurfaceLocation.left, highTemp);
			cells[0].SetTransitionSurface(SurfaceLocation.right, cells[1]);
			for (int i = 1; i < numCells - 1; ++i)
			{
				cells[i].SetTransitionSurface(SurfaceLocation.left, cells[i - 1]);
				cells[i].SetTransitionSurface(SurfaceLocation.right, cells[i + 1]);
			}
			cells[^1].SetEmitSurface(SurfaceLocation.right, lowTemp);
			cells[^1].SetTransitionSurface(SurfaceLocation.left, cells[numCells - 2]);
		}

		private void SetEmitPhonons(double tEq, double effEnergy, double timeStep)
		{
			foreach (var cell in cells)
			{
				cell.SetEmitPhonons(tEq, effEnergy, timeStep);
			}
		}

		private double GetTotalEnergy()
		{
			double energy = 0;
			foreach (var cell in cells)
			{
				energy += cell.InitEnergy(tEq) + cell.EmitEnergy(tEq, simTime);
			}
			return energy;
		}

		private void ExportResults(string exportPath)
		{
			foreach (var sensor in sensors)
			{
				outputManager.AddMeasurement(sensor.GetMeasurements());
			}
			outputManager.ExportResults(exportPath);
		}

		public class InsufficientCellsException : Exception
		{
			public InsufficientCellsException(string message) : base(message) { }
		}
	}
}
