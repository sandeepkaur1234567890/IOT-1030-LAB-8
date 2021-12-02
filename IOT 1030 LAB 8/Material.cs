using System;
using System.Linq;    // For Sum
using Psim.Particles; // For Polarization Enum

namespace Psim.Materials
{
	class Material
	{
		private static Random rand = new();

		private const uint NUM_FREQ_BINS = 1000;
		private const double HBAR = 1.054517e-34;
		private const double BOLTZ = 1.38065e-23;

		private double[] frequencies = new double[NUM_FREQ_BINS];
		private double[] densitiesLA = new double[NUM_FREQ_BINS];
		private double[] densitiesTA = new double[NUM_FREQ_BINS];
		private double[] velocitiesLA = new double[NUM_FREQ_BINS];
		private double[] velocitiesTA = new double[NUM_FREQ_BINS];

		private double bl;
		private double btn;
		private double btu;
		private double bi;
		private double w;
		private double wMaxLA;
		private double wMaxTA;
		private double freqWidth;

		public Material(in DispersionData dispData, in RelaxationData relaxData)
		{
			bl = relaxData.Bl;
			btn = relaxData.Btn;
			btu = relaxData.Btu;
			bi = relaxData.BI;
			w = relaxData.W;
			wMaxLA = dispData.WMaxLa;
			wMaxTA = dispData.WMaxTa;
			freqWidth = Math.Max(wMaxLA, wMaxTA) / NUM_FREQ_BINS;

			// Build frequency table
			frequencies[0] = freqWidth / 2;
			for (int i = 1; i < NUM_FREQ_BINS; ++i)
			{
				frequencies[i] = frequencies[i - 1] + freqWidth;
			}

			// Build velocity and density tables for the LA & TA phonon branches
			int index = 0;
			foreach (var freq in frequencies)
			{
				double la_gv = GetGV(freq, dispData.LaData);
				velocitiesLA[index] = la_gv;
				densitiesLA[index] = Math.Pow(GetK(freq, dispData.LaData), 2) / (2 * Math.Pow(Math.PI, 2) * la_gv);
				double ta_gv = GetGV(freq, dispData.TaData);
				// Assuming the TA branch has a lower maximum frequency than the LA branch
				if (Double.IsNaN(ta_gv) || Double.IsInfinity(ta_gv))
				{
					velocitiesTA[index] = 0;
					densitiesTA[index] = 0;
				}
				else
				{
					velocitiesTA[index] = ta_gv;
					densitiesTA[index] = Math.Pow(GetK(freq, dispData.TaData), 2) / (2 * Math.Pow(Math.PI, 2) * ta_gv);
				}
				++index;
			}
		}

		public double GetFreq(int index)
		{
			return frequencies[index] + (2 * rand.NextDouble() - 1) * freqWidth / 2;
		}

		public double GetVel(int index, Polarization polar)
		{
			return (polar == Polarization.LA) ? velocitiesLA[index] / 2 : velocitiesTA[index] / 2;
		}

		// Returns the scattering rates in the following order [N_rate, U_rate, I_rate]
		public double[] ScatteringRates(double temp, double freq, Polarization pol)
		{
			return new double[] { TauNInv(temp, freq, pol), TauUInv(temp, freq, pol), TauIInv(freq) };
		}

		public static Tuple<int, Polarization> FreqIndex(Tuple<double, double>[] dist)
		{
			uint low = 0, high = (uint)dist.Length, mid = low + (high - low) / 2;
			double rand = Material.rand.NextDouble();
			while (high - low > 1)
			{
				if (rand < dist[mid].Item1)
					high = mid;
				else
					low = mid;
				mid = (low + high) / 2;
			}
			return Tuple.Create((int)high, Material.rand.NextDouble() <= dist[high].Item2 ? Polarization.LA : Polarization.TA);
		}

		private static double GetGV(double freq, double[] coeffs)
		{
			return 2 * coeffs[0] * GetK(freq, coeffs) + coeffs[1];
		}

		public Tuple<double, double>[] BaseData(double temp, out double heatCapacity)
		{
			var laBase = PhononDist(temp, Polarization.LA);
			var taBase = PhononDist(temp, Polarization.TA);
			heatCapacity = laBase.Sum() + taBase.Sum();
			return BuildCumulDist(laBase, taBase);
		}
		public Tuple<double, double>[] EmitData(double temp, out double emitEnergy)
		{
			return BuildCumulDistEmit(PhononDist(temp, Polarization.LA), PhononDist(temp, Polarization.TA), out emitEnergy);
		}

		public Tuple<double, double>[] ScatterTable(double temp)
		{
			return BuildCumulDistScatter(PhononDist(temp, Polarization.LA), PhononDist(temp, Polarization.TA), temp);
		}

		public double TheoreticalEnergy(double temp)
		{
			BaseData(temp, out double heatCapacity);
			return heatCapacity;
		}

		private static double GetK(double freq, double[] coeffs)
		{
			double d = Math.Pow(coeffs[1], 2) - 4 * coeffs[0] * (coeffs[2] - freq);
			double a = (-coeffs[1] - Math.Sqrt(d)) / (2 * coeffs[0]);
			double b = (-coeffs[1] + Math.Sqrt(d)) / (2 * coeffs[0]);
			return (a < b) ? a : b;
		}

		private double[] PhononDist(double temp, Polarization polarization)
		{
			double[] dist = new double[NUM_FREQ_BINS];
			bool isLA = polarization == Polarization.LA;
			double[] densities = isLA ? densitiesLA : densitiesTA;

			for (int i = 0; i < NUM_FREQ_BINS; ++i)
			{
				double freq = frequencies[i];
				double constCalc = HBAR / (BOLTZ * temp);
#if true       // Derivative of Bose-Einstein
				double numer = Math.Pow(constCalc * freq, 2) * BOLTZ * Math.Exp(constCalc * freq);
				double denom = Math.Pow(Math.Exp(constCalc * freq) - 1, 2) / freqWidth / densities[i];
#else         // Bose-Einstein
				double numer = freq * HBAR * freqWidth * densities[i];
				double denom = (Math.Exp(constCalc * freq) - 1);
#endif
				dist[i] = isLA ? numer / denom : 2 * numer / denom;
			}
			return dist;
		}

		private Tuple<double, double>[] BuildCumulDist(double[] t1, double[] t2)
		{
			Tuple<double, double>[] cumulDist = new Tuple<double, double>[NUM_FREQ_BINS];
			double cumulSum = t1.Sum() + t2.Sum();
			cumulDist[0] = Tuple.Create((t1[0] + t2[0]) / cumulSum, t1[0] / (t1[0] + t2[0]));
			for (int i = 1; i < NUM_FREQ_BINS; ++i)
			{
				cumulDist[i] = Tuple.Create(cumulDist[i - 1].Item1 + (t1[i] + t2[i]) / cumulSum,
											 t1[i] / (t1[i] + t2[i]));
			}
			return cumulDist;
		}

		private Tuple<double, double>[] BuildCumulDistEmit(double[] t1, double[] t2, out double energy)
		{
			for (int i = 0; i < NUM_FREQ_BINS; ++i)
			{
				t1[i] *= velocitiesLA[i];
				t2[i] *= velocitiesTA[i];
			}
			energy = t1.Sum() + t2.Sum();
			return BuildCumulDist(t1, t2);
		}
		private Tuple<double, double>[] BuildCumulDistScatter(double[] t1, double[] t2, double temp)
		{
			for (int i = 0; i < NUM_FREQ_BINS; ++i)
			{
				double freq = frequencies[i];
				t1[i] *= ScatteringRates(temp, freq, Polarization.LA).Sum();
				t2[i] *= ScatteringRates(temp, freq, Polarization.TA).Sum();
			}
			return BuildCumulDist(t1, t2);
		}

		// Switch statement to allow easy incorporation of other phonon modes if necessary 
		private double TauNInv(double temp, double freq, Polarization polarization)
		{
			switch (polarization)
			{
				case (Polarization.LA): return bl * freq * freq * Math.Pow(temp, 3);
				case (Polarization.TA): return (freq < w) ? btn * freq * Math.Pow(temp, 4) : 0;
				default: return 0; // Should never be exercised
			}
		}

		private double TauUInv(double temp, double freq, Polarization polarization)
		{
			switch (polarization)
			{
				case (Polarization.LA): return bl * freq * freq * Math.Pow(temp, 3);
				case (Polarization.TA): return (freq >= w) ? btu * freq * freq / Math.Sinh(HBAR * freq / (temp * BOLTZ)) : 0;
				default: return 0; // Should never be exercised
			}
		}

		private double TauIInv(double freq)
		{
			return bi * Math.Pow(freq, 4);
		}
	}

	struct RelaxationData
	{
		public RelaxationData(double bl, double btn, double btu, double bi, double w)
		{
			Bl = bl;
			Btn = btn;
			Btu = btu;
			BI = bi;
			W = w;
		}
		public double Bl { get; }
		public double Btn { get; }
		public double Btu { get; }
		public double BI { get; }
		public double W { get; }
	};

	// Only handles quadratic data -> No error checking, arrays must be size 3 for each coefficient of the quadratic
	struct DispersionData
	{
		public DispersionData(double[] laData, double wMaxLa, double[] taData, double wMaxTa)
		{
			LaData = laData;
			WMaxLa = wMaxLa;
			TaData = taData;
			WMaxTa = wMaxTa;
		}
		public double[] LaData { get; }
		public double[] TaData { get; }
		public double WMaxLa { get; }
		public double WMaxTa { get; }
	};
}
