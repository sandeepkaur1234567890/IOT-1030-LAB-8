using System;
using System.Collections.Generic;

using Psim.Geometry2D;
using Psim.Surfaces;
using Psim.Particles;
using Psim.Materials;

namespace Psim.ModelComponents
{
	enum SurfaceLocation
	{
		left = 0,
		top = 1,
		right = 2,
		bot = 3
	}
	class Cell : Rectangle
	{
		private const int NUM_SURFACES = 4;
		private Sensor sensor;
		private List<Phonon> phonons = new() { };
		private List<Phonon> incomingPhonons = new() { };
		private ISurface[] surfaces = new ISurface[NUM_SURFACES];

		public Tuple<double, double>[] BaseTable { get { return sensor.BaseTable; } }
		public Tuple<double, double>[] ScatterTable { get { return sensor.ScatterTable; } }
		public Sensor Sensor { get { return sensor; } }
		public Material Material { get { return sensor.Material; } }
		public double InitTemp { get { return sensor.InitTemp; } }
		public List<Phonon> Phonons { get { return phonons; } }

		public Cell(double length, double width, Sensor sensor)
			: base(length, width)
		{
			this.sensor = sensor;
			this.sensor.AddToArea(Area);
			for (int i = 0; i < NUM_SURFACES; ++i)
			{
				surfaces[i] = new Surface((SurfaceLocation)i, this);
			}
		}

		public Tuple<double, double>[] EmitData(double temp, out double emitEnergy)
		{
			return sensor.GetEmitData(temp, out emitEnergy);
		}

		public void SetEmitSurface(SurfaceLocation location, double temp)
		{
			surfaces[(int)location] = new EmitSurface(location, this, temp);
		}

		public void SetTransitionSurface(SurfaceLocation location, Cell cell)
		{
			surfaces[(int)location] = new TransitionSurface(location, cell);
		}

		public double InitEnergy(double tEq)
		{
			return Area * sensor.HeatCapacity * Math.Abs(sensor.InitTemp - tEq);
		}

		public double EmitEnergy(double tEq, double simTime)
		{
			double totalEmitEnergy = 0;
			foreach (var surface in surfaces)
			{
				if (surface is EmitSurface emitSurface)
				{
					totalEmitEnergy += emitSurface.GetEmitEnergy(tEq, simTime, Width);
				}
			}
			return totalEmitEnergy;
		}

		public void SetEmitPhonons(double tEq, double effEnergy, double timeStep)
		{
			foreach (var surface in surfaces)
			{
				if (surface is EmitSurface emitSurface)
				{
					emitSurface.SetEmitPhonons(tEq, effEnergy, timeStep, Width);
				}
			}
		}

		public List<EmitSurfaceData> EmitPhononData(double rand)
		{
			List<EmitSurfaceData> emitPhononData = new() { };
			foreach (var surface in surfaces)
			{
				if (surface is EmitSurface emitSurface)
				{
					EmitSurfaceData data;
					data.Table = emitSurface.EmitTable;
					data.Location = emitSurface.Location;
					data.Temp = emitSurface.Temp;
					data.EmitPhonons = emitSurface.EmitPhonons;
					if (emitSurface.EmitPhononsFrac >= rand)
						++data.EmitPhonons;
					emitPhononData.Add(data);
				}
			}
			return emitPhononData;
		}

		public void TakeMeasurements(double effEnergy, double tEq)
		{
			sensor.TakeMeasurements(phonons, effEnergy, tEq);
		}

		public void AddPhonon(Phonon p) => phonons.Add(p);

		public void AddIncPhonon(Phonon p) => incomingPhonons.Add(p);

		public void MergeIncPhonons()
		{
			phonons.AddRange(incomingPhonons);
			incomingPhonons.Clear();
		}

		public ISurface GetSurface(SurfaceLocation loc) => surfaces[(int)loc];

		public SurfaceLocation? MoveToNearestSurface(Phonon p)
		{
			// Returns the time taken to for a phonon to move back into the cell or 0 if the phonon did not exit the cell
			double GetTime(double dist, double pos, double vel)
			{
				if (pos <= 0) { return pos / vel; } // pos is negative therefore vel must be negative
				else if (pos >= dist) { return (pos - dist) / vel; } // pos is + therefore vel is + and len < pos
				else return 0; // No surface was reached
			}

			p.Drift(p.DriftTime);
			p.GetCoords(out double px, out double py);
			p.GetDirection(out double dx, out double dy);
			double vx = dx * p.Speed;
			double vy = dy * p.Speed;

			// The longer the time, the sooner the phonon impacted the corresponding surface
			double timeToSurfaceX = (vx != 0) ? GetTime(Length, px, vx) : 0;
			double timeToSurfaceY = (vy != 0) ? GetTime(Width, py, vy) : 0;

			// Time needed to backtrack the phonon to the first surface collision
			double backtrackTime = Math.Max(timeToSurfaceX, timeToSurfaceY);
			p.DriftTime = backtrackTime;
			if (backtrackTime == 0) { return null; } // The phonon did not collide with a surface
			p.Drift(-backtrackTime);

			// Miminize FP errors and determine impacted surface
			if (backtrackTime == timeToSurfaceX)
			{
				if (vx < 0)
				{
					p.SetCoords(0, null);
					return SurfaceLocation.left;
				}
				else
					p.SetCoords(Length, null);
				return SurfaceLocation.right;
			}
			else
			{
				if (vy < 0)
				{
					p.SetCoords(null, 0);
					return SurfaceLocation.bot;
				}
				else
				{
					p.SetCoords(null, Width);
					return SurfaceLocation.top;
				}
			}
		}
		public override string ToString()
		{
			return string.Format("{0,-20} {1,-7} {2,-7}",
								  sensor.ToString(), phonons.Count, incomingPhonons.Count);
		}

		public class InvalidEmitSurfaceException : Exception
		{
			public InvalidEmitSurfaceException(string message) : base(message) { }
		}
	}
	struct EmitSurfaceData
	{
		public Tuple<double, double>[] Table;
		public SurfaceLocation Location;
		public double Temp;
		public int EmitPhonons;

		public void Deconstruct(out Tuple<double, double>[] table, out SurfaceLocation loc,
								out double temp, out int emitPhonons)
		{
			table = Table;
			loc = Location;
			temp = Temp;
			emitPhonons = EmitPhonons;
		}
	}
}
