using System;

// Lab Question: Convincing argument for any of the below classes to be a struct?

namespace Psim.Geometry2D
{
	class Point
	{
		public double X { get; set; }
		public double Y { get; set; }
		public Point(double x = 0, double y = 0)
		{
			X = x;
			Y = y;
		}

		public void GetCoords(out double x, out double y)
		{
			x = X;
			y = Y;
		}

		public void SetCoords(double x, double y)
		{
			X = x;
			Y = y;
		}
		/// <summary>
		/// Set the coordinates of the point. If either of the input parameters is null, then the 
		/// corresponding point will not be changed.
		/// </summary>
		public void SetCoords(double? x, double? y)
		{
			X = x ?? X;
			Y = y ?? Y;
		}
		public override string ToString() => $"({X}, {Y})";
	}


	/// <summary>
	///	A direction vector that has both an x and y component.
	///	Will throw if either component is not in the interval [-1, 1].
	/// </summary>
	class Vector
	{
		public double DX { get; private set; }
		public double DY { get; private set; }

		public Vector(double dx = 0, double dy = 0) => Set(dx, dy);

		/// <summary>
		/// Sets the x and y components of the vector.
		/// </summary>
		/// <param name="dx">The component of the vector in the x-direction</param>
		/// <param name="dy">The component of the vector in the y-direction</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void Set(double dx, double dy)
		{
			bool InRange(double x) => x <= 1 && x >= -1;
			if (InRange(dx) && InRange(dy))
			{
				DX = dx;
				DY = dy;
			}
			else
			{
				throw new ArgumentOutOfRangeException("Direction components must be in the range [-1, 1]");
			}
		}
		public override string ToString() => $"({DX}, {DY})";
	}

	/// <summary>
	/// An immutable rectangle. The length and width cannot be altered after the rectangle is created.
	/// </summary>		
	class Rectangle
	{
		public double Length { get; }
		public double Width { get; }
		public double Area { get; }
		public Rectangle(double length, double width)
		{
			Length = length;
			Width = width;
			Area = Length * Width;
		}

		public Point GetRandPoint(double r1, double r2)
		{
			bool InRange(double x) => x <= 1 && x >= 0;
			if (InRange(r1) && InRange(r2))
			{
				return new Point(r1 * Length, r2 * Width);
			}
			throw new ArgumentOutOfRangeException("r1 and r2 must be in the range [0, 1]");
		}


	}
}
