using System;
using Psim.ModelComponents;
using Psim.Particles;
using Psim.Geometry2D;

namespace Psim.Surfaces
{
	interface ISurface
	{
		Cell HandlePhonon(Phonon p);
	}

	class Surface : ISurface
	{
		public SurfaceLocation Location { get; }
		protected Cell cell;

		public Surface(SurfaceLocation location, Cell cell)
		{
			Location = location;
			this.cell = cell;
		}
		public virtual Cell HandlePhonon(Phonon p)
		{
			// Only account for perfectly specular reflection
			Vector direction = p.Direction;
			if (Location == SurfaceLocation.left || Location == SurfaceLocation.right)
				p.SetDirection(-direction.DX, direction.DY);
			else
				p.SetDirection(direction.DX, -direction.DY);
			return cell;
		}
	}

	class TransitionSurface : Surface
	{
		public TransitionSurface(SurfaceLocation location, Cell cell) : base(location, cell) { }
		public override Cell HandlePhonon(Phonon p)
		{
			p.GetDirection(out double dx, out double dy);
			// Adjust phonon coordinates such that it is on the boundary of the new cell
			// Abusing the hard coded linear nature of the system (t-surfaces only along x-axis)
			double px = (dx > 0) ? 0 : cell.Length;
			p.SetCoords(px, null);
			return cell;
		}
	}

	class EmitSurface : Surface
	{
		private readonly double emitEnergy;
		public Tuple<double, double>[] EmitTable { get; }
		public double Temp { get; }
		public int EmitPhonons { get; private set; }
		public double EmitPhononsFrac { get; private set; }

		public EmitSurface(SurfaceLocation location, Cell cell, double temp) : base(location, cell)
		{
			this.Temp = temp;
			EmitTable = cell.EmitData(temp, out emitEnergy);
		}

		public override Cell HandlePhonon(Phonon p)
		{
			p.DriftTime = 0;
			p.Active = false;
			return cell;
		}

		public double GetEmitEnergy(double tEq, double simTime, double length)
		{
			return emitEnergy * length * simTime / 4 * Math.Abs(Temp - tEq);
		}

		public void SetEmitPhonons(double tEq, double effEnergy, double timeStep, double length)
		{
			double emitPhonons = GetEmitEnergy(tEq, timeStep, length) / effEnergy;
			EmitPhononsFrac = emitPhonons - Math.Truncate(emitPhonons);
			EmitPhonons = (int)emitPhonons;
		}
	}
}
