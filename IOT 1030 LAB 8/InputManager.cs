using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using Psim.Materials;

namespace Psim.IOManagers
{
    static class InputManager
    {
        public static Model InitializeModel(string path)
        {
            JObject modelData = LoadJson(path);
            // This model can only handle 1 material
            Material material = GetMaterial(modelData["materials"][0]);
            Model model = GetModel(material, modelData["settings"]);
            AddSensors(model, modelData["sensors"]);
            AddCells(model, modelData["cells"]);
            return model;
        }
        private static JObject LoadJson(string path)
        {
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                JObject modelData = JObject.Parse(json);
                return modelData;
            }
        }

        private static void AddCells(Model m, JToken cellData)
        {
            IList<JToken> cellTokens = cellData.Children().ToList();
            foreach (var token in cellTokens)
            {
                var length = (double)token["length"];
                var width = (double)token["width"];
                var id = (int)token["sensorID"];
                m.AddCell(length, width, id);
                System.Console.WriteLine($"Successfully added a {length} x {width} cell to the model. The cell is linked to sensor {id}");
            }
        }

        private static void AddSensors(Model m, JToken sensorData)
        {
            IList<JToken> sensorsTokens = sensorData.Children().ToList();
            foreach (var token in sensorsTokens)
            {
                var id = (int)token["id"];
                var temp = (double)token["t_init"];
                m.AddSensor(id, temp);
                System.Console.WriteLine($"Successfully added sensor {id} to the model. The sensor's initial temperature is {temp}.");
            }
        }

        private static Model GetModel(Material material, JToken settingsData)
        {
            var highTemp = (double)settingsData["high_temp"];
            var lowTemp = (double)settingsData["low_temp"];
            var simTime = (double)settingsData["sim_time"];
            System.Console.WriteLine($"Successfully created a model {highTemp} {lowTemp} {simTime}.");
            return new Model(material, highTemp, lowTemp, simTime);
        }

        private static Material GetMaterial(JToken materialData)
        {
            var dData = GetDispersionData(materialData["d_data"]);
            var rData = GetRelaxationData(materialData["r_data"]);
            return new Material(dData, rData);
        }

        private static DispersionData GetDispersionData(JToken dData)
        {
            var laData = dData["la_data"].ToObject<double[]>();
            var taData = dData["ta_data"].ToObject<double[]>();
            var wMaxLa = (double)dData["max_freq_la"];
            var wMaxTa = (double)dData["max_freq_ta"];
            return new DispersionData(laData, wMaxLa, taData, wMaxTa);

        }
        private static RelaxationData GetRelaxationData(JToken rData)
        {
            var bl = (double)rData["b_l"];
            var btn = (double)rData["b_tn"];
            var btu = (double)rData["b_tu"];
            var bi = (double)rData["b_i"];
            var w = (double)rData["w"];
            return new RelaxationData(bl, btn, btu, bi, w);
        }
    }
}
