using Psim.IOManagers;

namespace Psim
{
	class Program
	{
		static void Main(string[] args)
		{
			const string path = "../../../model.json";
			Model model = InputManager.InitializeModel(path);

			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();

			model.RunSimulation("../../../");

			watch.Stop();
			System.Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds / 1000} [s]");
		}
	}
}
