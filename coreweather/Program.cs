using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace coreweather
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var Weather = new Weather(args[0]);
            WeatherRendererInfo forecast;
            if (File.Exists("cacheresult.json"))
            {
                forecast = JsonConvert.DeserializeObject<WeatherRendererInfo>(File.ReadAllText("cacheresult.json"));
            } else
            {
                forecast = await Weather.GetForecastAsync(32.9, -96.8);
                string output = JsonConvert.SerializeObject(forecast);
                File.WriteAllText("cacheresult.json", output);
            }

            using (MemoryStream img = Weather.RenderWeatherImage(forecast))
            using (FileStream fstream = File.OpenWrite("test.gif"))
            {
                img.Position = 0;
                img.CopyTo(fstream);
            }
        }
    }
}