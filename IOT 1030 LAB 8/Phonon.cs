using System;

namespace Psim.Particles
{
	enum Polarization
	{
		LA,
		TA
	}

	class Phonon : Particle
	{
		/// <summary>
		/// The sign of the phonon will determine whether it has a contributory or a 
		/// detracting effect on the system temperature/flux.
		/// </summary>
		public int Sign { get; private set; }
		public double Frequency { get; private set; }
		public Polarization Polarization { get; private set; }
		public bool Active { get; set; }
		public double DriftTime { get; set; }

		public Phonon(int sign)
		{
			SetSign(sign);
			Active = true;
		}

		public Phonon(Phonon p) : base(p)
		{
			Sign = p.Sign;
			Frequency = p.Frequency;
			Polarization = p.Polarization;
			Active = p.Active;
			DriftTime = p.DriftTime;
		}

		/// <summary>
		/// Set the sign of the phonon
		/// </summary>
		/// <param name="sign">The sign of the phonon. Must be 1 or -1</param>
		/// <exception cref="ArgumentOutOfRangeException">Throws if the sign is not 1 or -1</exception>
		public void SetSign(int sign)
		{
			if (sign == -1 || sign == 1)
				Sign = sign;
			else
				throw new ArgumentOutOfRangeException("Phonon Sign must be either -1 or 1.");
		}

		public override void Update(double frequency, double speed, Polarization pol)
		{
			Frequency = frequency;
			Speed = speed;
			Polarization = pol;
		}
		public override string ToString()
		{
			return $"Frequency: {Frequency}\n" +
				   $"Polarization: {Polarization}\n" +
				   base.ToString();
		}
	}
}
