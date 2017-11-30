using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Coremero.Plugin.Weather;

namespace coreweather
{
    class Program
    {
        static async Task Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();

        }

        static async Task MainAsync(string[] args)
        {
            var Weather = new Weather(args[0], "Resources/");
            WeatherRendererInfo forecast;

            if (!File.Exists("cacheresult.json"))
            {
                forecast = await Weather.GetForecastAsync("carrollton tx");
                string output = JsonConvert.SerializeObject(forecast);
                File.WriteAllText("cacheresult.json", output);
            }
            else
            {
                forecast = JsonConvert.DeserializeObject<WeatherRendererInfo>(File.ReadAllText("cacheresult.json"));
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