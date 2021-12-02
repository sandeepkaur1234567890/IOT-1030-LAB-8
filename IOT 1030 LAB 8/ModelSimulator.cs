/* Lab Questions (Test 2)
 * 
 * When in doubt, you should default to using a List. However, if you need to frequently remove items from random positions
 * in your container, you should prefer a LinkedList. Why is this?
 * If you do not need your container to be ordered, how can you efficiently remove items from random positions with a List?
 * What is the advantage of taking this approach compared to simply using a LinkedList - i.e., why are List generally preferred 
 * LinkedLists?
 * 
 * Do you think using generics would be appropriate for software like this? For example, would it be advantageous to have a Model<T>
 * so that we could potentially simulate different types of particles (phoTons, electrons) using this same code base?
 */

using System;
using System.Linq;
using System.Collections.Generic;
using Psim.Materials;
using Psim.ModelComponents;
using Psim.Particles;

namespace Psim
{
	static class ModelSimulator
	{
		private static Random rand = new();
		public static void RunSimulation(List<Cell> cells, double tEq, double effEnergy, double simTime, double timeStep)
		{
			int numSteps = (int)(simTime / timeStep);
			AddInitialPhonons(cells, tEq, effEnergy);
			int step = 0;
			Console.WriteLine("***\t\tStarting Conditions\t\t***");
			ConsoleUpdate(cells, numSteps, step);
			while (step++ < numSteps)
			{
				AddEmitPhonons(cells, tEq, timeStep);
				DriftPhonons(cells, timeStep);

				if (step % 10 == 0)
					ConsoleUpdate(cells, numSteps, step);

				MergePhonons(cells);
				Scatter(cells, timeStep);

				TakeMeasurements(cells, effEnergy, tEq);
			}
			Console.WriteLine($"Final");
			ConsoleUpdate(cells, numSteps, step);
		}

		private static void ConsoleUpdate(List<Cell> cells, int numSteps, int step)
		{
			Console.WriteLine($"Step {step} of {numSteps} - {Math.Round((float)(step * 100) / (float)numSteps, 2)}% complete");
			foreach (var cell in cells)
			{
				Console.WriteLine(cell);
			}
		}

		private static void AddInitialPhonons(List<Cell> cells, double tEq, double effEnergy)
		{
			foreach (var cell in cells)
			{
				double initEnergy = cell.InitEnergy(tEq);
				if (initEnergy > 0)
				{
					int initPhonons = (int)(initEnergy / effEnergy);
					int sign = cell.InitTemp > tEq ? 1 : -1;
					int phononNum = 0;
					while (++phononNum <= initPhonons)
					{
						Phonon p = new Phonon(sign);
						p.SetRandomDirection(rand.NextDouble(), rand.NextDouble());
						p.Position = cell.GetRandPoint(rand.NextDouble(), rand.NextDouble());
						Update(p, cell.Material, cell.BaseTable);
						cell.AddPhonon(p);
					}
				}
			}
		}

		private static void AddEmitPhonons(List<Cell> cells, double tEq, double timeStep)
		{
			foreach (var cell in cells)
			{
				var emitPhononData = cell.EmitPhononData(rand.NextDouble());
				foreach (var (emitTable, surfaceLoc, emitTemp, emitPhonons) in emitPhononData)
				{
					int sign = emitTemp > tEq ? 1 : -1;
					int phononNum = 0;
					while (++phononNum <= emitPhonons)
					{
						Phonon p = new Phonon(sign);
						// Get biased direction vector components for emitted phonons
						double r1 = rand.NextDouble();
						double biasedDir = Math.Sqrt(r1);
						double otherDir = Math.Sqrt(1 - r1) * Math.Cos(2 * Math.PI * rand.NextDouble());
						// Only left and right surfaces are supported in this simulation version
						switch (surfaceLoc)
						{
							case SurfaceLocation.left:
								p.SetCoords(0, cell.Width * rand.NextDouble());
								p.SetDirection(biasedDir, otherDir);
								break;
							case SurfaceLocation.right:
								p.SetCoords(cell.Length, cell.Width * rand.NextDouble());
								p.SetDirection(-biasedDir, otherDir);
								break;
							case SurfaceLocation.top:
								p.SetCoords(cell.Length * rand.NextDouble(), cell.Width);
								p.SetDirection(otherDir, -biasedDir);
								break;
							case SurfaceLocation.bot:
								p.SetCoords(cell.Length * rand.NextDouble(), 0);
								p.SetDirection(otherDir, biasedDir);
								break;
						}
						p.DriftTime = timeStep * rand.NextDouble();
						Update(p, cell.Material, emitTable);
						cell.AddPhonon(p);
					}
				}
			}
		}

		private static void DriftPhonons(List<Cell> cells, double time)
		{
			foreach (var cell in cells)
			{
				// Drift the phonons!
				throw new NotImplementedException();
			}
		}

		private static void MergePhonons(List<Cell> cells)
		{
			foreach (var cell in cells)
			{
				cell.MergeIncPhonons();
			}
		}

		private static void Scatter(List<Cell> cells, double timeStep)
		{
			foreach (var cell in cells)
			{
				var phonons = cell.Phonons;
				var sensor = cell.Sensor;
				var material = cell.Material;
				foreach (var p in phonons)
				{
					var scatterRates = material.ScatteringRates(sensor.Temperature, p.Frequency, p.Polarization);
					double invTau = scatterRates.Sum();
					double scatProb = 1 - Math.Exp(-timeStep * invTau);
					if (rand.NextDouble() <= scatProb)
					{
						double invTauN = scatterRates[0];
						double invTauU = scatterRates[1];
						double invTauI = scatterRates[2];

						double rnd = rand.NextDouble();
						if (rnd <= (invTauN + invTauU) / invTau) // Normal or Umklapp Scatter
						{
							Update(p, material, cell.ScatterTable);
							if (rnd > invTauN / invTau)
							{
								// Umklapp scatter - change direction
								p.SetRandomDirection(rand.NextDouble(), rand.NextDouble());
							}
						}
						else if (invTauI > 0)
						{
							// Impurity scatter- change direction only
							p.SetRandomDirection(rand.NextDouble(), rand.NextDouble());
						}
					}
				}
			}
		}

		private static void Update(Phonon p, Material material, Tuple<double, double>[] table)
		{
			(int index, Polarization polar) = Material.FreqIndex(table);
			p.Update(material.GetFreq(index), material.GetVel(index, polar), polar);
		}

		private static void TakeMeasurements(List<Cell> cells, double effEnergy, double tEq)
		{
			foreach (var cell in cells)
			{
				cell.TakeMeasurements(effEnergy, tEq);
			}
		}
	}
}

